using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using FzLib.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
    private static bool hasInitialized = false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (hasInitialized)
        {
            throw new Exception("SimpleFFmpegWebApplicationFactory 只能初始化一次");
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


        builder.UseSetting($"ConnectionStrings:{DependencyInjectionExtension.LocalSqliteConnectionStringKey}",
            $"DataSource={Path.Combine(tempDir, "db_test.sqlite")}");
        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), TestAppsettingsJson));
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
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