using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleFFmpegGUI.Models.MediaParameters
{
    public partial class InputParameters : ObservableObject
    {
        /// <summary>
        /// 输入文件的路径
        /// </summary>
        [ObservableProperty]
        private string? filePath;

        /// <summary>
        /// 开始时间
        /// </summary>
        [ObservableProperty]
        private TimeSpan? from;

        /// <summary>
        /// 结束时间
        /// </summary>
        [ObservableProperty]
        private TimeSpan? to;

        /// <summary>
        /// 持续时间
        /// </summary>
        [ObservableProperty]
        private TimeSpan? duration;

        /// <summary>
        /// 输入格式
        /// </summary>
        [ObservableProperty]
        private string? format;

        /// <summary>
        /// 输入帧率（主要针对图像序列）
        /// </summary>
        [ObservableProperty]
        private double? framerate;

        /// <summary>
        /// 输入是否为图像帧序列
        /// </summary>
        [ObservableProperty]
        private bool image2;

        /// <summary>
        /// 其他参数
        /// </summary>
        [ObservableProperty]
        private string? extra;
    }
}