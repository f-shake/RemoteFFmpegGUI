using System;
using System.IO;
using System.Net;
using FzLib.Web;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models;

namespace SimpleFFmpegGUI.Helpers;

public class FilePathHelper(IOptionsSnapshot<AppSettings> appSettings)
{
    private readonly string inputDir = appSettings.Value.InputDir ??
                                       throw new HttpStatusCodeException("没有配置输入文件夹",
                                           HttpStatusCode.InternalServerError);

    private readonly string outputDir = appSettings.Value.OutputDir ??
                                        throw new HttpStatusCodeException("没有配置输出文件夹",
                                            HttpStatusCode.InternalServerError);

    public string InputDir => inputDir;

    public string OutputDir => outputDir;

    /// <summary>
    /// 解析后的输入目录绝对路径。配置里允许写相对路径（出厂示例就是 "input"），但对外暴露
    /// （File/Dirs 接口）与写入任务的路径都必须是绝对路径：前端靠它给任务路径做前缀匹配、
    /// 砍掉前缀只显示相对路径；否则前端永远匹配不上，只能显示完整绝对路径。
    /// 注意：相对配置值是按<b>进程当前目录</b>解析的。WebAPI 启动时把工作目录设成 DLL 所在目录
    /// （Program.cs 的 SetCurrentDirectory），文件枚举、FileInfoDto 的相对路径也都用同一基准，三者一致；
    /// 若将来改掉/绕过那一步，相对配置下的这些相对路径会一起变脏。
    /// </summary>
    public string InputDirFullPath => Path.GetFullPath(inputDir);

    /// <summary>
    /// 解析后的输出目录绝对路径，理由同 <see cref="InputDirFullPath"/>。
    /// </summary>
    public string OutputDirFullPath => Path.GetFullPath(outputDir);

    public string GetFullPath(RootDirType type, string relPathOrFullPath, bool allowAbsolute = false)
    {
        var rootDir = type == RootDirType.InputDir ? InputDirFullPath : OutputDirFullPath;
        string fullPath;
        if (Path.IsPathFullyQualified(relPathOrFullPath))
        {
            // 默认拒绝绝对路径：v2 约定文件服务只接受相对路径（防任意路径读写，P3-1）。
            // 任务创建（allowAbsolute=true）允许绝对路径输入，但同样必须落在 rootDir 内。
            if (!allowAbsolute)
            {
                throw new HttpStatusCodeException($"不支持绝对路径：{relPathOrFullPath}",
                    HttpStatusCode.BadRequest);
            }

            fullPath = Path.GetFullPath(relPathOrFullPath);
        }
        else
        {
            fullPath = Path.GetFullPath(Path.Combine(rootDir, relPathOrFullPath));
        }

        // 规范化后必须仍在 rootDir 内，拒绝 ..\ 逃逸（P3-1）
        var rootWithSeparator = rootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpStatusCodeException($"路径越界：{relPathOrFullPath}",
                HttpStatusCode.BadRequest);
        }

        return fullPath;
    }
}