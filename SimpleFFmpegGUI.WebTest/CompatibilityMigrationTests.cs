using System;
using System.IO;
using Microsoft.Data.Sqlite;
using SimpleFFmpegGUI.Compatibility;
using Xunit;
using Xunit.Abstractions;

namespace SimpleFFmpegGUI.WebTest;

/// <summary>
/// 测试 v1.1 → v2.0 数据库迁移的完整流程。
/// 在测试内动态构造 v1 schema 与数据（不依赖提交的样本库），迁移后断言结果。
/// 与 API 测试同集合串行执行，避免与其他测试并行竞态。
/// </summary>
[Collection("FFmpegWebCollection")]
public class CompatibilityMigrationTests
{
    private readonly ITestOutputHelper output;

    public CompatibilityMigrationTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void MigrateV1_1Database_ShouldSucceed()
    {
        var tempDir = MigrationTestHelper.CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var testDbPath = Path.Combine(tempDir, "db.sqlite");
        var configPath = Path.Combine(tempDir, "config.json");
        MigrationTestHelper.CreateV1Database(testDbPath);

        try
        {
            var connectionString = $"Data Source={testDbPath}";

            // 执行迁移（通过新 MigrationRunner，config 目标路径显式传入，不再依赖当前目录）
            var migrated = MigrationRunner.Upgrade(connectionString, configPath);
            Assert.True(migrated, "应该检测到旧版数据库并执行迁移");

            // 验证迁移结果
            VerifyMigration(connectionString, configPath);

            // 再次运行迁移——应该跳过（已迁移）
            var migratedAgain = MigrationRunner.Upgrade(connectionString, configPath);
            Assert.False(migratedAgain, "再次运行应该跳过迁移");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); }
            catch { /* 忽略清理失败 */ }
        }
    }

    private void VerifyMigration(string connectionString, string configPath)
    {
        // 1. 验证 _MigrationHistory 存在且最新版本为文本 "2.0.0"
        Assert.True(MigrationTestHelper.TableExists(connectionString, "_MigrationHistory"),
            "_MigrationHistory 应该存在");
        Assert.True(MigrationTestHelper.IsTextVersionHistory(connectionString),
            "_MigrationHistory.Version 应为文本类型");
        Assert.Equal("2.0.0", MigrationTestHelper.GetCurrentHistoryVersion(connectionString));

        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Description FROM _MigrationHistory WHERE Version = '2.0.0'";
            var desc = cmd.ExecuteScalar() as string;
            Assert.NotNull(desc);
            Assert.Contains("v1.1", desc);
            Assert.Contains("v2.0", desc);
        }

        // 2. 验证 Configs 表已删除
        Assert.False(MigrationTestHelper.TableExists(connectionString, "Configs"), "Configs 表应被删除");

        // 3. 验证列名：Arguments 不存在，Parameters 存在
        Assert.False(MigrationTestHelper.ColumnExists(connectionString, "Tasks", "Arguments"), "Tasks.Arguments 应被重命名");
        Assert.True(MigrationTestHelper.ColumnExists(connectionString, "Tasks", "Parameters"), "Tasks.Parameters 应存在");
        Assert.False(MigrationTestHelper.ColumnExists(connectionString, "Presets", "Arguments"), "Presets.Arguments 应被重命名");
        Assert.True(MigrationTestHelper.ColumnExists(connectionString, "Presets", "Parameters"), "Presets.Parameters 应存在");

        // 4. 验证 TaskType.Custom 已修复（3 → 99）
        Assert.Equal(0L, ExecuteCount(connectionString, "SELECT COUNT(*) FROM Tasks WHERE Type = 3"));
        Assert.Equal(1L, ExecuteCount(connectionString, "SELECT COUNT(*) FROM Tasks WHERE Type = 99"));
        Assert.Equal(0L, ExecuteCount(connectionString, "SELECT COUNT(*) FROM Presets WHERE Type = 3"));
        Assert.Equal(1L, ExecuteCount(connectionString, "SELECT COUNT(*) FROM Presets WHERE Type = 99"));

        // 5. 抽检 JSON 格式转换——旧字段名不应存在，新字段名应存在
        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Parameters FROM Tasks WHERE Parameters IS NOT NULL LIMIT 1";
            var json = cmd.ExecuteScalar() as string;
            Assert.False(string.IsNullOrEmpty(json), "Tasks.Parameters 应有转换后的 JSON");
            Assert.DoesNotContain("\"DisableVideo\"", json);
            Assert.DoesNotContain("\"DisableAudio\"", json);
            Assert.DoesNotContain("\"ProcessedOptions\"", json);
            Assert.DoesNotContain("\"Video\":{\"Code\"", json);
            Assert.Contains("\"Codec\"", json);
            Assert.Contains("\"Strategy\"", json);
            Assert.Contains("\"ProcessedOperationParameters\"", json);
        }

        // 6. 用户配置应迁移到传入的 config.json 路径
        Assert.True(File.Exists(configPath), "config.json 应写入测试目录");
        var configJson = File.ReadAllText(configPath);
        Assert.Contains("\"DefaultProcessPriority\": 2", configJson);
        Assert.Contains("\"SnapshotSize\": \"-1:1080\"", configJson);

        output.WriteLine("迁移验证全部通过");
    }

    private static long ExecuteCount(string connectionString, string sql)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return (long)cmd.ExecuteScalar();
    }
}
