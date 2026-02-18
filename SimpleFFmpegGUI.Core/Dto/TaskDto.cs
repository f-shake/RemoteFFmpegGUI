using SimpleFFmpegGUI.Model;
using System.Collections.Generic;

namespace SimpleFFmpegGUI.Dto
{
    public class TaskDto
    {
        public List<InputArguments> Inputs { get; set; }
        public string Output { get; set; }
        public OutputArguments Argument { get; set; }
    }
}