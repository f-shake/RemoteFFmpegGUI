using System.Collections.Generic;

namespace SimpleFFmpegGUI.Models.MediaParameters
{
    public class StreamParameters
    {
        public List<StreamMapParameters> Maps { get; set; } = new List<StreamMapParameters>();
    }
}