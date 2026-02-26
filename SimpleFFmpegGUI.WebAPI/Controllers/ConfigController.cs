using Microsoft.AspNetCore.Mvc;
using SimpleFFmpegGUI.Services;

namespace SimpleFFmpegGUI.WebAPI.Controllers;

public class ConfigController(ConfigService config) : FFmpegControllerBase
{
    [HttpGet("DefaultProcessPriority")]
    public ActionResult<int> GetDefaultProcessPriority()
    {
        return config.DefaultProcessPriority;
    }

    [HttpPost("DefaultProcessPriority")]
    public IActionResult SetDefaultProcessPriority(int priority)
    {
        config.DefaultProcessPriority = priority;
        return NoContent();
    }
}