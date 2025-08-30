using SimpleFFmpegGUI.FFmpegArgument;

namespace SimpleFFmpegGUI.FFmpegLib
{
    public abstract class HardwareVideoCodec : VideoCodec
    {
        public override string CrfLabel => "CQ";

        public override FFmpegArgumentItem CRF(int level)
        {
            if (level < 0 || level > MaxCRF)
            {
                throw new FFmpegArgumentException("CQ的值超出范围");
            }
            return new FFmpegArgumentItem("cq", level.ToString());
        }
    }

}
