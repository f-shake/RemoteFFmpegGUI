using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Services;
using System;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class PowerController(PowerService power) : FFmpegControllerBase
    {
        [HttpGet("Cpu")]
        public async Task<ActionResult<CpuCoreUsageDto[]>> GetCpuCoreUsage()
        {
            return await PowerService.GetCpuUsageAsync(TimeSpan.FromSeconds(0.1));
        }

        [HttpGet("ShutdownQueue")]
        public ActionResult<bool> IsShutdownAfterQueueFinished()
        {
            return power.ShutdownAfterQueueFinished;
        }

        [HttpPost("ShutdownQueue")]
        public IActionResult SetShutdownAfterQueueFinished([FromForm] bool on)
        {
            power.ShutdownAfterQueueFinished = on;
            return NoContent();
        }

        [HttpPost("Shutdown")]
        public IActionResult Shutdown()
        {
            power.Shutdown();
            return NoContent();
        }

        [HttpPost("AbortShutdown")]
        public IActionResult AbortShutdown()
        {
            power.AbortShutdown();
            return NoContent();
        }
    }
}