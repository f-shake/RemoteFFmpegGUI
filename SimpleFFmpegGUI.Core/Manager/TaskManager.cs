using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Logging;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskStatus = SimpleFFmpegGUI.Model.TaskStatus;

namespace SimpleFFmpegGUI.Manager
{
    public class TaskManager
    {
        private static bool taskChecked = false;
        private static readonly object lockObj = new object();
        private readonly FFmpegDbContext db;
        private readonly QueueManager queue;

        public TaskManager(FFmpegDbContext db,  QueueManager queueManager)
        {
            this.db = db;
            queue = queueManager;
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

        public async Task<TaskInfo> AddTaskAsync(TaskType type, List<InputArguments> path, string outputPath, OutputArguments arg)
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
            DbLogger.Info(task, "新建任务");
            return task;
        }

        public async Task<List<TaskInfo>> GetCurrentTasksAsync(DateTime startTime)
        {
            var tasks = db.Tasks.Where(p => p.IsDeleted == false);
            var runningTasks = await tasks.Where(p => p.Status == TaskStatus.Processing).ToListAsync();
            var queueTasks = await tasks.Where(p => p.Status == TaskStatus.Queue).ToListAsync();
            var doneTasks = await tasks.Where(p => p.Status == TaskStatus.Done).Where(p => p.StartTime > startTime).ToListAsync();
            var errorTasks = await tasks.Where(p => p.Status == TaskStatus.Done).Where(p => p.StartTime > startTime).ToListAsync();
            var cancelTasks = await tasks.Where(p => p.Status == TaskStatus.Done).Where(p => p.StartTime > startTime).ToListAsync();
            return [.. runningTasks, .. queueTasks, .. doneTasks, .. errorTasks, .. cancelTasks];
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

        public async Task<TaskInfo> GetTaskAsync(int id)
        {
            var task = await db.Tasks.FindAsync(id);
            return task;
        }

        public Task<bool> HasQueueTasksAsync()
        {
            return db.Tasks.AnyAsync(p => p.IsDeleted == false && p.Status == TaskStatus.Queue);
        }

        public async Task ResetTaskAsync(int id)
        {
            TaskInfo task = await db.Tasks.FindAsync(id) ?? throw new ArgumentException($"找不到ID为{id}的任务");
            if (queue.Tasks.Any(p => p.Id == id))
            {
                throw new Exception($"ID为{id}的任务正在进行中");
            }
            task.Status = TaskStatus.Queue;
            db.Update(task);
            await db.SaveChangesAsync();
        }

        public async Task<int> TryResetTasksAsync(IEnumerable<int> ids)
        {
            int count = 0;
            foreach (var id in ids)
            {
                TaskInfo task = await db.Tasks.FindAsync(id);
                if (task == null)
                {
                    continue;
                }
                if (queue.Tasks.Any(p => p.Id == id))
                {
                    continue;
                }
                task.Status = TaskStatus.Queue;
                db.Update(task);
                count++;
            }

            await db.SaveChangesAsync();
            return count;
        }

        public async Task CancelTaskAsync(int id)
        {
            TaskInfo task = await db.Tasks.FindAsync(id) ?? throw new ArgumentException($"找不到ID为{id}的任务");
            CheckCancelingTask(task);
            if (queue.Tasks.Any(p => p.Id == task.Id))
            {
                queue.Managers.First(p => p.Task.Id == task.Id).Cancel();
            }
            task.Status = TaskStatus.Cancel;
            db.Update(task);
            await db.SaveChangesAsync();
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

        public async Task<int> TryCancelTasksAsync(IEnumerable<int> ids)
        {
            int count = 0;
            foreach (var id in ids)
            {
                TaskInfo task = await db.Tasks.FindAsync(id);
                if (task == null)
                {
                    continue;
                }
                if (task.Status == TaskStatus.Cancel)
                {
                    continue;
                }
                if (task.Status == TaskStatus.Done)
                {
                    continue;
                }
                if (task.Status == TaskStatus.Error)
                {
                    continue;
                }
                if (queue.Tasks.Any(p => p.Id == task.Id))
                {
                    queue.Managers.First(p => p.Task.Id == task.Id).Cancel();
                }
                task.Status = TaskStatus.Cancel;
                db.Update(task);
                count++;
            }
            db.SaveChanges();
            return count;
        }

        public async Task DeleteTaskAsync(int id)
        {
            TaskInfo task = await db.Tasks.FindAsync(id) ?? throw new ArgumentException($"找不到ID为{id}的任务");
            if (queue.Tasks.Any(p => p.Id == task.Id))
            {
                queue.Managers.First(p => p.Task.Id == task.Id).Cancel();
            }
            task.IsDeleted = true;
            db.Update(task);
            await db.SaveChangesAsync();
        }

        public async Task<int> TryDeleteTasks(IEnumerable<int> ids)
        {
            int count = 0;
            foreach (var id in ids)
            {
                TaskInfo task = await db.Tasks.FindAsync(id);
                if (task == null)
                {
                    continue;
                }
                if (queue.Tasks.Any(p => p.Id == task.Id))
                {
                    queue.Managers.First(p => p.Task.Id == task.Id).Cancel();
                }
                task.IsDeleted = true;
                db.Update(task);
                count++;
            }
            db.SaveChanges();
            return count;
        }
    }
}