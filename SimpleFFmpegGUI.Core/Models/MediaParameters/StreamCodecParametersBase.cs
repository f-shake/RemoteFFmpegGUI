using CommunityToolkit.Mvvm.ComponentModel;
using SimpleFFmpegGUI.Enums;

namespace SimpleFFmpegGUI.Models.MediaParameters;

public abstract partial class StreamCodecParametersBase : ObservableObject
{
    /// <summary>
    /// 处理策略
    /// </summary>
    [ObservableProperty]
    private StreamStrategy strategy = StreamStrategy.Transcode;

    /// <summary>
    /// 编码
    /// </summary>
    [ObservableProperty]
    private string codec = "";
}