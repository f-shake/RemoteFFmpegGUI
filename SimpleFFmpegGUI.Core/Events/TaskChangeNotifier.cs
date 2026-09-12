using System;

namespace SimpleFFmpegGUI.Events
{
    /// <summary>
    /// <see cref="ITaskChangeNotifier"/> 的默认实现（无状态，注册为单例）。
    /// </summary>
    public sealed class TaskChangeNotifier : ITaskChangeNotifier
    {
        public event EventHandler<TaskChangeEventArgs> Changed;

        public void Notify(TaskChangeKind kind)
        {
            // 先取快照再遍历：订阅方可能在处理过程中退订，直接遍历字段会漏掉/抛异常
            EventHandler<TaskChangeEventArgs> handler = Changed;
            if (handler == null)
            {
                return;
            }

            // 逐个调用：Delegate.GetInvocationList 拿到的是调用瞬间的订阅者列表
            TaskChangeEventArgs args = new TaskChangeEventArgs(kind);
            foreach (EventHandler<TaskChangeEventArgs> subscriber in handler.GetInvocationList())
            {
                try
                {
                    subscriber(this, args);
                }
                catch (Exception)
                {
                    // 有意吞掉：通知发生在任务增删改、队列执行等流程中途，订阅方的推送失败绝不能把这些
                    // 流程带崩。订阅方自己负责记录内部错误（RealtimeBroadcaster 的每次广播都有 try/catch + 日志）
                }
            }
        }
    }
}
