using FluentAssertions;
using SimpleFFmpegGUI.Events;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// 任务/队列变更通知器（<see cref="TaskChangeNotifier"/>）的订阅与通知行为测试。
/// </summary>
public class TaskChangeNotifierTests
{
    [Fact]
    public void Notify_NoSubscriber_ShouldNotThrow()
    {
        var notifier = new TaskChangeNotifier();

        var act = () => notifier.Notify(TaskChangeKind.Tasks);

        act.Should().NotThrow();
    }

    [Fact]
    public void Notify_MultipleSubscribers_ShouldReceiveOnceWithSameKind()
    {
        var notifier = new TaskChangeNotifier();
        var receivedKinds = new List<TaskChangeKind>();
        var senders = new List<object>();
        notifier.Changed += (sender, e) =>
        {
            senders.Add(sender);
            receivedKinds.Add(e.Kind);
        };
        notifier.Changed += (sender, e) => receivedKinds.Add(e.Kind);

        notifier.Notify(TaskChangeKind.Schedule);

        receivedKinds.Should().Equal(TaskChangeKind.Schedule, TaskChangeKind.Schedule);
        senders.Should().AllBeEquivalentTo(notifier);
    }

    [Fact]
    public void Unsubscribe_ShouldNotReceiveFurtherNotifications()
    {
        var notifier = new TaskChangeNotifier();
        int count = 0;
        void Handler(object sender, TaskChangeEventArgs e) => count++;

        notifier.Changed += Handler;
        notifier.Notify(TaskChangeKind.Tasks);
        notifier.Changed -= Handler;
        notifier.Notify(TaskChangeKind.Tasks);

        count.Should().Be(1);
    }

    [Fact]
    public void Notify_SubscriberThrows_ShouldNotBreakOtherSubscribersOrCaller()
    {
        var notifier = new TaskChangeNotifier();
        int count = 0;
        notifier.Changed += (_, _) => throw new InvalidOperationException("订阅方内部异常");
        notifier.Changed += (_, _) => count++;

        var act = () => notifier.Notify(TaskChangeKind.QueueFinished);

        // 通知发生在任务增删改、队列执行等流程中途，单个订阅方抛异常不能把调用方带崩
        act.Should().NotThrow();
        count.Should().Be(1);
    }

    [Fact]
    public void Notify_SubscriberUnsubscribesDuringNotification_ShouldStillNotifyRemainingSubscribers()
    {
        var notifier = new TaskChangeNotifier();
        int count = 0;
        EventHandler<TaskChangeEventArgs> selfUnsubscribe = null;
        selfUnsubscribe = (_, _) => notifier.Changed -= selfUnsubscribe;
        notifier.Changed += selfUnsubscribe;
        notifier.Changed += (_, _) => count++;

        notifier.Notify(TaskChangeKind.Tasks);

        count.Should().Be(1);
    }

    [Fact]
    public void Notify_Concurrent_ShouldDeliverEveryEvent()
    {
        var notifier = new TaskChangeNotifier();
        int count = 0;
        notifier.Changed += (_, _) => Interlocked.Increment(ref count);
        const int times = 2000;

        Parallel.For(0, times, _ => notifier.Notify(TaskChangeKind.Tasks));

        count.Should().Be(times);
    }
}
