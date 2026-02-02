using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;

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
                .AddSingleton<QueueManager>();
        }
    }
}