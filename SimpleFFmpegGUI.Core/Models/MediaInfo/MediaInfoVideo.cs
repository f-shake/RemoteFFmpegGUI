using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimpleFFmpegGUI.Models.MediaInfo
{
    public class MediaInfoVideo : MediaInfoTrackBase
    {
        [JsonPropertyName("BitRate_Maximum")]
        public int BitRateMaximum { get; set; }

        public string ChromaSubsampling { get; set; }
        public string ColorSpace { get; set; }
        public double Delay { get; set; }

        [JsonPropertyName("Delay_DropFrame")]
        public string DelayDropFrame { get; set; }

        [JsonPropertyName("DelaySettings")]
        public string DelaySettings { get; set; }

        [JsonPropertyName("Delay_Source")]
        public string DelaySource { get; set; }

        public double DisplayAspectRatio { get; set; }

        [JsonPropertyName("Encoded_Library")]
        public string EncodedLibrary { get; set; }

        [JsonPropertyName("Encoded_Library_Name")]
        public string EncodedLibraryName { get; set; }

        [JsonPropertyName("Encoded_Library_Settings")]
        public string EncodedLibrarySettings { get; set; }

        [JsonPropertyName("Encoded_Library_Version")]
        public string EncodedLibraryVersion { get; set; }

        public List<MediaInfoItem> EncodingSettings { get; set; }

        [JsonPropertyName("Format_Level")]
        public double FormatLevel { get; set; }

        [JsonPropertyName("Format_Profile")]
        public string FormatProfile { get; set; }

        [JsonPropertyName("Format_Tier")]
        public string FormatTier { get; set; }

        public int FrameCount { get; set; }

        [JsonPropertyName("FrameRate_Mode")]
        public string FrameRateMode { get; set; }

        public double PixelAspectRatio { get; set; }
        public double Rotation { get; set; }

        [JsonPropertyName("Sampled_Height")]
        public int SampledHeight { get; set; }

        [JsonPropertyName("Sampled_Width")]
        public int SampledWidth { get; set; }

        public string ScanType { get; set; }
    }
}