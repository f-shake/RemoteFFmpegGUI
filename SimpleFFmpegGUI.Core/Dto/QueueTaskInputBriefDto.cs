namespace SimpleFFmpegGUI.Dto
{
    /// <summary>
    /// 实时推送用的输入文件摘要（前端只用到路径）。
    /// </summary>
    public class QueueTaskInputBriefDto
    {
        public string FilePath { get; set; }
    }
}
