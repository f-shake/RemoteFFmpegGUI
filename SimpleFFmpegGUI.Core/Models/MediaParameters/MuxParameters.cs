using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FzLib.Programming;

namespace SimpleFFmpegGUI.Models.MediaParameters
{
    public partial class MuxParameters : ObservableObject
    {
        [ObservableProperty]
        private bool shortest;
    }
}