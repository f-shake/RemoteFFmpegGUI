using System;

namespace SimpleFFmpegGUI.Model.MediaInfo
{
    public class MediaInfoText : MediaInfoTrackBase
    {
        public int ElementCount { get; set; }
        public string Forced { get; set; }
        public int FrameCount { get; set; }
        public string Language { get; set; }
        public string Title { get; set; }
        public int Typeorder { get; set; }
        public long UniqueID { get; set; }
    }
}
