using System;

namespace SimpleFFmpegGUI.Events
{
    /// <summary>
    /// 任务与队列状态变化的通知器。Core 内部在各状态变更点调用 <see cref="Notify"/>，宿主订阅
    /// <see cref="Changed"/> 后把变化推给客户端（WebAPI 的 SignalR 广播服务即为此用途），
    /// 使前端不必再定时轮询。
    /// <para>
    /// 必须注册为<b>单例</b>：发通知的 TaskService/TaskRepository 是瞬时服务，订阅必须挂在一个
    /// 长生命周期对象上，否则订阅会随请求对象一起消失。
    /// </para>
    /// <para>
    /// WPF 进程内直连 Core、本就事件驱动，不订阅它，因此行为不受影响。
    /// </para>
    /// </summary>
    public interface ITaskChangeNotifier
    {
        /// <summary>
        /// 状态发生了变化。可能在任意线程触发（请求线程、队列执行线程、定时器线程），订阅方需自行编组。
        /// </summary>
        event EventHandler<TaskChangeEventArgs> Changed;

        /// <summary>
        /// 通知一次变化。线程安全；单个订阅方抛出异常不会影响其它订阅方，也不会影响调用方
        /// （通知是在队列执行、请求处理等流程中间发出的，不能被推送问题带崩）。
        /// </summary>
        void Notify(TaskChangeKind kind);
    }
}
