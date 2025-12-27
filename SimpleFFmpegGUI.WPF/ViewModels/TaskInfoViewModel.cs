using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FzLib;
using FzLib.WPF.Converters;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.WPF.Converters;
using SimpleFFmpegGUI.WPF.Messages;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaskStatus = SimpleFFmpegGUI.Model.TaskStatus;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class TaskInfoViewModel : ViewModelBase, IModel
    {
        [ObservableProperty]
        private OutputArguments arguments;

        [ObservableProperty]
        private DateTime createTime;

        [ObservableProperty]
        private string ffmpegArguments;


        [ObservableProperty]
        private DateTime? finishTime;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(InputText), nameof(InputsText), nameof(IOText))]
        private List<InputArguments> inputs;

        [ObservableProperty]
        private bool isSelected;

        /// <summary>
        /// 上一个缩略图的时间
        /// </summary>
        private TimeSpan lastTime = TimeSpan.MaxValue;

        [ObservableProperty]
        private string message;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OutputText), nameof(IOText))]
        private string output;

        private FFmpegManager processManager;

        [ObservableProperty]
        private int processPriority = App.ServiceProvider.GetRequiredService<ConfigManager>().DefaultProcessPriority;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Percent), nameof(Status), nameof(StatusText), nameof(IsIndeterminate))]
        private StatusDto processStatus;

        [ObservableProperty]
        private string realOutput;

        [ObservableProperty]
        private DateTime? startTime;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText), nameof(Color), nameof(Percent))]
        private TaskStatus status;

        /// <summary>
        /// 更新缩略图计时器
        /// </summary>
        private PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        [ObservableProperty]
        private TaskType type;

        public TaskInfoViewModel()
        {
            StartTimer();
        }

        ~TaskInfoViewModel()
        {
            timer.Dispose();
        }

        public Brush Color => Status switch
        {
            TaskStatus.Queue => System.Windows.Application.Current.FindResource("SystemControlForegroundBaseHighBrush") as Brush,
            TaskStatus.Processing => Brushes.Orange,
            TaskStatus.Done => Brushes.Green,
            TaskStatus.Error => Brushes.Red,
            TaskStatus.Cancel => Brushes.Gray,
            _ => throw new InvalidEnumArgumentException(),
        };

        public int Id { get; set; }
        public string InputDetailText
        {
            get
            {
                if (Inputs.Count != 1)
                {
                    return string.Join(Environment.NewLine, Inputs.Select(p => Path.GetFileName(p.FilePath)));
                }
                string name = Path.GetFileName(Inputs[0].FilePath);
                return name
                    + (Inputs[0].From.HasValue ? $" 开始：{Inputs[0].From.Value:hh\\:mm\\:ss\\.fff}" : "")
                    + (Inputs[0].To.HasValue ? $" 结束：{Inputs[0].To.Value:hh\\:mm\\:ss\\.fff}" : "")
                    + (Inputs[0].Duration.HasValue ? $" 经过：{Inputs[0].Duration.Value:hh\\:mm\\:ss\\.fff}" : "");
            }
        }

        public string InputDetailToolTipText
        {
            get
            {
                if (Inputs.Count != 1)
                {
                    return string.Join(Environment.NewLine, Inputs.Select(p => p.FilePath));
                }
                return InputDetailText;
            }
        }

        public string InputsText
        {
            get
            {
                if (Inputs.Count == 0)
                {
                    return "未指定输入";
                }
                return string.Join("\r\n", Inputs.Select(p => Path.GetFileName(p.FilePath)));
            }
        }

        public string InputText
        {
            get
            {
                if (Inputs.Count == 0)
                {
                    return "未指定输入";
                }
                string path = Path.GetFileName(Inputs[0].FilePath);
                return Inputs.Count == 1 ? path : path + "等";
            }
        }

        public string IOText => $"{InputText} → {OutputText}";
        public bool IsDeleted { get; set; }
        public bool IsIndeterminate => ProcessStatus == null || ProcessStatus.HasDetail == false || ProcessStatus.Progress.IsIndeterminate;
        public string OutputText
        {
            get
            {
                string output = Output;
                if (!string.IsNullOrEmpty(RealOutput))
                {
                    output = Path.GetFileName(RealOutput);
                }
                else if (output == null)
                {
                    output = "未指定输出";
                }
                else
                {
                    output = Path.GetFileName(Output);
                }
                return output;
            }
        }

        public double Percent => ProcessStatus == null || ProcessStatus.HasDetail == false ? 0 : ProcessStatus.Progress.Percent;
        public FFmpegManager ProcessManager
        {
            get => processManager;
            set
            {
                if (processManager != null)
                {
                    processManager.ProcessChanged -= Manager_ProcessChanged;
                }
                processManager = value;
                if (value != null)
                {
                    value.ProcessChanged += Manager_ProcessChanged;
                }
            }
        }

        public string StatusText => Status switch
        {
            TaskStatus.Processing => IsIndeterminate ? "进行中" : Percent.ToString("0.00%"),
            _ => DescriptionConverter.GetDescription(Status)
        };

        public string Title => Type == TaskType.Custom ? AttributeHelper.GetAttributeValue<NameDescriptionAttribute, string>(Type, p => p.Name)
            : AttributeHelper.GetAttributeValue<NameDescriptionAttribute, string>(Type, p => p.Name) + "：" + InputText;

        public SnapshotViewModel Snapshot { get; } = new SnapshotViewModel();
        public static TaskInfoViewModel FromTask(TaskInfo task)
        {
            return task.Adapt<TaskInfoViewModel>();
        }

        public Task<TaskInfo> GetTaskAsync()
        {
            return App.ServiceProvider.GetRequiredService<TaskManager>().GetTaskAsync(Id);
        }

        public TaskInfo ToTask()
        {
            return this.Adapt<TaskInfo>();
        }

        public async Task UpdateSelfAsync()
        {
            var task = await GetTaskAsync();
            task.Adapt(this);
            FfmpegArguments = task.FFmpegArguments;
        }

        public async Task UpdateSnapshotAsync()
        {
            if (Snapshot.DisplayFrame == false
                || Type != TaskType.Code //不是编码类型的任务
                || ProcessStatus == null //没有状态
                || !ProcessStatus.HasDetail) //状态无详情)
            {
                Snapshot.DisplayFrame = false;
                //取消执行并不显示缩略图
                return;
            }

            var time = ProcessStatus.Time + (Inputs[0].From ?? TimeSpan.Zero);
            if (ProcessStatus.IsPaused //任务暂停中
             || !Snapshot.CanUpdate
                || (lastTime - time).Duration().TotalSeconds < 1) //上一张缩略图和现在的时间差不到1s
            {
                //仅取消执行
                return;
            }
            lastTime = time;
            string path = null;
            try
            {
                path = await MediaInfoManager.GetSnapshotAsync(Inputs[0].FilePath, time, Config.Instance.SnapshotSize);
            }
            catch (Exception ex)
            {
                App.AppLog.Error($"获取视频{Inputs[0].FilePath}在{time}的快照失败", ex);
            }
            Snapshot.Source = path == null ? null : new Uri(path);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(ProcessPriority))
            {
                if (ProcessManager.Process != null && ProcessManager.Process.Priority != ProcessPriority)
                {
                    ProcessManager.Process.Priority = ProcessPriority;
                }
            }
        }
        private void Manager_ProcessChanged(object sender, ProcessChangedEventArgs e)
        {
            //进程改变后（比如二压），重新应用
            if (e.NewProcess != null)
            {
                ProcessPriority = e.NewProcess.Priority;
            }
        }

        private async void StartTimer()
        {
            while (await timer.WaitForNextTickAsync())
            {
                Stopwatch sw = Stopwatch.StartNew();
                await UpdateSnapshotAsync();
                sw.Stop();
            }
        }
    }
}