using FzLib;
using FzLib.Programming;
using System.ComponentModel;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.Models.Entities
{
    public class PresetEntity : EntityBase
    {
        /// <summary>
        /// 输出参数
        /// </summary>
        public OutputParameters Parameters { get; set; }

        /// <summary>
        /// 是否为该类中的默认预设
        /// </summary>
        public bool Default{ get; set; }
        /// <summary>
        /// 预设名
        /// </summary>
        public string Name{ get; set; }

        /// <summary>
        /// 预设对应的类型
        /// </summary>
        public TaskType Type{ get; set; }
    }
}