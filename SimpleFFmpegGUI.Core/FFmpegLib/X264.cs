using SimpleFFmpegGUI.FFmpegArgument;

namespace SimpleFFmpegGUI.FFmpegLib
{
    public class X264 : SoftwareVideoCodec
    {
        public readonly static string[] Profiles = new string[]
        {
            "baseline",
            "main",
            "high",
            "high10",
            "high422",
            "high444"
        };

        public override string Name => "H264";
        public override string Lib => "libx264";
        public override int MaxSpeedLevel => FFmpegEnums.Presets.Length - 1;
        public override int DefaultSpeedLevel => 3;
        public override int DefaultCRF => 23;
        public override int MaxCRF => 51;

        public override double[] SpeedFPSRelationship => new[] { 9.6405, 21.3954, 47.3501, 65.9966, 74.8024, 89.1454, 126.9204, 163.6194, 236.2421 };


        public override FFmpegArgumentItem Speed(int speed)
        {
            base.Speed(speed);
            return new FFmpegArgumentItem("preset", FFmpegEnums.Presets[speed]);
        }
    }

}
