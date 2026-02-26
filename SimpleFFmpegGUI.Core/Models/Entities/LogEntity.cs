using System;

namespace SimpleFFmpegGUI.Models.Entities
{
    public class LogEntity : EntityBase
    {
        public LogEntity()
        {
        }

        public DateTime Time { get; set; }
        public char Type { get; set; }
        public string Message { get; set; }
        public int? TaskId { get; set; }
    }
}