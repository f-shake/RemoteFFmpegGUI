using System.Windows;

namespace SimpleFFmpegGUI.WPF.FzLib.WPF
{
    public static class WindowExtension
    {
        public static void BringToFront(this Window window)
        {
            if (!window.IsVisible)
            {
                window.Show();
            }

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Activate();
            window.Topmost = true;  // important
            window.Topmost = false; // important
            window.Focus();
        }

        public static bool? ShowDialog(this Window win, Window owner, bool setCenterOwner = true)
        {
            win.Owner = owner;
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return win.ShowDialog();
        }

        public static Window GetWindow(this DependencyObject obj)
        {
            return Window.GetWindow(obj);
        }

        public static T GetWindow<T>(this DependencyObject obj) where T : Window
        {
            return Window.GetWindow(obj) as T;
        }
    }
}