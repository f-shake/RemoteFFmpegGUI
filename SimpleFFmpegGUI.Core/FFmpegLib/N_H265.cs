namespace SimpleFFmpegGUI.FFmpegLib
{
    public class N_H265 : N_VideoCodec
    {
        public override int DefaultCRF => 28;
        public override int MaxCRF => 51;
        public override string Name => "H265 (Nvdia)";
        public override string Lib => "hevc_nvenc";
    }

}
