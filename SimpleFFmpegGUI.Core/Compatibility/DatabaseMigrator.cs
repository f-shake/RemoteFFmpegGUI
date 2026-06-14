using System;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.Compatibility;

/// <summary>
/// 数据库迁移器 — 检测 v1.1 数据库（master 分支）并自动迁移到 v2.0 格式（master_v2 分支）。
/// 在 EF Core 的 EnsureCreated() 之前调用，使用 raw ADO.NET 操作。
/// </summary>
public static class DatabaseMigrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 检测并执行数据库迁移。
    /// </summary>
    /// <param name="connectionString">SQLite 连接字符串（如 "Data Source=db.sqlite"）</param>
    /// <returns>是否执行了迁移</returns>
    public static bool MigrateIfNeeded(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dbPath = builder.DataSource;

        if (string.IsNullOrEmpty(dbPath) || !File.Exists(dbPath))
            return false;

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        if (!NeedsMigration(conn))
            return false;

        // 备份旧数据库
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

        // 在事务中执行迁移
        using var tx = conn.BeginTransaction();
        try
        {
            MigrateSchema(conn);
            MigrateTasksParameters(conn);
            MigratePresetsParameters(conn);
            MigrateTaskTypeCustom(conn);
            DropConfigsTable(conn);
            WriteMigrationHistory(conn);
            tx.Commit();
            Console.WriteLine("数据库迁移完成");
            return true;
        }
        catch
        {
            tx.Rollback();
            Console.Error.WriteLine("数据库迁移失败，已回滚");
            throw;
        }
    }

    /// <summary>
    /// 检测是否为旧版本数据库。
    /// </summary>
    private static bool NeedsMigration(SqliteConnection conn)
    {
        // 已有 _MigrationHistory 表 → 已迁移过
        if (TableExists(conn, "_MigrationHistory"))
            return false;

        // 没有 Configs 表 → 不是旧版数据库
        if (!TableExists(conn, "Configs"))
            return false;

        // Tasks 表有 Arguments 列 → 旧版列名
        return ColumnExists(conn, "Tasks", "Arguments");
    }

    /// <summary>
    /// 重命名列：Arguments → Parameters
    /// </summary>
    private static void MigrateSchema(SqliteConnection conn)
    {
        if (ColumnExists(conn, "Tasks", "Arguments"))
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE Tasks RENAME COLUMN Arguments TO Parameters";
            cmd.ExecuteNonQuery();
        }

        if (ColumnExists(conn, "Presets", "Arguments"))
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "ALTER TABLE Presets RENAME COLUMN Arguments TO Parameters";
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 转换 Tasks 表中 Parameters 列的旧 JSON 为新格式。
    /// </summary>
    private static void MigrateTasksParameters(SqliteConnection conn)
    {
        MigrateJsonColumn(conn, "Tasks", "Parameters");
    }

    /// <summary>
    /// 转换 Presets 表中 Parameters 列的旧 JSON 为新格式。
    /// </summary>
    private static void MigratePresetsParameters(SqliteConnection conn)
    {
        MigrateJsonColumn(conn, "Presets", "Parameters");
    }

    /// <summary>
    /// 读取指定表的 JSON 列，逐行将旧格式转换为新格式。
    /// </summary>
    private static void MigrateJsonColumn(SqliteConnection conn, string table, string column)
    {
        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = $"SELECT rowid, {column} FROM {table} WHERE {column} IS NOT NULL";

        using var reader = selectCmd.ExecuteReader();
        while (reader.Read())
        {
            var rowid = reader.GetInt64(0);
            var json = reader.IsDBNull(1) ? null : reader.GetString(1);

            if (string.IsNullOrWhiteSpace(json))
                continue;

            string newJson;
            try
            {
                var oldDto = JsonSerializer.Deserialize<OldOutputArgumentsDto>(json, JsonOptions);
                if (oldDto == null)
                    continue;

                var newParams = PresetConverter.ConvertFromV1_1(oldDto);
                newJson = newParams.SerializeWithDefaultSettings();

                if (string.IsNullOrWhiteSpace(newJson))
                    continue;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"警告: [{table}] rowid={rowid} 反序列化失败: {ex.Message}，设为默认值");
                newJson = new OutputParameters().SerializeWithDefaultSettings();
            }

            using var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = $"UPDATE {table} SET {column} = @json WHERE rowid = @rowid";
            updateCmd.Parameters.AddWithValue("@json", newJson);
            updateCmd.Parameters.AddWithValue("@rowid", rowid);
            updateCmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 修复 TaskType.Custom 枚举值：3 → 99。
    /// </summary>
    private static void MigrateTaskTypeCustom(SqliteConnection conn)
    {
        using var cmd1 = conn.CreateCommand();
        cmd1.CommandText = "UPDATE Tasks SET Type = 99 WHERE Type = 3";
        var affected1 = cmd1.ExecuteNonQuery();
        if (affected1 > 0)
            Console.WriteLine($"已修复 {affected1} 个任务的 TaskType (Custom 3→99)");

        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = "UPDATE Presets SET Type = 99 WHERE Type = 3";
        var affected2 = cmd2.ExecuteNonQuery();
        if (affected2 > 0)
            Console.WriteLine($"已修复 {affected2} 个预设的 TaskType (Custom 3→99)");
    }

    /// <summary>
    /// 删除旧版的 Configs 表。
    /// </summary>
    private static void DropConfigsTable(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS Configs";
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 创建 _MigrationHistory 表并写入版本 1 记录。
    /// </summary>
    private static void WriteMigrationHistory(SqliteConnection conn)
    {
        using var createCmd = conn.CreateCommand();
        createCmd.CommandText = """
            CREATE TABLE IF NOT EXISTS _MigrationHistory (
                Version     INTEGER PRIMARY KEY,
                MigrationDate TEXT NOT NULL,
                Description TEXT NOT NULL
            )
            """;
        createCmd.ExecuteNonQuery();

        using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = """
            INSERT INTO _MigrationHistory (Version, MigrationDate, Description)
            VALUES (1, datetime('now', 'localtime'), 'v1.1 → v2.0: 列重命名、JSON格式转换、Custom枚举修复、清理Configs')
            """;
        insertCmd.ExecuteNonQuery();
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
