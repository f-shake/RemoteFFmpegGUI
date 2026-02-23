namespace SimpleFFmpegGUI.Model
{
    public interface IVideoCodeArguments
    {
        string AspectRatio { get; set; }
        double? AverageBitrate { get; set; }
        string Code { get; set; }
        int? Crf { get; set; }
        double? Fps { get; set; }
        double? MaxBitrate { get; set; }
        double? MaxBitrateBuffer { get; set; }
        string PixelFormat { get; set; }
        int Preset { get; set; }
        string Size { get; set; }
        bool TwoPass { get; set; }
    }
}