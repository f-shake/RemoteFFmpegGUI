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
            videoPath = filePathHelper.GetFullPath(RootDirType.InputDir, videoPath);
            if (!System.IO.File.Exists(videoPath))
            {
                return NotFound();
            }

            string path = await mediaInfoService.GetSnapshotAsync(videoPath, TimeSpan.FromSeconds(seconds));

            return PhysicalFile(path, "image/jpeg");
        }
    }
}