using System;
using System.Text.Json.Serialization;

namespace SimpleFFmpegGUI.Models.MediaInfo
{
    public class MediaInfoText : MediaInfoTrackBase
    {
        public int ElementCount { get; set; }
        public string Forced { get; set; }
        public int FrameCount { get; set; }
        public string Language { get; set; }
        public string Title { get; set; }
        [JsonPropertyName("Typeorder")]
        public int TypeOrder { get; set; }
        [JsonPropertyName("UniqueID")]
        public long UniqueId { get; set; }
    }
}
