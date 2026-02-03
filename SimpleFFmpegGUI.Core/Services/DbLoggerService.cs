using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Logging;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Services
{
    public class DbLoggerService: BackgroundService
    {
        private static readonly object lockObj = new();

        private readonly IDbContextFactory<FFmpegDbContext> dbFactory;

        private ConcurrentBag<Log> queueLogs = new ConcurrentBag<Log>();

        private int saving = 0;

        private Task timerTask;

        public DbLoggerService(IDbContextFactory<FFmpegDbContext> dbFactory)
        {
            lock (lockObj)
            {
                if (Instance != null)
                {
                    throw new InvalidOperationException("DbLoggerService只能有一个实例");
                }

                Instance = this;
            }
            this.dbFactory = dbFactory;
            StartTimer();
        }

        public event EventHandler<LogEventArgs> Log;

        public event EventHandler<ExceptionEventArgs> LogSaveFailed;

        internal static DbLoggerService Instance { get; private set; }
        public async Task SaveAllAsync()
        {
            if (Interlocked.Exchange(ref saving, 1) == 1)
            {
                return;
            }
            try
            {
                var oldBag = Interlocked.Exchange(ref queueLogs, new ConcurrentBag<Log>());
                if (oldBag.IsEmpty)
                {
                    return;
                }
                using var db = await dbFactory.CreateDbContextAsync();
                var logs = oldBag.ToList();
                db.Logs.AddRange(logs);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("保存日志失败");
                Debug.WriteLine(ex);
                LogSaveFailed?.Invoke(null, new ExceptionEventArgs(ex, "保存日志失败"));
            }
            finally
            {
                Volatile.Write(ref saving, 0);
            }
        }

        internal void AddLog(Log log)
        {
            queueLogs.Add(log);
        }
        private async Task RunTimerAsync()
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            while (await timer.WaitForNextTickAsync())
            {
                await SaveAllAsync();
            }
        }

        private void StartTimer()
        {
            timerTask = RunTimerAsync();
        }
    }
}