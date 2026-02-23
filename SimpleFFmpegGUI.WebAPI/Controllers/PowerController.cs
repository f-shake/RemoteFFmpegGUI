using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Services;
using System;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class PowerController(DbConfigService dbConfig, PowerService power)
        : FFmpegControllerBase
    {
        [HttpPost("AbortShutdown")]
        public IActionResult AbortShutdown()
        {
            power.AbortShutdown();
            return NoContent();
        }

        [HttpGet("CpuCoreUsage")]
        public async Task<ActionResult<CpuCoreUsageDto[]>> GetCpuCoreUsage()
        {
            return await PowerService.GetCpuUsageAsync(TimeSpan.FromSeconds(0.1));
        }

        [HttpGet("DefaultProcessPriority")]
        public ActionResult<int> GetDefaultProcessPriority()
        {
            return dbConfig.DefaultProcessPriority;
        }

        [HttpGet("ShutdownQueue")]
        public ActionResult<bool> IsShutdownAfterQueueFinished()
        {
            return power.ShutdownAfterQueueFinished;
        }

        [HttpPost("DefaultProcessPriority")]
        public IActionResult SetDefaultProcessPriority(int priority)
        {
            dbConfig.DefaultProcessPriority = priority;
            return NoContent();
        }

        [HttpPost("ShutdownQueue")]
        public IActionResult SetShutdownAfterQueueFinished([FromForm] bool on)
        {
            power.ShutdownAfterQueueFinished = true;
            return NoContent();
        }

        [HttpPost("Shutdown")]
        public IActionResult Shutdown()
        {
            power.Shutdown();
            return NoContent();
        }
    }
}