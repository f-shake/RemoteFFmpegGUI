using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Model.MediaInfo
{
    public class MediaInfoGeneral
    {
        public List<MediaInfoAudio> Audios { get; set; } = new List<MediaInfoAudio>();
        public string CodecID { get; set; }
        public string CodecID_Compatible { get; set; }
        public long DataSize { get; set; }
        [JsonIgnore]
        public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

        [JsonPropertyName("Duration")]
        public double DurationSeconds { get; set; }

        public string Encoded_Application { get; set; }
        public string FileExtension { get; set; }
        public long FileSize { get; set; }
        public long FooterSize { get; set; }
        public string Format { get; set; }
        public string Format_Profile { get; set; }
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
