using FzLib;
using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public class AudioCodeArguments : IAudioCodeArguments
    {
        /// <summary>
        /// 码率
        /// </summary>
        public int? Bitrate { get; set; }

        /// <summary>
        /// 编码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 采样率
        /// </summary>
        public int? SamplingRate { get; set; }
    }
}