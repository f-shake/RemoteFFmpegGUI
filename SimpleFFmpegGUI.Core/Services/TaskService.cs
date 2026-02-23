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
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Helpers;
using TaskStatus = SimpleFFmpegGUI.Model.TaskStatus;

namespace SimpleFFmpegGUI.Services;

public class TaskService(TaskRepository taskRepository, QueueService queue, FilePathHelper filePathHelper)
{
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
                    file.FilePath = filePathHelper.GetFullPath(RootDirType.InputDir, file.FilePath);
                    var task = await taskRepository.AddTaskAsync(TaskType.Code, [file], GetOutputByInput(request, i),
                        request.Argument);
                    ids.Add(task.Id);
                }

                break;

            case "combine":
            case "compare":
                ValidateInputs(request, exact: 2);
                foreach (var file in inputs)
                {
                    file.FilePath = filePathHelper.GetFullPath(RootDirType.InputDir, file.FilePath);
                }

                var taskType = type.ToLower() == "combine" ? TaskType.Combine : TaskType.Compare;
                var output = taskType == TaskType.Combine ? GetOutputByInput(request, 0) : null;
                var arg = taskType == TaskType.Combine ? request.Argument : null;

                var t = await taskRepository.AddTaskAsync(taskType, inputs, output, arg);
                ids.Add(t.Id);
                break;

            case "concat":
                ValidateInputs(request, min: 2);
                foreach (var file in inputs)
                {
                    file.FilePath = filePathHelper.GetFullPath(RootDirType.InputDir, file.FilePath);
                }

                var concatTask = await taskRepository.AddTaskAsync(TaskType.Concat, inputs,
                    GetOutputByInput(request, 0), request.Argument);
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

    public async Task<TaskStatusChangeResult> CancelTasksAsync(ICollection<int> ids)
    {
        TaskStatusChangeResult result = new TaskStatusChangeResult();
        var allIds = await taskRepository.GetTaskIdsAsync();
        var requestIdToTask = (await taskRepository.GetTasksAsync(ids)).ToDictionary(p => p.Id);
        List<int> processingIds = new List<int>();
        foreach (var id in ids)
        {
            if (!allIds.Contains(id))
            {
                result.NotFoundIds.Add(id);
            }
            else if (requestIdToTask[id].Status is (TaskStatus.Cancel or TaskStatus.Done or TaskStatus.Error))
            {
                result.FailedIds.Add(id, "任务已结束或已取消");
            }
            else if (queue.Tasks.Any(p => p.Id == id))
            {
                queue.Managers.First(p => p.Task.Id == id).Cancel();
                processingIds.Add(id);
            }
            else
            {
                processingIds.Add(id);
            }
        }

        result.AffectedRows = await taskRepository.UpdateStatusAsync(processingIds, TaskStatus.Cancel);
        return result;
    }


    public async Task<TaskStatusChangeResult> DeleteTasksAsync(ICollection<int> ids)
    {
        TaskStatusChangeResult result = new TaskStatusChangeResult();
        var allIds = await taskRepository.GetTaskIdsAsync();
        List<int> processingIds = new List<int>();
        foreach (var id in ids)
        {
            if (!allIds.Contains(id))
            {
                result.NotFoundIds.Add(id);
            }
            else if (queue.Tasks.Any(p => p.Id == id))
            {
                result.FailedIds.Add(id, "任务正在执行中，无法删除");
            }
            else
            {
                processingIds.Add(id);
            }
        }

        result.AffectedRows = await taskRepository.SoftDeleteAsync(processingIds);
        return result;
    }


    public async Task<TaskStatusChangeResult> ResetTasksAsync(IEnumerable<int> ids)
    {
        TaskStatusChangeResult result = new TaskStatusChangeResult();
        var allIds = await taskRepository.GetTaskIdsAsync();
        List<int> processingIds = new List<int>();
        foreach (var id in ids)
        {
            if (!allIds.Contains(id))
            {
                result.NotFoundIds.Add(id);
            }
            else if (queue.Tasks.Any(p => p.Id == id))
            {
                result.FailedIds.Add(id, "任务正在执行中，无法重置");
            }
            else
            {
                processingIds.Add(id);
            }
        }

        result.AffectedRows = await taskRepository.UpdateStatusAsync(processingIds, TaskStatus.Queue);
        return result;
    }


    private string GetOutputByInput(TaskDto request, int inputIndex)
    {
        Debug.Assert(inputIndex >= 0 && inputIndex < request.Inputs.Count);
        string output = request.Output;

        if (string.IsNullOrWhiteSpace(output))
        {
            if (request.Inputs.Count <= 0)
            {
                return output;
            }

            //如果没有输出文件名，则使用输入文件名
            var input = request.Inputs[inputIndex];
            if (input?.FilePath != null)
            {
                output = filePathHelper.GetFullPath(RootDirType.OutputDir, input.FilePath, false);
            }
        }
        else
        {
            //如果是相对路径，补充为绝对路径
            var outputDir = filePathHelper.GetFullPath(RootDirType.OutputDir, output, false);
            output = Path.IsPathFullyQualified(output) ? output : Path.Combine(outputDir, output);
        }

        return output;
    }

    private void ValidateInputs(TaskDto request, int? min = null, int? exact = null)
    {
        var inputs = request.Inputs ?? [];
        var count = inputs?.Count ?? 0;
        if (count == 0 || inputs.Any(p => string.IsNullOrEmpty(p.FilePath)))
        {
            throw new HttpStatusCodeException("输入文件为空", System.Net.HttpStatusCode.BadRequest);
        }

        if (count < min)
        {
            throw new HttpStatusCodeException($"输入文件至少需要 {min.Value} 个", System.Net.HttpStatusCode.BadRequest);
        }

        if (exact.HasValue && count != exact.Value)
        {
            throw new HttpStatusCodeException($"输入文件必须为 {exact.Value} 个", System.Net.HttpStatusCode.BadRequest);
        }
    }
}