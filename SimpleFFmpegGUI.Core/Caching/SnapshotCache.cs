using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Caching
{
    /// <summary>
    /// 快照（缩略图）结果的异步缓存：按"不变的键"复用一份生成代价较高的结果，容量上限 + 滑动过期 + LRU 淘汰。
    /// <para>
    /// 每次快照都要起一个 ffmpeg 进程截图（几十 KB 的 JPEG，是全站最贵的请求），
    /// 而"同一个视频、同一个播放时间点、同一个缩放尺寸"的结果是确定的——多台设备同时看同一个转码任务时
    /// 请求完全相同，暂停时同一时间点还会被反复请求，按这个三元的键复用结果即可。
    /// </para>
    /// <para>
    /// 值用 <see cref="Lazy{T}"/> 包住 Task：同一个键的并发请求只会真正执行一次 factory，其余等同一份结果。
    /// 注意这个保证只在条目存活期内成立——被淘汰的条目如果仍有请求在等，那些后续请求会重新执行一次 factory。
    /// </para>
    /// <para>
    /// 放在 Core 而不是 WebAPI：它不依赖任何 ASP.NET 类型，这样单元测试可以直接测它的并发与淘汰行为；
    /// 注册仍然只在 WebAPI（WPF 的快照路径不走这里）。
    /// </para>
    /// </summary>
    public class SnapshotCache
    {
        /// <summary>缓存条数上限（每条约几十 KB，20 条约 2 MB）</summary>
        private const int MaxItems = 20;

        /// <summary>缓存有效期（按最后一次访问起算）</summary>
        private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(10);

        private readonly ConcurrentDictionary<string, Entry> entries = new();
        private readonly object evictLock = new();

        private sealed class Entry
        {
            public Entry(Func<Task<byte[]>> factory)
            {
                Value = new Lazy<Task<byte[]>>(factory);
            }

            public Lazy<Task<byte[]>> Value { get; }

            /// <summary>最后一次被取用的时间（用于 LRU 淘汰）</summary>
            public DateTime LastAccess { get; set; } = DateTime.UtcNow;

            public bool IsExpired => DateTime.UtcNow - LastAccess > TimeToLive;
        }

        /// <summary>
        /// 取缓存；没有（或已过期/上次生成失败）则调用 <paramref name="factory"/> 生成并缓存。
        /// 生成失败不写缓存（否则之后每次都会拿到同一个异常），下次请求会重试。
        /// </summary>
        public async Task<byte[]> GetOrAddAsync(string key, Func<Task<byte[]>> factory)
        {
            while (true)
            {
                Entry entry = entries.GetOrAdd(key, _ => new Entry(factory));
                if (entry.IsExpired)
                {
                    // 过期就回头用"当前字典里的那一份"（下一圈 GetOrAdd 要么拿到别人刚换上的新条目、
                    // 要么新建一份，都不会死循环）。TryRemove 只是顺手清掉自己这一份，失败说明别人已经换过了
                    entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
                    continue;
                }

                entry.LastAccess = DateTime.UtcNow;
                EvictIfNeeded();
                try
                {
                    return await entry.Value.Value;
                }
                catch
                {
                    // 只移除自己这一份，避免误删其它线程刚换上的新条目
                    entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
                    throw;
                }
            }
        }

        /// <summary>当前缓存条数（供测试与排障使用）</summary>
        public int Count => entries.Count;

        private void EvictIfNeeded()
        {
            if (entries.Count <= MaxItems)
            {
                return;
            }

            // 淘汰是低频操作（只在插入后超过上限时才可能发生），直接加锁最省心
            lock (evictLock)
            {
                if (entries.Count <= MaxItems)
                {
                    return;
                }

                // 先清过期，再按最久未用淘汰，直到回到上限
                foreach (var item in entries.Where(p => p.Value.IsExpired).ToList())
                {
                    entries.TryRemove(new KeyValuePair<string, Entry>(item.Key, item.Value));
                }

                foreach (var item in entries.OrderBy(p => p.Value.LastAccess).ToList())
                {
                    if (entries.Count <= MaxItems)
                    {
                        break;
                    }

                    entries.TryRemove(new KeyValuePair<string, Entry>(item.Key, item.Value));
                }
            }
        }
    }
}
