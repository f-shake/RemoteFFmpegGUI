using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Services;

namespace SimpleFFmpegGUI;

public static class DependencyInjectionExtension
{
    public static void AddFFmpegServices(this IServiceCollection services)
    {
        services
            .AddDbContextFactory<FFmpegDbContext>()
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
            .AddSingleton<DbLoggerService>()
            .AddHostedService(provider => provider.GetRequiredService<DbLoggerService>());
    }
}