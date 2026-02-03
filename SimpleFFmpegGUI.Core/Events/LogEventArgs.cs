using SimpleFFmpegGUI.Model;
using System;

namespace SimpleFFmpegGUI.Events
{
    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(Log log)
        {
            Log = log;
        }

        public Log Log { get; set; }
    }
}