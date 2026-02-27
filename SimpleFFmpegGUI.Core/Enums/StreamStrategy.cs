namespace SimpleFFmpegGUI.Enums;

public enum StreamStrategy
{
    /// <summary>
    /// 转码：根据设定的参数重新编码
    /// </summary>
    Transcode = 0,

    /// <summary>
    /// 复制：不经过重新编码，直接拷贝原始流
    /// </summary>
    Copy = 1,

    /// <summary>
    /// 禁用：不导出该流
    /// </summary>
    Disable = 2
}