using FzLib;
using FzLib.Net;
using FzLib.Programming;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.WebAPI.Dto;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class TaskController : FFmpegControllerBase
    {
        public TaskController(ILogger<MediaInfoController> Logger,
            IConfiguration config,
        PipeClient pipeClient) : base(Logger, config, pipeClient) { }

        [HttpPost]
        [Route("Add/Code")]
        public async Task<List<int>> AddCodeTaskAsync([FromBody] TaskDto request)
        {
            if (request.Inputs == null || request.Inputs.Count() == 0
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

                ids.Add(await pipeClient.InvokeAsync(p =>
                 p.AddTaskAsync(TaskType.Code,
                 new List<InputArguments>() { file },
              GetOutput(request, i),
                 request.Argument)));
            }
            if (request.Start)
            {
                await pipeClient.InvokeAsync(p => p.StartQueue()).ConfigureAwait(false);
            }
            return ids;
        }

        [HttpPost]
        [Route("Add/Combine")]
        public async Task<int> AddCombineTaskAsync([FromBody] TaskDto request)
        {
            if (request.Inputs == null || request.Inputs.Count() == 0 || request.Inputs.Any(p => string.IsNullOrEmpty(p.FilePath)))
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
            var id = await pipeClient.InvokeAsync(p =>
               p.AddTaskAsync(TaskType.Combine,
               request.Inputs,
              GetOutput(request, 0),
               request.Argument));
            if (request.Start)
            {
                await pipeClient.InvokeAsync(p => p.StartQueue()).ConfigureAwait(false);
            }
            return id;
        }

        [HttpPost]
        [Route("Add/Compare")]
        public async Task<int> AddCompareTaskAsync([FromBody] TaskDto request)
        {
            if (request.Inputs == null || request.Inputs.Count() == 0 || request.Inputs.Any(p => string.IsNullOrEmpty(p.FilePath)))
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
            var id = await pipeClient.InvokeAsync(p =>
               p.AddTaskAsync(TaskType.Compare, request.Inputs, null, null));
            if (request.Start)
            {
                await pipeClient.InvokeAsync(p => p.StartQueue()).ConfigureAwait(false);
            }
            return id;
        }

        [HttpPost]
        [Route("Add/Concat")]
        public async Task<List<int>> AddConcatTaskAsync([FromBody] TaskDto request)
        {
            if (request.Inputs == null || request.Inputs.Count() < 2
                || request.Inputs.Any(p => string.IsNullOrEmpty(p.FilePath)))
            {
                throw new HttpStatusCodeException("输入文件为空或少于2个", System.Net.HttpStatusCode.BadRequest);
            }
            List<int> ids = new List<int>();

            foreach (var file in request.Inputs)
            {
                file.FilePath = await CheckAndGetInputFilePathAsync(file.FilePath);
            }
            ids.Add(await pipeClient.InvokeAsync(p =>
                 p.AddTaskAsync(TaskType.Concat,
                 request.Inputs,
                 GetOutput(request, 0),
                 request.Argument)));
            if (request.Start)
            {
                await pipeClient.InvokeAsync(p => p.StartQueue()).ConfigureAwait(false);
            }
            return ids;
        }

        [HttpPost]
        [Route("Add/Custom")]
        public async Task<int> AddCustomTaskAsync([FromBody] TaskDto request)
        {
            CheckNull(request.Argument, "参数");
            CheckNull(request.Argument.Extra, "参数");
            var id = await pipeClient.InvokeAsync(p =>
               p.AddTaskAsync(TaskType.Custom, null, null,
               request.Argument));
            if (request.Start)
            {
                await pipeClient.InvokeAsync(p => p.StartQueue()).ConfigureAwait(false);
            }
            return id;
        }

        [HttpPost]
        [Route("Cancel")]
        public async Task CancelTaskAsync(int id)
        {
            await pipeClient.InvokeAsync(p => p.CancelTaskAsync(id));
        }

        [HttpPost]
        [Route("Cancel/List")]
        public async Task CancelTasksAsync(IEnumerable<int> ids)
        {
            await pipeClient.InvokeAsync(p => p.CancelTasksAsync(ids));
        }

        [HttpPost]
        [Route("Delete")]
        public async Task DeleteTaskAsync(int id)
        {
            await pipeClient.InvokeAsync(p => p.DeleteTaskAsync(id));
        }

        [HttpPost]
        [Route("Delete/List")]
        public async Task DeleteTasksAsync(IEnumerable<int> ids)
        {
            await pipeClient.InvokeAsync(p => p.DeleteTasksAsync(ids));
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetTask(int id)
        {
            var task = await pipeClient.InvokeAsync(p => p.GetTaskAsync(id));
            if (task == null)
            {
                return NotFound();
            }
            return Ok(HideAbsolutePath(task));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="status">1：队列中；2：进行中；3：完成；4：错误；5：取消</param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("List")]
        public async Task<PagedListDto<TaskInfo>> GetTasks(int status = 0, int skip = 0, int take = 0)
        {
            var tasks = await pipeClient.InvokeAsync(p => p.GetTasksAsync(status == 0 ? null : (Model.TaskStatus)status, skip, take));

            tasks.List.ForEach(p => HideAbsolutePath(p));
            return tasks;
        }
        [HttpGet]
        [Route("Formats")]
        public Task<VideoFormat[]> GetVideoFormats()
        {
            return pipeClient.InvokeAsync(p => p.GetSuggestedFormats());
        }

        [HttpPost]
        [Route("Reset")]
        public async Task ResetTaskAsync(int id)
        {
            await pipeClient.InvokeAsync(p => p.ResetTaskAsync(id));
        }

        [HttpPost]
        [Route("Reset/List")]
        public async Task ResetTasksAsync(IEnumerable<int> ids)
        {
            await pipeClient.InvokeAsync(p => p.ResetTasksAsync(ids));
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