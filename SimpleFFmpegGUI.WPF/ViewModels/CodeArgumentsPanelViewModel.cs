using CommunityToolkit.Mvvm.ComponentModel;
using FzLib;
using FzLib.Collection;
using Mapster;
using Newtonsoft.Json.Linq;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WPF.ViewModels;
using SimpleFFmpegGUI.WPF.Panels;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class CodeArgumentsPanelViewModel : ViewModelBase
    {
        private readonly PresetRepository presetRepository;
        private readonly PresetService presetService;

        private AudioArgumentsViewModel audio = new AudioArgumentsViewModel();

        [ObservableProperty]
        private StreamStrategy audioOutputStrategy = StreamStrategy.Copy;

        [ObservableProperty]
        private bool canApplyDefaultPreset = true;

        [ObservableProperty]
        private bool canSetMux;

        [ObservableProperty]
        private bool canSetConcat;

        [ObservableProperty]
        private bool canSetVideoAndAudio;

        [ObservableProperty]
        private bool canSpecifyFormat;

        [ObservableProperty]
        private MuxParameters mux = new MuxParameters();

        [ObservableProperty]
        private string extra;

        [ObservableProperty]
        private FormatArgumentViewModel format = new FormatArgumentViewModel();

        [ObservableProperty]
        private ProcessedOperationParameters processedOptions = new ProcessedOperationParameters();

        [ObservableProperty]
        private VideoArgumentsViewModel video = new VideoArgumentsViewModel();

        [ObservableProperty]
        private StreamStrategy videoOutputStrategy = StreamStrategy.Copy;

        public CodeArgumentsPanelViewModel(PresetRepository presetRepository, PresetService presetService)
        {
            this.presetRepository = presetRepository;
            this.presetService = presetService;
        }

        public OutputParameters Arguments { get; set; }

        public IEnumerable AspectRatios { get; } = new[] { "16:9", "4:3", "1:1", "3:4", "16:9", "2.35" };

        public AudioArgumentsViewModel Audio
        {
            get => audio;
            set => this.SetValueAndNotify(ref audio, value, nameof(Audio));
        }

        public IEnumerable AudioBitrates { get; } = new[] { 32, 64, 96, 128, 192, 256, 320 };

        public IEnumerable AudioCodecs { get; } = new[] { "自动", "AAC", "OPUS" };

        public IEnumerable AudioSamplingRates { get; } = new[] { 8000, 16000, 32000, 44100, 48000, 96000, 192000 };

        public IEnumerable ChannelOutputStrategies => Enum.GetValues(typeof(StreamStrategy));

        public IEnumerable Formats => VideoFormat.Formats;

        public IEnumerable Fpses => new double[] { 10, 20, 23.976, 24, 25, 29.97, 30, 48, 59.94, 60, 120 };

        public IEnumerable PixelFormats { get; } = new[] { "yuv420p", "yuvj420p", "yuv422p", "yuvj422p", "rgb24", "gray", "yuv420p10le" };

        public IEnumerable Sizes { get; } = new[] { "-1:2160", "-1:1440", "-1:1080", "-1:720", "-1:576", "-1:480" };

        public IEnumerable VideoCodecs { get; } = new[] { "自动" }.Concat(VideoCodec.VideoCodecs.Select(p => p.Name));

        public OutputParameters GetArguments()
        {
            if (VideoOutputStrategy == StreamStrategy.Transcode)
            {
                Video.Apply();
            }
            if (AudioOutputStrategy == StreamStrategy.Transcode)
            {
                Audio.Apply();
            }
            Format.Apply();

            var videoParams = new VideoCodecParameters();
            videoParams.Strategy = VideoOutputStrategy;
            if (VideoOutputStrategy == StreamStrategy.Transcode)
            {
                Video.Adapt(videoParams);
                videoParams.Codec = Video.Code; // Code → Codec，Mapster 按名字映射对不上
            }

            var audioParams = new AudioCodecParameters();
            audioParams.Strategy = AudioOutputStrategy;
            if (AudioOutputStrategy == StreamStrategy.Transcode)
            {
                Audio.Adapt(audioParams);
                audioParams.Codec = Audio.Code; // Code → Codec，Mapster 按名字映射对不上
            }

            return new OutputParameters()
            {
                Video = videoParams,
                Audio = audioParams,
                Format = Format.Format,
                Mux = Mux,
                Extra = Extra,
                ProcessedOperationParameters = ProcessedOptions,
            };
        }

        public void Update(TaskType type, OutputParameters argument = null)
        {
            CanSpecifyFormat = type is TaskType.Transcode or TaskType.Mux or TaskType.Concat;
            CanSetVideoAndAudio = type is TaskType.Transcode;
            CanSetMux = type is TaskType.Mux;
            CanSetConcat = type is TaskType.Concat;
            if (argument != null)
            {
                Video = argument.Video.Adapt<VideoArgumentsViewModel>();
                Video.Code = argument.Video.Codec;
                Video?.Update();
                VideoOutputStrategy = argument.Video.Strategy;
                Audio = argument.Audio.Adapt<AudioArgumentsViewModel>();
                Audio.Code = argument.Audio.Codec;
                Audio?.Update();
                AudioOutputStrategy = argument.Audio.Strategy;
                Format = new FormatArgumentViewModel() { Format = argument.Format };
                Format.Update();
                Mux = argument.Mux ?? new MuxParameters();
                ProcessedOptions = argument.ProcessedOperationParameters ?? new ProcessedOperationParameters();
                Extra = argument.Extra;
            }
        }

        public async Task UpdateTypeAsync(TaskType type)
        {
            bool updated = false;
            if (CanApplyDefaultPreset)//允许修改参数
            {
                if (Config.Instance.RememberLastArguments)//记住上次输出参数
                {
                    if (Config.Instance.LastOutputArguments.GetOrDefault(type) is OutputParameters lastArguments)
                    {
                        Update(type, lastArguments);
                        //(await this.CreateMessageAsync()).QueueSuccess($"已加载上次任务的参数");
                        updated = true;
                    }
                }
                if (!updated)//记住上次输出参数为False，或不存在上次的参数
                {
                    var defaultPreset = await presetRepository.GetDefaultByTypeAsync(type);
                    if (defaultPreset != null)
                    {
                        Update(type, defaultPreset.Parameters);
                        QueueSuccessMessage($"已加载默认预设“{defaultPreset.Name}”");
                        updated = true;
                    }
                }
            }
            if (!updated)
            {
                Update(type);
            }
        }

        partial void OnAudioOutputStrategyChanged(StreamStrategy value)
        {
            if (value == StreamStrategy.Transcode && Audio == null)
            {
                Audio = new AudioArgumentsViewModel();
            }
        }

        partial void OnCanSetVideoAndAudioChanged(bool value)
        {
            if (CanSetVideoAndAudio)
            {
                VideoOutputStrategy = StreamStrategy.Transcode;
                AudioOutputStrategy = StreamStrategy.Transcode;
            }
            else
            {
                VideoOutputStrategy = StreamStrategy.Disable;
                AudioOutputStrategy = StreamStrategy.Disable;
            }
        }

        partial void OnVideoOutputStrategyChanged(StreamStrategy value)
        {
            if (value == StreamStrategy.Transcode && Video == null)
            {
                Video = new VideoArgumentsViewModel();
            }
        }
    }
}