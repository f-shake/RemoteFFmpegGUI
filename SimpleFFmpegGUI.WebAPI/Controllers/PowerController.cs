using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Services;
using System;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class PowerController(IConfiguration config, DbConfigService dbConfig, PowerService power) : FFmpegControllerBase(config)
    {
        [HttpPost]
        [Route("AbortShutdown")]
        public void AbortShutdown()
        {
            power.AbortShutdown();
        }

        [HttpGet]
        [Route("CpuCoreUsage")]
        public Task<CpuCoreUsageDto[]> GetCpuCoreUsage()
        {
            return PowerService.GetCpuUsageAsync(TimeSpan.FromSeconds(0.1));
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
            return power.ShutdownAfterQueueFinished;
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
            power.ShutdownAfterQueueFinished = true;
        }

        [HttpPost]
        [Route("Shutdown")]
        public async Task Shutdown()
        {
            power.Shutdown();
        }
    }
}