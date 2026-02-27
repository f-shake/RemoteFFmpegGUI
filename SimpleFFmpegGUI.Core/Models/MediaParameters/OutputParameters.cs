using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FzLib.Programming;
using SimpleFFmpegGUI.Enums;

namespace SimpleFFmpegGUI.Models.MediaParameters
{
    public partial class OutputParameters : ObservableObject
    {
        [ObservableProperty]
        private AudioCodecParameters audio = new AudioCodecParameters();

        [ObservableProperty]
        private MuxParameters mux = new MuxParameters();

        [ObservableProperty]
        private string? extra;

        [ObservableProperty]
        private string? format;

        [ObservableProperty]
        private StreamParameters stream = new StreamParameters();

        [ObservableProperty]
        private VideoCodecParameters video = new VideoCodecParameters();

        [ObservableProperty]
        private ProcessedOperationParameters processedOperationParameters = new ProcessedOperationParameters();
    }
}