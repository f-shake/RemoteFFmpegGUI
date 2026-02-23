using FzLib;
using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public class VideoCodeArguments :  IVideoCodeArguments
    {

        /// <summary>
        /// 画面比例
        /// </summary>
        public string AspectRatio{ get; set; }

        /// <summary>
        /// 平均码率
        /// </summary>
        public double? AverageBitrate { get; set; }

        /// <summary>
        /// 编码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// CRF（视频目标质量）
        /// </summary>
        public int? Crf { get; set; }

        /// <summary>
        /// 帧率
        /// </summary>
        public double? Fps { get; set; }

        /// <summary>
        /// 最大码率
        /// </summary>
        public double? MaxBitrate { get; set; }

        /// <summary>
        /// 最大码率缓冲倍率
        /// </summary>
        public double? MaxBitrateBuffer { get; set; }

        /// <summary>
        /// 像素格式
        /// </summary>
        public string PixelFormat { get; set; }

        /// <summary>
        /// 编码速度或速度预设
        /// </summary>
        public int Preset { get; set; }

        /// <summary>
        /// 视频尺寸（分辨率）
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// 是否二次编码
        /// </summary>
        public bool TwoPass { get; set; }
    }
}