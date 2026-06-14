using CommunityToolkit.Mvvm.ComponentModel;
using FzLib;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.WPF.ViewModels;
using System.ComponentModel;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class AudioArgumentsViewModel : ViewModelBase, IArgumentVideModel
    {
        [ObservableProperty]
        private int? bitrate;

        [ObservableProperty]
        private string code;

        [ObservableProperty]
        private bool enableBitrate;

        [ObservableProperty]
        private bool enableSamplingRate;

        [ObservableProperty]
        private int? samplingRate;

        public AudioArgumentsViewModel()
        {
            Bitrate = 128;
            SamplingRate = 48000;
            Code = "自动";
        }
        public void Apply()
        {
            Bitrate = EnableBitrate ? Bitrate : null;
            SamplingRate = EnableSamplingRate ? SamplingRate : null;
        }

        public void Update()
        {
            EnableBitrate = Bitrate.HasValue;
            EnableSamplingRate = SamplingRate.HasValue;
        }
    }
}