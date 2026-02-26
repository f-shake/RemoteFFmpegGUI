namespace SimpleFFmpegGUI.Models
{
    public interface IAudioCodecParameters
    {
        int? Bitrate { get; set; }
        string Code { get; set; }
        int? SamplingRate { get; set; }
    }
}