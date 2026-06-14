using FzLib.Program.Runtime;
using log4net;
using log4net.Appender;
using log4net.Layout;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Compatibility;
using SimpleFFmpegGUI.Data;
using SimpleFFmpegGUI.Events;
using SimpleFFmpegGUI.WPF.ViewModels;
using SimpleFFmpegGUI.WPF.Pages;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static SimpleFFmpegGUI.DependencyInjectionExtension;
using System.Windows.Interop;
using FFMpegCore;
using SimpleFFmpegGUI.Services;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace SimpleFFmpegGUI.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static DateTime AppStartTime { get; } = DateTime.Now;
        public static ILog AppLog { get; private set; }
        public static ServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            InitializeLogs();
#if !DEBUG

                WPFUnhandledExceptionCatcher.RegistAll().UnhandledExceptionCatched += UnhandledException_UnhandledExceptionCatched;
#endif

            try
            {
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{DependencyInjectionExtension.LocalSqliteConnectionStringKey}"] = "Data Source=db.sqlite"
                    })
                    .Build();

                // 迁移 v1.1 → v2.0 数据库（如果检测到旧版）
                DatabaseMigrator.MigrateIfNeeded(config.GetConnectionString(DependencyInjectionExtension.LocalSqliteConnectionStringKey));

                var tempServices = new ServiceCollection();
                tempServices.AddSingleton<IConfiguration>(config);
                tempServices.AddFFmpegServices();
                var sp = tempServices.BuildServiceProvider();
                var factory = sp.GetRequiredService<IDbContextFactory<FFmpegDbContext>>();
                using var context = factory.CreateDbContext();
                context.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                throw new Exception("数据库初始化失败", ex);
            }

            Unosquare.FFME.Library.FFmpegDirectory = Path.Combine(FzLib.Program.App.ProgramDirectoryPath,"ffmpeg_FFME");
            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = Path.Combine(FzLib.Program.App.ProgramDirectoryPath, "ffmpeg") });
            var config2 = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{DependencyInjectionExtension.LocalSqliteConnectionStringKey}"] = "Data Source=db.sqlite"
                })
                .Build();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(config2);
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            // 订阅数据库日志事件
            var dbLogger = ServiceProvider.GetService<DbLoggerService>();
            if (dbLogger != null)
            {
                dbLogger.Log += Logger_Log;
                dbLogger.LogSaveFailed += Logger_LogSaveFailed;
            }

            if (e.Args.Length > 1)
            {
                if (e.Args[0] == "cut")
                {
                    MainWindow = new CutWindow(e.Args[2..]);
                    WindowInteropHelper helper = new WindowInteropHelper(MainWindow);
                    helper.Owner = IntPtr.Parse(e.Args[1]);
                    try
                    {
                        MainWindow.ShowDialog();
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                        helper.Owner = 0;
                        MainWindow.ShowDialog();
                    }
                }
                else
                {
                    throw new ArgumentException("未知参数：" + e.Args[0]);
                }
            }
            else
            {
                ServiceProvider.GetService<FFmpegOutputPageViewModel>();
                MainWindow = ServiceProvider.GetService<MainWindow>();
                MainWindow.Show();
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddFFmpegServices();
            services.AddSingleton<Config>();

            services.AddSingleton<CurrentTasksViewModel>();
            services.AddSingleton<AllTasksViewModel>();

            services.AddSingleton<MainWindow>();
            services.AddTransient<MainWindowViewModel>();

            services.AddTransient<TestWindow>();
            services.AddTransient<TestWindowViewModel>();

            services.AddTransient<AddTaskPage>();
            services.AddTransient<AddTaskPageViewModel>();

            services.AddTransient<MediaInfoPage>();
            services.AddTransient<MediaInfoPageViewModel>();

            services.AddTransient<LogsPage>();
            services.AddTransient<LogsPageViewModel>();

            services.AddTransient<TasksPage>();
            services.AddTransient<TasksPageViewModel>();

            services.AddTransient<SettingPage>();
            services.AddTransient<SettingPageViewModel>();

            services.AddTransient<PresetsPage>();
            services.AddTransient<PresetsPageViewModel>();

            services.AddTransient<FFmpegOutputPage>();
            services.AddSingleton<FFmpegOutputPageViewModel>();

            services.AddTransient<CutWindowViewModel>();

            services.AddTransient<TaskListViewModel>();
            services.AddTransient<CodeArgumentsPanelViewModel>();
            services.AddTransient<FileIOPanelViewModel>();
            services.AddTransient<PresetsPanelViewModel>();
            services.AddTransient<StatusPanelViewModel>();
        }

        private void InitializeLogs()
        {
            //本地日志
            AppLog = log4net.LogManager.GetLogger(GetType());
            AppLog.Info("程序启动");
        }

        private void Logger_Log(object sender, LogEventArgs e)
        {
            switch (e.Log.Type)
            {
                case 'E': AppLog.Error(e.Log.Message); break;
                case 'W': AppLog.Warn(e.Log.Message); break;
                case 'I': AppLog.Info(e.Log.Message); break;
            }
        }

        private void Logger_LogSaveFailed(object sender, ExceptionEventArgs e)
        {
            AppLog.Error(e.Exception.Message, e.Exception);
        }

        private void UnhandledException_UnhandledExceptionCatched(object sender, FzLib.Program.Runtime.UnhandledExceptionEventArgs e)
        {
            try
            {
                AppLog.Error(e.Exception);
                Dispatcher.Invoke(() =>
                {
                    var result = MessageBox.Show("程序发生异常，可能出现数据丢失等问题。是否关闭？" + Environment.NewLine + Environment.NewLine + e.Exception.ToString(), FzLib.Program.App.ProgramName + " - 未捕获的异常", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (result == MessageBoxResult.Yes)
                    {
                        Shutdown(-1);
                    }
                });
            }
            catch (Exception ex)
            {
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            App.ServiceProvider?.GetService<DbLoggerService>()?.SaveAllAsync();
        }
    }
}