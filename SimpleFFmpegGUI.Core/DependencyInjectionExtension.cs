using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Services;

namespace SimpleFFmpegGUI;

public static class DependencyInjectionExtension
{
    public static void AddFFmpegServices(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString(AppSettingsKeys.LocalDbKey);
        services
            .AddDbContextFactory<FFmpegDbContext>(o => { o.UseSqlite(connectionString); })
            .AddTransient<LogRepository>()
            .AddTransient<DbConfigService>()
            .AddTransient<PresetRepository>()
            .AddTransient<PresetService>()
            .AddTransient<TaskRepository>()
            .AddTransient<TaskService>()
            .AddSingleton<PowerService>()
            .AddSingleton<QueueService>()
            .AddTransient<MediaInfoService>()
            .AddTransient<IFFmpegTaskServiceFactory, FFmpegTaskServiceFactory>()
            .AddTransient<IFFmpegProcessServiceFactory, FFmpegProcessServiceFactory>()
            .AddTransient<FilePathHelper>()
            .AddSingleton<DbLoggerService>()
            .AddHostedService(provider => provider.GetRequiredService<DbLoggerService>());
    }
}