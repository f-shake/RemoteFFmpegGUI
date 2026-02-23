namespace SimpleFFmpegGUI.Model
{
    public class StreamMapInfo
    {
        /// <summary>
        /// 输入文件的序号
        /// </summary>
        public int InputIndex { get; set; }
        /// <summary>
        /// 指定的通道
        /// </summary>
        public StreamChannel Channel { get; set; }
        /// <summary>
        /// 在指定文件（和通道）中，选取的流的序号
        /// </summary>
        public int? StreamIndex { get; set; }
    }
}