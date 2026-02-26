using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using SimpleFFmpegGUI.Data;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Helpers;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Services;

namespace SimpleFFmpegGUI;

public static class DependencyInjectionExtension
{
    public const string LocalSqliteConnectionStringKey = "LocalSqlite";

    public static void AddFFmpegServices(this IServiceCollection services)
    {
        // 1. 数据库配置
        // 注意：DbContext 的配置通常还是需要读取连接字符串。
        // 我们可以在 Program.cs 中配置，或者在这里通过一个临时 serviceProvider 读取。
        services.AddDbContextFactory<FFmpegDbContext>((sp, o) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString(LocalSqliteConnectionStringKey);
            o.UseSqlite(connectionString);
        });

        // 2. 注册仓储和业务服务
        services
            .AddTransient<LogRepository>()
            .AddTransient<PresetRepository>()
            .AddTransient<PresetService>()
            .AddTransient<TaskRepository>()
            .AddTransient<TaskService>()
            .AddSingleton<PowerService>()
            .AddSingleton<ConfigService>(s => ConfigService.Create())
            .AddSingleton<QueueService>()
            .AddTransient<MediaInfoService>()
            .AddTransient<IFFmpegTaskServiceFactory, FFmpegTaskServiceFactory>()
            .AddTransient<IFFmpegProcessServiceFactory, FFmpegProcessServiceFactory>()
            .AddTransient<FilePathHelper>()
            .AddSingleton<DbLoggerService>();

        // 3. 注册托管服务
        services
            .AddHostedService(provider => provider.GetRequiredService<DbLoggerService>())
            .AddHostedService<AppLifetimeService>();
    }
}