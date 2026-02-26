using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Models;

namespace SimpleFFmpegGUI.Services;

public class AppLifetimeService : IHostedService
{
    private readonly IOptions<AppSettings> appSettings;
    private readonly ConfigService configService;
    private int isStopping = 0; 


    public AppLifetimeService(IOptions<AppSettings> appSettings,
        ConfigService configService)
    {
        Console.WriteLine("AppLifetimeService 构造函数");
        this.appSettings = appSettings;
        this.configService = configService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("AppLifetimeService 启动");
        var ffmpegDir = appSettings.Value.FFmpegDir;
        if (string.IsNullOrEmpty(ffmpegDir))
        {
        }
        else if (Path.IsPathFullyQualified(ffmpegDir))
        {
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegDir });
        }
        else
        {
            GlobalFFOptions.Configure(new FFOptions
                { BinaryFolder = Path.Combine(Directory.GetCurrentDirectory(), ffmpegDir) });
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref isStopping, 1) == 1)
        {
            return;
        }

        Console.WriteLine("AppLifetimeService 停止 (正在执行保存...)");
        await configService.SaveAsync();
    }
}