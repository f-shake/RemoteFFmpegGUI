using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleFFmpegGUI.Models.MediaParameters
{
    public partial class AudioCodecParameters : StreamCodecParametersBase
    {
        /// <summary>
        /// 码率
        /// </summary>
        [ObservableProperty]
        private int? bitrate;

        /// <summary>
        /// 采样率
        /// </summary>
        [ObservableProperty]
        private int? samplingRate;
    }
}