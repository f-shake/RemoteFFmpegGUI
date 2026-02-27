using SimpleFFmpegGUI.Models;
using System.Collections.Generic;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.Dto
{
    public class TaskDto
    {
        public List<InputParameters> Inputs { get; set; }
        public string Output { get; set; }
        public OutputParameters Parameter { get; set; }
    }
}