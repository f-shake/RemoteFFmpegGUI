using System;
using System.Globalization;

namespace SimpleFFmpegGUI.Compatibility;

/// <summary>
/// 语义版本工具类：把 "major.minor.patch" 字符串解析为数值三元组，按组件级比较。
/// 忽略预发布后缀（-rc.x / +build），仅比较数字部分。
/// </summary>
internal static class VersionUtil
{
    /// <summary>
    /// 解析 "2.0.1" 为 (2, 0, 1)。非纯数字三元组抛 <see cref="FormatException"/>。
    /// </summary>
    public static (int Major, int Minor, int Patch) Parse(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new FormatException("版本号不能为空");

        // 忽略预发布/构建后缀
        var trimmed = version;
        var plus = trimmed.IndexOf('+');
        if (plus >= 0)
            trimmed = trimmed[..plus];
        var dash = trimmed.IndexOf('-');
        if (dash >= 0)
            trimmed = trimmed[..dash];

        var parts = trimmed.Split('.');
        if (parts.Length is < 1 or > 3 || !TryParsePart(parts[0], out var major))
            throw new FormatException($"非法版本号: {version}");

        var minor = 0;
        var patch = 0;
        if (parts.Length >= 2 && !TryParsePart(parts[1], out minor))
            throw new FormatException($"非法版本号: {version}");
        if (parts.Length >= 3 && !TryParsePart(parts[2], out patch))
            throw new FormatException($"非法版本号: {version}");

        return (major, minor, patch);
    }

    /// <summary>
    /// 组件级比较：a 大于 b 返回正数，小于返回负数，相等返回 0。
    /// </summary>
    public static int Compare(string a, string b)
    {
        var (aMajor, aMinor, aPatch) = Parse(a);
        var (bMajor, bMinor, bPatch) = Parse(b);

        if (aMajor != bMajor) return aMajor.CompareTo(bMajor);
        if (aMinor != bMinor) return aMinor.CompareTo(bMinor);
        return aPatch.CompareTo(bPatch);
    }

    public static bool GreaterThan(string a, string b) => Compare(a, b) > 0;
    public static bool LessThanOrEqual(string a, string b) => Compare(a, b) <= 0;
    public static bool Equal(string a, string b) => Compare(a, b) == 0;

    private static bool TryParsePart(string part, out int value)
    {
        return int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value >= 0;
    }
}
