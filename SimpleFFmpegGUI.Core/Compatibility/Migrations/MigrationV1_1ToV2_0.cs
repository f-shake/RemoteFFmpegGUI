using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.Compatibility;

/// <summary>
/// v1.1 → v2.0 数据库迁移（目标版本 2.0.0）。
/// 内容源自旧 <c>DatabaseMigrator</c>：列重命名、JSON 格式转换、Custom 枚举修复、用户配置迁移、删除 Configs。
/// 版本记录不再写于此，由 <see cref="MigrationRunner"/> 在每迁移成功后统一写入。
/// </summary>
public sealed class MigrationV1_1ToV2_0 : IMigration
{
    public string Version => "2.0.0";
    public string Description => "v1.1 → v2.0: 列重命名、JSON格式转换、Custom枚举修复、迁移用户配置、清理Configs";

    public void Up(SqliteConnection conn, SqliteTransaction tx, MigrationContext context)
    {
        MigrateSchema(conn, tx);
        MigrateJsonColumn(conn, tx, "Tasks", "Parameters");
        MigrateJsonColumn(conn, tx, "Presets", "Parameters");
        MigrateTaskTypeCustom(conn, tx);
        MigrateConfigs(conn, tx, context.ConfigJsonPath);
        DropConfigsTable(conn, tx);
    }

    private static void MigrateSchema(SqliteConnection conn, SqliteTransaction tx)
    {
        if (ColumnExists(conn, tx, "Tasks", "Arguments"))
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "ALTER TABLE Tasks RENAME COLUMN Arguments TO Parameters";
            cmd.ExecuteNonQuery();
        }

        if (ColumnExists(conn, tx, "Presets", "Arguments"))
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "ALTER TABLE Presets RENAME COLUMN Arguments TO Parameters";
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 读取指定表的 JSON 列，逐行将旧格式转换为新格式。
    /// </summary>
    private static void MigrateJsonColumn(SqliteConnection conn, SqliteTransaction tx, string table, string column)
    {
        using var selectCmd = conn.CreateCommand();
        selectCmd.Transaction = tx;
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
            updateCmd.Transaction = tx;
            updateCmd.CommandText = $"UPDATE {table} SET {column} = @json WHERE rowid = @rowid";
            updateCmd.Parameters.AddWithValue("@json", newJson);
            updateCmd.Parameters.AddWithValue("@rowid", rowid);
            updateCmd.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 修复 TaskType.Custom 枚举值：3 → 99。
    /// </summary>
    private static void MigrateTaskTypeCustom(SqliteConnection conn, SqliteTransaction tx)
    {
        using var cmd1 = conn.CreateCommand();
        cmd1.Transaction = tx;
        cmd1.CommandText = "UPDATE Tasks SET Type = 99 WHERE Type = 3";
        var affected1 = cmd1.ExecuteNonQuery();
        if (affected1 > 0)
            Console.WriteLine($"已修复 {affected1} 个任务的 TaskType (Custom 3→99)");

        using var cmd2 = conn.CreateCommand();
        cmd2.Transaction = tx;
        cmd2.CommandText = "UPDATE Presets SET Type = 99 WHERE Type = 3";
        var affected2 = cmd2.ExecuteNonQuery();
        if (affected2 > 0)
            Console.WriteLine($"已修复 {affected2} 个预设的 TaskType (Custom 3→99)");
    }

    /// <summary>
    /// 迁移前读出 v1 Configs 表中的用户配置（DefaultProcessPriority、SnapshotSize），
    /// 写入 <paramref name="configJsonPath"/>（v2 配置存储），避免 DROP 丢失用户设置。
    /// v1 的 Configs.Value 为 JSON 序列化字符串。
    /// </summary>
    private static void MigrateConfigs(SqliteConnection conn, SqliteTransaction tx, string configJsonPath)
    {
        var configs = new Dictionary<string, string>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText =
                "SELECT Key, Value FROM Configs WHERE Key IN ('DefaultProcessPriority', 'SnapshotSize')";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                configs[reader.GetString(0)] = reader.GetString(1);
            }
        }

        if (configs.Count == 0)
        {
            return;
        }

        var path = string.IsNullOrWhiteSpace(configJsonPath)
            ? Path.Combine(Environment.CurrentDirectory, "config.json")
            : configJsonPath;

        Dictionary<string, JsonNode> config = new();
        if (File.Exists(path))
        {
            try
            {
                var parsed = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
                if (parsed != null)
                {
                    config = parsed.ToDictionary(p => p.Key, p => p.Value);
                }
            }
            catch
            {
                // config.json 损坏时忽略，重新生成
            }
        }

        if (configs.TryGetValue("DefaultProcessPriority", out var priorityJson))
        {
            try
            {
                config["DefaultProcessPriority"] = JsonSerializer.Deserialize<int>(priorityJson);
            }
            catch
            {
                // 忽略无法解析的旧值
            }
        }

        if (configs.TryGetValue("SnapshotSize", out var snapshotJson))
        {
            try
            {
                var size = JsonSerializer.Deserialize<string>(snapshotJson);
                if (size != null)
                {
                    config["SnapshotSize"] = size;
                }
            }
            catch
            {
                // 忽略无法解析的旧值
            }
        }

        File.WriteAllText(path, JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
        Console.WriteLine($"已迁移 {configs.Count} 项用户配置到 {path}");
    }

    private static void DropConfigsTable(SqliteConnection conn, SqliteTransaction tx)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DROP TABLE IF EXISTS Configs";
        cmd.ExecuteNonQuery();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static bool ColumnExists(SqliteConnection conn, SqliteTransaction tx, string table, string column)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
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
