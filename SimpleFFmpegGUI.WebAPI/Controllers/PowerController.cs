using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Manager;
using System;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class PowerController(IConfiguration config, QueueManager queue, ConfigManager dbConfig) : FFmpegControllerBase(config)
    {
        [HttpPost]
        [Route("AbortShutdown")]
        public void AbortShutdown()
        {
            queue.PowerManager.AbortShutdown();
        }

        [HttpGet]
        [Route("CpuCoreUsage")]
        public Task<CpuCoreUsageDto[]> GetCpuCoreUsage()
        {
            return PowerManager.GetCpuUsageAsync(TimeSpan.FromSeconds(0.1));
        }

        [HttpGet]
        [Route("DefaultProcessPriority")]
        public int GetDefaultProcessPriority()
        {
            return dbConfig.DefaultProcessPriority;
        }

        [HttpGet]
        [Route("ShutdownQueue")]
        public bool IsShutdownAfterQueueFinished()
        {
            return queue.PowerManager.ShutdownAfterQueueFinished;
        }

        [HttpPost]
        [Route("DefaultProcessPriority")]
        public void SetDefaultProcessPriority(int priority)
        {
            dbConfig.DefaultProcessPriority = priority;
        }

        [HttpPost]
        [Route("ShutdownQueue")]
        public void SetShutdownAfterQueueFinished([FromForm] bool on)
        {
            queue.PowerManager.ShutdownAfterQueueFinished = true;
        }

        [HttpPost]
        [Route("Shutdown")]
        public async Task Shutdown()
        {
            queue.PowerManager.Shutdown();
        }
    }
}