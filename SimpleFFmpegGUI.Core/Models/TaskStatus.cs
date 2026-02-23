using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public enum TaskStatus
    {
        [Description("排队中")]
        Queue = 1,

        [Description("处理中")]
        Processing = 2,

        [Description("已完成")]
        Done = 3,

        [Description("发生错误")]
        Error = 4,

        [Description("取消")]
        Cancel = 5
    }
}