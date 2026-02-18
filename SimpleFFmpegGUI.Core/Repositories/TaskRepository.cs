using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using TaskStatus = SimpleFFmpegGUI.Model.TaskStatus;

namespace SimpleFFmpegGUI.Repositories;

public class TaskRepository
{
    private static readonly object lockObj = new object();

    private static bool taskChecked = false;

    private readonly FFmpegDbContext db;


    public TaskRepository(FFmpegDbContext db)
    {
        this.db = db;
        if (!taskChecked)
        {
            lock (lockObj)
            {
                taskChecked = true;
                foreach (var item in db.Tasks.Where(p => p.Status == TaskStatus.Processing))
                {
                    item.Status = TaskStatus.Error;
                    item.Message = "状态异常：启动时处于正在运行状态";
                }

                db.SaveChanges();
            }
        }
    }

    public async Task<TaskInfo> AddTaskAsync(TaskType type, List<InputArguments> path, string outputPath,
        OutputArguments arg)
    {
        var task = new TaskInfo()
        {
            Type = type,
            Inputs = path,
            Output = outputPath,
            Arguments = arg
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    public async Task<List<TaskInfo>> GetCurrentTasksAsync(DateTime startTime)
    {
        var tasks = db.Tasks.Where(p => p.IsDeleted == false);
        var runningTasks = await tasks.Where(p => p.Status == TaskStatus.Processing).ToListAsync();
        var queueTasks = await tasks.Where(p => p.Status == TaskStatus.Queue).ToListAsync();
        var doneTasks = await tasks.Where(p => p.Status == TaskStatus.Done).Where(p => p.StartTime > startTime)
            .ToListAsync();
        var errorTasks = await tasks.Where(p => p.Status == TaskStatus.Error).Where(p => p.StartTime > startTime)
            .ToListAsync();
        var cancelTasks = await tasks.Where(p => p.Status == TaskStatus.Cancel).Where(p => p.StartTime > startTime)
            .ToListAsync();
        return [.. runningTasks, .. queueTasks, .. doneTasks, .. errorTasks, .. cancelTasks];
    }

    public async Task<TaskInfo> GetTaskAsync(int id)
    {
        var task = await db.Tasks.FindAsync(id);
        return task;
    }

    public async Task<PagedListDto<TaskInfo>> GetTasksAsync(TaskStatus? status = null, int skip = 0, int take = 0)
    {
        IQueryable<TaskInfo> tasks = db.Tasks
            .Where(p => p.IsDeleted == false);
        if (status.HasValue)
        {
            tasks = tasks.Where(p => p.Status == status);
            tasks = status.Value switch
            {
                TaskStatus.Queue => tasks.OrderBy(p => p.CreateTime),
                TaskStatus.Processing or TaskStatus.Done => tasks.OrderByDescending(p => p.StartTime),
                _ => tasks.OrderByDescending(p => p.CreateTime),
            };
        }
        else
        {
            tasks = tasks.OrderByDescending(p => p.CreateTime);
        }

        int count = await tasks.CountAsync();
        if (skip > 0)
        {
            tasks = tasks.Skip(skip);
        }

        if (take > 0)
        {
            tasks = tasks.Take(take);
        }

        return new PagedListDto<TaskInfo>(await tasks.ToListAsync(), count);
    }

    public async Task<List<TaskInfo>> GetTasksAsync(ICollection<int> ids)
    {
        return await db.Tasks
            .Where(e => ids.Any(id => id == e.Id))
            .ToListAsync();
    }

    public Task<bool> HasQueueTasksAsync()
    {
        return db.Tasks.AnyAsync(p => p.IsDeleted == false && p.Status == TaskStatus.Queue);
    }


    public async Task<int> SoftDeleteAsync(ICollection<int> ids)
    {
        switch (ids.Count)
        {
            case 0:
                return 0;
            case 1:
                var id = ids.First();
                return await db.Tasks.Where(p => p.Id == id)
                    .ExecuteUpdateAsync(p => p.SetProperty(t => t.IsDeleted, true));
            default:
                return await db.Tasks.Where(p => ids.Any(id => id == p.Id))
                    .ExecuteUpdateAsync(p => p.SetProperty(t => t.IsDeleted, true));
        }
    }

    public Task<int> UpdateStatusAsync(ICollection<int> ids, TaskStatus status)
    {
        return db.Tasks.Where(p => ids.Any(id => id == p.Id))
            .ExecuteUpdateAsync(p => p.SetProperty(t => t.Status, status));
    }
}