namespace SimpleFFmpegGUI.Events
{
    public class FFmpegOutputEventArgs
    {
        public FFmpegOutputEventArgs()
        {
        }

        public FFmpegOutputEventArgs(string data)
        {
            Data = data;
        }

        public string Data { get; set; }
    }
}