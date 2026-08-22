using Microsoft.Data.Sqlite;

namespace SimpleFFmpegGUI.Compatibility;

/// <summary>
/// 版本账簿修复迁移：识别"已升级到 v2.0 但 _MigrationHistory.Version 为整数"的旧库，
/// 把版本表重建为文本 `Version` 列。目标版本 2.0.0。
/// 由 <see cref="MigrationRunner"/> 在识别到 LegacyIntHistory 时显式执行（不在默认注册表内），
/// 绝不重跑 v1→v2（该库的 Arguments 列已改名）。
/// </summary>
public sealed class MigrationNormalizeLegacyHistory : IMigration
{
    public string Version => "2.0.0";
    public string Description => "规范化遗留整数版本历史为文本，并置为2.0.0";

    public void Up(SqliteConnection conn, SqliteTransaction tx, MigrationContext context)
    {
        using var drop = conn.CreateCommand();
        drop.Transaction = tx;
        drop.CommandText = "DROP TABLE IF EXISTS _MigrationHistory";
        drop.ExecuteNonQuery();

        using var create = conn.CreateCommand();
        create.Transaction = tx;
        create.CommandText = """
            CREATE TABLE _MigrationHistory (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Version     TEXT NOT NULL,
                AppliedDate TEXT NOT NULL,
                Description TEXT NOT NULL
            )
            """;
        create.ExecuteNonQuery();
    }
}
