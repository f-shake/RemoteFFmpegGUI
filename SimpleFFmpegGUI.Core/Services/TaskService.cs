using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskStatus = SimpleFFmpegGUI.Model.TaskStatus;

namespace SimpleFFmpegGUI.Services;

public class TaskService(TaskRepository taskRepository, QueueService queue)
{
    public async Task ResetTaskAsync(int id)
    {
        if (queue.Tasks.Any(p => p.Id == id))
        {
            throw new Exception($"ID为{id}的任务正在进行中");
        }
        var result = await taskRepository.UpdateStatusAsync(id, TaskStatus.Queue);
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

    public async Task CancelTaskAsync(int id)
    {
        TaskInfo task = await taskRepository.GetTaskAsync(id) ?? throw new ArgumentException($"找不到ID为{id}的任务");
        CheckCancelingTask(task);
        if (queue.Tasks.Any(p => p.Id == task.Id))
        {
            queue.Managers.First(p => p.Task.Id == task.Id).Cancel();
        }
        await taskRepository.UpdateStatusAsync(id, TaskStatus.Cancel);
    }

    private void CheckCancelingTask(TaskInfo task)
    {
        int id = task.Id;
        if (task.Status == TaskStatus.Cancel)
        {
            throw new Exception($"ID为{id}的任务已被取消");
        }
        if (task.Status == TaskStatus.Done)
        {
            throw new Exception($"ID为{id}的任务已完成");
        }
        if (task.Status == TaskStatus.Error)
        {
            throw new Exception($"ID为{id}的任务已完成并出现错误");
        }
    }

    public async Task<int> TryCancelTasksAsync(ICollection<int> ids)
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

    public async Task DeleteTaskAsync(int id)
    {
        if (queue.Tasks.Any(p => p.Id == id))
        {
            queue.Managers.First(p => p.Task.Id == id).Cancel();
        }
        await taskRepository.SoftDeleteAsync(id);
    }

    public async Task<int> TryDeleteTasksAsync(ICollection<int> ids)
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
}