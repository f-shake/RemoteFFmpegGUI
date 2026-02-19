using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SimpleFFmpegGUI.Events;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Services;

public class DbLoggerService : BackgroundService
{
    private readonly IDbContextFactory<FFmpegDbContext> dbFactory;
    private ConcurrentBag<Log> queueLogs = new ConcurrentBag<Log>();

    private int saving = 0;

    private PeriodicTimer timer;
    private static bool hasInstance = false;

    public DbLoggerService(IDbContextFactory<FFmpegDbContext> dbFactory)
    {
        if (hasInstance)
        {
            throw new InvalidOperationException("DbLoggerService只能有一个实例");
        }

        hasInstance = true;
        this.dbFactory = dbFactory;
    }

    public event EventHandler<LogEventArgs> Log;

    public event EventHandler<ExceptionEventArgs> LogSaveFailed;

    public void Error(string message)
    {
        AddLog('E', message);
    }

    public void Error(TaskInfo task, string message)
    {
        AddLog('E', message, task);
    }

    public void Info(TaskInfo task, string message)
    {
        AddLog('I', message, task);
    }

    public void Info(string message)
    {
        AddLog('I', message);
    }

    public void Output(TaskInfo task, string message)
    {
        AddLog('O', message, task);
    }

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

            await using var db = await dbFactory.CreateDbContextAsync();
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

    public void Warn(string message)
    {
        AddLog('W', message);
    }

    public void Warn(TaskInfo task, string message)
    {
        AddLog('W', message, task);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SaveAllAsync();
        }
    }

    private void AddLog(char type, string message, TaskInfo task = null)
    {
        Log log = new Log()
        {
            Time = DateTime.Now,
            Type = type,
            Message = message,
            TaskId = task?.Id
        };
        Log?.Invoke(null, new LogEventArgs(log));
        AddLog(log);
        Debug.WriteLine($"[{type}] {message}");
        Console.WriteLine($"[{type}] {message}");
    }

    private void AddLog(Log log)
    {
        queueLogs.Add(log);
    }
}