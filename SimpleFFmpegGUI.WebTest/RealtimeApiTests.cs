using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Events;
using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebTest;

/// <summary>
/// 实时推送（SignalR）的集成测试：鉴权、事件推送、HTTP 兜底接口的形状。
/// <para>
/// 与前端一致，只走 WebSocket（skipNegotiation）；凭据按 URL 查询参数传递（前端用 Cookie，
/// 两者走的是中间件里同一个分支）。
/// </para>
/// <para>
/// 单独跑（全量 dotnet test 会挂起，见 docs/release-process.md）：
/// <c>dotnet test SimpleFFmpegGUI.WebTest --filter FullyQualifiedName~RealtimeApiTests</c>
/// </para>
/// </summary>
public class RealtimeApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    private const string HubPath = "/api/queue-hub";
    private readonly SimpleFFmpegWebApplicationFactory webFactory = factory;

    private sealed record TasksChangedPayload(string Reason, bool HasPending);

    private sealed record ScheduleChangedPayload(DateTime? ScheduleTime);

    private sealed record StatusPayload(long Seq, bool IsProcessing, JsonElement Time);

    /// <summary>
    /// 不带凭据访问 Hub 应 401（与 AppActionFilter 同一套规则：测试环境配置了 Token）
    /// </summary>
    [Fact]
    public async Task TestHubWithoutTokenUnauthorizedAsync()
    {
        var response = await webFactory.CreateClient().GetAsync(HubPath);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 带上 correct token 时不应再被中间件拒绝（普通 GET 不是合法的 SignalR 握手，
    /// 因此这里只断言"不是 401"）
    /// </summary>
    [Fact]
    public async Task TestHubWithTokenNotRejectedAsync()
    {
        var response = await webFactory.CreateClient().GetAsync($"{HubPath}?access_token=Test_Token_123");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 凭据放在 Cookie 里（前端浏览器的实际用法）时同样不应被拒绝
    /// </summary>
    [Fact]
    public async Task TestHubWithCookieNotRejectedAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, HubPath);
        request.Headers.Add("Cookie", "token=Test_Token_123");

        var response = await webFactory.CreateClient().SendAsync(request);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 凭据放在 Authorization 头里（非 WebSocket 传输的客户端只能这样传）时，
    /// 正确的 token 通过、错误的 token 401
    /// </summary>
    [Fact]
    public async Task TestHubWithAuthorizationHeaderAsync()
    {
        var client = webFactory.CreateClient();

        var valid = new HttpRequestMessage(HttpMethod.Get, HubPath);
        valid.Headers.Add("Authorization", "Bearer Test_Token_123");
        (await client.SendAsync(valid)).StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

        var invalid = new HttpRequestMessage(HttpMethod.Get, HubPath);
        invalid.Headers.Add("Authorization", "Bearer wrong_token");
        (await client.SendAsync(invalid)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TestHubWithoutTokenShouldFailToConnectAsync()
    {
        await using var connection = BuildConnection(withToken: false);
        var act = async () => await connection.StartAsync();
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 任务变化的通知应推给已连接的客户端：notifier → 广播服务 → SignalR → 客户端。
    /// 同时验证推送的 JSON 选项（camelCase、TimeSpan 转秒数）与状态带上自增序号。
    /// </summary>
    [Fact]
    public async Task TestTasksChangedAndStatusPushedAsync()
    {
        await using var connection = BuildConnection(withToken: true);
        var tasksChanged = new TaskCompletionSource<TasksChangedPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        var statusReceived = new TaskCompletionSource<StatusPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<TasksChangedPayload>("tasksChanged", payload => tasksChanged.TrySetResult(payload));
        connection.On<StatusPayload>("queueStatus", payload => statusReceived.TrySetResult(payload));
        await connection.StartAsync();

        // 直接触发 Core 的通知（等价于"新增/取消/删除任务"），验证通知链路本身
        webFactory.Services.GetRequiredService<ITaskChangeNotifier>().Notify(TaskChangeKind.Tasks);

        var changed = await tasksChanged.Task.WaitAsync(TimeSpan.FromSeconds(10));
        changed.Reason.Should().Be(nameof(TaskChangeKind.Tasks));
        changed.HasPending.Should().BeFalse(); // 前面的测试已清空数据库，没有待处理任务

        // 任务的开始/结束会立刻补推一条队列状态，不必等 1 秒的采样
        var status = await statusReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        status.Seq.Should().BeGreaterThan(0);
        status.IsProcessing.Should().BeFalse(); // 空闲：没有正在执行的队列
        // TimeSpan 必须被序列化成数字（秒），否则前端"用播放时间点决定是否重拉快照"的比较会失效
        status.Time.ValueKind.Should().Be(JsonValueKind.Number);
    }

    /// <summary>
    /// 设置计划时间应推给客户端，并可通过 HTTP 兜底接口读回（两条路都经过 Core 的通知器）
    /// </summary>
    [Fact]
    public async Task TestScheduleChangedPushedAsync()
    {
        await using var connection = BuildConnection(withToken: true);
        var scheduleChanged = new TaskCompletionSource<ScheduleChangedPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<ScheduleChangedPayload>("scheduleChanged", payload => scheduleChanged.TrySetResult(payload));
        await connection.StartAsync();

        try
        {
            var time = DateTime.Now.AddMinutes(5);
            await PostAsync("/api/Queue/Schedule", new { time });

            var payload = await scheduleChanged.Task.WaitAsync(TimeSpan.FromSeconds(10));
            payload.ScheduleTime.Should().NotBeNull();
            payload.ScheduleTime!.Value.Should().BeCloseTo(time, TimeSpan.FromSeconds(5));

            var state = await GetObjectFromJsonAsync<SimpleFFmpegGUI.Dto.QueueStateDto>("/api/Queue/State");
            state.ScheduleTime.Should().BeCloseTo(time, TimeSpan.FromSeconds(5));
        }
        finally
        {
            // 计划时间是单例状态，会跨测试残留：一定要清掉，避免别的测试跑到一半被"到点自动开始队列"影响
            await PostAsync("/api/Queue/CancelSchedule");
        }
    }

    /// <summary>
    /// HTTP 兜底接口的形状必须与推送一致（前端用同一套解析逻辑）：status 里含 seq、
    /// time 是数字、hasPending 与 scheduleTime 齐备
    /// </summary>
    [Fact]
    public async Task TestQueueStateShapeAsync()
    {
        var json = await GetStringAsync("/api/Queue/State");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        root.TryGetProperty("hasPending", out _).Should().BeTrue();
        root.TryGetProperty("scheduleTime", out _).Should().BeTrue();
        root.TryGetProperty("status", out var status).Should().BeTrue();
        status.TryGetProperty("seq", out var seq).Should().BeTrue();
        seq.GetInt64().Should().BeGreaterThan(0);
        status.TryGetProperty("isProcessing", out _).Should().BeTrue();
        status.TryGetProperty("lastOutput", out _).Should().BeTrue();
        status.TryGetProperty("progress", out _).Should().BeTrue();
        status.TryGetProperty("task", out _).Should().BeTrue();
        status.TryGetProperty("time", out var time).Should().BeTrue();
        time.ValueKind.Should().Be(JsonValueKind.Number);
    }

    private HubConnection BuildConnection(bool withToken)
    {
        var url = withToken
            ? $"http://localhost{HubPath}?access_token=Test_Token_123"
            : $"http://localhost{HubPath}";
        return new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                // 与前端一致：只走 WebSocket，不做传输层回退（连不上时前端自行降级为 HTTP 轮询）
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.HttpMessageHandlerFactory = _ => webFactory.Server.CreateHandler();
                options.WebSocketFactory = async (context, token) =>
                    await webFactory.Server.CreateWebSocketClient().ConnectAsync(context.Uri, token);
            })
            // 服务端用 camelCase 推送，这里显式声明大小写不敏感，避免依赖客户端默认选项
            .AddJsonProtocol(options => options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true)
            .Build();
    }
}
