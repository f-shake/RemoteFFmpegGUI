using SimpleFFmpegGUI.FFmpegArgument;
using System;
using System.Collections.Generic;

namespace SimpleFFmpegGUI.FFmpegLib
{
    public class AomAV1 : SoftwareVideoCodec
    {
        public override string Name => "AV1 (aom)";
        public override string Lib => "libaom-av1";
        public override int MaxSpeedLevel => 8;
        public override int DefaultSpeedLevel => 5;
        public override int DefaultCRF => 28;
        public override int MaxCRF => 63;

        public override double[] SpeedFPSRelationship => new[] { 0.02, 0.0677, 0.2514, 0.7954, 1.1870, 1.4515, 2.7150, 2.7180, 2.7496 };

        public override FFmpegArgumentItem Speed(int speed)
        {
            base.Speed(speed);
            return new FFmpegArgumentItem("cpu-used", speed.ToString());
        }

        public override IEnumerable<FFmpegArgumentItem> ExtraArguments()
        {
            yield return new FFmpegArgumentItem("row-mt", "1");

            //寻找将线程数切成两个最接近的数
            int threadCount = Environment.ProcessorCount;
            int best = 1;
            double sqrt = Math.Sqrt(threadCount);
            for (int i = Convert.ToInt32(sqrt); i > 0; i--)
            {
                if (threadCount / i * i == threadCount)
                {
                    best = i;
                    break;
                }
            }

            yield return new FFmpegArgumentItem("tiles", $"{best}x{threadCount / best}");
        }
    }

}
