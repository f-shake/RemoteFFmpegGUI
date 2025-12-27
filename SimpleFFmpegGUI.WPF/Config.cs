using CommunityToolkit.Mvvm.ComponentModel;
using FzLib;
using FzLib.DataStorage.Serialization;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SimpleFFmpegGUI.Model;
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
        private string snapshotSize = "-1:1080";

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

        public Dictionary<TaskType, OutputArguments> LastOutputArguments { get; set; } = new Dictionary<TaskType, OutputArguments>();

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

    public class RemoteHost
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public string Token { get; set; }
    }
}