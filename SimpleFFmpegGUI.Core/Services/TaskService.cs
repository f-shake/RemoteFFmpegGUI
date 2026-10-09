using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FzLib.Web;
using Microsoft.Extensions.Configuration;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Events;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Helpers;
using TaskStatus = SimpleFFmpegGUI.Enums.TaskStatus;

namespace SimpleFFmpegGUI.Services;

public class TaskService(TaskRepository taskRepository, QueueService queue, FilePathHelper filePathHelper,
    ITaskChangeNotifier notifier, DbLoggerService logger)
{
    public async Task<ServiceResult<List<int>>> AddTasks(string type, TaskDto request)
    {
        List<int> ids = new List<int>();
        var inputs = request.Inputs ?? [];
        var inputCount = request.Inputs?.Count ?? 0;

        switch (type)
        {
            case nameof(TaskType.Transcode):
                ValidateInputs(request, min: 1);
                // Code 类型是循环创建多个任务。
                // 多输入时请求里的输出文件名不参与命名：每个输入各建一个任务，若共用同一个目标，
                // 多个任务就会写同一个文件（Web 端多输入时输出框被禁用、但框里的旧值仍会被提交上来，
                // 2026-10-09 那批 1007-4-1/2/3 → 1007-3-2.mp4 就是这么撞上的），改为各按自己的输入文件名命名
                bool ignoreExplicitOutput = inputCount > 1;

                // 批次内去重：同一个输入被提交多次、或不同目录下的同名输入，会算出同一个目标名，库里看着像要撞车
                // （执行时虽会被自动改名，但那时才看得出来），提交时就把它们区分开
                var usedOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                bool warnedIgnoredOutput = false;
                for (int i = 0; i < inputCount; i++)
                {
                    var file = inputs[i];
                    file.FilePath = filePathHelper.GetFullPath(RootDirType.InputDir, file.FilePath, allowAbsolute: true);
                    if (!File.Exists(file.FilePath))
                    {
                        return ServiceResult<List<int>>.Failure($"输入文件不存在: {file.FilePath}", HttpStatusCode.NotFound);
                    }

                    var task = await taskRepository.AddTaskAsync(TaskType.Transcode, [file],
                        GetOutputByInput(request, i, ignoreExplicitOutput, usedOutputs),
                        request.Parameter);
                    ids.Add(task.Id);

                    // 真的建出了任务才留痕：若前面的输入就不存在、一个任务都没建，日志不该说"已忽略并改名"
                    if (!warnedIgnoredOutput && ignoreExplicitOutput && !string.IsNullOrWhiteSpace(request.Output))
                    {
                        warnedIgnoredOutput = true;
                        logger.Warn($"多输入提交（{inputCount} 个），已忽略指定的输出文件名「{request.Output}」，各任务按自己的输入文件名命名");
                    }
                }

                break;

            case nameof(TaskType.Mux):
            case nameof(TaskType.QualityCheck):
                ValidateInputs(request, exact: 2);
                foreach (var file in inputs)
                {
                    file.FilePath = filePathHelper.GetFullPath(RootDirType.InputDir, file.FilePath, allowAbsolute: true);
                    if (!File.Exists(file.FilePath))
                    {
                        return ServiceResult<List<int>>.Failure($"输入文件不存在: {file.FilePath}", HttpStatusCode.NotFound);
                    }
                }

                var taskType = type.ToLower() == "mux" ? TaskType.Mux : TaskType.QualityCheck;
                var output = taskType == TaskType.Mux ? GetOutputByInput(request, 0) : null;
                var arg = taskType == TaskType.Mux ? request.Parameter : null;

                var t = await taskRepository.AddTaskAsync(taskType, inputs, output, arg);
                ids.Add(t.Id);
                break;

            case nameof(TaskType.Concat):
                ValidateInputs(request, min: 2);
                foreach (var file in inputs)
                {
                    file.FilePath = filePathHelper.GetFullPath(RootDirType.InputDir, file.FilePath, allowAbsolute: true);
                    if (!File.Exists(file.FilePath))
                    {
                        return ServiceResult<List<int>>.Failure($"输入文件不存在: {file.FilePath}", HttpStatusCode.NotFound);
                    }
                }

                var concatTask = await taskRepository.AddTaskAsync(TaskType.Concat, inputs,
                    GetOutputByInput(request, 0), request.Parameter);
                ids.Add(concatTask.Id);
                break;

            case nameof(TaskType.Custom):
                if (request.Parameter?.Extra == null)
                {
                    return ServiceResult<List<int>>.Failure("自定义任务需要额外参数", HttpStatusCode.BadRequest);
                }

                var customTask =
                    await taskRepository.AddTaskAsync(TaskType.Custom, null, null, request.Parameter);
                ids.Add(customTask.Id);
                break;

            default:
                return ServiceResult<List<int>>.Failure($"不支持的任务类型: {type}", HttpStatusCode.BadRequest);
            // throw new HttpStatusCodeException($"不支持的任务类型: {type}", System.Net.HttpStatusCode.BadRequest);
        }

        // 批量新增（Code 类型可能是循环创建多个）只在这里通知一次，避免一次请求推 N 条
        if (ids.Count > 0)
        {
            notifier.Notify(TaskChangeKind.Tasks);
        }

        return ids;
    }

    public async Task<TaskStatusChangeResult> CancelTasksAsync(ICollection<int> ids)
    {
        TaskStatusChangeResult result = new TaskStatusChangeResult();
        var allIds = await taskRepository.GetTaskIdsAsync();
        // ids 去重：重复 id 会导致 ToDictionary 抛异常（P3-5）
        var requestIdToTask = (await taskRepository.GetTasksAsync(ids.Distinct().ToList())).ToDictionary(p => p.Id);
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
        if (result.AffectedRows > 0)
        {
            notifier.Notify(TaskChangeKind.Tasks);
        }

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
        if (result.AffectedRows > 0)
        {
            notifier.Notify(TaskChangeKind.Tasks);
        }

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
        if (result.AffectedRows > 0)
        {
            notifier.Notify(TaskChangeKind.Tasks);
        }

        return result;
    }


    /// <summary>
    /// 取某个输入对应的输出路径
    /// </summary>
    /// <param name="request">请求</param>
    /// <param name="inputIndex">输入序号</param>
    /// <param name="ignoreExplicitOutput">
    /// 是否忽略请求里填的输出文件名。一次提交多个输入、且每个输入各建一个任务（Transcode 多输入）时为 true：
    /// 那种场景下共用同一个目标名会让多个任务写同一个文件，必须各按自己的输入文件名命名
    /// </param>
    /// <param name="usedOutputs">本批次已用过的输出路径，用于把重名目标区分开；可为 null</param>
    private string GetOutputByInput(TaskDto request, int inputIndex, bool ignoreExplicitOutput = false,
        ISet<string> usedOutputs = null)
    {
        Debug.Assert(inputIndex >= 0 && inputIndex < request.Inputs.Count);
        //忽略统一的输出名时置空，落到下面"按输入文件名"的分支
        string output = ignoreExplicitOutput ? null : request.Output;

        if (string.IsNullOrWhiteSpace(output))
        {
            if (request.Inputs.Count <= 0)
            {
                return output;
            }

            //如果没有输出文件名，则使用输入文件名；输入为绝对路径时仅取文件名，确保输出落在 OutputDir 下
            var input = request.Inputs[inputIndex];
            if (input?.FilePath != null)
            {
                // 同样解析成 OutputDir 下的绝对路径，与下方 else 分支保持同一形态：直接 Path.Combine
                // 配置原值，在配置写成相对示例值（"output"）时会存出相对路径，同一列出现"相对/绝对"两种形态
                output = filePathHelper.GetFullPath(RootDirType.OutputDir, Path.GetFileName(input.FilePath));
            }
        }
        else
        {
            // 相对路径补充为 OutputDir 下的完整路径（含文件名）；绝对路径由 GetFullPath 拒绝（P3-1 路径安全）
            output = filePathHelper.GetFullPath(RootDirType.OutputDir, output);
        }

        if (usedOutputs != null && output != null)
        {
            output = FileSystemHelper.GetUniquePathInBatch(output, usedOutputs);
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