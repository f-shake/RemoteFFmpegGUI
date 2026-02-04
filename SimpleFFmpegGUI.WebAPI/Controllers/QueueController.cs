using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Services;
using System;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class QueueController(IConfiguration config, QueueService queue) : FFmpegControllerBase(config)
    {
        [HttpPost]
        [Route("Cancel")]
        public async Task CancelAsync()
        {
            await queue.CancelAsync();
        }

        [HttpPost]
        [Route("CancelSchedule")]
        public void CancelSchedule()
        {
            queue.CancelQueueSchedule();
        }

        [HttpGet]
        [Route("QueueScheduleTime")]
        public DateTime? GetQueueScheduleTime()
        {
            return queue.GetQueueScheduleTime();
        }

        [HttpGet]
        [Route("Status")]
        public StatusDto GetMainQueueStatus()
        {
            var status = queue.MainQueueManager == null ? new StatusDto() : queue.MainQueueManager.GetStatus();
            HideAbsolutePath(status.Task);
            return status;
        }

        [HttpPost]
        [Route("Pause")]
        public void PauseMainQueue()
        {
            queue.SuspendMainQueue();
        }

        [HttpPost]
        [Route("Resume")]
        public void Resume()
        {
            queue.ResumeMainQueue();
        }

        [HttpPost]
        [Route("Schedule")]
        public void Schedule(DateTime time)
        {
            if (time <= DateTime.Now)
            {
                throw new ArgumentException("计划的时间早于当前时间");
            }
            queue.ScheduleQueue(time);
        }

        [HttpPost]
        [Route("Start")]
        public void Start()
        {
          queue.StartQueue();
        }
    }
}