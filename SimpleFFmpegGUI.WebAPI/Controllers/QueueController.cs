using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using System;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class QueueController : FFmpegControllerBase
    {
        public QueueController(ILogger<MediaInfoController> Logger,
            IConfiguration config,
        PipeClient pipeClient) : base(Logger, config, pipeClient) { }

        [HttpPost]
        [Route("Cancel")]
        public async Task CancelAsync()
        {
            await pipeClient.InvokeAsync(p => p.CancelQueue());
        }

        [HttpPost]
        [Route("CancelSchedule")]
        public async Task CancelScheduleAsync()
        {
            await pipeClient.InvokeAsync(p => p.CancelQueueSchedule());
        }

        [HttpGet]
        [Route("QueueScheduleTime")]
        public async Task<DateTime?> GetQueueScheduleTime()
        {
            return await pipeClient.InvokeAsync(p => p.GetQueueScheduleTime());
        }

        [HttpGet]
        [Route("Status")]
        public async Task<StatusDto> GetStatus()
        {
            var status = await pipeClient.InvokeAsync(p => p.GetStatus());
            HideAbsolutePath(status.Task);
            return status;
        }

        [HttpPost]
        [Route("Pause")]
        public async Task PauseAsync()
        {
            await pipeClient.InvokeAsync(p => p.PauseQueue());
        }

        [HttpPost]
        [Route("Resume")]
        public async Task ResumeAsync()
        {
            await pipeClient.InvokeAsync(p => p.ResumeQueue());
        }

        [HttpPost]
        [Route("Schedule")]
        public async Task ScheduleAsync(DateTime time)
        {
            if (time <= DateTime.Now)
            {
                throw new ArgumentException("计划的时间早于当前时间");
            }
            await pipeClient.InvokeAsync(p => p.ScheduleQueue(time));
        }

        [HttpPost]
        [Route("Start")]
        public async Task StartAsync()
        {
            await pipeClient.InvokeAsync(p => p.StartQueue());
        }
    }
}