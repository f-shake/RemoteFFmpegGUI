using System.Collections.Generic;

namespace SimpleFFmpegGUI.Compatibility;

/// <summary>
/// 内置迁移注册表。WebAPI 与 WPF 共用。
/// 注意：<see cref="MigrationNormalizeLegacyHistory"/> 是"版本账簿修复"（针对旧整数历史），
/// 由 <see cref="MigrationRunner"/> 在识别到 LegacyIntHistory 时显式执行，不在此注册表内。
/// </summary>
public static class MigrationRegistry
{
    /// <summary>
    /// 默认迁移表（按目标版本升序）。
    /// </summary>
    public static IReadOnlyList<IMigration> Default { get; } =
        new IMigration[]
        {
            new MigrationV1_1ToV2_0()
        };
}
