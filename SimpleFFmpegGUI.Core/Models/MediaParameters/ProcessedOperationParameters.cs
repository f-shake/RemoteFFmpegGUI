using CommunityToolkit.Mvvm.ComponentModel;

namespace SimpleFFmpegGUI.Models.MediaParameters;

public partial class ProcessedOperationParameters : ObservableObject
{
    /// <summary>
    /// 将输出文件的修改时间设置为最后一个输入文件的修改时间
    /// </summary>
    [ObservableProperty]
    private bool syncModifiedTime;

    /// <summary>
    /// 处理后删除输入文件。若可以，将删除到回收站。
    /// </summary>
    [ObservableProperty]
    private bool deleteInputFiles;
}