using System;
using FzLib;
using FzLib.Net;
using FzLib.Programming;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WebAPI.Dto;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class TaskController(
        IConfiguration config,
        TaskService taskService,
        TaskRepository taskRepository,
        QueueService queue) : FFmpegControllerBase(config)
    {
        [HttpPost]
        [Route("Add/{type}")]
        public async Task<ActionResult<List<int>>> AddTaskAsync(string type, [FromBody] TaskDto request)
        {
            if (request == null)
            {
                return BadRequest("请求对象不能为空");
            }

            return await taskService.AddTasks(type, request);
        }


        [HttpPost]
        [Route("Cancel")]
        public async Task<IActionResult> CancelTaskAsync(int id)
        {
            var rows = await taskService.CancelTaskAsync(id);
            if (rows == 0)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost]
        [Route("Cancel/List")]
        public async Task<ActionResult<int>> CancelTasksAsync(ICollection<int> ids)
        {
            var rows = await taskService.CancelTasksAsync(ids);
            if (rows == 0)
            {
                return NotFound();
            }

            return rows;
        }

        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IActionResult> DeleteTaskAsync(int id)
        {
            int rows = await taskService.DeleteTaskAsync(id);
            if (rows == 0)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost]
        [Route("Delete")]
        public async Task<ActionResult<int>> DeleteTasksAsync([FromBody] ICollection<int> ids)
        {
            var rows = await taskService.DeleteTasksAsync(ids);
            if (rows == 0)
            {
                return NotFound();
            }

            return rows;
        }

        [HttpGet("Detail/{id:int}")]
        public async Task<ActionResult<TaskInfo>> GetTaskAsync(int id)
        {
            var task = await taskRepository.GetTaskAsync(id);
            if (task == null)
            {
                throw new HttpStatusCodeException($"任务{id}不存在", System.Net.HttpStatusCode.NotFound);
            }

            return task;
        }


        /// <summary>
        /// 获取任务列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("List")]
        public async Task<PagedListDto<TaskInfo>> GetTasksAsync([FromQuery] TaskQueryDto query)
        {
            int skip = (query.Page - 1) * query.PageSize;
            var statusEnum = query.Status.HasValue ? (Model.TaskStatus)query.Status.Value : (Model.TaskStatus?)null;
            var tasks = await taskRepository.GetTasksAsync(statusEnum, skip, query.PageSize);

            return tasks;
        }

        [HttpGet]
        [Route("Formats")]
        public VideoFormat[] GetVideoFormats()
        {
            return VideoFormat.Formats;
        }

        [HttpPost]
        [Route("Reset")]
        public Task ResetTaskAsync(int id)
        {
            return taskService.ResetTaskAsync(id);
        }

        [HttpPost]
        [Route("Reset/List")]
        public Task ResetTasksAsync(IEnumerable<int> ids)
        {
            return taskService.TryResetTasksAsync(ids);
        }
    }
}