using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using FzLib.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WebAPI;

namespace SimpleFFmpegGUI.WebTest;

public class SimpleFFmpegWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestAppsettingsJson = "appsettings.test.json";
    private const string MemorySqliteConnectionString = "DataSource=file:db_test?mode=memory&cache=shared";
    private bool hasInitialized = false;

    // 显式保活一条共享缓存内存库连接：cache=shared 下最后一个连接关闭时库即销毁，
    // 避免依赖连接池的隐式保活行为（池被清空/重建会导致库静默消失）
    private static SqliteConnection keepAliveConnection;

    /// <summary>
    /// 将仓库 bin 下的 ffmpeg、MediaInfo、测试视频复制到测试输出目录（git 忽略的 bin 不可提交，测试环境无这些文件）
    /// </summary>
    static SimpleFFmpegWebApplicationFactory()
    {
        var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
        var sourceBin = Path.Combine(repoRoot, "bin");
        var destDir = AppContext.BaseDirectory;
        CopyDirectoryIfExists(Path.Combine(sourceBin, "ffmpeg"), Path.Combine(destDir, "ffmpeg"));
        CopyFileIfExists(Path.Combine(sourceBin, "MediaInfo.exe"), Path.Combine(destDir, "MediaInfo.exe"));
        // 测试视频：优先用仓库内提交的固定资产（clone 后无 bin 也能跑测试），否则退回本地 bin
        var committedTestVideo = Path.Combine(repoRoot, "SimpleFFmpegGUI.WebTest", "test", "test10s.mp4");
        if (File.Exists(committedTestVideo))
        {
            File.Copy(committedTestVideo, Path.Combine(destDir, "test.mp4"), true);
        }
        else
        {
            CopyFileIfExists(Path.Combine(sourceBin, "test.mp4"), Path.Combine(destDir, "test.mp4"));
        }
    }

    protected override void Dispose(bool disposing)
    {
        keepAliveConnection?.Dispose();
        keepAliveConnection = null;
        base.Dispose(disposing);
    }

    private static string FindRepoRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "SimpleFFmpegGUI.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new Exception("未找到仓库根目录（SimpleFFmpegGUI.sln）");
    }

    private static void CopyDirectoryIfExists(string source, string dest)
    {
        if (!Directory.Exists(source))
        {
            return;
        }
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(file, target, true);
        }
    }

    private static void CopyFileIfExists(string source, string dest)
    {
        if (File.Exists(source))
        {
            File.Copy(source, dest, true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (hasInitialized)
        {
            return;
        }

        hasInitialized = true;

        var tempDir = Path.Combine(Path.GetTempPath(), nameof(SimpleFFmpegGUI) + nameof(WebTest));
        var inputDir = Path.Combine(tempDir, "input");
        var outputDir = Path.Combine(tempDir, "output");
        Directory.CreateDirectory(inputDir);
        Directory.CreateDirectory(outputDir);
        Console.WriteLine($"测试目录：{inputDir}");

        var testVideo10s = PrepareTestVideos(inputDir);
        var testOutputVideo10s = PrepareTestVideos(outputDir);


        // SQLite 内存数据库（共享缓存）：测试数据不落盘，无 db_test.sqlite 文件残留
        builder.UseSetting($"ConnectionStrings:{DependencyInjectionExtension.LocalSqliteConnectionStringKey}",
            MemorySqliteConnectionString);

        // 打开保活连接（保持到测试集合结束），确保内存库在全部测试期间存活
        keepAliveConnection ??= new SqliteConnection(MemorySqliteConnectionString);
        keepAliveConnection.Open();
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), TestAppsettingsJson));
            // 内存库连接串必须在 AddJsonFile 之后注入：Core 的 appsettings.json（复制到测试输出）含
            // "ConnectionStrings:LocalSqlite" 文件库键，会覆盖 UseSetting 的内存库设置，导致测试残留
            // db.sqlite 文件库并被多个并行服务器实例共享写锁（间歇 500）
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                [$"ConnectionStrings:{DependencyInjectionExtension.LocalSqliteConnectionStringKey}"] =
                    MemorySqliteConnectionString,
                [nameof(AppSettings.Token)] = "Test_Token_123",
                [nameof(AppSettings.InputDir)] = inputDir,
                [nameof(AppSettings.OutputDir)] = outputDir,
                [nameof(AppTestSettings.TestVideo10s)] = testVideo10s,
                [nameof(AppTestSettings.TestOutputVideo10s)] = testOutputVideo10s,
            });
        });

        builder.ConfigureServices((context, services) =>
        {
            services.Configure<AppSettings>(context.Configuration);
            services.Configure<AppTestSettings>(context.Configuration);
        });
    }

    private static string PrepareTestVideos(string testDir)
    {
        JsonObject testAppSettings = (JsonObject)JsonNode.Parse(File.ReadAllText(TestAppsettingsJson));
        var testVideo = testAppSettings[nameof(AppTestSettings.TestVideo)].GetValue<string>();
        var ffmpegPath = Path.GetFullPath(Path.Combine("ffmpeg", "ffmpeg.exe"));
        if (!File.Exists(ffmpegPath))
        {
            throw new Exception("ffmpeg.exe 不存在");
        }

        //给测试视频裁剪前10秒
        var outputPath = Path.Combine(testDir, Path.GetFileNameWithoutExtension(testVideo) + ".10s.mp4");
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        var argument = $"-i \"{testVideo}\" -t 10 -c copy \"{outputPath}\"";
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = ffmpegPath,
                Arguments = argument,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            },
            EnableRaisingEvents = true,
        };
        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"裁剪测试视频失败：{process.ExitCode}");
        }

        return outputPath;
    }
}