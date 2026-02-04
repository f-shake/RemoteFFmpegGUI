using System;

namespace SimpleFFmpegGUI.Model.MediaInfo
{
    public class MediaInfoAudio : MediaInfoTrackBase
    {
        public int AlternateGroup { get; set; }
        public string BitRate_Mode { get; set; }
        public string ChannelLayout { get; set; }
        public string ChannelPositions { get; set; }
        public int Channels { get; set; }
        public string Compression_Mode { get; set; }
        public double Delay { get; set; }
        public string Delay_DropFrame { get; set; }
        public string Delay_Source { get; set; }
        public string Format_AdditionalFeatures { get; set; }
        public int FrameCount { get; set; }
        public int SamplesPerFrame { get; set; }
        public long SamplingCount { get; set; }
        public int SamplingRate { get; set; }
        public double Video_Delay { get; set; }
    }
}
