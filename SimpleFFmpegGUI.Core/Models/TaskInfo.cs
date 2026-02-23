using System;
using System.Collections.Generic;

namespace SimpleFFmpegGUI.Model
{
    /// <summary>
    /// FFmpeg任务
    /// </summary>
    public class TaskInfo : ModelBase
    {
        public TaskInfo()
        {
            CreateTime = DateTime.Now;
            Status = TaskStatus.Queue;
        }

        /// <summary>
        /// 任务类型
        /// </summary>
        public TaskType Type { get; set; }

        /// <summary>
        /// 当前任务状态
        /// </summary>
        public TaskStatus Status { get; set; }

        /// <summary>
        /// 输入文件和参数
        /// </summary>
        public List<InputArguments> Inputs { get; set; }

        /// <summary>
        /// 指定的输出路径
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// 实际的输出路径
        /// </summary>
        public string RealOutput { get; set; }

        /// <summary>
        /// 输出参数
        /// </summary>
        public OutputArguments Arguments { get; set; }

        /// <summary>
        /// 任务创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? FinishTime { get; set; }

        /// <summary>
        /// 相关信息，包括错误信息、执行结果等
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 执行时FFmpeg的执行参数
        /// </summary>
        public string FFmpegArguments { get; set; }
    }
}