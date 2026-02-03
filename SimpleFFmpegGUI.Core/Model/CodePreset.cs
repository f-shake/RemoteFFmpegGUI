using FzLib;
using FzLib.Programming;
using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public class CodePreset : ModelBase, INotifyPropertyChanged
    {
        private bool @default;
        private OutputArguments arguments;
        private string name;
        private TaskType type;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 输出参数
        /// </summary>
        public OutputArguments Arguments
        {
            get => arguments;
            set => this.SetValueAndNotify(ref arguments, value, nameof(Arguments));
        }

        /// <summary>
        /// 是否为该类中的默认预设
        /// </summary>
        public bool Default
        {
            get => @default;
            set => this.SetValueAndNotify(ref @default, value, nameof(Default));
        }

        /// <summary>
        /// 预设名
        /// </summary>
        public string Name
        {
            get => name;
            set => this.SetValueAndNotify(ref name, value, nameof(Name));
        }

        /// <summary>
        /// 预设对应的类型
        /// </summary>
        public TaskType Type
        {
            get => type;
            set => this.SetValueAndNotify(ref type, value, nameof(Type));
        }
    }
}