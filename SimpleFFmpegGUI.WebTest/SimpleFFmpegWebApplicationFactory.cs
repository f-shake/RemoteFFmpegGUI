using System.Diagnostics;
using FzLib.Application;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WebAPI;

namespace SimpleFFmpegGUI.WebTest;

public class SimpleFFmpegWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestVideo = @"C:\Users\autod\Desktop\DJI_20251025145405_0831_D.MP4";

    private static bool hasInitialized = false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configBuilder) =>
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

            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                [AppSettingsKeys.TokenKey] = "Test_Token_123",
                ["Database:ConnectionString"] = "DataSource=:memory:",
                [AppSettingsKeys.InputDirKey] = inputDir,
                [AppSettingsKeys.OutputDirKey] = outputDir,
                [AppTestSettingsKeys.TestVideoKey] = TestVideo,
                [AppTestSettingsKeys.TestVideo10sKey] = testVideo10s,
                [AppTestSettingsKeys.TestOutputVideo10sKey] = testOutputVideo10s,
            });
        });
    }

    private static string PrepareTestVideos(string testDir)
    {
        var ffmpegPath = Path.GetFullPath(Path.Combine("ffmpeg", "ffmpeg.exe"));
        if (!File.Exists(ffmpegPath))
        {
            throw new Exception("ffmpeg.exe 不存在");
        }

        //给测试视频裁剪前10秒
        var outputPath = Path.Combine(testDir, Path.GetFileNameWithoutExtension(TestVideo) + ".10s.mp4");
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        var argument = $"-i \"{TestVideo}\" -t 10 -c copy \"{outputPath}\"";
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