using System;

namespace SimpleFFmpegGUI.WPF.Views
{
    public interface ICloseableView
    {
        public event EventHandler RequestToClose;
    }
}