using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Services;
using System;
using System.Threading.Tasks;
using SimpleFFmpegGUI.WebAPI.Dto;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class QueueController(IConfiguration config, QueueService queue) : FFmpegControllerBase(config)
    {
        [HttpPost("Cancel")]
        public async Task<IActionResult> CancelAsync()
        {
            await queue.CancelAsync();
            return NoContent();
        }

        [HttpPost("CancelSchedule")]
        public IActionResult CancelSchedule()
        {
            queue.CancelQueueSchedule();
            return NoContent();
        }

        [HttpGet("QueueScheduleTime")]
        public ActionResult<DateTime?> GetQueueScheduleTime()
        {
            return queue.GetQueueScheduleTime();
        }

        [HttpGet("Status")]
        public ActionResult<StatusDto> GetMainQueueStatus()
        {
            var status = queue.MainQueueManager == null ? new StatusDto() : queue.MainQueueManager.GetStatus();
            return status;
        }

        [HttpPost("Pause")]
        public IActionResult PauseMainQueue()
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

        [HttpPost("Schedule")]
        public IActionResult Schedule(ScheduleRequest req)
        {
            if (req.Time <= DateTime.Now)
            {
                return BadRequest("计划的时间早于当前时间");
            }

            queue.ScheduleQueue(req.Time);
            return NoContent();
        }

        [HttpPost("Start")]
        public IActionResult Start()
        {
            queue.StartQueue();
            return NoContent();
        }
    }
}