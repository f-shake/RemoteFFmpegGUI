using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;

namespace SimpleFFmpegGUI.Services;

public class AppLifetimeService(IOptions<AppSettings> appSettings) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}