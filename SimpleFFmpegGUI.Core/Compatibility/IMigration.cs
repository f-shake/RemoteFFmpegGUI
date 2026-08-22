using Microsoft.Data.Sqlite;

namespace SimpleFFmpegGUI.Compatibility;

/// <summary>
/// 数据库迁移器。每个迁移把数据库升级到某个目标程序版本（语义版本）。
/// 只标注目标版本（不设 FromVersion）：等价于"从现在 DB 版本升级到该版本"。
/// 排序与唯一性由 <see cref="MigrationRunner"/> 校验。
/// </summary>
public interface IMigration
{
    /// <summary>
    /// 该迁移把数据库升级到的目标程序版本（"major.minor.patch"）。
    /// </summary>
    string Version { get; }

    /// <summary>
    /// 迁移说明（写入 _MigrationHistory 的 Description）。
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 执行迁移。必须在给定的 <paramref name="tx"/> 内执行；
    /// 版本记录由 <see cref="MigrationRunner"/> 在同一事务内写入，保证原子性。
    /// </summary>
    void Up(SqliteConnection conn, SqliteTransaction tx, MigrationContext context);
}

/// <summary>
/// 迁移执行上下文（按调用注入的运行时参数）。
/// </summary>
public sealed class MigrationContext
{
    /// <summary>
    /// v1 用户配置迁移的目标 config.json 路径（消除对当前目录的依赖）。
    /// </summary>
    public string ConfigJsonPath { get; init; } = string.Empty;
}
