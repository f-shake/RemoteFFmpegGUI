using SimpleFFmpegGUI.FFmpegArgument;

namespace SimpleFFmpegGUI.FFmpegLib
{
    public abstract class SoftwareVideoCodec : VideoCodec
    {
        public override string CrfLabel => "CRF";

        public override FFmpegArgumentItem CRF(int level)
        {
            if (level < 0 || level > MaxCRF)
            {
                throw new FFmpegArgumentException("CRF的值超出范围");
            }
            return new FFmpegArgumentItem("crf", level.ToString());
        }
    }

}
