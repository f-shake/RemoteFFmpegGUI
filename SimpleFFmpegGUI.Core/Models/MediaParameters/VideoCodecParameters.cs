using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleFFmpegGUI.Models.MediaParameters;

public partial class VideoCodecParameters : StreamCodecParametersBase
{
    /// <summary>
    /// 画面比例
    /// </summary>
    [ObservableProperty]
    private string? aspectRatio;

    /// <summary>
    /// 平均码率
    /// </summary>
    [ObservableProperty]
    private double? averageBitrate;

    /// <summary>
    /// CRF（视频目标质量）
    /// </summary>
    [ObservableProperty]
    private int? crf;

    /// <summary>
    /// 帧率
    /// </summary>
    [ObservableProperty]
    private double? fps;

    /// <summary>
    /// 最大码率
    /// </summary>
    [ObservableProperty]
    private double? maxBitrate;

    /// <summary>
    /// 最大码率缓冲倍率
    /// </summary>
    [ObservableProperty]
    private double? maxBitrateBuffer;

    /// <summary>
    /// 像素格式
    /// </summary>
    [ObservableProperty]
    private string? pixelFormat;

    /// <summary>
    /// 编码速度或速度预设
    /// </summary>
    [ObservableProperty]
    private int preset;

    /// <summary>
    /// 视频尺寸（分辨率）
    /// </summary>
    [ObservableProperty]
    private string? size;

    /// <summary>
    /// 是否二次编码
    /// </summary>
    [ObservableProperty]
    private bool twoPass;
}