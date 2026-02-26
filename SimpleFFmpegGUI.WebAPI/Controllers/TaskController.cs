using System;
using FzLib;
using FzLib.Web;
using FzLib.Programming;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Services;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class TaskController(
        TaskService taskService,
        TaskRepository taskRepository) : FFmpegControllerBase()
    {
        [HttpPost]
        [Route("Add/{type}")]
        public async Task<ActionResult<List<int>>> AddTaskAsync(string type, [FromBody] TaskDto request)
        {
            if (request == null)
            {
                return BadRequest("请求对象不能为空");
            }

            var result= await taskService.AddTasks(type, request);
            return result.ToActionResult();
        }


        [HttpPost]
        [Route("Cancel")]
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

        [HttpPost]
        [Route("Cancel/List")]
        public async Task<ActionResult<TaskStatusChangeResult>> CancelTasksAsync(ICollection<int> ids)
        {
            return await taskService.CancelTasksAsync(ids);
        }

        [HttpDelete]
        [Route("{id:int}")]
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

        [HttpPost]
        [Route("Delete")]
        public async Task<ActionResult<TaskStatusChangeResult>> DeleteTasksAsync([FromBody] ICollection<int> ids)
        {
            return await taskService.DeleteTasksAsync(ids);
        }

        [HttpGet("Detail/{id:int}")]
        public async Task<ActionResult<TaskInfo>> GetTaskAsync(int id)
        {
            var task = await taskRepository.GetTaskAsync(id);
            if (task == null)
            {
                return NotFound($"任务{id}不存在");
            }

            return task;
        }


        /// <summary>
        /// 获取任务列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpGet("List")]
        public async Task<PagedListResponse<TaskInfo>> GetTasksAsync([FromQuery] TaskQueryDto query)
        {
            // int skip = (query.Page - 1) * query.PageSize;
            // var statusEnum = query.Status.HasValue ? (Model.TaskStatus)query.Status.Value : (Model.TaskStatus?)null;
            // var tasks = await taskRepository.GetTasksAsync(statusEnum, skip, query.PageSize);
            var tasks = await taskRepository.GetTasksAsync(query);
            return tasks;
        }

        [HttpGet]
        [Route("Formats")]
        public VideoFormat[] GetVideoFormats()
        {
            return VideoFormat.Formats;
        }

        [HttpPost]
        [Route("Reset/{id:int}")]
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

        [HttpPost]
        [Route("Reset/List")]
        public async Task<ActionResult<TaskStatusChangeResult>> ResetTasksAsync(IEnumerable<int> ids)
        {
            return await taskService.ResetTasksAsync(ids);
        }
    }
}