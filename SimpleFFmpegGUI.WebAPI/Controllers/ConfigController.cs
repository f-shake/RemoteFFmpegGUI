using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SimpleFFmpegGUI.Services;

namespace SimpleFFmpegGUI.WebAPI.Controllers;

public class ConfigController(ConfigService config) : FFmpegControllerBase
{
    [HttpGet("ProcessPriority")]
    public ActionResult<int> GetDefaultProcessPriority()
    {
        return config.DefaultProcessPriority;
    }

    [HttpPost("ProcessPriority")]
    public async Task<IActionResult> SetDefaultProcessPriority(int priority)
    {
        if (priority < 0 || priority > 5)
        {
            return BadRequest("优先级必须在0-5之间");
        }
        config.DefaultProcessPriority = priority;
        await config.SaveAsync();
        return NoContent();
    }

    [HttpGet("SnapshotSize")]
    public ActionResult<string> GetSnapshotSize()
    {
        // 用 JsonResult 强制 JSON 序列化，避免 string 被当作纯文本返回
        return new JsonResult(config.SnapshotSize);
    }

    [HttpPost("SnapshotSize")]
    public async Task<IActionResult> SetSnapshotSize(string snapshotSize)
    {
        if (string.IsNullOrWhiteSpace(snapshotSize))
        {
            return BadRequest("快照尺寸不能为空");
        }
        // 校验格式（允许 -1:1080 这类按比例缩放的负值），避免非法值持久化后拼入 ffmpeg scale 参数
        if (!System.Text.RegularExpressions.Regex.IsMatch(snapshotSize, @"^-?\d+:-?\d+$"))
        {
            return BadRequest("快照尺寸格式应为「宽度:高度」，例如 1920:1080 或 -1:1080");
        }
        config.SnapshotSize = snapshotSize;
        await config.SaveAsync();
        return NoContent();
    }
}