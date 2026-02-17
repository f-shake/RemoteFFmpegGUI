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
        [Route("Add/Code")]
        public async Task<List<int>> AddCodeTaskAsync([FromBody] TaskDto request)
        {
            if (request.Inputs == null || request.Inputs.Count == 0
                                       || request.Inputs.Any(p => string.IsNullOrEmpty(p.FilePath)))
            {
                throw new HttpStatusCodeException("输入文件为空", System.Net.HttpStatusCode.BadRequest);
            }

            List<int> ids = new List<int>();

            for (int i = 0; i < request.Inputs.Count; i++)
            {
                var file = request.Inputs[i];
                //检查输入文件存在


                file.FilePath = await CheckAndGetInputFilePathAsync(file.FilePath);
                var task = await taskRepository.AddTaskAsync(TaskType.Code, [file], GetOutput(request, i),
                    request.Argument);
                ids.Add(task.Id);
            }

            if (request.Start)
            {
                queue.StartQueue();
            }

            return ids;
        }

        [HttpPost]
        [Route("Add/Combine")]
        public async Task<int> AddCombineTaskAsync([FromBody] TaskDto request)
        {
            if (request.Inputs == null || request.Inputs.Count() == 0 ||
                request.Inputs.Any(p => string.IsNullOrEmpty(p.FilePath)))
            {
                throw new HttpStatusCodeException("输入文件为空", System.Net.HttpStatusCode.BadRequest);
            }

            if (request.Inputs.Count() != 2)
            {
                throw new HttpStatusCodeException("输入文件必须为2个", System.Net.HttpStatusCode.BadRequest);
            }

            foreach (var file in request.Inputs)
            {
                file.FilePath = await CheckAndGetInputFilePathAsync(file.FilePath);
            }

            request.Inputs.ForEach(p => p.FilePath = Path.Combine(InputDir, p.FilePath));
            var task = await taskRepository.AddTaskAsync(TaskType.Combine, request.Inputs, GetOutput(request, 0),
                request.Argument);

            if (request.Start)
            {
                queue.StartQueue();
            }

            return task.Id;
        }

        [HttpPost]
        [Route("Add/Compare")]
        public async Task<int> AddCompareTaskAsync([FromBody] TaskDto request)
        {
            if (request.Inputs == null || request.Inputs.Count() == 0 ||
                request.Inputs.Any(p => string.IsNullOrEmpty(p.FilePath)))
            {
                throw new HttpStatusCodeException("输入文件为空", System.Net.HttpStatusCode.BadRequest);
            }

            if (request.Inputs.Count != 2)
            {
                throw new HttpStatusCodeException("输入文件必须为2个", System.Net.HttpStatusCode.BadRequest);
            }

            foreach (var file in request.Inputs)
            {
                file.FilePath = await CheckAndGetInputFilePathAsync(file.FilePath);
            }

            request.Inputs.ForEach(p => p.FilePath = Path.Combine(InputDir, p.FilePath));
            var task = await taskRepository.AddTaskAsync(TaskType.Compare, request.Inputs, null, null);
            if (request.Start)
            {
                queue.StartQueue();
            }

            return task.Id;
        }

        [HttpPost]
        [Route("Add/Concat")]
        public async Task<List<int>> AddConcatTaskAsync([FromBody] TaskDto request)
        {
            if (request.Inputs == null || request.Inputs.Count < 2
                                       || request.Inputs.Any(p => string.IsNullOrEmpty(p.FilePath)))
            {
                throw new HttpStatusCodeException("输入文件为空或少于2个", System.Net.HttpStatusCode.BadRequest);
            }

            List<int> ids = new List<int>();

            foreach (var file in request.Inputs)
            {
                file.FilePath = await CheckAndGetInputFilePathAsync(file.FilePath);
            }

            var task = await taskRepository.AddTaskAsync(TaskType.Concat, request.Inputs, GetOutput(request, 0),
                request.Argument);
            ids.Add(task.Id);
            if (request.Start)
            {
                queue.StartQueue();
            }

            return ids;
        }

        [HttpPost]
        [Route("Add/Custom")]
        public async Task<int> AddCustomTaskAsync([FromBody] TaskDto request)
        {
            CheckNull(request.Argument, "参数");
            CheckNull(request.Argument.Extra, "参数");
            var task = await taskRepository.AddTaskAsync(TaskType.Custom, null, null, request.Argument);
            if (request.Start)
            {
                queue.StartQueue();
            }

            return task.Id;
        }

        [HttpPost]
        [Route("Cancel")]
        public Task CancelTaskAsync(int id)
        {
            return taskService.CancelTaskAsync(id);
        }

        [HttpPost]
        [Route("Cancel/List")]
        public Task CancelTasksAsync(ICollection<int> ids)
        {
            return taskService.TryCancelTasksAsync(ids);
        }

        [HttpPost]
        [Route("Delete")]
        public Task DeleteTaskAsync(int id)
        {
            return taskService.DeleteTaskAsync(id);
        }

        [HttpPost]
        [Route("Delete/List")]
        public Task DeleteTasksAsync(ICollection<int> ids)
        {
            return taskService.TryDeleteTasksAsync(ids);
        }

        [HttpGet]
        [Route("Detail/{id:int}")]
        public async Task<TaskInfo> GetTaskAsync(int id)
        {
            var task = await taskRepository.GetTaskAsync(id);
            if (task == null)
            {
                throw new HttpStatusCodeException($"任务{id}不存在", System.Net.HttpStatusCode.NotFound);
            }

            return HideAbsolutePath(task);
        }

        [Obsolete("请使用 /Task/Detail/{id} 访问")]
        [HttpGet]
        [Route("")]
        public Task<TaskInfo> GetTaskOld(int id)
        {
            return GetTaskAsync(id);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="status">1：队列中；2：进行中；3：完成；4：错误；5：取消</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("ListOld")]
        public async Task<PagedListDto<TaskInfo>> GetTasksOld(int status = 0, int skip = 0, int take = 0)
        {
            var tasks = await taskRepository.GetTasksAsync(status == 0 ? null : (Model.TaskStatus)status, skip, take);
            tasks.List.ForEach(p => HideAbsolutePath(p));
            return tasks;
        }


        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpGet("List")]
        public async Task<PagedListDto<TaskInfo>> GetTasksAsync([FromQuery] TaskQueryDto query)
        {
            int skip = (query.Page - 1) * query.PageSize;
            var statusEnum = query.Status.HasValue ? (Model.TaskStatus)query.Status.Value : (Model.TaskStatus?)null;
            var tasks = await taskRepository.GetTasksAsync(statusEnum, skip, query.PageSize);
            tasks.List.ForEach(p => HideAbsolutePath(p));

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

        private string GetOutput(TaskDto request, int inputIndex)
        {
            Debug.Assert(inputIndex >= 0 && inputIndex < request.Inputs.Count);
            string output = request.Output;
            if (output != null && output.StartsWith(':'))
            {
                output = output[1..];
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                if (request.Inputs.Count > 0)
                {
                    output = Path.Combine(OutputDir, Path.GetFileName(request.Inputs[inputIndex].FilePath));
                }
            }
            else
            {
                output = Path.Combine(OutputDir, request.Output);
            }

            return output;
        }
    }
}