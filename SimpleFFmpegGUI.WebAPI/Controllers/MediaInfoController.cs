using FzLib.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Model.MediaInfo;
using System;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class MediaInfoController(IConfiguration config, ConfigManager dbConfig) : FFmpegControllerBase(config)
    {
        [HttpGet]
        public async Task<MediaInfoGeneral> GetAsync(string name)
        {
            CheckNull(name, "文件");
            string path = await CheckAndGetInputFilePathAsync(name);

            var result = await MediaInfoManager.GetMediaInfoAsync(path);
            return result;
        }

        [HttpGet]
        [Route("Snapshot")]
        public async Task<IActionResult> GetSnapshotAsync(string videoPath, double seconds)
        {
            try
            {
                videoPath = await CheckAndGetInputFilePathAsync(videoPath);
                string path = await MediaInfoManager.GetSnapshotAsync(videoPath, TimeSpan.FromSeconds(seconds), dbConfig.SnapshotSize);

                return PhysicalFile(path, "image/jpeg");
            }
            catch (Exception ex)
            {
                throw new HttpStatusCodeException($"获取截图失败：{ex.Message}", System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}