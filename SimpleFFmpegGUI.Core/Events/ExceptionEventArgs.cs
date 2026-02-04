using System;

namespace SimpleFFmpegGUI.Events
{
    public class ExceptionEventArgs : EventArgs
    {
        public ExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public ExceptionEventArgs(Exception exception, string message)
        {
            Exception = exception;
            Message = message;
        }
        public Exception Exception { get; }
        public string Message { get; }
    }
}