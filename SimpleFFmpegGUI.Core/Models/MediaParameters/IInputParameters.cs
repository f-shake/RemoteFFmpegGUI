using System;

namespace SimpleFFmpegGUI.Models
{
    public interface IInputParameters
    {
        TimeSpan? Duration { get; set; }
        string Extra { get; set; }
        string FilePath { get; set; }
        string Format { get; set; }
        double? Framerate { get; set; }
        TimeSpan? From { get; set; }
        bool Image2 { get; set; }
        TimeSpan? To { get; set; }
    }
}