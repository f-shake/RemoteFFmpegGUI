using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Repositories;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.Entities;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class PresetController(
        PresetService presetsService,
        PresetRepository presetRepository) : FFmpegControllerBase()
    {
        [HttpPost("Add")]
        public async Task<ActionResult<int>> AddAsync(AddPresetRequest request)
        {
            if (request == null)
            {
                return BadRequest("请求对象不能为空");
            }

            if (request.Parameters == null)
            {
                return BadRequest("参数不能为空");
            }

            if (request.Name == null)
            {
                return BadRequest("名称不能为空");
            }

            var result = await presetsService.AddPresetAsync(request);
            return result.ToActionResult();
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdatePresetRequest request)
        {
            if (request == null)
            {
                return BadRequest("请求对象不能为空");
            }

            if (request.Parameters == null)
            {
                return BadRequest("参数不能为空");
            }

            if (request.Name == null)
            {
                return BadRequest("名称不能为空");
            }

            var result = await presetsService.UpdatePresetAsync(id, request);
            return result.ToActionResult();
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
        public async Task<ActionResult<List<PresetEntity>>> GetPresets(TaskType? type)
        {
            return type.HasValue
                ? await presetRepository.GetByTypeAsync(type.Value)
                : await presetRepository.GetAllAsync();
        }

        [HttpPost("Import")]
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