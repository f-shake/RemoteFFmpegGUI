using SimpleFFmpegGUI.Attributes;
using SimpleFFmpegGUI.Models;

namespace SimpleFFmpegGUI.Enums
{
    public enum TaskType
    {
        [NameDescription("转码", "将视频重新编码")]
        Transcode = 0,

        [NameDescription("合并视音频", "将视频和音频合并为一个文件")]
        Mux = 1,

        [NameDescription("视频比较", "比较两个视频之间的一致性")]
        QualityCheck = 2,

        [NameDescription("视频拼接", "将多个视频首尾相连生成一个视频")]
        Concat = 4,

        [NameDescription("自定义参数", "完全自定义参数")]
        Custom = 99,
    }
}