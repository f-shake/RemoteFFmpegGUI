using iNKORE.Extension;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SimpleFFmpegGUI.WPF
{
    /// <summary>
    /// 决定“忙碌”加载环显示在哪个窗口上。
    /// 原来的实现只在整个应用里设置一个位于主窗口的加载环（ProgressRingOverlay），
    /// 于是无论操作是在哪个窗口（例如“新建任务”弹出的子窗口）里触发，
    /// 加载对话框都会出现在主窗口上——这正是“在子窗口操作却看到主窗口转圈”的原因。
    /// 现在给每个窗口都挂上自己的加载环，谁处于前台（正在操作的那个窗口）就显示在谁身上。
    /// </summary>
    public static class WindowBusyOverlay
    {
        private static readonly Dictionary<Window, ProgressRingOverlay> rings = new();
        private static Window busyWindow;

        public static void Register(Window window, ProgressRingOverlay ring) => rings[window] = ring;

        public static void Unregister(Window window)
        {
            rings.Remove(window);
            if (busyWindow == window)
            {
                busyWindow = null;
            }
        }

        public static void SetBusy(bool busy)
        {
            if (busy)
            {
                var target = FindForegroundWindow();
                if (target == null)
                {
                    return;
                }

                busyWindow = target;
                rings[target].Show();
            }
            else
            {
                if (busyWindow != null)
                {
                    rings[busyWindow].Hide();
                    busyWindow = null;
                }
            }
        }

        /// <summary>
        /// 挑选承载操作所在的窗口：优先前台窗口，其次任意可见窗口，最后回退到主窗口。
        /// </summary>
        private static Window FindForegroundWindow()
        {
            var candidates = Application.Current.Windows
                .OfType<Window>()
                .Where(w => rings.ContainsKey(w))
                .ToArray();

            return candidates.FirstOrDefault(w => w.IsActive)
                ?? candidates.FirstOrDefault(w => w.IsVisible)
                ?? candidates.FirstOrDefault(w => w is MainWindow);
        }
    }
}
