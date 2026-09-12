using System;

namespace SimpleFFmpegGUI.Dto
{
    /// <summary>
    /// 实时推送用的队列状态（<see cref="StatusDto"/> 的瘦身版）。
    /// <para>
    /// 属性名与 <see cref="StatusDto"/> <b>完全一致</b>，前端对"推送"与"HTTP 兜底"两条路可以用同一套解析逻辑。
    /// </para>
    /// <para>
    /// 裁掉的都是前端状态栏不读的字段（<c>Task.Parameters</c>、<c>Task.FFmpegArguments</c>、
    /// <c>Task.Message</c>/<c>RealOutput</c>/各种时间戳等），一条约 400 字节，
    /// 而原样推送 <see cref="StatusDto"/> 是 1.5~3 KB（1 秒一条 ≈ 10 MB/小时，比改造前的轮询还费）。
    /// </para>
    /// </summary>
    public class QueueStatusPushDto
    {
        /// <summary>
        /// 自增序号。前端只接受序号更大的消息，避免"降级轮询的结果"与"推送"并存时旧数据盖掉新数据。
        /// 由 <c>QueueStateProvider</c> 分配；本 DTO 自身不关心它的值。
        /// </summary>
        public long Seq { get; set; }

        public bool IsProcessing { get; set; }

        public bool IsPaused { get; set; }

        /// <summary>是否有更详细的信息（进度）</summary>
        public bool HasDetail { get; set; }

        /// <summary>码率</summary>
        public string Bitrate { get; set; }

        /// <summary>转码帧速度（-1 表示未知）</summary>
        public double Fps { get; set; }

        /// <summary>当前帧</summary>
        public int Frame { get; set; }

        /// <summary>质量（-1 表示未知）</summary>
        public double Q { get; set; }

        /// <summary>当前文件大小</summary>
        public string Size { get; set; }

        /// <summary>处理速度</summary>
        public string Speed { get; set; }

        /// <summary>当前视频里的时间（配合快照使用）</summary>
        public TimeSpan Time { get; set; }

        /// <summary>原始输出（「详细进度」弹窗里显示）</summary>
        public string LastOutput { get; set; }

        /// <summary>进度信息</summary>
        public ProgressDto Progress { get; set; }

        /// <summary>任务摘要</summary>
        public QueueTaskBriefDto Task { get; set; }

        /// <summary>
        /// 由完整状态生成瘦身状态（调用方保证 <paramref name="status"/> 非空，空闲时传 <c>new StatusDto()</c>）。
        /// </summary>
        public static QueueStatusPushDto From(StatusDto status)
        {
            return new QueueStatusPushDto
            {
                IsProcessing = status.IsProcessing,
                IsPaused = status.IsPaused,
                HasDetail = status.HasDetail,
                Bitrate = status.Bitrate,
                Fps = status.Fps,
                Frame = status.Frame,
                Q = status.Q,
                Size = status.Size,
                Speed = status.Speed,
                Time = status.Time,
                LastOutput = status.LastOutput,
                Progress = status.Progress,
                Task = QueueTaskBriefDto.From(status.Task),
            };
        }
    }
}
