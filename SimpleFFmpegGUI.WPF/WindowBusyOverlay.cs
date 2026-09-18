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

        /// <summary>
        /// 同时在进行的"忙碌"操作数。环只有一个，但 <see cref="SetBusy"/> / <see cref="SetNotBusy"/> 是按
        /// **操作**配对发的（7 处调用点各一对），所以要靠计数决定"还有没有人在忙"：减到 0 才收环。
        /// </summary>
        private static int busyCount;

        public static void Register(Window window, ProgressRingOverlay ring) => rings[window] = ring;

        public static void Unregister(Window window)
        {
            rings.Remove(window);
            if (busyWindow == window)
            {
                busyWindow = null;
            }
        }

        /// <summary>
        /// 标记"这次操作开始忙碌"（与 <see cref="SetNotBusy"/> 按操作配对）。环显示在哪个窗口由
        /// <see cref="FindForegroundWindow"/> 决定——谁在前台就显示在谁身上；<paramref name="message"/>
        /// 是这次操作在做什么，显示在卡片里。
        /// <para>
        /// 已经在忙时**不挪窗口**：环挪到新窗口就等于把原来那个窗口提前解锁（它那边的操作还在跑）。
        /// 只有新操作也落在同一个窗口上时，才把文案换成最新的这一次。
        /// </para>
        /// </summary>
        public static void SetBusy(string message)
        {
            // 计数先加：它的语义是"已经开始的忙碌操作数"，必须与调用方的 SetBusy/SetNotBusy 一一对应
            // （7 处调用点各一对）。若放在下面 target == null 的提前 return 之后，那一次 SetBusy 不计数、
            // 而配对的 SetNotBusy 照样会减一次，就会把别的操作的环提前收掉
            busyCount++;

            var target = FindForegroundWindow();
            if (target == null)
            {
                return;
            }

            if (busyWindow == null)
            {
                busyWindow = target;
                rings[target].Message = message ?? string.Empty;
                rings[target].Show();
            }
            else if (busyWindow == target)
            {
                rings[target].Message = message ?? string.Empty;
            }
        }

        /// <summary>
        /// 标记"这次操作的忙碌结束"。计数没减到 0（还有别的操作在忙）就不收环——否则先结束的那个会把
        /// 还在跑的那个的环关掉，那个窗口提前变回可点。计数只减不增，调用方漏发过 <see cref="SetBusy"/>
        /// 也只会变成 0，不会变成负数。
        /// </summary>
        public static void SetNotBusy()
        {
            if (busyCount > 0)
            {
                busyCount--;
            }
            if (busyCount > 0 || busyWindow == null)
            {
                return;
            }

            rings[busyWindow].Hide();
            busyWindow = null;
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
