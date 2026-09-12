using CommunityToolkit.Mvvm.ComponentModel;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Path = System.IO.Path;

namespace SimpleFFmpegGUI.WPF
{
    public enum DefaultOutputDirType
    {
        [Description("输入文件所在文件夹")]
        InputDir,
        [Description("输入文件下指定文件夹")]
        InputNewDir,
        [Description("指定文件夹")]
        SpecialDir
    }

    public partial class Config : ObservableObject
    {
        private const string path = "config.json";

        private static bool loaded = false;

        [ObservableProperty]
        private bool clearFilesAfterAddTask;

        [ObservableProperty]
        private string defaultOutputDirInputSubDirName = "output";

        [ObservableProperty]
        private string defaultOutputDirSpecialDirPath = "C:\\output";

        [ObservableProperty]
        private DefaultOutputDirType defaultOutputDirType = DefaultOutputDirType.InputDir;

        [ObservableProperty]
        private bool rememberLastArguments = true;

        [ObservableProperty]
        private List<RemoteHost> remoteHosts = new List<RemoteHost>();

        [ObservableProperty]
        private bool smoothScroll = true;

        [ObservableProperty]
        private bool startQueueAfterAddTask = true;

        public static Config Instance
        {
            get
            {
                var config = App.ServiceProvider.GetService<Config>();
                if (!loaded)
                {
                    loaded = true;
                    if (File.Exists(path))
                    {
                        try
                        {
                            var json = File.ReadAllText(path);
                            var obj = JsonConvert.DeserializeObject<Config>(json);
                            obj.Adapt(config);
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                }
                return config;
            }
        }

        public Dictionary<TaskType, OutputParameters> LastOutputArguments { get; set; } = new Dictionary<TaskType, OutputParameters>();

        public PerformanceTestCodecParameterViewModel[] TestCodecs { get; set; }
        public PerformanceTestLine[] TestItems { get; set; }
        public int TestQCMode { get; set; } = 0;
        public string TestVideo { get; set; }
        public bool WindowMaximum { get; set; } = false;
        public Config DeepCopy()
        {
            var serialized = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<Config>(serialized);
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            });
            File.WriteAllText(path, json);
        }
    }

    /// <summary>
    /// 远程主机配置。继承 ObservableObject 是为了设置页：那里的「浏览」按钮由代码直接给
    /// LocalInputDir 赋值，纯 POCO 时 DataGrid 单元格收不到变更通知、显示不会刷新
    /// （手动输入走的是编辑态，本来就能正常显示）。
    /// </summary>
    public partial class RemoteHost : ObservableObject
    {
        [ObservableProperty]
        private string address;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string token;

        /// <summary>
        /// 本机上对应远端输入目录（InputDir）的位置，例如远端是 \\NAS\共享\待处理、本机映射成 Z:\待处理。
        /// 向该主机提交任务时，本地输入文件按此目录求相对路径（保留子目录）后发给远端。
        /// 留空则退回"按远端上报的 InputDir 匹配，匹配不上只发文件名"的旧行为。
        /// </summary>
        [ObservableProperty]
        private string localInputDir;
    }
}