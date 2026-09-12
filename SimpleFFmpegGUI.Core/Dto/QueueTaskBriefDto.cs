using System;
using SimpleFFmpegGUI.Models.Entities;
using TaskStatus = SimpleFFmpegGUI.Enums.TaskStatus;

namespace SimpleFFmpegGUI.Dto
{
    /// <summary>
    /// 实时推送用的任务摘要。
    /// <see cref="StatusDto.Task"/> 是完整实体（<c>Parameters</c> 约 27 个字段、<c>FFmpegArguments</c> 是整条命令行，
    /// null 字段还不省略，一条状态就有 1.5~3 KB），而前端的状态栏只用到这里的几个字段。
    /// </summary>
    public class QueueTaskBriefDto
    {
        public int Id { get; set; }

        /// <summary>任务状态（前端用它把进度条染红：Error 时）</summary>
        public TaskStatus Status { get; set; }

        /// <summary>输出文件（「详细进度」弹窗显示）</summary>
        public string Output { get; set; }

        /// <summary>输入文件（状态栏取第一个输入去截图）</summary>
        public System.Collections.Generic.List<QueueTaskInputBriefDto> Inputs { get; set; } = [];

        public static QueueTaskBriefDto From(TaskEntity task)
        {
            if (task == null)
            {
                return null;
            }

            var dto = new QueueTaskBriefDto
            {
                Id = task.Id,
                Status = task.Status,
                Output = task.Output,
            };
            if (task.Inputs != null)
            {
                foreach (var input in task.Inputs)
                {
                    dto.Inputs.Add(new QueueTaskInputBriefDto { FilePath = input.FilePath });
                }
            }

            return dto;
        }
    }
}
