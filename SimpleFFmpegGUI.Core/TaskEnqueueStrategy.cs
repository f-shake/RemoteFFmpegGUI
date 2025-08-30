using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI
{
    public enum TaskEnqueueStrategy
    {
        /// <summary>
        /// 仅加入队列
        /// </summary>
        EnqueueOnly,
        /// <summary>
        /// 加入并开始队列
        /// </summary>
        EnqueueAndRun,
        /// <summary>
        /// 加入队列并独立执行
        /// </summary>
        RunIndependently
    }
}
