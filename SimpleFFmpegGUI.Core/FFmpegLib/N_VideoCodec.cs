namespace SimpleFFmpegGUI.FFmpegLib
{
    public abstract class N_VideoCodec : HardwareVideoCodec
    {
        public override int MaxSpeedLevel => FFmpegEnums.N_Presets.Length - 1;
        public override int DefaultSpeedLevel => 3;
        public override double[] SpeedFPSRelationship => throw new System.NotImplementedException();
    }

}
