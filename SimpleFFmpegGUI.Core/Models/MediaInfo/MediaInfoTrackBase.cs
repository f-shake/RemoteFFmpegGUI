using System;
using System.Text.Json.Serialization;

namespace SimpleFFmpegGUI.Models.MediaInfo
{
    public class MediaInfoTrackBase
    {
        public string BitDepth { get; set; }

        public int BitRate { get; set; }

        [JsonPropertyName("CodecID")]
        public string CodecId { get; set; }

        public string Default { get; set; }

        [JsonIgnore]
        public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

        [JsonPropertyName("Duration")]
        public double DurationSeconds { get; set; }

        public string Format { get; set; }
        public double FrameRate { get; set; }
        public int Height { get; set; }
        [JsonPropertyName("ID")]
        public int Id { get; set; }
        public int Index { get; set; }
        public int StreamOrder { get; set; }
        public long StreamSize { get; set; }
        public int Width { get; set; }
    }
}