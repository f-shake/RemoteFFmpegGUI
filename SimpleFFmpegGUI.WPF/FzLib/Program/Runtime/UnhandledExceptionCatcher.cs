using SimpleFFmpegGUI.WPF.FzLib.Program;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleFFmpegGUI.WPF.FzLib.Program.Runtime
{
    public class UnhandledExceptionCatcher
    {
        public delegate void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs e);

        public event UnhandledExceptionEventHandler UnhandledExceptionCatched;

        public void RegisterTaskCatcher()
        {
            TaskScheduler.UnobservedTaskException += (p1, p2) => //Task
            {
                if (!p2.Observed)
                {
                    RaiseEvent(p1, p2.Exception.InnerException, ExceptionSource.Task);
                    try
                    {
                        p2.SetObserved();
                    }
                    catch
                    {
                    }
                }
            };
        }

        public void RegisterThreadsCatcher()
        {
            AppDomain.CurrentDomain.UnhandledException += (p1, p2) =>//Thread
            {
                RaiseEvent(p1, (Exception)p2.ExceptionObject, ExceptionSource.Thread);
            };
        }

        protected void RaiseEvent(object sender, Exception ex, ExceptionSource source)
        {
            var e = new UnhandledExceptionEventArgs(ex, source);
            UnhandledExceptionCatched?.Invoke(sender, e);
        }
    }

    public class UnhandledExceptionEventArgs : EventArgs
    {
        public UnhandledExceptionEventArgs(Exception exception, ExceptionSource source)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Source = source;
        }

        public Exception Exception { get; private set; }
        public ExceptionSource Source { get; private set; }
    }

    public enum ExceptionSource
    {
        Task,
        Thread,
        UI,
    }
}