namespace SimpleFFmpegGUI.Events
{
    /// <summary>
    /// 任务/队列状态变化的种类，供订阅方（WebAPI 的实时推送服务）决定推送什么内容。
    /// </summary>
    public enum TaskChangeKind
    {
        /// <summary>
        /// 任务清单发生了变化：新增、取消、删除、重置、被队列置为"进行中"、执行结束落库。
        /// </summary>
        Tasks,

        /// <summary>
        /// 主队列开始运行（每轮队列开始时一次）。
        /// </summary>
        QueueStarted,

        /// <summary>
        /// 主队列运行结束（正常跑完、被取消、异常退出都算）。
        /// </summary>
        QueueFinished,

        /// <summary>
        /// 计划的开始时间被设置、取消，或在队列开跑时被清空。
        /// </summary>
        Schedule,
    }
}
