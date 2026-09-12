using System;

namespace SimpleFFmpegGUI.Dto
{
    /// <summary>
    /// HTTP 兜底（<c>GET api/Queue/State</c>）用的合并状态：一次请求拿齐前端需要的三样东西，
    /// 替代原来的 <c>GET api/Queue</c> + <c>GET api/Queue/HasPending</c> + <c>GET api/Queue/Schedule</c> 三个请求。
    /// </summary>
    public class QueueStateDto
    {
        /// <summary>
        /// 队列状态。与推送的 <c>queueStatus</c> 载荷是同一个类型，前端只有一套解析逻辑。
        /// </summary>
        public QueueStatusPushDto Status { get; set; }

        /// <summary>是否有等待处理的任务</summary>
        public bool HasPending { get; set; }

        /// <summary>计划的开始时间（null 表示没有计划）</summary>
        public DateTime? ScheduleTime { get; set; }
    }
}
