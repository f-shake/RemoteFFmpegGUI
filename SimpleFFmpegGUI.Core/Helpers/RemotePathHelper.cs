using System;
using System.IO;

namespace SimpleFFmpegGUI.Helpers;

/// <summary>
/// 远程提交时的路径映射：把本机文件路径换算成远端输入目录（InputDir）下的相对路径。
/// 放在 Core 而不是 WPF：映射是纯逻辑，WPF 项目是 net10.0-windows，单元测试项目引用不了它。
/// </summary>
public static class RemotePathHelper
{
    /// <summary>
    /// 本地路径 → 远端可用路径。位于 <paramref name="localBaseDir"/> 之内时返回相对路径并保留子目录；
    /// 在其之外（同级目录逃逸、跨盘符）或基准目录未知时退回只发文件名
    /// （约定：由用户保证远端 InputDir 根目录下有同名文件）。
    /// 背景：v1 只在路径为绝对路径时才压成文件名，v2 一度无条件压平，导致源文件放在子文件夹里时
    /// 子目录丢失、远端按 InputDir 根目录找不到文件而报 404。
    /// </summary>
    public static string ToRemoteInputPath(string localPath, string localBaseDir)
    {
        if (string.IsNullOrEmpty(localPath))
        {
            return "";
        }

        if (!string.IsNullOrEmpty(localBaseDir))
        {
            // GetRelativePath 已处理尾部分隔符、混用斜杠、盘符与大小写差异；不在其下时返回
            // ".."、"..\…"（同盘其它目录）或原绝对路径（跨盘符），这些都不能提交，退回文件名
            string relative = Path.GetRelativePath(localBaseDir, localPath);
            if (!IsOutside(relative) && !Path.IsPathRooted(relative))
            {
                return relative;
            }
        }

        return Path.GetFileName(localPath);
    }

    /// <summary>
    /// 是否是"逃出了基准目录"的相对路径。只拒绝 ".." 本身和以 "..\"/"../" 开头的路径，
    /// 不能简单用 StartsWith("..")——名字以两个点开头的合法子目录（如 "..backup\a.mp4"）会被误判成逃逸，
    /// 那样会静默退化成文件名，进而可能让远端处理根目录下的同名文件（处理错文件）。
    /// </summary>
    private static bool IsOutside(string relative)
    {
        return relative == ".."
            || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
    }
}
