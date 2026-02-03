using FzLib;
using FzLib.Programming;
using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public class CombineArguments : INotifyPropertyChanged
    {
        private bool shortest;

        public bool Shortest
        {
            get => shortest;
            set => this.SetValueAndNotify(ref shortest, value, nameof(Shortest));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}