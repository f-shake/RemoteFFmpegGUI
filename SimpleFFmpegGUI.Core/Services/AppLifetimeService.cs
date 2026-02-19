using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace SimpleFFmpegGUI.Services;

public class AppLifetimeService(IConfiguration config) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var ffmpegDir = config.GetValue<string>(AppSettingsKeys.FFmpegDirKey);
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