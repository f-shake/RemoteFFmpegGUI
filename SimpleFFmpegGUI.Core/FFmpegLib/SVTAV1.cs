using SimpleFFmpegGUI.FFmpegArgument;
using System;

namespace SimpleFFmpegGUI.FFmpegLib
{
    public class SVTAV1 : SoftwareVideoCodec
    {
        public override string Name => "AV1 (SVT)";
        public override string Lib => "libsvtav1";
        public override int MaxSpeedLevel => 8;
        public override int DefaultSpeedLevel => 6;
        public override int DefaultCRF => 28;
        public override int MaxCRF => 63;

        public override double[] SpeedFPSRelationship => new[] { 0.05, 0.05, 0.1063, 0.2032, 0.5364, 3.5965, 9.4903, 16.3359, 27.3078 };


        public override FFmpegArgumentItem Speed(int speed)
        {
            base.Speed(speed);
            return new FFmpegArgumentItem("preset", speed.ToString());
        }

        public override FFmpegArgumentItem AverageBitrate(double mb)
        {
            if (mb < 0)
            {
                throw new FFmpegArgumentException("平均码率超出范围");
            }
            return new FFmpegArgumentItem("rc", "1", "svtav1-params", ':')
            {
                Other = new FFmpegArgumentItem("tbr", Convert.ToInt32(mb * 1000).ToString(), "svtav1-params", ':')
            };
        }

        public override FFmpegArgumentItem MaxBitrate(double mb)
        {
            if (mb < 0)
            {
                throw new FFmpegArgumentException("最大码率超出范围");
            }
            return new FFmpegArgumentItem("mbr", Convert.ToInt32(mb * 1000).ToString(), "svtav1-params", ':');
        }
        public override FFmpegArgumentItem BufferSize(double mb)
        {
            throw new FFmpegArgumentException("SVTAV1编译器不支持该参数：" + nameof(BufferSize));
        }
        public override FFmpegArgumentItem FrameRate(double fps)
        {
            if (fps < 0)
            {
                throw new FFmpegArgumentException("帧速率大小超出范围");
            }
            return new FFmpegArgumentItem("fps", fps.ToString(), "svtav1-params", ':');
        }

        public override FFmpegArgumentItem Pass(int pass)
        {
            throw new FFmpegArgumentException("SVTAV1暂不支持2Pass");
        }
    }

}
