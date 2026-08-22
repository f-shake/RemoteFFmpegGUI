using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using SimpleFFmpegGUI;

namespace SimpleFFmpegGUI.Compatibility;

/// <summary>
/// 数据库迁移运行器。根据数据库结构识别状态，按目标版本升序应用注册的迁移器。
/// 数据库版本跟随 <see cref="AppInfo.AppVersion"/>（程序语义版本）。
/// 调用约定：调用方先执行 EF <c>EnsureCreated()</c>（创建当前 schema），再调用 <see cref="Upgrade"/>。
/// </summary>
public static class MigrationRunner
{
    /// <summary>占位基线版本，任何迁移都比它新。</summary>
    private const string BaselineVersion = "0.0.0";

    /// <summary>已升级到 v2.0 的旧库（整数 _MigrationHistory）规范化后落到的版本。</summary>
    private const string LegacyV2Baseline = "2.0.0";

    /// <summary>
    /// 升级数据库到最新版本。
    /// </summary>
    /// <param name="connectionString">SQLite 连接字符串（如 "Data Source=db.sqlite"）</param>
    /// <param name="configJsonPath">v1 用户配置迁移的目标 config.json 路径（消除对当前目录的依赖）</param>
    /// <param name="migrations">可选迁移列表；缺省使用 <see cref="MigrationRegistry.Default"/></param>
    /// <returns>是否执行了迁移（含打标基线）</returns>
    public static bool Upgrade(string connectionString, string configJsonPath, IEnumerable<IMigration> migrations = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dbPath = builder.DataSource;
        if (string.IsNullOrEmpty(dbPath))
            return false;

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        var state = Classify(conn);

        if (state == DbState.Empty || state == DbState.FreshCurrent)
        {
            StampBaseline(conn);
            return true;
        }

        if (state == DbState.Unknown)
        {
            Console.WriteLine("警告: 无法识别数据库结构，跳过迁移");
            return false;
        }

        var registry = SortAndValidate(migrations ?? MigrationRegistry.Default);

        var chain = BuildChain(conn, state, registry);
        if (chain.Count == 0)
            return false;

        Backup(dbPath);

        var context = new MigrationContext { ConfigJsonPath = configJsonPath };

        foreach (var migration in chain)
        {
            using var tx = conn.BeginTransaction();
            try
            {
                // 确保版本表存在（与迁移/版本记录同事务，失败一并回滚）。
                // 空库/遗留库由 StampBaseline / Normalize 创建，但 V1_1 / 文本历史分支这里兜底。
                EnsureHistoryTable(conn, tx);
                migration.Up(conn, tx, context);
                Record(conn, tx, migration.Version, migration.Description);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        return true;
    }

    /// <summary>
    /// 待应用的迁移链。
    /// </summary>
    private static List<IMigration> BuildChain(SqliteConnection conn, DbState state, List<IMigration> registry)
    {
        if (state == DbState.V1_1)
            return registry.Where(m => ShouldApply(m.Version, BaselineVersion)).ToList();

        if (state == DbState.LegacyIntHistory)
        {
            // 旧库已升级到 v2.0（Arguments 已改名），先规范化整数历史为文本并置 2.0.0，
            // 之后只应用 > 2.0.0 的迁移，绝不重跑 v1→v2。
            var chain = new List<IMigration> { new MigrationNormalizeLegacyHistory() };
            chain.AddRange(registry.Where(m => ShouldApply(m.Version, LegacyV2Baseline)));
            return chain;
        }

        // TextHistory
        var current = GetCurrentVersion(conn);
        if (string.IsNullOrEmpty(current))
            current = AppInfo.AppVersion;
        return registry.Where(m => ShouldApply(m.Version, current)).ToList();
    }

    /// <summary>
    /// 判断某迁移目标版本是否落在"当前版本 &lt; 目标版本 &lt;= 程序版本"的升级区间内。
    /// 防止把注册表里比当前程序版本更新的迁移（如误加 2.1.0 但未同步 <see cref="AppInfo.AppVersion"/>）
    /// 压到库上，保证数据库版本不会超出程序版本。
    /// </summary>
    private static bool ShouldApply(string candidate, string current) =>
        VersionUtil.GreaterThan(candidate, current) && VersionUtil.LessThanOrEqual(candidate, AppInfo.AppVersion);

    private static void StampBaseline(SqliteConnection conn)
    {
        using var tx = conn.BeginTransaction();
        EnsureHistoryTable(conn, tx);
        Record(conn, tx, AppInfo.AppVersion, "初始化数据库基线版本");
        tx.Commit();
    }

    private static void EnsureHistoryTable(SqliteConnection conn, SqliteTransaction tx)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS _MigrationHistory (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Version     TEXT NOT NULL,
                AppliedDate TEXT NOT NULL,
                Description TEXT NOT NULL
            )
            """;
        cmd.ExecuteNonQuery();
    }

    private static void Record(SqliteConnection conn, SqliteTransaction tx, string version, string description)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO _MigrationHistory (Version, AppliedDate, Description)
            VALUES (@version, datetime('now', 'localtime'), @description)
            """;
        cmd.Parameters.AddWithValue("@version", version);
        cmd.Parameters.AddWithValue("@description", description);
        cmd.ExecuteNonQuery();
    }

    private static string GetCurrentVersion(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Version FROM _MigrationHistory";
        using var reader = cmd.ExecuteReader();
        string current = null;
        while (reader.Read())
        {
            var v = reader.GetString(0);
            if (current == null || VersionUtil.GreaterThan(v, current))
                current = v;
        }
        return current;
    }

    private static void Backup(string dbPath)
    {
        try
        {
            var backupPath = $"{dbPath}.backup.{DateTime.Now:yyyyMMddHHmmss}";
            File.Copy(dbPath, backupPath);
            Console.WriteLine($"数据库已备份到: {backupPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"数据库备份失败，继续迁移: {ex.Message}");
        }
    }

    private static List<IMigration> SortAndValidate(IEnumerable<IMigration> migrations)
    {
        var list = migrations.OrderBy(m => VersionUtil.Parse(m.Version)).ToList();
        string prev = null;
        foreach (var m in list)
        {
            if (prev != null && !VersionUtil.GreaterThan(m.Version, prev))
                throw new InvalidOperationException($"迁移版本必须唯一且升序: {m.Version} 与 {prev} 冲突");
            prev = m.Version;
        }
        return list;
    }

    private static DbState Classify(SqliteConnection conn)
    {
        if (!TableExists(conn, "_MigrationHistory"))
        {
            if (!HasAnyUserTable(conn))
                return DbState.Empty;
            if (TableExists(conn, "Configs") || ColumnExists(conn, "Tasks", "Arguments"))
                return DbState.V1_1;
            return DbState.FreshCurrent;
        }

        var versionType = GetColumnType(conn, "_MigrationHistory", "Version");
        if (versionType == null)
            return DbState.Unknown;
        if (versionType.Contains("INT", StringComparison.OrdinalIgnoreCase))
            return DbState.LegacyIntHistory;
        if (versionType.Contains("TEXT", StringComparison.OrdinalIgnoreCase) ||
            versionType.Contains("CHAR", StringComparison.OrdinalIgnoreCase))
            return DbState.TextHistory;
        return DbState.Unknown;
    }

    private static bool HasAnyUserTable(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' LIMIT 1";
        return cmd.ExecuteScalar() != null;
    }

    private static bool TableExists(SqliteConnection conn, string tableName)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@name";
        cmd.Parameters.AddWithValue("@name", tableName);
        return cmd.ExecuteScalar() != null;
    }

    private static bool ColumnExists(SqliteConnection conn, string table, string column)
    {
        var type = GetColumnType(conn, table, column);
        return type != null;
    }

    private static string GetColumnType(SqliteConnection conn, string table, string column)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetString(1) == column)
                return reader.GetString(2);
        }
        return null;
    }

    private enum DbState
    {
        /// <summary>无任何用户表。</summary>
        Empty,

        /// <summary>已有当前 schema（EnsureCreated 之后）但无 _MigrationHistory。</summary>
        FreshCurrent,

        /// <summary>v1.1 旧库（Configs 表或 Tasks.Arguments 列存在）。</summary>
        V1_1,

        /// <summary>已升级到 v2.0 的旧库，_MigrationHistory.Version 为整数。规范化后戳 2.0.0。</summary>
        LegacyIntHistory,

        /// <summary>新系统，_MigrationHistory.Version 为文本。</summary>
        TextHistory,

        /// <summary>结构不符，跳过。</summary>
        Unknown
    }
}
