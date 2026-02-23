using FzLib.Net;
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
        IConfiguration config,
        DbConfigService dbConfig,
        MediaInfoService mediaInfoService,
        FilePathHelper filePathHelper) : FFmpegControllerBase(config)
    {
        [HttpGet("{name}")]
        public async Task<MediaInfoGeneral> GetAsync(string name)
        {
            CheckNull(name, "文件");
            var path = filePathHelper.GetFullPath(RootDirType.InputDir, name);
            var result = await mediaInfoService.GetMediaInfoAsync(path);
            return result;
        }

        [HttpGet]
        [Route("Snapshot")]
        public async Task<IActionResult> GetSnapshotAsync(string videoPath, double seconds)
        {
            try
            {
                videoPath =  filePathHelper.GetFullPath(RootDirType.InputDir, videoPath);
                string path = await mediaInfoService.GetSnapshotAsync(videoPath, TimeSpan.FromSeconds(seconds),
                    dbConfig.SnapshotSize);

                return PhysicalFile(path, "image/jpeg");
            }
            catch (Exception ex)
            {
                throw new HttpStatusCodeException($"获取截图失败：{ex.Message}",
                    System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}