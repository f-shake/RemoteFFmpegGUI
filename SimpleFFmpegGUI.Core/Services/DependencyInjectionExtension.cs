using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;

namespace SimpleFFmpegGUI.Services;

public static class DependencyInjectionExtension
{
    public static void AddFFmpegServices(this IServiceCollection services)
    {
        services
            .AddDbContextFactory<FFmpegDbContext>()
            .AddTransient<LogRepository>()
            .AddTransient<DbConfigService>()
            .AddTransient<PresetManager>()
            .AddTransient<TaskRepository>()
            .AddTransient<PowerService>()
            .AddSingleton<QueueService>()
            .AddTransient<IFFmpegTaskServiceFactory, FFmpegTaskServiceFactory>()
            .AddHostedService<DbLoggerService>();

    }
}