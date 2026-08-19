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

    public string GetFullPath(RootDirType type, string relPathOrFullPath, bool allowAbsolute = false)
    {
        var rootDir = Path.GetFullPath(type == RootDirType.InputDir ? inputDir : outputDir);
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