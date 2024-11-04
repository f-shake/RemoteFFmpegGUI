using SimpleFFmpegGUI.FFmpegArgument;
using System.Collections.Generic;

namespace SimpleFFmpegGUI.FFmpegLib
{
    public class XVP9 : SoftwareVideoCodec
    {
        public override string Name => "VP9";
        public override string Lib => "libvpx-vp9";
        public override int MaxSpeedLevel => 5;
        public override int DefaultSpeedLevel => 3;
        public override int DefaultCRF => 23;
        public override int MaxCRF => 63;

        public override double[] SpeedFPSRelationship => new[] { 2.1692, 6.0832, 6.0899, 9.7819, 12.6956, 14.1973, 9.2309 };


        public override FFmpegArgumentItem Speed(int speed)
        {
            base.Speed(speed);
            return new FFmpegArgumentItem("cpu-used", speed.ToString());
        }
        public override IEnumerable<FFmpegArgumentItem> ExtraArguments()
        {
            yield return new FFmpegArgumentItem("row-mt", "1");
        }
    }

}
