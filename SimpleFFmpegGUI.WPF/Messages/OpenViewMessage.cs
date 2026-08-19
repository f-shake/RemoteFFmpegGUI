using System;
using System.Windows.Controls;

namespace SimpleFFmpegGUI.WPF.Messages
{
    public sealed class OpenViewMessage(Type type, bool modal = false, bool window = false, Action<UserControl> initialize = null)
    {
        public Type Type { get; } = type;
        public bool Modal { get; } = modal;
        public bool Window { get; } = window;
        public Action<UserControl> Initialize { get; } = initialize;
    }
}
