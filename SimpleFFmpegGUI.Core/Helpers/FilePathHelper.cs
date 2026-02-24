using System.IO;
using System.Net;
using FzLib.Web;
using Microsoft.Extensions.Options;
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

    public string GetFullPath(RootDirType type, string relPathOrFullPath, bool checkExists = true)
    {
        var rootDir = type == RootDirType.InputDir ? inputDir : outputDir;
        string path = Path.IsPathFullyQualified(relPathOrFullPath)
            ? relPathOrFullPath
            : Path.Combine(rootDir, relPathOrFullPath);

        if (checkExists && !File.Exists(path))
        {
            throw new HttpStatusCodeException($"不存在文件{relPathOrFullPath}", System.Net.HttpStatusCode.NotFound);
        }

        return path;
    }
}