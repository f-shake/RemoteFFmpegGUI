using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Data;
using SimpleFFmpegGUI.Events;
using SimpleFFmpegGUI.Models.Entities;
using Task = System.Threading.Tasks.Task;
using Tasks = System.Threading.Tasks;
using TaskStatus = SimpleFFmpegGUI.Enums.TaskStatus;

namespace SimpleFFmpegGUI.Services
{
    public class QueueService : IDisposable
    {
        private readonly IDbContextFactory<FFmpegDbContext> dbFactory;
        private readonly DbLoggerService logger;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ITaskChangeNotifier notifier;
        private volatile bool cancelQueue = false;
        private Timer queueTimer; // 定时器
        private int runningFlag = 0;
        private DateTime? scheduleTime = null;
        // 并发集合：队列与独立任务可能并行执行，避免并发修改 List 抛 InvalidOperationException（P2-3）
        private readonly ConcurrentDictionary<int, FFmpegTaskService> taskProcessManagers = new();

        public QueueService(IDbContextFactory<FFmpegDbContext> dbFactory,
            DbLoggerService logger,
            IServiceScopeFactory scopeFactory,
            ITaskChangeNotifier notifier
        )
        {
            this.dbFactory = dbFactory;
            this.logger = logger;
            this.scopeFactory = scopeFactory;
            this.notifier = notifier;
            // Release 下 10 秒 tick：计划任务启动精度从约 1 分钟提升到约 10 秒，
            // 且保证 Release 构建下运行集成测试（计划任务 +5s、轮询超时 30s）不会超时
            queueTimer = new Timer(QueueTimerCallback, null,
#if DEBUG
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)
#else
                TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)
#endif
            );
        }

        /// <summary>
        /// 任务发生改变。
        /// 注意：事件可能在任意线程（队列/独立任务执行线程）触发，订阅方需自行编组（如 WPF 的 Dispatcher）。
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
        /// 所有任务（快照）
        /// </summary>
        public IReadOnlyList<FFmpegTaskService> Managers => taskProcessManagers.Values.ToList();

        /// <summary>
        /// 独立任务（快照）
        /// </summary>
        public IEnumerable<TaskEntity> StandaloneTasks =>
            Managers.Where(p => p.Task != MainQueueTask).Select(p => p.Task);

        /// <summary>
        /// 所有任务（快照）
        /// </summary>
        public IEnumerable<TaskEntity> Tasks => Managers.Select(p => p.Task);

        /// <summary>
        /// 取消主队列
        /// </summary>
        public void Cancel()
        {
            // 幂等：队列未运行或任务恰好完成时取消不报错（原实现抛 500）。
            // 标志使运行中的队列循环在任务边界退出；残留标志由下次 RunQueueAsync 启动时复位
            cancelQueue = true;
            MainQueueManager?.Cancel();
        }

        /// <summary>
        /// 取消主队列
        /// </summary>
        public Task CancelAsync()
        {
            // 幂等：与 Cancel() 一致，队列未运行或任务恰好完成时不报错
            cancelQueue = true;
            var manager = MainQueueManager;
            return manager == null ? Task.CompletedTask : manager.CancelAsync();
        }

        /// <summary>
        /// 取消计划的队列
        /// </summary>
        public void CancelQueueSchedule()
        {
            scheduleTime = null;
            notifier.Notify(TaskChangeKind.Schedule);
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

            // 复位取消标志：上次队列结束后调用过 Cancel（队列已停止）会残留 true，防止阻止本次启动
            cancelQueue = false;

            bool cancelManually = false;
            try
            {
                scheduleTime = null;
                // 计划时间在队列开跑时清空（到点触发与手动开始都一样），通知前端同步清掉"已计划开始时间"
                notifier.Notify(TaskChangeKind.Schedule);
                logger.Info("开始队列");
                notifier.Notify(TaskChangeKind.QueueStarted);
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

                    if (task == null)
                    {
                        // 查询结果在取任务时被并发删除，结束本轮
                        break;
                    }

                    try
                    {
                        await ProcessTaskAsync(task, true);
                    }
                    catch (Exception ex)
                    {
                        // 单任务异常不终止队列：记日志并把任务标记为错误，避免队列卡死
                        logger.Error(task, $"队列执行任务异常：{ex}");
                        try
                        {
                            await using var errorDb = await dbFactory.CreateDbContextAsync();
                            // 状态可能已改为 Processing（ProcessTaskAsync 开头已落库），放宽条件确保异常任务能被标记为错误而非永久卡住
                            await errorDb.Tasks.Where(t => t.Id == task.Id
                                    && (t.Status == TaskStatus.Queue || t.Status == TaskStatus.Processing))
                                .ExecuteUpdateAsync(s => s
                                    .SetProperty(t => t.Status, TaskStatus.Error)
                                    .SetProperty(t => t.Message, ex.Message)
                                    .SetProperty(t => t.FinishTime, DateTime.Now));
                            notifier.Notify(TaskChangeKind.Tasks);
                        }
                        catch (Exception ex2)
                        {
                            // 连错误标记都无法落库，说明数据库异常，终止队列防止死循环
                            logger.Error($"标记任务失败异常：{ex2}");
                            break;
                        }
                    }
                }
                cancelManually = cancelQueue;
            }
            finally
            {
                cancelQueue = false;
                Interlocked.Exchange(ref runningFlag, 0);
                logger.Info("队列完成");
                notifier.Notify(TaskChangeKind.QueueFinished);
            }

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
            notifier.Notify(TaskChangeKind.Schedule);
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
            if (!taskProcessManagers.TryAdd(task.Id, ffmpegManager))
            {
                throw new Exception($"任务{task.Id}的管理器已存在");
            }

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
            // 任务已被置为"进行中"（上面已落库）且管理器已就位，通知任务清单与状态已变化。
            // 必须放在 AddManager 之后：此刻 MainQueueManager 才是非空，"正在处理中"的状态才拿得对
            notifier.Notify(TaskChangeKind.Tasks);
            try
            {
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
                    thisDbTask.RealOutput = task.RealOutput;
                    thisDbTask.FFmpegArguments = task.FFmpegArguments;
                    thisDbTask.FinishTime = DateTime.Now;
                    db.Update(thisDbTask);
                    await db.SaveChangesAsync();
                }
            }
            finally
            {
                // 无论结果如何都移除管理器，避免 DB 保存失败等异常路径导致管理器泄漏
                RemoveManager(task, ffmpegManager, main);
                // 任务已进入结束态（完成/取消/错误；异常路径下可能尚未落库）。放在 RemoveManager 之后，
                // 保证推送出去的状态快照与"管理器已清空"一致，前端不会收到"已结束但仍在处理中"的状态
                notifier.Notify(TaskChangeKind.Tasks);
            }
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
            if (!taskProcessManagers.TryRemove(task.Id, out _))
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

        public void Dispose()
        {
            queueTimer?.Dispose();
        }
    }
}