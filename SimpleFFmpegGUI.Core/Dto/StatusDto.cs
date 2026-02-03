using FzLib;
using FzLib.Programming;
using Newtonsoft.Json;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Dto
{
    public class StatusDto : INotifyPropertyChanged
    {
        /// <summary>
        /// 识别FFmpeg输出的进度信息的正则
        /// </summary>
        private static readonly Regex rFFmpegOutput = new Regex(
            @"(frame= *(?<f>[0-9]+) *fps= *(?<fps>[0-9\.]+) *(q= *(?<q>[0-9\.\-]+) *)+)?size= *(?<size>([0-9\.a-zA-Z ]+)|(N/A)) *time= *(?<time>[0-9\.\-:]+) *bitrate= *(?<b>([0-9\.a-z/]+)|(N/A)).*speed= *(?<speed>([0-9\. ]+)|(N/A))x?", RegexOptions.Compiled);

        private string bitrate;

        private double fps;

        private int frame;

        private bool hasDetail = false;

        private bool isPaused;

        private bool isProcessing;

        private string lastOutput;

        private ProgressDto progress;

        private double q;

        private string size;

        private string speed;

        private TaskInfo task;

        private TimeSpan time;

        public StatusDto()
        {
        }

        public StatusDto(TaskInfo task)
        {
            Task = task;
        }

        public StatusDto(TaskInfo task, ProgressDto progress, string lastOutput, bool paused)
        {
            Task = task;
            LastOutput = lastOutput;
            IsProcessing = true;
            IsPaused = paused;
            if (lastOutput != null && rFFmpegOutput.IsMatch(lastOutput))
            {
                try
                {
                    var match = rFFmpegOutput.Match(lastOutput);
                    Frame = match.Groups["f"].Success ? int.Parse(match.Groups["f"].Value) : -1;
                    Fps = match.Groups["fps"].Success ? double.Parse(match.Groups["fps"].Value) : -1;
                    Size = match.Groups["size"].Value.ToUpper();
                    Time = TimeSpan.Parse(match.Groups["time"].Value);
                    if (Time < TimeSpan.Zero)
                    {
                        Time = TimeSpan.Zero;
                    }
                    Bitrate = match.Groups["b"].Value;
                    Speed = match.Groups["speed"].Value;
                    Q = match.Groups["q"].Success ? double.Parse(match.Groups["q"].Value) : -1;

                    if (progress != null)
                    {
                        if (!IsPaused)
                        {
                            progress.Update(Time);
                        }
                        HasDetail = true;
                    }
                    Progress = progress;
                }
                catch
                {

                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 码率
        /// </summary>
        public string Bitrate
        {
            get => bitrate;
            set => this.SetValueAndNotify(ref bitrate, value, nameof(Bitrate));
        }

        /// <summary>
        /// 转码帧速度
        /// </summary>
        public double Fps
        {
            get => fps;
            set => this.SetValueAndNotify(ref fps, value, nameof(Fps));
        }

        /// <summary>
        /// 当前帧
        /// </summary>
        public int Frame
        {
            get => frame;
            set => this.SetValueAndNotify(ref frame, value, nameof(Frame));
        }

        /// <summary>
        /// 是否有更详细的信息（进度）
        /// </summary>
        public bool HasDetail
        {
            get => hasDetail;
            set => this.SetValueAndNotify(ref hasDetail, value, nameof(HasDetail));
        }

        /// <summary>
        /// 是否已暂停
        /// </summary>
        public bool IsPaused
        {
            get => isPaused;
            set => this.SetValueAndNotify(ref isPaused, value, nameof(IsPaused));
        }

        /// <summary>
        /// 是否正在执行
        /// </summary>
        public bool IsProcessing
        {
            get => isProcessing;
            set => this.SetValueAndNotify(ref isProcessing, value, nameof(IsProcessing));
        }

        /// <summary>
        /// 输出字符串
        /// </summary>
        public string LastOutput
        {
            get => lastOutput;
            set => this.SetValueAndNotify(ref lastOutput, value, nameof(LastOutput));
        }

        /// <summary>
        /// 进度信息
        /// </summary>
        public ProgressDto Progress
        {
            get => progress;
            set => this.SetValueAndNotify(ref progress, value, nameof(Progress));
        }

        /// <summary>
        /// 质量
        /// </summary>
        public double Q
        {
            get => q;
            set => this.SetValueAndNotify(ref q, value, nameof(Q));
        }

        /// <summary>
        /// 当前文件大小
        /// </summary>
        public string Size
        {
            get => size;
            set => this.SetValueAndNotify(ref size, value, nameof(Size));
        }

        /// <summary>
        /// 处理速度信息
        /// </summary>
        public string Speed
        {
            get => speed;
            set => this.SetValueAndNotify(ref speed, value, nameof(Speed));
        }

        /// <summary>
        /// 任务
        /// </summary>
        public TaskInfo Task
        {
            get => task;
            set => this.SetValueAndNotify(ref task, value, nameof(Task));
        }

        /// <summary>
        /// 当前视频里的时间
        /// </summary>
        public TimeSpan Time
        {
            get => time;
            set => this.SetValueAndNotify(ref time, value, nameof(Time));
        }

    }
}