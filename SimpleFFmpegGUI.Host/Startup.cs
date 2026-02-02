using FFMpegCore;
using FzLib.Program.Runtime;
using JKang.IpcServiceFramework.Hosting;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Model;
using System;
using System.IO;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace SimpleFFmpegGUI
{
    public class Startup
    {
        public const string DefaultPipeName = "ffpipe";

        public static ILog AppLog { get; private set; }

        public static IHostBuilder InitializeServices(IHostBuilder builder, string pipeName = DefaultPipeName)
        {
#if !DEBUG
            UnhandledExceptionCatcher catcher = new UnhandledExceptionCatcher();
            catcher.RegisterTaskCatcher();
            catcher.RegisterThreadsCatcher();
            catcher.UnhandledExceptionCatched += UnhandledException_UnhandledExceptionCatched;
#endif
            LogManager.
            InitializeLogs();
            try
            {
                FFmpegDbContext.Migrate();
            }
            catch (Exception ex)
            {
                AppLog.Error(ex);
                Console.WriteLine("数据库迁移失败：" + ex);
                Console.WriteLine("程序终止");
                Console.ReadKey();
                return null;
            }
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = Path.Combine(FzLib.Program.App.ProgramDirectoryPath, "ffmpeg") });

            builder ??= Host.CreateDefaultBuilder();
            return builder
                .ConfigureServices(services =>
                    {
                        services.AddFFmpegServices();
                        services.AddSingleton<IPipeService, PipeService>();
                    })
                    .ConfigureIpcHost(builder =>
                    {
                        builder.AddNamedPipeEndpoint<IPipeService>(p =>
                        {
                            p.PipeName = pipeName;
                            p.IncludeFailureDetailsInResponse = true;
                        });
                    })
                    .ConfigureLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
        }

        

    
    }
}