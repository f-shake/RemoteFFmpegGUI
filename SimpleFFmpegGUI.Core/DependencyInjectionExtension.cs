using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Logging;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Services;

namespace SimpleFFmpegGUI
{
    public static class DependencyInjectionExtension
    {
        public static void AddFFmpegServices(this IServiceCollection services)
        {
            services
                .AddDbContextFactory<FFmpegDbContext>()
                .AddTransient<LogManager>()
                .AddTransient<ConfigManager>()
                .AddTransient<PresetManager>()
                .AddTransient<TaskManager>()
                .AddTransient<PowerManager>()
                .AddSingleton<QueueManager>()
                .AddSingleton<DbLoggerService>();

        }
    }
}