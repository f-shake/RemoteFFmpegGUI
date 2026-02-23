using SimpleFFmpegGUI.Model;

namespace SimpleFFmpegGUI.Dto
{
    public class CodePresetDto
    {
        public string Name { get; set; }
        public OutputArguments Arguments { get; set; }
        public TaskType Type { get; set; }
    }
}