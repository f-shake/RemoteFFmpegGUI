using FzLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.WebAPI.Dto;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class PresetController : FFmpegControllerBase
    {
        public PresetController(ILogger<MediaInfoController> Logger,
            IConfiguration config,
        PipeClient pipeClient) : base(Logger, config, pipeClient) { }

        [HttpGet]
        [Route("List")]
        public async Task<List<CodePreset>> GetPresets(TaskType? type)
        {
            if (type.HasValue)
            {
                return (await pipeClient.InvokeAsync(p => p.GetPresetsAsync())).Where(p => p.Type == type).ToList();
            }
            return await pipeClient.InvokeAsync(p => p.GetPresetsAsync());
        }

        [HttpPost]
        [Route("Add")]
        public Task<int> AddAsync([FromBody] CodePresetDto request)
        {
            CheckNull(request, "请求");
            return pipeClient.InvokeAsync(p => p.AddOrUpdatePresetAsync(request.Name, request.Type, request.Arguments));
        }

        [HttpPost]
        [Route("Delete")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            await pipeClient.InvokeAsync(p => p.DeletePresetAsync(id));
            return Ok();
        }

        [HttpGet]
        [Route("Export")]
        public async Task<FileResult> ExportAsync()
        {
            string json = await pipeClient.InvokeAsync(p => p.ExportPresetsAsync());
            return File(Encoding.UTF8.GetBytes(json), "application/octet-stream", "presets.json");
        }

        [HttpPost, HttpOptions]
        [Route("Import")]
        public async Task<IActionResult> ImportAsync([FromQuery] IFormFile file)
        {
            using var s = file.OpenReadStream();
            byte[] buffer = new byte[s.Length];
            await s.ReadExactlyAsync(buffer);
            string json = Encoding.UTF8.GetString(buffer);
            await pipeClient.InvokeAsync(p => p.ImportPresetsAsync(json));
            return Ok();
        }
    }
}