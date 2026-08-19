using CommunityToolkit.Mvvm.ComponentModel;
using SimpleFFmpegGUI.WPF.FzLib;
using SimpleFFmpegGUI.WPF.ViewModels;
using System.ComponentModel;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class FormatArgumentViewModel : ViewModelBase, IArgumentVideModel
    {
        [ObservableProperty]
        private bool enableFormat = true;

        [ObservableProperty]
        private string format = "mp4";

        public void Apply()
        {
            Format = EnableFormat ? Format : null;
        }

        public void Update()
        {
            EnableFormat = Format != null;
        }
    }
}