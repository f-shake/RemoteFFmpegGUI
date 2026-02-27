using CommunityToolkit.Mvvm.ComponentModel;
using SimpleFFmpegGUI.Enums;

namespace SimpleFFmpegGUI.Models.MediaParameters;

public partial class StreamMapParameters : ObservableObject
{
    /// <summary>
    /// 输入文件的序号
    /// </summary>
    [ObservableProperty]
    private int inputIndex;

    /// <summary>
    /// 指定的通道 (如 Video, Audio, Subtitle)
    /// </summary>
    [ObservableProperty]
    private StreamChannel channel;

    /// <summary>
    /// 在指定文件（和通道）中，选取的流的序号。
    /// 若为 null，通常表示选取该通道下的所有流或由 FFmpeg 自动选择。
    /// </summary>
    [ObservableProperty]
    private int? streamIndex;
}