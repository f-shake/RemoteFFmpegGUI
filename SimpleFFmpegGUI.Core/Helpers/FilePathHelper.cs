using System.IO;
using FzLib.Net;
using Microsoft.Extensions.Configuration;
using SimpleFFmpegGUI.Enums;

namespace SimpleFFmpegGUI.Extensions;

public class FilePathHelper(IConfiguration config)
{
    private readonly string inputDir = config.GetValue<string>(AppSettingsKeys.InputDirKey) ??
                                       throw new HttpStatusCodeException("没有配置输入文件夹",
                                           System.Net.HttpStatusCode.InternalServerError);

    private readonly string outputDir = config.GetValue<string>(AppSettingsKeys.OutputDirKey) ??
                                        throw new HttpStatusCodeException("没有配置输出文件夹",
                                            System.Net.HttpStatusCode.InternalServerError);

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