using FzLib.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Models.MediaInfo;
using SimpleFFmpegGUI.Services;
using System;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Helpers;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class MediaInfoController(
        MediaInfoService mediaInfoService,
        FilePathHelper filePathHelper) : FFmpegControllerBase
    {
        [HttpGet("{name}")]
        public async Task<ActionResult<MediaInfoGeneral>> GetAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return BadRequest("文件名不能为空");
            }

            name = filePathHelper.GetFullPath(RootDirType.InputDir, name);
            if (!System.IO.File.Exists(name))
            {
                return NotFound();
            }

            var result = await mediaInfoService.GetMediaInfoAsync(name);
            return result;
        }

        [HttpGet]
        [Route("Snapshot")]
        public async Task<IActionResult> GetSnapshotAsync(string videoPath, double seconds)
        {
            if (string.IsNullOrEmpty(videoPath))
            {
                return BadRequest("视频路径不能为空");
            }
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            {
                return BadRequest("seconds 必须为非负数字");
            }
            // 允许绝对路径：任务创建时（TaskService）已把输入路径规范化为 InputDir 内的绝对路径存储并传给前端，
            // 而 StatusBar 快照预览直接用该路径；此处若仍拒绝对（默认 allowAbsolute=false）会恒 400，预览永不显示。
            // 边界校验（须落在 InputDir 内）依旧生效，拒绝 .. 逃逸。
            videoPath = filePathHelper.GetFullPath(RootDirType.InputDir, videoPath, allowAbsolute: true);
            if (!System.IO.File.Exists(videoPath))
            {
                return NotFound();
            }

            string path = await mediaInfoService.GetSnapshotAsync(videoPath, TimeSpan.FromSeconds(seconds));

            // 快照是临时文件，读入内存后删除，避免 %TEMP% 残留（P3-6）
            var bytes = await System.IO.File.ReadAllBytesAsync(path);
            System.IO.File.Delete(path);
            return File(bytes, "image/jpeg");
        }
    }
}