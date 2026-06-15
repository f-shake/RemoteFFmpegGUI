using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Data;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;
using TaskStatus = SimpleFFmpegGUI.Enums.TaskStatus;

namespace SimpleFFmpegGUI.Repositories;

public class TaskRepository(IDbContextFactory<FFmpegDbContext> dbFactory)
{
    public async Task<TaskEntity> AddTaskAsync(TaskType type, List<InputParameters> path, string outputPath,
        OutputParameters arg)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var task = new TaskEntity()
        {
            Type = type,
            Inputs = path,
            Output = outputPath,
            Parameters = arg
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    public async Task<List<TaskEntity>> GetCurrentTasksAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var runningTasks = await db.Tasks
            .Where(p => !p.IsDeleted && p.Status == TaskStatus.Processing)
            .ToListAsync();
        var queueTasks = await db.Tasks
            .Where(p => !p.IsDeleted && p.Status == TaskStatus.Queue)
            .ToListAsync();
        return [.. runningTasks, .. queueTasks];
    }

    public async Task<TaskEntity> GetTaskAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Tasks.FindAsync(id);
    }

    public async Task<PagedListResponse<TaskEntity>> GetTasksAsync(TaskQueryDto query)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        IQueryable<TaskEntity> tasks = db.Tasks
            .Where(p => p.IsDeleted == false);
        if (query.Status.HasValue)
        {
            var status = (TaskStatus)query.Status.Value;
            tasks = tasks.Where(p => p.Status == status);
            tasks = status switch
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
        var skip = (query.Page - 1) * query.PageSize;
        var take = query.PageSize;
        if (skip > 0)
        {
            tasks = tasks.Skip(skip);
        }

        if (take > 0)
        {
            tasks = tasks.Take(take);
        }

        return new PagedListResponse<TaskEntity>(await tasks.ToListAsync(), count, query.Page, query.PageSize);
    }

    public async Task<List<TaskEntity>> GetTasksAsync(ICollection<int> ids)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Tasks
            .Where(e => ids.Any(id => id == e.Id))
            .ToListAsync();
    }

    public async Task<bool> HasQueueTasksAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Tasks.AnyAsync(p => p.IsDeleted == false && p.Status == TaskStatus.Queue);
    }

    public async Task<int> SoftDeleteAsync(ICollection<int> ids)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
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

    public async Task<int> UpdateStatusAsync(ICollection<int> ids, TaskStatus status)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Tasks.Where(p => ids.Any(id => id == p.Id))
            .ExecuteUpdateAsync(p => p.SetProperty(t => t.Status, status));
    }

    internal async Task<ISet<int>> GetTaskIdsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Tasks.Select(p => p.Id).ToHashSetAsync();
    }
}
