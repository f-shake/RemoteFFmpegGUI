namespace SimpleFFmpegGUI.Model
{
    public interface IAudioCodeArguments
    {
        int? Bitrate { get; set; }
        string Code { get; set; }
        int? SamplingRate { get; set; }
    }
}