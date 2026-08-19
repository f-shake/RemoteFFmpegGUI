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
/// 在测试内动态构造 v1 schema 与数据（不依赖提交的样本库），迁移后断言结果（P4-2）。
/// 与 API 测试同集合串行执行（迁移会临时改变当前目录，避免与其他测试并行竞态）。
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
        var tempDir = Path.Combine(Path.GetTempPath(), "FFmpegMigrateTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var testDbPath = Path.Combine(tempDir, "db.sqlite");
        CreateV1Database(testDbPath);

        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            // MigrateConfigs 会把用户配置写入当前目录的 config.json，迁移期间将 cwd 指到临时目录
            Directory.SetCurrentDirectory(tempDir);
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
            Directory.SetCurrentDirectory(originalDir);
            // 清理临时文件
            try { Directory.Delete(tempDir, true); }
            catch { /* 忽略清理失败 */ }
        }
    }

    /// <summary>
    /// 构造 v1.1 版数据库（Configs 表、Tasks/Presets 的 Arguments 旧列名、Type=3 的 Custom 等）
    /// </summary>
    private static void CreateV1Database(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE Configs (Id INTEGER PRIMARY KEY, Key TEXT, Value TEXT);
            CREATE TABLE Tasks (
                Id INTEGER PRIMARY KEY, Type INTEGER, Status INTEGER, Inputs TEXT, Arguments TEXT,
                Output TEXT, RealOutput TEXT, CreateTime TEXT, StartTime TEXT, FinishTime TEXT,
                IsDeleted INTEGER, Message TEXT, FFmpegArguments TEXT);
            CREATE TABLE Presets (
                Id INTEGER PRIMARY KEY, Name TEXT, Type INTEGER, "Default" INTEGER, Arguments TEXT, IsDeleted INTEGER);

            INSERT INTO Configs (Key, Value) VALUES
                ('DefaultProcessPriority', '2'),
                ('SnapshotSize', '"-1:1080"'),
                ('Version', '"1.0"');

            INSERT INTO Tasks (Type, Status, Arguments) VALUES
                (0, 0, '{"Video":{"Code":"H264","Crf":23,"Preset":0},"Audio":{"Code":"AAC","Bitrate":128},"DisableVideo":false,"DisableAudio":false,"Extra":"","Format":"mp4"}'),
                (3, 0, '{"Extra":"-threads 4"}');

            INSERT INTO Presets (Name, Type, "Default", Arguments) VALUES
                ('custom_preset', 3, 0, '{"Extra":"-preset fast"}');
            """;
        cmd.ExecuteNonQuery();
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

        // 4. 验证 TaskType.Custom 已修复（3 → 99）
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Tasks WHERE Type = 3";
            Assert.Equal(0L, cmd.ExecuteScalar());
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Tasks WHERE Type = 99";
            Assert.Equal(1L, cmd.ExecuteScalar());
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Presets WHERE Type = 3";
            Assert.Equal(0L, cmd.ExecuteScalar());
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM Presets WHERE Type = 99";
            Assert.Equal(1L, cmd.ExecuteScalar());
        }

        // 5. 抽检 JSON 格式转换——旧字段名不应存在，新字段名应存在
        using (var cmd = conn.CreateCommand())
        {
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

        // 6. 用户配置应迁移到当前目录的 config.json（P1-11）——断言完整键值对，避免仅键名/子串被误匹配
        var configJson = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "config.json"));
        Assert.Contains("\"DefaultProcessPriority\": 2", configJson);
        Assert.Contains("\"SnapshotSize\": \"-1:1080\"", configJson);

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
