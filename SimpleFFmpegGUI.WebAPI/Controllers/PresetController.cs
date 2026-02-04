using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.WebAPI.Dto;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class PresetController(IConfiguration config, PresetService presetsService,PresetRepository presetRepository) : FFmpegControllerBase(config)
    {
        [HttpPost]
        [Route("Add")]
        public Task<int> AddAsync([FromBody] CodePresetDto request)
        {
            CheckNull(request, "请求");
            return presetsService.AddOrUpdatePresetAsync(request.Name, request.Type, request.Arguments);
        }

        [HttpPost]
        [Route("Delete")]
        public Task DeleteAsync(int id)
        {
            return presetsService.DeletePresetAsync(id);
        }

        [HttpGet]
        [Route("Export")]
        public async Task<FileResult> ExportAsync()
        {
            string json = await presetsService.ExportAsync();
            return File(Encoding.UTF8.GetBytes(json), "application/octet-stream", "presetsService.json");
        }

        [HttpGet]
        [Route("List")]
        public Task<List<CodePreset>> GetPresets(TaskType? type)
        {
            return type.HasValue ? presetRepository.GetByTypeAsync(type.Value) : presetRepository.GetAllAsync();
        }

        [HttpPost, HttpOptions]
        [Route("Import")]
        public async Task ImportAsync([FromQuery] IFormFile file)
        {
            using var s = file.OpenReadStream();
            byte[] buffer = new byte[s.Length];
            await s.ReadExactlyAsync(buffer);
            string json = Encoding.UTF8.GetString(buffer);
            await presetsService.ImportAsync(json);
        }
    }
}