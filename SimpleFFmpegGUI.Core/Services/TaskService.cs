using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FzLib.Net;
using Microsoft.Extensions.Configuration;
using SimpleFFmpegGUI.Dto;
using TaskStatus = SimpleFFmpegGUI.Model.TaskStatus;

namespace SimpleFFmpegGUI.Services;

public class TaskService(TaskRepository taskRepository, QueueService queue, IConfiguration config)
{
    private readonly string inputDir = config.GetValue<string>(AppSettingsKeys.InputDirKey) ??
                                       throw new HttpStatusCodeException("没有配置输入文件夹",
                                           System.Net.HttpStatusCode.InternalServerError);

    private readonly string outputDir = config.GetValue<string>(AppSettingsKeys.OutputDirKey) ??
                                        throw new HttpStatusCodeException("没有配置输出文件夹",
                                            System.Net.HttpStatusCode.InternalServerError);

    public async Task<List<int>> AddTasks(string type, TaskDto request)
    {
        List<int> ids = new List<int>();
        var inputs = request.Inputs ?? [];
        var inputCount = request.Inputs?.Count ?? 0;

        switch (type.ToLower())
        {
            case "code":
                ValidateInputs(request, min: 1);
                // Code 类型是循环创建多个任务
                for (int i = 0; i < inputCount; i++)
                {
                    var file = inputs[i];
                    file.FilePath = GetInput(file.FilePath);
                    var task = await taskRepository.AddTaskAsync(TaskType.Code, [file], GetOutput(request, i),
                        request.Argument);
                    ids.Add(task.Id);
                }

                break;

            case "combine":
            case "compare":
                ValidateInputs(request, exact: 2);
                foreach (var file in inputs)
                {
                    file.FilePath = GetInput(file.FilePath);
                }

                var taskType = type.ToLower() == "combine" ? TaskType.Combine : TaskType.Compare;
                var output = taskType == TaskType.Combine ? GetOutput(request, 0) : null;
                var arg = taskType == TaskType.Combine ? request.Argument : null;

                var t = await taskRepository.AddTaskAsync(taskType, inputs, output, arg);
                ids.Add(t.Id);
                break;

            case "concat":
                ValidateInputs(request, min: 2);
                foreach (var file in inputs)
                {
                    file.FilePath = GetInput(file.FilePath);
                }

                var concatTask = await taskRepository.AddTaskAsync(TaskType.Concat, inputs,
                    GetOutput(request, 0), request.Argument);
                ids.Add(concatTask.Id);
                break;

            case "custom":
                if (request.Argument?.Extra == null)
                {
                    throw new HttpStatusCodeException("自定义任务需要额外参数", System.Net.HttpStatusCode.BadRequest);
                }

                var customTask =
                    await taskRepository.AddTaskAsync(TaskType.Custom, null, null, request.Argument);
                ids.Add(customTask.Id);
                break;

            default:
                throw new HttpStatusCodeException($"不支持的任务类型: {type}", System.Net.HttpStatusCode.BadRequest);
        }


        return ids;
    }

    public Task<int> CancelTaskAsync(int id)
    {
        return CancelTasksAsync([id]);
    }

    public async Task<int> CancelTasksAsync(ICollection<int> ids)
    {
        var tasks = (await taskRepository.GetTasksAsync(ids))
            .Where(p => p.Status is not (TaskStatus.Cancel or TaskStatus.Done or TaskStatus.Error))
            .ToList();
        foreach (var task in tasks)
        {
            if (queue.Tasks.Any(p => p.Id == task.Id))
            {
                queue.Managers.First(p => p.Task.Id == task.Id).Cancel();
            }
        }

        return await taskRepository.UpdateStatusAsync([.. tasks.Select(p => p.Id)], TaskStatus.Cancel);
    }

    public Task<int> DeleteTaskAsync(int id)
    {
        return DeleteTasksAsync([id]);
    }

    public async Task<int> DeleteTasksAsync(ICollection<int> ids)
    {
        foreach (var id in ids)
        {
            if (queue.Tasks.Any(p => p.Id == id))
            {
                queue.Managers.First(p => p.Task.Id == id).Cancel();
            }
        }

        return await taskRepository.SoftDeleteAsync(ids);
    }

    public async Task ResetTaskAsync(int id)
    {
        if (queue.Tasks.Any(p => p.Id == id))
        {
            throw new Exception($"ID为{id}的任务正在进行中");
        }

        var result = await taskRepository.UpdateStatusAsync([id], TaskStatus.Queue);
        if (result == 0)
        {
            throw new ArgumentException($"找不到ID为{id}的任务");
        }
    }

    public async Task<int> TryResetTasksAsync(IEnumerable<int> ids)
    {
        var notQueueTaskIds = ids.Where(id => !queue.Tasks.Any(p => p.Id == id)).ToList();
        return await taskRepository.UpdateStatusAsync(notQueueTaskIds, TaskStatus.Queue);
    }

    private string GetInput(string subPath)
    {
        string path = Path.IsPathFullyQualified(subPath) ? subPath : Path.Combine(inputDir, subPath);

        if (!File.Exists(path))
        {
            throw new HttpStatusCodeException($"不存在文件{subPath}", System.Net.HttpStatusCode.NotFound);
        }

        return path;
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
                output = Path.Combine(outputDir, Path.GetFileName(request.Inputs[inputIndex].FilePath));
            }
        }
        else
        {
            output = Path.Combine(outputDir, request.Output);
        }

        return output;
    }

    private void ValidateInputs(TaskDto request, int? min = null, int? exact = null)
    {
        var inputs = request.Inputs ?? [];
        var count = inputs?.Count ?? 0;
        if (count == 0 || inputs.Any(p => string.IsNullOrEmpty(p.FilePath)))
            throw new HttpStatusCodeException("输入文件为空", System.Net.HttpStatusCode.BadRequest);

        if (min.HasValue && count < min.Value)
            throw new HttpStatusCodeException($"输入文件至少需要 {min.Value} 个", System.Net.HttpStatusCode.BadRequest);

        if (exact.HasValue && count != exact.Value)
            throw new HttpStatusCodeException($"输入文件必须为 {exact.Value} 个", System.Net.HttpStatusCode.BadRequest);
    }
}