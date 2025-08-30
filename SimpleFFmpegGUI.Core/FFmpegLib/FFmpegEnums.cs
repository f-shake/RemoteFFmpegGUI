namespace SimpleFFmpegGUI.FFmpegLib
{
    public class FFmpegEnums
    {
        /// <summary>
        /// X264、X265中的速度预设
        /// </summary>
        public readonly static string[] Presets = new[] {
            "veryslow",
            "slower",
            "slow",
            "medium",
            "fast",
            "faster",
            "veryfast",
            "superfast",
            "ultrafast",
        };  
        
        public readonly static string[] N_Presets = new[] {
            "p7",
            "p6",
            "p5",
            "p4",
            "p3",
            "p2",
            "p1",
        };

        /// <summary>
        /// 支持的像素格式
        /// </summary>
        public readonly static string[] PixelFormats = new[] {
            "yuv420p",
            "yuvj420p",
            "yuv422p",
            "yuvj422p",
            "rgb24",
            "gray",
            "yuv420p10le"
        };
    }

}
