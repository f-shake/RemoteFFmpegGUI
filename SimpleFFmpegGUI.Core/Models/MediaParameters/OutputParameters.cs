using FzLib;
using FzLib.Programming;
using System.ComponentModel;

namespace SimpleFFmpegGUI.Models
{
    public class OutputParameters : INotifyPropertyChanged
    {
        private AudioCodecParameters audio;
        private CombineParameters combine;
        private bool disableAudio;
        private bool disableVideo;
        private string extra;
        private string format;
        private StreamParameters stream;
        private VideoCodecParameters video;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 音频参数
        /// </summary>
        public AudioCodecParameters Audio
        {
            get => audio;
            set => this.SetValueAndNotify(ref audio, value, nameof(Audio));
        }

        /// <summary>
        /// 音视频合并参数
        /// </summary>
        public CombineParameters Combine
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
        public StreamParameters Stream
        {
            get => stream;
            set => this.SetValueAndNotify(ref stream, value, nameof(Stream));
        }

        /// <summary>
        /// 视频参数
        /// </summary>
        public VideoCodecParameters Video
        {
            get => video;
            set => this.SetValueAndNotify(ref video, value, nameof(Video));
        }

        public ProcessedOperationParameters ProcessedOperationParameters { get; set; } = new ProcessedOperationParameters();
    }
}