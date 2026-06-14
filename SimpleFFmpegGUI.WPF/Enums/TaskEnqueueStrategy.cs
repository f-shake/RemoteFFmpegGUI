using System.ComponentModel;

namespace SimpleFFmpegGUI.WPF.Enums;

public enum TaskEnqueueStrategy
{
    [Description("加入队列")]
    EnqueueOnly,

    [Description("加入队列并开始")]
    EnqueueAndRun,

    [Description("独立执行")]
    RunIndependently,
}
