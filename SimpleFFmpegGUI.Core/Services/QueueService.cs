using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Data;
using SimpleFFmpegGUI.Models.Entities;
using Task = System.Threading.Tasks.Task;
using Tasks = System.Threading.Tasks;
using TaskStatus = SimpleFFmpegGUI.Enums.TaskStatus;

namespace SimpleFFmpegGUI.Services
{
    public class QueueService
    {
        private readonly IDbContextFactory<FFmpegDbContext> dbFactory;
        private readonly DbLoggerService logger;
        private readonly IServiceScopeFactory scopeFactory;
        private volatile bool cancelQueue = false;
        private Timer queueTimer; // 定时器
        private int runningFlag = 0;
        private DateTime? scheduleTime = null;
        private List<FFmpegTaskService> taskProcessManagers = new List<FFmpegTaskService>();

        public QueueService(IDbContextFactory<FFmpegDbContext> dbFactory,
            DbLoggerService logger,
            IServiceScopeFactory scopeFactory
        )
        {
            this.dbFactory = dbFactory;
            this.logger = logger;
            this.scopeFactory = scopeFactory;
            queueTimer = new Timer(QueueTimerCallback, null,
#if DEBUG
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)
#else
                TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60)
#endif
            );
        }

        /// <summary>
        /// 任务发生改变
        /// </summary>
        public event NotifyCollectionChangedEventHandler TaskManagersChanged;

        /// <summary>
        /// 主队列任务
        /// </summary>
        public FFmpegTaskService MainQueueManager => Managers.FirstOrDefault(p => p.Task == MainQueueTask);

        /// <summary>
        /// 主队列的Task
        /// </summary>
        public TaskEntity MainQueueTask { get; private set; }

        /// <summary>
        /// 所有任务
        /// </summary>
        public IReadOnlyList<FFmpegTaskService> Managers => taskProcessManagers.AsReadOnly();

        /// <summary>
        /// 独立任务
        /// </summary>
        public IEnumerable<TaskEntity> StandaloneTasks =>
            Managers.Where(p => p.Task != MainQueueTask).Select(p => p.Task);

        /// <summary>
        /// 所有任务
        /// </summary>
        public IEnumerable<TaskEntity> Tasks => Managers.Select(p => p.Task);

        /// <summary>
        /// 取消主队列
        /// </summary>
        public void Cancel()
        {
            CheckMainQueueProcessingTaskManager();
            cancelQueue = true;

            MainQueueManager.Cancel();
        }

        /// <summary>
        /// 取消主队列
        /// </summary>
        public Task CancelAsync()
        {
            CheckMainQueueProcessingTaskManager();
            cancelQueue = true;

            return MainQueueManager.CancelAsync();
        }

        /// <summary>
        /// 取消计划的队列
        /// </summary>
        public void CancelQueueSchedule()
        {
            scheduleTime = null;
        }

        public DateTime? GetQueueScheduleTime()
        {
            return scheduleTime;
        }

        public void ResumeMainQueue()
        {
            CheckMainQueueProcessingTaskManager();
            MainQueueManager.Resume();
        }

        public async Task RunQueueAsync()
        {
            if (Interlocked.Exchange(ref runningFlag, 1) == 1)
            {
                logger.Warn("队列正在运行，开始队列失败");
                return;
            }

            try
            {
                scheduleTime = null;
                logger.Info("开始队列");
                while (!cancelQueue)
                {
                    TaskEntity task;
                    await using (var db = await dbFactory.CreateDbContextAsync())
                    {
                        if (!await db.Tasks.Where(p => p.IsDeleted == false && p.Status == TaskStatus.Queue).AnyAsync())
                        {
                            break;
                        }

                        task = await db.Tasks.Where(p => p.IsDeleted == false && p.Status == TaskStatus.Queue)
                            .OrderBy(p => p.CreateTime).FirstOrDefaultAsync();
                    }

                    Debug.Assert(task != null, "task != null");
                    await ProcessTaskAsync(task, true);
                }
            }
            finally
            {
                Interlocked.Exchange(ref runningFlag, 0);
            }

            bool cancelManually = cancelQueue;
            cancelQueue = false;
            logger.Info("队列完成");
            using var scope = scopeFactory.CreateScope();
            var power = scope.ServiceProvider.GetRequiredService<PowerService>();
            if (!cancelManually && power.ShutdownAfterQueueFinished)
            {
                power.Shutdown();
            }
        }

        /// <summary>
        /// 开始独立任务
        /// </summary>
        /// <param name="id"></param>
        /// <exception cref="Exception"></exception>
        public async Task RunStandaloneAsync(int id)
        {
            TaskEntity task = null;

            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                task = await db.Tasks.FindAsync(id) ?? throw new Exception("找不到ID为" + id + "的任务");
            }

            if (task.Status != TaskStatus.Queue)
            {
                throw new Exception("任务的状态不正确，不可开始任务");
            }

            if (Tasks.Any(p => p.Id == task.Id))
            {
                throw new Exception("任务正在进行中，但状态不是正在处理中");
            }

            logger.Info(task, "开始独立任务");
            await ProcessTaskAsync(task, false);
            logger.Info(task, "独立任务完成");
        }

        /// <summary>
        /// 计划一个未来某个时刻开始队列的任务
        /// </summary>
        /// <param name="time"></param>
        /// <exception cref="ArgumentException"></exception>
        public void ScheduleQueue(DateTime time)
        {
            scheduleTime = time;
        }

        /// <summary>
        /// 开始队列
        /// </summary>
        public void StartQueue()
        {
            RunQueueAsync()
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        logger.Error($"队列运行错误：{t.Exception}");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
        }

        public void StartStandalone(int id)
        {
            RunStandaloneAsync(id)
                .ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        logger.Error($"独立运行{id}错误：{t.Exception}");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// 暂停主任务
        /// </summary>
        public void SuspendMainQueue()
        {
            CheckMainQueueProcessingTaskManager();
            MainQueueManager.Suspend();
        }

        private void AddManager(TaskEntity task, FFmpegTaskService ffmpegManager, bool main)
        {
            taskProcessManagers.Add(ffmpegManager);
            if (main)
            {
                MainQueueTask = task;
            }

            TaskManagersChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, ffmpegManager));
        }

        private void CheckMainQueueProcessingTaskManager()
        {
            if (Managers.All(p => p.Task != MainQueueTask))
            {
                throw new Exception("主队列未运行或当前任务正在准备中");
            }
        }

        private async Task ProcessTaskAsync(TaskEntity task, bool main)
        {
            using var scope = scopeFactory.CreateScope();

            FFmpegTaskService ffmpegManager =
                scope.ServiceProvider.GetRequiredService<IFFmpegTaskServiceFactory>().Create(task);
            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                var rows = await db.Tasks
                    .Where(t => t.Id == task.Id && t.Status == TaskStatus.Queue)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.Status, TaskStatus.Processing)
                        .SetProperty(t => t.StartTime, DateTime.Now)
                        .SetProperty(t => t.Message, "")
                        .SetProperty(t => t.FFmpegArguments, "")
                    );

                if (rows == 0)
                {
                    // 被别人抢走了
                    return;
                }
            }

            AddManager(task, ffmpegManager, main);
            try
            {
                await ffmpegManager.RunAsync();
                task.Status = TaskStatus.Done;
            }
            catch (Exception ex)
            {
                if (task.Status != TaskStatus.Cancel)
                {
                    logger.Error(task, "运行错误：" + ex.ToString());
                    task.Status = TaskStatus.Error;
                    task.Message = ex is FFmpegArgumentException
                        ? ex.Message
                        : ffmpegManager.Process?.GetErrorMessage() ?? ex.Message;
                }
                else
                {
                    logger.Warn(task, "任务被取消");
                }
            }

            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                var thisDbTask = await db.Tasks.FindAsync(task.Id);
                if (thisDbTask == null)
                {
                    logger.Warn($"找不到数据库中ID为{task.Id}的任务");
                    return;
                }

                thisDbTask.Status = task.Status;
                thisDbTask.Message = task.Message;
                thisDbTask.FinishTime = DateTime.Now;
                db.Update(thisDbTask);
                await db.SaveChangesAsync();
            }

            RemoveManager(task, ffmpegManager, main);
        }

        private void QueueTimerCallback(object state)
        {
            // 如果当前的计划ID有效，则开始执行队列任务
            if (scheduleTime != null && scheduleTime < DateTime.Now)
            {
                StartQueue();
            }
        }

        private void RemoveManager(TaskEntity task, FFmpegTaskService ffmpegManager, bool main)
        {
            if (!taskProcessManagers.Remove(ffmpegManager))
            {
                throw new Exception("管理器未在管理器集合中");
            }

            if (main)
            {
                MainQueueTask = null;
            }

            TaskManagersChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, ffmpegManager));
        }
    }
}