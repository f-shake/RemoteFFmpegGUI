namespace SimpleFFmpegGUI.FFmpegLib
{
    public class N_H265 : N_VideoCodec
    {
        public override int DefaultCRF => 28;
        public override int MaxCRF => 51;
        public override string Name => "H265 (Nvidia)";
        public override string Lib => "hevc_nvenc";
    }

}
