using FzLib.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Model.MediaInfo;
using SimpleFFmpegGUI.Services;
using System;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Helpers;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class MediaInfoController(
        DbConfigService dbConfig,
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
            try
            {
                videoPath = filePathHelper.GetFullPath(RootDirType.InputDir, videoPath);
                if (!System.IO.File.Exists(videoPath))
                {
                    return NotFound();
                }
                string path = await mediaInfoService.GetSnapshotAsync(videoPath, TimeSpan.FromSeconds(seconds),
                    dbConfig.SnapshotSize);

                return PhysicalFile(path, "image/jpeg");
            }
            catch (Exception ex)
            {
                // throw new HttpStatusCodeException($"获取截图失败：{ex.Message}",
                //     System.Net.HttpStatusCode.InternalServerError);
                return Problem(ex.Message);
            }
        }
    }
}