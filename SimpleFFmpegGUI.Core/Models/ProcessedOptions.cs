namespace SimpleFFmpegGUI.Model
{
    public class ProcessedOptions
    {
        /// <summary>
        /// 将输出文件的修改时间设置为最后一个输入文件的修改时间
        /// </summary>
        public bool SyncModifiedTime { get; set; }

        /// <summary>
        /// 处理后删除输入文件。若可以，将删除到回收站。
        /// </summary>
        public bool DeleteInputFiles { get; set; }
    }
}