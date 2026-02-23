using System.Collections.Generic;

namespace SimpleFFmpegGUI.Model.MediaInfo
{
    public class MediaInfoVideo : MediaInfoTrackBase
    {
        public int BitRate_Maximum { get; set; }
        public string ChromaSubsampling { get; set; }
        public string ColorSpace { get; set; }
        public double Delay { get; set; }
        public string Delay_DropFrame { get; set; }
        public string Delay_Settings { get; set; }
        public string Delay_Source { get; set; }
        public double DisplayAspectRatio { get; set; }
        public string Encoded_Library { get; set; }
        public string Encoded_Library_Name { get; set; }
        public string Encoded_Library_Settings { get; set; }
        public string Encoded_Library_Version { get; set; }
        public List<MediaInfoItem> EncodingSettings { get; set; }
        public double Format_Level { get; set; }
        public string Format_Profile { get; set; }
        public string Format_Tier { get; set; }
        public int FrameCount { get; set; }
        public string FrameRate_Mode { get; set; }
        public double PixelAspectRatio { get; set; }
        public double Rotation { get; set; }
        public int Sampled_Height { get; set; }
        public int Sampled_Width { get; set; }
        public string ScanType { get; set; }
    }
}
