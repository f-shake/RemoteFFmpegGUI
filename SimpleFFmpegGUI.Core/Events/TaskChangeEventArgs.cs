using System;

namespace SimpleFFmpegGUI.Events
{
    /// <summary>
    /// 任务/队列状态变化事件参数。
    /// </summary>
    public class TaskChangeEventArgs : EventArgs
    {
        public TaskChangeEventArgs(TaskChangeKind kind)
        {
            Kind = kind;
        }

        /// <summary>
        /// 变化的种类。
        /// </summary>
        public TaskChangeKind Kind { get; }
    }
}
