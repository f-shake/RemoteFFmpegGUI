using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Data;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Repositories;
using TaskStatus = SimpleFFmpegGUI.Enums.TaskStatus;

namespace SimpleFFmpegGUI.Services;

public class AppLifetimeService(
    IOptions<AppSettings> appSettings,
    ConfigService configService,
    IDbContextFactory<FFmpegDbContext> dbFactory)
    : IHostedService
{
    private int isStopping = 0;


    public async Task StartAsync(CancellationToken cancellationToken)
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

        await using (var db = await dbFactory.CreateDbContextAsync(cancellationToken))
        {
            foreach (var item in db.Tasks.Where(p => p.Status == TaskStatus.Processing))
            {
                item.Status = TaskStatus.Error;
                item.Message = "状态异常：启动时处于正在运行状态";
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref isStopping, 1) == 1)
        {
            return;
        }

        await configService.SaveAsync();
    }
}