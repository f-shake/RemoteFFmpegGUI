using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Models.MediaInfo
{
    public class MediaInfoGeneral
    {
        public List<MediaInfoAudio> Audios { get; set; } = new List<MediaInfoAudio>();
        [JsonPropertyName("CodecID")]
        public string CodecId { get; set; }
        [JsonPropertyName("CodecID_Compatible")]
        public string CodecdCompatible { get; set; }
        public long DataSize { get; set; }
        [JsonIgnore]
        public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);
        [JsonPropertyName("Duration")]
        public double DurationSeconds { get; set; }
        [JsonPropertyName("Encoded_Application")]
        public string EncodedApplication { get; set; }
        public string FileExtension { get; set; }
        public long FileSize { get; set; }
        public long FooterSize { get; set; }
        public string Format { get; set; }
        [JsonPropertyName("Format_Profile")]
        public string FormatProfile { get; set; }
        public int FrameCount { get; set; }
        public double FrameRate { get; set; }
        public int HeaderSize { get; set; }
        public string IsStreamable { get; set; }
        public int OverallBitRate { get; set; }
        public string Raw { get; set; }
        public long StreamSize { get; set; }
        public List<MediaInfoText> Texts { get; set; } = new List<MediaInfoText>();
        public List<MediaInfoVideo> Videos { get; set; } = new List<MediaInfoVideo>();
    }
}
