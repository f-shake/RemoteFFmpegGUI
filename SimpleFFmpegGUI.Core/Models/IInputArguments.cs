using System;

namespace SimpleFFmpegGUI.Model
{
    public interface IInputArguments
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