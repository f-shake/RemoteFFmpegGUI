using SimpleFFmpegGUI.Services;
using System;

namespace SimpleFFmpegGUI.Events
{
    public class ProcessChangedEventArgs : EventArgs
    {
        public ProcessChangedEventArgs(FFmpegProcessService oldProcess, FFmpegProcessService newProcess)
        {
            OldProcess = oldProcess;
            NewProcess = newProcess;
        }

        public FFmpegProcessService NewProcess { get; }
        public FFmpegProcessService OldProcess { get; }
    }
}