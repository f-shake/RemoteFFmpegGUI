using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Data;
using SimpleFFmpegGUI.Events;
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
            .AddSingleton<ConfigService>(s =>
            {
                var config = ConfigService.Create();
                // config.json 未保存过配置时，用 appsettings.json 的默认值填充（如默认进程优先级）
                if (!config.LoadedFromFile)
                {
                    var appSettings = s.GetRequiredService<IOptions<AppSettings>>().Value;
                    config.DefaultProcessPriority = appSettings.DefaultProcessPriority;
                }
                return config;
            })
            .AddSingleton<QueueService>()
            // 任务/队列变更通知器必须单例：TaskService/TaskRepository 是瞬时服务，
            // 宿主（WebAPI 的实时推送服务）必须订阅到长生命周期对象上才能持续收到通知
            .AddSingleton<ITaskChangeNotifier, TaskChangeNotifier>()
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