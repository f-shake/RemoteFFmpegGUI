using System;
using System.Text.Json.Serialization;

namespace SimpleFFmpegGUI.Models.MediaInfo
{
    public class MediaInfoAudio : MediaInfoTrackBase
    {
        public int AlternateGroup { get; set; }
        [JsonPropertyName("BitRate_Mode")]
        public string BitRateMode { get; set; }
        public string ChannelLayout { get; set; }
        public string ChannelPositions { get; set; }
        public int Channels { get; set; }
        [JsonPropertyName("Compression_Mode")]
        public string CompressionMode { get; set; }
        public double Delay { get; set; }
        [JsonPropertyName("Delay_DropFrame")]
        public string DelayDropFrame { get; set; }
        [JsonPropertyName("Delay_Source")]
        public string DelaySource { get; set; }
        [JsonPropertyName("Format_AdditionalFeatures")]
        public string FormatAdditionalFeatures { get; set; }
        public int FrameCount { get; set; }
        public int SamplesPerFrame { get; set; }
        public long SamplingCount { get; set; }
        public int SamplingRate { get; set; }
        [JsonPropertyName("Video_Delay")]
        public double VideoDelay { get; set; }
    }
}
