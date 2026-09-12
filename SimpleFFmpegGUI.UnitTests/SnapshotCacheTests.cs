using FluentAssertions;
using SimpleFFmpegGUI.Caching;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// 快照缓存（<see cref="SnapshotCache"/>）的复用、并发去重、失败不缓存与淘汰行为测试。
/// 过期（TTL 10 分钟）无法在单测里等待，未覆盖。
/// </summary>
public class SnapshotCacheTests
{
    [Fact]
    public async Task GetOrAddAsync_SameKey_ShouldGenerateOnce()
    {
        var cache = new SnapshotCache();
        int generateCount = 0;
        Task<byte[]> Factory()
        {
            generateCount++;
            return Task.FromResult(new byte[] { 1, 2, 3 });
        }

        var first = await cache.GetOrAddAsync("k", Factory);
        var second = await cache.GetOrAddAsync("k", Factory);

        generateCount.Should().Be(1);
        first.Should().BeEquivalentTo(second);
        cache.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetOrAddAsync_DifferentKeys_ShouldGenerateEach()
    {
        var cache = new SnapshotCache();
        int generateCount = 0;
        Task<byte[]> Factory()
        {
            generateCount++;
            return Task.FromResult(Array.Empty<byte>());
        }

        await cache.GetOrAddAsync("a", Factory);
        await cache.GetOrAddAsync("b", Factory);

        generateCount.Should().Be(2);
        cache.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetOrAddAsync_ConcurrentSameKey_ShouldGenerateOnce()
    {
        var cache = new SnapshotCache();
        int generateCount = 0;
        async Task<byte[]> Factory()
        {
            Interlocked.Increment(ref generateCount);
            await Task.Delay(50); // 让并发请求都落在"生成中"这个窗口里
            return new byte[] { 42 };
        }

        byte[][] results = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => cache.GetOrAddAsync("same", Factory)));

        generateCount.Should().Be(1);
        results.Should().AllSatisfy(r => r.Should().Equal(42));
    }

    [Fact]
    public async Task GetOrAddAsync_FactoryFails_ShouldNotCacheFailure()
    {
        var cache = new SnapshotCache();
        int attempts = 0;
        Task<byte[]> Factory()
        {
            attempts++;
            if (attempts == 1)
            {
                throw new InvalidOperationException("第一次生成失败");
            }

            return Task.FromResult(new byte[] { 7 });
        }

        var firstAttempt = async () => await cache.GetOrAddAsync("k", Factory);
        await firstAttempt.Should().ThrowAsync<InvalidOperationException>();

        // 失败不能留下"毒条目"：下一次必须重试并成功
        var second = await cache.GetOrAddAsync("k", Factory);
        second.Should().Equal(7);
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task GetOrAddAsync_ExceedingCapacity_ShouldEvictLeastRecentlyUsed()
    {
        var cache = new SnapshotCache();
        // 容量上限是 20：填满 20 个键，再访问一次最旧的键让它变成"最近用过"，然后插入第 21 个键
        for (int i = 0; i < 20; i++)
        {
            await cache.GetOrAddAsync($"k{i}", () => Task.FromResult(new byte[] { (byte)i }));
        }

        await cache.GetOrAddAsync("k0", () => Task.FromResult(new byte[] { 0 }));
        // LastAccess 用真实时钟，而上面这一串纯 CPU 循环很可能落在同一个时间戳上；
        // 并列时 OrderBy 保持的是并发字典的枚举顺序（按进程随机化的字符串哈希），淘汰谁就不确定了。
        // 这里显式拉开时间戳，避免偶发失败
        await Task.Delay(20);
        await cache.GetOrAddAsync("k20", () => Task.FromResult(new byte[] { 20 }));

        cache.Count.Should().BeLessThanOrEqualTo(20);
        // k0 刚被访问过，应仍在缓存里（命中时不会再调用 factory）
        int k0GenerateCount = 0;
        await cache.GetOrAddAsync("k0", () =>
        {
            k0GenerateCount++;
            return Task.FromResult(new byte[] { 0 });
        });
        k0GenerateCount.Should().Be(0);
    }
}
