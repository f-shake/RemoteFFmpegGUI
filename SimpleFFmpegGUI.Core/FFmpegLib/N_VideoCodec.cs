namespace SimpleFFmpegGUI.FFmpegLib
{
    public abstract class N_VideoCodec : HardwareVideoCodec
    {
        public override int MaxSpeedLevel => FFmpegEnums.N_Presets.Length - 1;
        public override int DefaultSpeedLevel => 3;
        // p7~p1 的相对速度暂按相等处理（未实测）
        public override double[] SpeedFPSRelationship => new[] { 1d, 1, 1, 1, 1, 1, 1 };
    }

}
