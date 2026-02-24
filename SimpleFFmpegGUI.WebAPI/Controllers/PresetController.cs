using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Dto;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class PresetController(
        PresetService presetsService,
        PresetRepository presetRepository) : FFmpegControllerBase()
    {
        [HttpPost("AddOrUpdate")]
        public async Task<ActionResult<int>> AddAsync([FromBody] CodePresetDto request)
        {
            if (request == null)
            {
                return BadRequest("请求对象不能为空");
            }

            if (request.Arguments == null)
            {
                return BadRequest("参数不能为空");
            }

            if (request.Name == null)
            {
                return BadRequest("名称不能为空");
            }

            return await presetsService.AddOrUpdatePresetAsync(request.Name, request.Type, request.Arguments);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            await presetsService.DeletePresetAsync(id);
            return NoContent();
        }

        [HttpGet("Export")]
        public async Task<FileResult> ExportAsync()
        {
            string json = await presetsService.ExportAsync();
            return File(Encoding.UTF8.GetBytes(json), "application/octet-stream", "presetsService.json");
        }

        [HttpGet("List")]
        public async Task<ActionResult<List<CodePreset>>> GetPresets(TaskType? type)
        {
            return type.HasValue
                ? await presetRepository.GetByTypeAsync(type.Value)
                : await presetRepository.GetAllAsync();
        }

        [HttpPost, HttpOptions]
        [Route("Import")]
        public async Task<IActionResult> ImportAsync([FromQuery] IFormFile file)
        {
            await using var s = file.OpenReadStream();
            byte[] buffer = new byte[s.Length];
            await s.ReadExactlyAsync(buffer);
            string json = Encoding.UTF8.GetString(buffer);
            await presetsService.ImportAsync(json);
            return NoContent();
        }
    }
}