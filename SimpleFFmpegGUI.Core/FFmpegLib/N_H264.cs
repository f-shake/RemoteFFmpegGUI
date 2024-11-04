namespace SimpleFFmpegGUI.FFmpegLib
{
    public class N_H264 : N_VideoCodec
    {
        public override int DefaultCRF => 28;
        public override int MaxCRF => 51;
        public override string Name => "H264 (Nvdia)";
        public override string Lib => "h264_nvenc";
    }

}
