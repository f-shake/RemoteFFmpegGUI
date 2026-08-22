using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace SimpleFFmpegGUI.WebTest;

/// <summary>
/// 数据库迁移测试共用的构造与断言工具。
/// 在临时目录构造各种形态的 SQLite 库（v1.1 / 已升级 v2.0 整数历史 / 空库），
/// 迁移后读取 _MigrationHistory 辅助断言。
/// </summary>
public static class MigrationTestHelper
{
    /// <summary>
    /// 构造一个 v1.1 版数据库（Configs 表、Tasks/Presets 的 Arguments 旧列名、Type=3 的 Custom 等）。
    /// </summary>
    public static void CreateV1Database(string dbPath)
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

    /// <summary>
    /// 构造一份"已升级到 v2.0 但 _MigrationHistory.Version 为整数"的旧库
    /// （Arguments 已改名 Parameters, 无 Configs 表）。
    /// </summary>
    public static void CreateLegacyIntHistoryDatabase(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE Tasks (
                Id INTEGER PRIMARY KEY, Type INTEGER, Status INTEGER, Inputs TEXT, Parameters TEXT,
                Output TEXT, RealOutput TEXT, CreateTime TEXT, StartTime TEXT, FinishTime TEXT,
                IsDeleted INTEGER, Message TEXT, FFmpegArguments TEXT);
            CREATE TABLE Presets (
                Id INTEGER PRIMARY KEY, Name TEXT, Type INTEGER, "Default" INTEGER, Parameters TEXT, IsDeleted INTEGER);
            CREATE TABLE _MigrationHistory (Version INTEGER PRIMARY KEY, MigrationDate TEXT, Description TEXT);

            INSERT INTO Tasks (Type, Status, Parameters) VALUES
                (0, 0, '{"Video":{"Codec":"H264","Crf":23},"Audio":{"Codec":"AAC"}}');

            INSERT INTO _MigrationHistory (Version, MigrationDate, Description) VALUES
                (1, datetime('now'), 'v1.1 → v2.0: 旧版迁移记录');
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 构造一个空白 SQLite 库（打开即创建文件，无任何用户表）。
    /// </summary>
    public static void CreateEmptyDatabase(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
    }

    /// <summary>
    /// 读取 _MigrationHistory 的最新版本（按 Id 降序取第一条，因迁移按版本升序落库）。
    /// </summary>
    public static string GetCurrentHistoryVersion(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Version FROM _MigrationHistory ORDER BY Id DESC LIMIT 1";
        return cmd.ExecuteScalar() as string;
    }

    /// <summary>
    /// 判断 _MigrationHistory.Version 列是否为文本类型。
    /// </summary>
    public static bool IsTextVersionHistory(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        var type = GetColumnType(conn, "_MigrationHistory", "Version");
        return type != null && type.Contains("TEXT", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ColumnExists(string connectionString, string table, string column)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        return GetColumnType(conn, table, column) != null;
    }

    public static bool TableExists(string connectionString, string tableName)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@name";
        cmd.Parameters.AddWithValue("@name", tableName);
        return cmd.ExecuteScalar() != null;
    }

    /// <summary>
    /// 新建一个随机临时目录，用完由调用方删除。
    /// </summary>
    public static string CreateTempDir() =>
        Path.Combine(Path.GetTempPath(), "FFmpegMigrateTest_" + Guid.NewGuid().ToString("N"));

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
}
