using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Services;
using System;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Realtime
{
    /// <summary>
    /// 队列状态的唯一出处：既给实时推送用，也给 HTTP 兜底（<c>GET api/Queue/State</c>）用，
    /// 保证两条路的数据形状与序号一致。
    /// <para>
    /// 注册为单例，内部维护推送序号 <see cref="CreateStatusWithNextSeq"/>：前端只接受序号更大的消息，
    /// 这样"降级轮询期间收到的 HTTP 结果"与"重连后收到的推送"之间不会互相用旧数据覆盖。
    /// </para>
    /// <para>
    /// 捕获了瞬时服务 <c>TaskRepository</c>：它无状态（只持有单例的 DbContextFactory），当前是安全的；
    /// 将来若给它加上 scoped/瞬时的依赖，这里要改成用 IServiceScopeFactory 取。
    /// </para>
    /// </summary>
    public class QueueStateProvider(QueueService queue, TaskRepository taskRepository)
    {
        // 序号从"当前毫秒时间戳"起步而不是 0：服务端重启后新序号一定比旧的更大，
        // 否则前端会把新服务端的状态当成旧数据全部丢掉（它只认更大的序号），界面就冻住了。
        // 用毫秒而不是 Ticks 是因为 JS 的 Number 只有 53 位精度，Ticks 量级会丢精度导致比较失效
        private long seq = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        /// <summary>保护"读状态 + 取序号"这一对操作，见 <see cref="CreateStatusWithNextSeq"/></summary>
        private readonly object seqLock = new();

        /// <summary>
        /// 读取一次队列状态并分配一个新的序号。
        /// <para>
        /// "读状态"与"取序号"必须在同一把锁里完成：否则可能读到旧状态、被抢占、等别的推送先取走较小的序号，
        /// 自己随后拿到一个**更大**的序号把旧状态发出去——前端只接受更大的序号，于是会把旧状态当成最新数据；
        /// 而队列结束后既不再采样、队列起止也不推状态，没有下一次推送来纠正，界面会一直停在"处理中"。
        /// </para>
        /// </summary>
        public QueueStatusPushDto CreateStatusWithNextSeq()
        {
            lock (seqLock)
            {
                QueueStatusPushDto status = CreateStatus();
                status.Seq = ++seq;
                return status;
            }
        }

        /// <summary>
        /// HTTP 兜底用的合并状态：一次请求拿齐状态、是否有待处理任务、计划开始时间。
        /// <para>
        /// 这里的状态是"请求那一刻"读出来的，比任何已经发出的推送都新，所以直接吃掉一个新序号
        /// （比当前已发出的所有推送都大）——前端据此忽略掉那些更旧的推送。
        /// </para>
        /// </summary>
        public async Task<QueueStateDto> GetStateAsync()
        {
            return new QueueStateDto
            {
                Status = CreateStatusWithNextSeq(),
                HasPending = await taskRepository.HasQueueTasksAsync(),
                ScheduleTime = queue.GetQueueScheduleTime(),
            };
        }

        /// <summary>
        /// 读取一次队列状态（不含序号：去重比较时序号必须不参与，否则每条都不相同，去重永远不生效）。
        /// 空闲时返回"什么都不在做"的空状态。
        /// </summary>
        private QueueStatusPushDto CreateStatus()
        {
            var manager = queue.MainQueueManager;
            if (manager == null)
            {
                return QueueStatusPushDto.From(new StatusDto());
            }

            var status = QueueStatusPushDto.From(manager.GetStatus());
            // 管理器已存在（队列正在跑这个任务）就按"正在处理"上报：从管理器建立到 ffmpeg 进程真正启动
            // 之间有一个很短的窗口，此时 StatusDto 的 IsProcessing 是 false，会让界面把状态栏收起来
            // 再弹出来（点"开始队列"时看得见的闪烁）
            status.IsProcessing = true;
            return status;
        }
    }
}
