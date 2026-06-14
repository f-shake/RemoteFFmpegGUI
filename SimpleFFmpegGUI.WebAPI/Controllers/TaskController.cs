using System;
using FzLib;
using FzLib.Web;
using FzLib.Programming;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Services;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class TaskController(
        TaskService taskService,
        TaskRepository taskRepository) : FFmpegControllerBase()
    {
        /// <summary>
        /// 获取任务列表
        /// </summary>
        [HttpGet]
        public async Task<PagedListResponse<TaskEntity>> GetTasksAsync([FromQuery] TaskQueryDto query)
        {
            var tasks = await taskRepository.GetTasksAsync(query);
            return tasks;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TaskEntity>> GetTaskAsync(int id)
        {
            var task = await taskRepository.GetTaskAsync(id);
            if (task == null)
            {
                return NotFound($"任务{id}不存在");
            }

            return task;
        }

        /// <summary>
        /// 创建任务
        /// </summary>
        [HttpPost("{type}")]
        public async Task<ActionResult<List<int>>> AddTaskAsync(string type, [FromBody] TaskDto request)
        {
            if (request == null)
            {
                return BadRequest("请求对象不能为空");
            }

            var result = await taskService.AddTasks(type, request);
            return result.ToActionResult();
        }

        [HttpPost("{id:int}/Cancel")]
        public async Task<IActionResult> CancelTaskAsync(int id)
        {
            var result = await taskService.CancelTasksAsync([id]);
            if (result.NotFoundIds.Count != 0)
            {
                return NotFound();
            }

            if (result.FailedIds.Count != 0)
            {
                return BadRequest(result.FailedIds.First().Value);
            }

            return NoContent();
        }

        [HttpPost("Batch/Cancel")]
        public async Task<ActionResult<TaskStatusChangeResult>> CancelTasksAsync([FromBody] ICollection<int> ids)
        {
            return await taskService.CancelTasksAsync(ids);
        }

        [HttpPost("{id:int}/Delete")]
        public async Task<IActionResult> DeleteTaskAsync(int id)
        {
            var result = await taskService.DeleteTasksAsync([id]);
            if (result.NotFoundIds.Count != 0)
            {
                return NotFound();
            }

            if (result.FailedIds.Count != 0)
            {
                return BadRequest(result.FailedIds.First().Value);
            }

            return NoContent();
        }

        [HttpPost("Batch/Delete")]
        public async Task<ActionResult<TaskStatusChangeResult>> DeleteTasksAsync([FromBody] ICollection<int> ids)
        {
            return await taskService.DeleteTasksAsync(ids);
        }

        [HttpPost("{id:int}/Reset")]
        public async Task<IActionResult> ResetTaskAsync(int id)
        {
            var result = await taskService.ResetTasksAsync([id]);
            if (result.NotFoundIds.Count != 0)
            {
                return NotFound();
            }

            if (result.FailedIds.Count != 0)
            {
                return BadRequest(result.FailedIds.First().Value);
            }

            return NoContent();
        }

        [HttpPost("Batch/Reset")]
        public async Task<ActionResult<TaskStatusChangeResult>> ResetTasksAsync([FromBody] IEnumerable<int> ids)
        {
            return await taskService.ResetTasksAsync(ids);
        }

        [HttpPost("PreviewArguments")]
        public ActionResult<string> PreviewArguments([FromBody] OutputParameters parameters)
        {
            return FFmpegTaskService.TestOutputArguments(parameters);
        }

        [HttpGet("Formats")]
        public VideoFormat[] GetVideoFormats()
        {
            return VideoFormat.Formats;
        }
    }
}