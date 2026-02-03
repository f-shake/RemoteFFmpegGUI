using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace SimpleFFmpegGUI.Logging
{
    public static class DbLogger
    {

        private static int eventRegistered = 0;

        public static event EventHandler<LogEventArgs> Log;

        public static event EventHandler<ExceptionEventArgs> LogSaveFailed;

        public static void Error(string message)
        {
            AddLog('E', message);
        }

        public static void Error(TaskInfo task, string message)
        {
            AddLog('E', message, task);
        }

        public static void Info(TaskInfo task, string message)
        {
            AddLog('I', message, task);
        }

        public static void Info(string message)
        {
            AddLog('I', message);
        }

        public static void Output(TaskInfo task, string message)
        {
            AddLog('O', message, task);
        }

        public static void Warn(string message)
        {
            AddLog('W', message);
        }

        public static void Warn(TaskInfo task, string message)
        {
            AddLog('W', message, task);
        }
        private static void AddLog(char type, string message, TaskInfo task = null)
        {
            if (DbLoggerService.Instance == null)
            {
                throw new InvalidOperationException("DbLoggerService未初始化，无法记录日志");
            }
            if (Interlocked.Exchange(ref eventRegistered, 1) == 0)
            {
                DbLoggerService.Instance.Log += (s, e) => Log?.Invoke(s, e);
                DbLoggerService.Instance.LogSaveFailed += (s, e) => LogSaveFailed?.Invoke(s, e);
            }
            Log log = new Log()
            {
                Time = DateTime.Now,
                Type = type,
                Message = message,
                TaskId = task?.Id
            };
            Log?.Invoke(null, new LogEventArgs(log));
            DbLoggerService.Instance.AddLog(log);
            Debug.WriteLine($"[{type}] {message}");
        }
    }
}