using Microsoft.AspNetCore.SignalR;

namespace SimpleFFmpegGUI.WebAPI.Realtime
{
    /// <summary>
    /// 队列实时推送的 Hub。
    /// <para>
    /// <b>故意没有任何方法</b>：消息全部由服务端推给客户端，客户端只负责连上来接收；
    /// 命令（开始/暂停/取消/建任务）仍走 HTTP，保留状态码、超时与可集成测试的特性。
    /// 定义一个具体的 Hub 类是为了能用 <see cref="IHubContext{T}"/> 做强类型广播。
    /// </para>
    /// <para>
    /// 鉴权不在 Hub 里做：Hub 是 SignalR 自管的路由，MVC 的 <c>AppActionFilter</c> 管不到它，
    /// 由 <c>Program.cs</c> 里的内联中间件在升级连接之前校验（与 AppActionFilter 同一套规则）。
    /// </para>
    /// </summary>
    public class QueueHub : Hub
    {
    }
}
