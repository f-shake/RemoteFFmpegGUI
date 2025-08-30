using FFMpegCore.Enums;

namespace SimpleFFmpegGUI.FFmpegLib
{
    public class N_AV1 : N_VideoCodec
    {
        public override int DefaultCRF => 35;
        public override int MaxCRF => 63;
        public override string Name => "AV1 (Nvdia)";
        public override string Lib => "av1_nvenc";
    }

}
