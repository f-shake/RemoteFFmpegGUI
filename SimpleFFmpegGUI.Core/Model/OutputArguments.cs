using FzLib;
using FzLib.Programming;
using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public class OutputArguments : INotifyPropertyChanged
    {
        private AudioCodeArguments audio;
        private CombineArguments combine;
        private bool disableAudio;
        private bool disableVideo;
        private string extra;
        private string format;
        private StreamArguments stream;
        private VideoCodeArguments video;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 音频参数
        /// </summary>
        public AudioCodeArguments Audio
        {
            get => audio;
            set => this.SetValueAndNotify(ref audio, value, nameof(Audio));
        }

        /// <summary>
        /// 音视频合并参数
        /// </summary>
        public CombineArguments Combine
        {
            get => combine;
            set => this.SetValueAndNotify(ref combine, value, nameof(Combine));
        }

        /// <summary>
        /// 是否禁用音频
        /// </summary>
        public bool DisableAudio
        {
            get => disableAudio;
            set => this.SetValueAndNotify(ref disableAudio, value, nameof(DisableAudio));
        }

        /// <summary>
        /// 是否禁用视频（画面）
        /// </summary>
        public bool DisableVideo
        {
            get => disableVideo;
            set => this.SetValueAndNotify(ref disableVideo, value, nameof(DisableVideo));
        }

        /// <summary>
        /// 额外参数
        /// </summary>
        public string Extra
        {
            get => extra;
            set => this.SetValueAndNotify(ref extra, value, nameof(Extra));
        }

        /// <summary>
        /// 容器格式（后缀名）
        /// </summary>
        public string Format
        {
            get => format;
            set => this.SetValueAndNotify(ref format, value, nameof(Format));
        }

        /// <summary>
        /// 流参数
        /// </summary>
        public StreamArguments Stream
        {
            get => stream;
            set => this.SetValueAndNotify(ref stream, value, nameof(Stream));
        }

        /// <summary>
        /// 视频参数
        /// </summary>
        public VideoCodeArguments Video
        {
            get => video;
            set => this.SetValueAndNotify(ref video, value, nameof(Video));
        }

        public ProcessedOptions ProcessedOptions { get; set; } = new ProcessedOptions();
    }
}