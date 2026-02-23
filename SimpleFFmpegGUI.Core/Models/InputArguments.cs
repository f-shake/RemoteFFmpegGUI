using FzLib;
using System;
using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public class InputArguments : IInputArguments
    {
        /// <summary>
        /// 持续时间
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// 其他参数
        /// </summary>
        public string Extra { get; set; }

        /// <summary>
        /// 输入文件的路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 输入格式
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// 输入帧率（主要针对图像序列）
        /// </summary>
        public double? Framerate { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public TimeSpan? From { get; set; }
        /// <summary>
        /// 输入是否为图像帧序列
        /// </summary>
        public bool Image2 { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public TimeSpan? To { get; set; }
    }
}