using System.Text.Json.Serialization;

namespace SimpleFFmpegGUI.Models.MediaInfo
{
    public class MediaInfoImage: MediaInfoTrackBase
    {
        public string ColorSpace { get; set; }
        [JsonPropertyName("Compression_Mode")]
        public string CompressionMode { get; set; }
    }
}
