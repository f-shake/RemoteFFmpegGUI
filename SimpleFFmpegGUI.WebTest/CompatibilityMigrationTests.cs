using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using SimpleFFmpegGUI.Compatibility;
using Xunit;
using Xunit.Abstractions;

namespace SimpleFFmpegGUI.WebTest;

/// <summary>
/// 测试 v1.1 → v2.0 数据库迁移的完整流程。
/// 使用 <c>test/old_db.sqlite</c>（实际旧数据）验证迁移正确性。
/// </summary>
public class CompatibilityMigrationTests
{
    private readonly ITestOutputHelper output;

    public CompatibilityMigrationTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    /// <summary>
    /// 找到 test/old_db.sqlite 文件（从输出目录向上搜索）。
    /// </summary>
    private static string FindTestDbPath()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "test", "old_db.sqlite");
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    [Fact]
    public void MigrateV1_1Database_ShouldSucceed()
    {
        var sourcePath = FindTestDbPath();
        if (sourcePath == null)
        {
            output.WriteLine("跳过测试：找不到 test/old_db.sqlite");
            return;
        }

        // 复制到临时目录（避免修改源文件）
        var tempDir = Path.Combine(Path.GetTempPath(), "FFmpegMigrateTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var testDbPath = Path.Combine(tempDir, "db.sqlite");
        File.Copy(sourcePath, testDbPath);

        try
        {
            var connectionString = $"Data Source={testDbPath}";

            // 执行迁移
            var migrated = DatabaseMigrator.MigrateIfNeeded(connectionString);
            Assert.True(migrated, "应该检测到旧版数据库并执行迁移");

            // 验证迁移结果
            VerifyMigration(connectionString);

            // 再次运行迁移——应该跳过（已迁移）
            var migratedAgain = DatabaseMigrator.MigrateIfNeeded(connectionString);
            Assert.False(migratedAgain, "再次运行应该跳过迁移");
        }
        finally
        {
            // 清理临时文件
            try { Directory.Delete(tempDir, true); }
            catch { /* 忽略清理失败 */ }
        }
    }

    private void VerifyMigration(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        // 1. 验证 _MigrationHistory 存在且有版本 1
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='_MigrationHistory'";
            Assert.NotNull(cmd.ExecuteScalar());
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Version, Description FROM _MigrationHistory ORDER BY Version DESC LIMIT 1";
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read(), "_MigrationHistory 应该有一条记录");
            Assert.Equal(1, reader.GetInt32(0));
            var desc = reader.GetString(1);
            Assert.Contains("v1.1", desc);
            Assert.Contains("v2.0", desc);
        }

        // 2. 验证 Configs 表已删除
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Configs'";
            Assert.Null(cmd.ExecuteScalar());
        }

        // 3. 验证列名：Arguments 不存在，Parameters 存在
        Assert.False(ColumnExists(conn, "Tasks", "Arguments"), "Tasks.Arguments 应被重命名");
        Assert.True(ColumnExists(conn, "Tasks", "Parameters"), "Tasks.Parameters 应存在");
        Assert.False(ColumnExists(conn, "Presets", "Arguments"), "Presets.Arguments 应被重命名");
        Assert.True(ColumnExists(conn, "Presets", "Parameters"), "Presets.Parameters 应存在");

        // 4. 验证 TaskType.Custom 已修复
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Tasks WHERE Type = 3";
            Assert.Equal(0L, cmd.ExecuteScalar());
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Presets WHERE Type = 3";
            Assert.Equal(0L, cmd.ExecuteScalar());
        }

        // 5. 抽检 JSON 格式转换——选择一条非空的 Parameters 数据
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT Parameters FROM Tasks WHERE Parameters IS NOT NULL LIMIT 1";
            var json = cmd.ExecuteScalar() as string;
            if (json != null)
            {
                // 新格式不应该包含旧字段名
                Assert.DoesNotContain("\"DisableVideo\"", json);
                Assert.DoesNotContain("\"DisableAudio\"", json);
                Assert.DoesNotContain("\"Combine\"", json);
                Assert.DoesNotContain("\"ProcessedOptions\"", json);
                Assert.DoesNotContain("\"Video\":{\"Code\"", json);

                // 新格式应该包含新字段名
                Assert.Contains("\"Codec\"", json);
                Assert.Contains("\"Strategy\"", json);
                Assert.Contains("\"Mux\"", json);
                Assert.Contains("\"ProcessedOperationParameters\"", json);
            }
        }

        output.WriteLine("迁移验证全部通过");
    }

    private static bool ColumnExists(SqliteConnection conn, string table, string column)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(1) == column)
                return true;
        }
        return false;
    }
}
