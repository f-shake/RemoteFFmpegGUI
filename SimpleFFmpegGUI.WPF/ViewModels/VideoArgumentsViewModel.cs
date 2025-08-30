using CommunityToolkit.Mvvm.ComponentModel;
using FzLib;
using Newtonsoft.Json.Linq;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.WPF.ViewModels;
using System.ComponentModel;
using System.Linq;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class VideoArgumentsViewModel : ViewModelBase,IVideoCodeArguments, IArgumentVideModel
    {
        [ObservableProperty]
        private string aspectRatio;

        [ObservableProperty]
        private double? averageBitrate;

        [ObservableProperty]
        private string code;

        [ObservableProperty]
        private int? crf;

        [ObservableProperty]
        private bool enableAspectRatio;

        [ObservableProperty]
        private bool enableAverageBitrate;

        [ObservableProperty]
        private bool enableCrf;

        [ObservableProperty]
        private bool enableFps;

        [ObservableProperty]
        private bool enableMaxBitrate;

        [ObservableProperty]
        private bool enablePixelFormat;

        [ObservableProperty]
        private bool enableSize;

        [ObservableProperty]
        private double? fps;

        [ObservableProperty]
        private double? maxBitrate;

        [ObservableProperty]
        private double? maxBitrateBuffer;

        [ObservableProperty]
        private int maxCRF;

        [ObservableProperty]
        private int maxPreset;

        [ObservableProperty]
        private string pixelFormat;

        [ObservableProperty]
        private int preset;

        [ObservableProperty]
        private string size;

        [ObservableProperty]
        private bool twoPass;

        [ObservableProperty]
        private string crfLabel;

        public VideoArgumentsViewModel()
        {
            Code = "H265";
            Preset = 2;
            Crf = 25;
            AverageBitrate = 10;
            MaxBitrate = 20;
            MaxBitrateBuffer = 2;
            VideoArgumentsWithSwitch_PropertyChanged(this, new PropertyChangedEventArgs(nameof(Code)));
            PropertyChanged += VideoArgumentsWithSwitch_PropertyChanged;
        }

        public void Apply()
        {
            Crf = EnableCrf ? Crf : null;
            Fps = EnableFps ? Fps : null;
            AverageBitrate = EnableAverageBitrate ? AverageBitrate : null;
            MaxBitrate = EnableMaxBitrate ? MaxBitrate : null;
            Size = EnableSize ? Size : null;
            PixelFormat = EnablePixelFormat ? PixelFormat : null;
            AspectRatio = EnableAspectRatio ? AspectRatio : null;
        }

        public void Update()
        {
            EnableCrf = Crf.HasValue;
            EnableFps = Fps.HasValue;
            EnableAverageBitrate = AverageBitrate.HasValue;
            EnableMaxBitrate = MaxBitrate.HasValue;
            EnableSize = !string.IsNullOrEmpty(Size);
            EnablePixelFormat = !string.IsNullOrEmpty(PixelFormat);
            EnableAspectRatio = !string.IsNullOrEmpty(AspectRatio);
            //从数据库提取的Argument，若某些开关没有启动，则其值为null，这不符合UI，因此需要对null值进行赋初始值。
            Crf ??= 25;
            MaxBitrate ??= 20;
            AverageBitrate = AverageBitrate ?? 10;
            MaxBitrateBuffer ??= 2;
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            switch (e.PropertyName)
            {
                case nameof(EnableCrf):
                    if (EnableCrf && Crf == null)
                    {
                        Crf = 25;
                    }
                    break;
                case nameof(EnableSize):
                    if (EnableSize && string.IsNullOrWhiteSpace(Size))
                    {
                        Size = "-1:1080";
                    }
                    break;
                case nameof(EnableAspectRatio):
                    if (EnableAspectRatio && string.IsNullOrWhiteSpace(AspectRatio))
                    {
                        AspectRatio = "16:9";
                    }
                    break;
                case nameof(EnableFps):
                    if (EnableFps && Fps == null)
                    {
                        Fps = 30;
                    }
                    break;
                case nameof(EnableAverageBitrate):
                    if (EnableAverageBitrate && AverageBitrate == null)
                    {
                        AverageBitrate = 10;
                    }
                    break;
                case nameof(EnableMaxBitrate):
                    if (EnableMaxBitrate && MaxBitrate == null)
                    {
                        MaxBitrate = 20;
                    }
                    break;
                case nameof(EnablePixelFormat):
                    if (EnablePixelFormat && string.IsNullOrWhiteSpace(PixelFormat))
                    {
                        PixelFormat = "yuv420p";
                    }
                    break;
            }
        }

        private void VideoArgumentsWithSwitch_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TwoPass) when TwoPass:
                    EnableCrf = false;
                    EnableAverageBitrate = true;
                    break;
                case nameof(EnableCrf) when EnableCrf:
                    TwoPass = false;
                    EnableAverageBitrate = false;
                    break;
                case nameof(Code):
                    var codec = VideoCodec.GetCodec(Code) ?? VideoCodec.General;
                    MaxPreset = codec.MaxSpeedLevel;
                    Preset = codec.DefaultSpeedLevel;
                    MaxCRF = codec.MaxCRF;
                    Crf = codec.DefaultCRF;
                    CrfLabel = codec.CrfLabel;
                    break;
            }
        }
    }
}