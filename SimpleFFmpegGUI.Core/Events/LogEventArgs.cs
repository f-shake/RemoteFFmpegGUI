using SimpleFFmpegGUI.Models;
using System;
using SimpleFFmpegGUI.Models.Entities;

namespace SimpleFFmpegGUI.Events
{
    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(LogEntity log)
        {
            Log = log;
        }

        public LogEntity Log { get; set; }
    }
}