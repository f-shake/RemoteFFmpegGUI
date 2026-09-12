using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Events;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Services;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Realtime
{
    /// <summary>
    /// 把 Core 的状态变化翻译成 SignalR 消息推给所有浏览器。
    /// <para>
    /// 两条来源：① 事件驱动 —— 订阅 <see cref="ITaskChangeNotifier"/>，任务/队列一变就推（保证"点开始"立刻有反馈）；
    /// ② 节拍采样 —— 处理中每秒采样一次队列状态（进度只能靠采样，ffmpeg 的输出行约 0.5 秒一行，全推太费）。
    /// 空闲时两者都不发消息，连接靠 SignalR 自身的 ping 保持。
    /// </para>
    /// <para>
    /// 推送失败只记日志：它绝不能影响队列执行（通知是在队列执行线程上发出的）。
    /// </para>
    /// </summary>
    public class RealtimeBroadcaster : BackgroundService
    {
        private readonly IHubContext<QueueHub> hub;
        private readonly QueueService queue;
        private readonly QueueStateProvider stateProvider;
        private readonly TaskRepository taskRepository;
        private readonly ITaskChangeNotifier notifier;
        private readonly ILogger<RealtimeBroadcaster> logger;
        /// <summary>
        /// 去重比较用的序列化选项：**直接复用 Hub 自己的 JSON 选项**（不要另建一份），
        /// 否则"推送用的选项"与"去重用的选项"各写一套，早晚漂移（例如忘了加 TimeSpan 转换器就会抛异常）
        /// </summary>
        private readonly JsonSerializerOptions statusJsonOptions;
        /// <summary>
        /// 保护去重用的上一条内容快照（读状态与取序号由 <c>QueueStateProvider</c> 自己加锁）
        /// </summary>
        private readonly object lastStatusJsonLock = new();
        private string lastStatusJson;

        // TaskRepository 是瞬时服务，但它无状态（只持有 DbContextFactory），被本单例捕获是安全的；
        // 将来若给它加上 scoped/瞬时的依赖，这里要改成用 IServiceScopeFactory 取
        public RealtimeBroadcaster(IHubContext<QueueHub> hub,
            QueueService queue,
            QueueStateProvider stateProvider,
            TaskRepository taskRepository,
            ITaskChangeNotifier notifier,
            IOptions<JsonHubProtocolOptions> hubJsonOptions,
            ILogger<RealtimeBroadcaster> logger)
        {
            this.hub = hub;
            this.queue = queue;
            this.stateProvider = stateProvider;
            this.taskRepository = taskRepository;
            this.notifier = notifier;
            this.logger = logger;
            statusJsonOptions = hubJsonOptions.Value.PayloadSerializerOptions;
            notifier.Changed += OnTaskChanged;
        }

        /// <summary>
        /// 任务/队列发生了变化。可能在任何线程上触发（请求线程、队列执行线程），
        /// 这里把推送丢到线程池上执行——不能占用调用方线程：`Notify` 是同步遍历订阅方，
        /// 而异步方法在被第一次 await 拦住之前一直跑在调用方线程上（那一步包含网络写和一次数据库查询）。
        /// <para>
        /// 计划时间必须在<b>这里</b>（写入线程，也就是调用 <c>Notify</c> 的线程）读出来带上，
        /// 不能等丢进线程池后再去读 QueueService 的字段——那会变成跨线程读一个 16 字节的可空结构。
        /// </para>
        /// </summary>
        private void OnTaskChanged(object sender, TaskChangeEventArgs e)
        {
            TaskChangeKind kind = e.Kind;
            DateTime? scheduleTime = kind == TaskChangeKind.Schedule ? queue.GetQueueScheduleTime() : null;
            ThreadPool.QueueUserWorkItem(_ => _ = BroadcastChangeSafeAsync(kind, scheduleTime));
        }

        private async Task BroadcastChangeSafeAsync(TaskChangeKind kind, DateTime? scheduleTime)
        {
            try
            {
                if (kind == TaskChangeKind.Schedule)
                {
                    await hub.Clients.All.SendAsync("scheduleChanged", new { scheduleTime });
                    return;
                }

                // 任务清单可能有变化，顺带把 hasPending 一起发过去，前端省一次请求
                await hub.Clients.All.SendAsync("tasksChanged", new
                {
                    reason = kind.ToString(),
                    hasPending = await taskRepository.HasQueueTasksAsync(),
                });

                // 任务的开始与结束会改变"是否正在处理"，立即补一条状态，不等下一个采样点（最多 1 秒）。
                // 队列起止（QueueStarted/QueueFinished）不在这里推状态：前者主队列尚未建立、
                // 后者已清空，状态由任务开始/结束时的那条 Tasks 通知负责，避免推出"没在处理"的错误快照
                if (kind == TaskChangeKind.Tasks)
                {
                    await BroadcastStatusAsync(dedup: false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "实时推送失败：{Kind}", kind);
            }
        }

        /// <summary>
        /// 推送一次队列状态。<paramref name="dedup"/> 为 true 时，与上一条内容相同则跳过发送
        /// （定时采样用，例如暂停期间状态一直不变）。
        /// </summary>
        private async Task BroadcastStatusAsync(bool dedup)
        {
            // 读状态与取序号由 QueueStateProvider 在同一把锁内完成，保证"序号顺序 = 状态新旧顺序"
            QueueStatusPushDto status = stateProvider.CreateStatusWithNextSeq();
            // 比较/记录时把序号抹掉：序号每条都不同，不抹掉的话去重永远不生效。
            // （被去重掉的采样仍然消耗了一个序号，序号出现空档无害——前端只比较大小）
            long sentSeq = status.Seq;
            status.Seq = 0;
            string json = JsonSerializer.Serialize(status, statusJsonOptions);
            status.Seq = sentSeq;
            lock (lastStatusJsonLock)
            {
                // 事件推送（dedup=false）也要刷新这份快照，否则采样器下一拍会把内容完全相同的状态再推一条
                if (dedup && json == lastStatusJson)
                {
                    return;
                }

                lastStatusJson = json;
            }

            await hub.Clients.All.SendAsync("queueStatus", status);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("实时推送服务已启动");
            using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        if (queue.MainQueueManager == null)
                        {
                            // 空闲不发消息；同时清掉去重快照，避免下一轮队列开始时拿上一轮的状态去比较
                            lock (lastStatusJsonLock)
                            {
                                lastStatusJson = null;
                            }

                            continue;
                        }

                        await BroadcastStatusAsync(dedup: true);
                    }
                    catch (Exception ex)
                    {
                        // 采样/推送出一次意外异常不能被抛出循环：托管服务的异常默认会 StopHost，
                        // 也就是"推一条状态失败"会把整个 Web 服务停掉
                        logger.LogError(ex, "推送队列状态失败");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 宿主正常停机
            }
        }

        public override void Dispose()
        {
            notifier.Changed -= OnTaskChanged;
            base.Dispose();
        }
    }
}
