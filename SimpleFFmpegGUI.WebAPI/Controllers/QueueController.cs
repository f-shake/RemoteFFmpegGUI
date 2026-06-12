using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Services;
using System;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class QueueController(QueueService queue) : FFmpegControllerBase()
    {
        [HttpGet]
        public ActionResult<StatusDto> GetStatus()
        {
            var status = queue.MainQueueManager == null ? new StatusDto() : queue.MainQueueManager.GetStatus();
            return status;
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