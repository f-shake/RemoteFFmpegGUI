using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WebAPI.Realtime;
using System;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class QueueController(QueueService queue, TaskRepository taskRepository, QueueStateProvider stateProvider)
        : FFmpegControllerBase()
    {
        [HttpGet]
        public ActionResult<StatusDto> GetStatus()
        {
            var status = queue.MainQueueManager == null ? new StatusDto() : queue.MainQueueManager.GetStatus();
            return status;
        }

        /// <summary>
        /// 前端实时通道断开后的轮询兜底接口：一次拿齐队列状态、是否有待处理任务、计划开始时间。
        /// <para>
        /// 上面的 <see cref="GetStatus"/>/<see cref="HasPendingAsync"/>/<see cref="GetScheduleTime"/>
        /// 全部保留不动：WPF 的远程模式、集成测试与旧版前端仍在使用它们。
        /// </para>
        /// </summary>
        [HttpGet("State")]
        public async Task<ActionResult<QueueStateDto>> GetStateAsync()
        {
            return await stateProvider.GetStateAsync();
        }

        [HttpGet("HasPending")]
        public async Task<bool> HasPendingAsync()
        {
            return await taskRepository.HasQueueTasksAsync();
        }

        [HttpGet("Schedule")]
        public ActionResult<DateTime?> GetScheduleTime()
        {
            return queue.GetQueueScheduleTime();
        }

        [HttpPost("Start")]
        public IActionResult Start()
        {
            queue.StartQueue();
            return NoContent();
        }

        [HttpPost("Pause")]
        public IActionResult Pause()
        {
            queue.SuspendMainQueue();
            return NoContent();
        }

        [HttpPost("Resume")]
        public IActionResult Resume()
        {
            queue.ResumeMainQueue();
            return NoContent();
        }

        [HttpPost("Cancel")]
        public async Task<IActionResult> CancelAsync()
        {
            await queue.CancelAsync();
            return NoContent();
        }

        [HttpPost("Schedule")]
        public IActionResult SetSchedule(ScheduleRequest req)
        {
            if (req == null)
            {
                return BadRequest("请求对象不能为空");
            }
            if (req.Time <= DateTime.Now)
            {
                return BadRequest("计划的时间早于当前时间");
            }

            queue.ScheduleQueue(req.Time);
            return NoContent();
        }

        [HttpPost("CancelSchedule")]
        public IActionResult CancelSchedule()
        {
            queue.CancelQueueSchedule();
            return NoContent();
        }
    }
}