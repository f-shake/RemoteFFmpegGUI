using System;
using System.IO;
using Microsoft.Data.Sqlite;
using SimpleFFmpegGUI;
using SimpleFFmpegGUI.Compatibility;
using Xunit;

namespace SimpleFFmpegGUI.WebTest;

/// <summary>
/// 测试数据库版本化迁移管道的扩展性：
/// 空库打标基线、v1→v2→v2.0.1 合成迁移叠加、遗留整数历史规范化（不重跑 v1→v2）、幂等。
/// </summary>
[Collection("FFmpegWebCollection")]
public class CompatibilityPipelineTests
{
    [Fact]
    public void EmptyDatabase_ShouldStampBaseline()
    {
        var tempDir = MigrationTestHelper.CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var testDbPath = Path.Combine(tempDir, "db.sqlite");
        var configPath = Path.Combine(tempDir, "config.json");
        MigrationTestHelper.CreateEmptyDatabase(testDbPath);

        try
        {
            var connectionString = $"Data Source={testDbPath}";

            var migrated = MigrationRunner.Upgrade(connectionString, configPath);
            Assert.True(migrated, "空库应被标记为基线版本");

            Assert.True(MigrationTestHelper.TableExists(connectionString, "_MigrationHistory"));
            Assert.True(MigrationTestHelper.IsTextVersionHistory(connectionString));
            Assert.Equal(AppInfo.AppVersion, MigrationTestHelper.GetCurrentHistoryVersion(connectionString));

            // 基线打标只应写入一条历史记录
            Assert.Equal(1L, CountHistoryRows(connectionString));
        }
        finally
        {
            try { Directory.Delete(tempDir, true); }
            catch { /* 忽略清理失败 */ }
        }
    }

    /// <summary>
    /// v1 库应能逐级应用 v2.0 与合成 v2.0.1 迁移（验证管道可叠加）。
    /// </summary>
    [Fact]
    public void V1_1_ShouldApplyStackedMigrations()
    {
        var tempDir = MigrationTestHelper.CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var testDbPath = Path.Combine(tempDir, "db.sqlite");
        var configPath = Path.Combine(tempDir, "config.json");
        MigrationTestHelper.CreateV1Database(testDbPath);

        try
        {
            var connectionString = $"Data Source={testDbPath}";
            IMigration[] custom = { new MigrationV1_1ToV2_0(), new SyntheticMigrationV2_0_1() };

            var migrated = MigrationRunner.Upgrade(connectionString, configPath, custom);
            Assert.True(migrated, "应执行迁移链");

            // 最终版本应停在最高目标版本 2.0.1
            Assert.Equal("2.0.1", MigrationTestHelper.GetCurrentHistoryVersion(connectionString));

            // v1→v2 的列重命名已生效
            Assert.True(MigrationTestHelper.ColumnExists(connectionString, "Tasks", "Parameters"));
            Assert.False(MigrationTestHelper.ColumnExists(connectionString, "Tasks", "Arguments"));

            // 合成迁移已应用（叠加成功）
            Assert.True(MigrationTestHelper.ColumnExists(connectionString, "Tasks", "TestAddedColumn"),
                "合成 v2.0.1 迁移应已执行");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); }
            catch { /* 忽略清理失败 */ }
        }
    }

    /// <summary>
    /// 已升级到 v2.0 的旧库（整数历史）应只规范化版本表，绝不重跑 v1→v2。
    /// </summary>
    [Fact]
    public void LegacyIntHistory_ShouldNormalizeWithoutReRunningV1_1()
    {
        var tempDir = MigrationTestHelper.CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var testDbPath = Path.Combine(tempDir, "db.sqlite");
        var configPath = Path.Combine(tempDir, "config.json");
        MigrationTestHelper.CreateLegacyIntHistoryDatabase(testDbPath);

        try
        {
            var connectionString = $"Data Source={testDbPath}";

            var migrated = MigrationRunner.Upgrade(connectionString, configPath);
            Assert.True(migrated, "应规范化遗留整数历史");

            Assert.True(MigrationTestHelper.IsTextVersionHistory(connectionString));
            Assert.Equal("2.0.0", MigrationTestHelper.GetCurrentHistoryVersion(connectionString));

            // 证明未重跑 v1→v2：Parameters 仍在且未被重命名为 Arguments
            Assert.True(MigrationTestHelper.ColumnExists(connectionString, "Tasks", "Parameters"));
            Assert.False(MigrationTestHelper.ColumnExists(connectionString, "Tasks", "Arguments"));

            // Configs 表本就不存在，不应被创建出来
            Assert.False(MigrationTestHelper.TableExists(connectionString, "Configs"));
        }
        finally
        {
            try { Directory.Delete(tempDir, true); }
            catch { /* 忽略清理失败 */ }
        }
    }

    /// <summary>
    /// 已迁移/已打标的库再次升级应幂等（无待跑迁移，返回 false）。
    /// </summary>
    [Fact]
    public void Upgrade_ShouldBeIdempotent_AfterStamp()
    {
        var tempDir = MigrationTestHelper.CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var testDbPath = Path.Combine(tempDir, "db.sqlite");
        var configPath = Path.Combine(tempDir, "config.json");
        MigrationTestHelper.CreateEmptyDatabase(testDbPath);

        try
        {
            var connectionString = $"Data Source={testDbPath}";

            var first = MigrationRunner.Upgrade(connectionString, configPath);
            Assert.True(first);

            var second = MigrationRunner.Upgrade(connectionString, configPath);
            Assert.False(second, "已打标基线的库再次升级应跳过");
        }
        finally
        {
            try { Directory.Delete(tempDir, true); }
            catch { /* 忽略清理失败 */ }
        }
    }

    /// <summary>
    /// 返回 _MigrationHistory 的当前记录数。
    /// </summary>
    private static long CountHistoryRows(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM _MigrationHistory";
        return (long)cmd.ExecuteScalar();
    }

    /// <summary>
    /// 合成测试迁移：目标 2.0.1，给 Tasks 增加一列以验证叠加。
    /// </summary>
    private sealed class SyntheticMigrationV2_0_1 : IMigration
    {
        public string Version => "2.0.1";
        public string Description => "合成测试迁移: v2.0.1 给Tasks增加TestAddedColumn";

        public void Up(SqliteConnection conn, SqliteTransaction tx, MigrationContext context)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "ALTER TABLE Tasks ADD COLUMN TestAddedColumn TEXT";
            cmd.ExecuteNonQuery();
        }
    }
}
