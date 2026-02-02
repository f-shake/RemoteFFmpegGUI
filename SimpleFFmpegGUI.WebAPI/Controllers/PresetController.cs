using FzLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.WebAPI.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class PresetController(IConfiguration config, PresetManager presets) : FFmpegControllerBase(config)
    {
        [HttpPost]
        [Route("Add")]
        public Task<int> AddAsync([FromBody] CodePresetDto request)
        {
            CheckNull(request, "请求");
            return presets.AddOrUpdatePresetAsync(request.Name, request.Type, request.Arguments);
        }

        [HttpPost]
        [Route("Delete")]
        public Task DeleteAsync(int id)
        {
            return presets.DeletePresetAsync(id);
        }

        [HttpGet]
        [Route("Export")]
        public async Task<FileResult> ExportAsync()
        {
            string json = await presets.ExportAsync();
            return File(Encoding.UTF8.GetBytes(json), "application/octet-stream", "presets.json");
        }

        [HttpGet]
        [Route("List")]
        public Task<List<CodePreset>> GetPresets(TaskType? type)
        {
            return type.HasValue ? presets.GetPresetsAsync(type.Value) : presets.GetPresetsAsync();
        }

        [HttpPost, HttpOptions]
        [Route("Import")]
        public async Task ImportAsync([FromQuery] IFormFile file)
        {
            using var s = file.OpenReadStream();
            byte[] buffer = new byte[s.Length];
            await s.ReadExactlyAsync(buffer);
            string json = Encoding.UTF8.GetString(buffer);
            await presets.ImportAsync(json);
        }
    }
}