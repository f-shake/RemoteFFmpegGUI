using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;

namespace SimpleFFmpegGUI.WPF.FzLib.WPF.Controls
{
    public class SmoothScrollViewerHelper
    {
        /// <summary>
        /// 为带有ScrollViewer的控件启动平滑滚动
        /// </summary>
        /// <param name="ctrl"></param>
        /// <returns></returns>
        public static async Task<SmoothScrollViewerHelper> RegistAsync(Control ctrl)
        {
            if (!ctrl.IsLoaded)
            {
                await ctrl.WaitForLoadedAsync();
            }
            if (ctrl is ScrollViewer s)
            {
                RegistScrollViewer(s);
            }
            ScrollViewer scr = ctrl.GetChild<ScrollViewer>();
            if (scr == null)
            {
                throw new Exception("Control内没有ScrollViewer");
            }
            return RegistScrollViewer(scr);
        }

        public static SmoothScrollViewerHelper RegistScrollViewer(ScrollViewer scr)
        {
            scr.CanContentScroll = false;
            SmoothScrollViewerHelper helper = new SmoothScrollViewerHelper(scr);
            return helper;
        }

        public static readonly DependencyProperty SmoothScrollProperty = DependencyProperty.RegisterAttached(
  "SmoothScroll",
  typeof(bool),
  typeof(SmoothScrollViewerHelper),
  new PropertyMetadata(false, async (s, e) =>
    {
        if (e.NewValue.Equals(e.OldValue))
        {
            return;
        }
        Control ctrl = s as Control;
        if (e.NewValue.Equals(true) && !scr2Helper.ContainsKey(ctrl))
        {
            try
            {
                scr2Helper.Add(ctrl, await RegistAsync(ctrl));
            }
            catch
            {
            }
        }
        else if (e.NewValue.Equals(false) && scr2Helper.ContainsKey(ctrl))
        {
            scr2Helper[ctrl]?.Stop();
            scr2Helper.Remove(ctrl);
        }
    })
);

        private static Dictionary<Control, SmoothScrollViewerHelper> scr2Helper = new Dictionary<Control, SmoothScrollViewerHelper>();

        public static void SetSmoothScroll(Control control, bool value)
        {
            control.SetValue(SmoothScrollProperty, value);
        }

        public static bool GetSmoothScroll(Control control)
        {
            return (bool)control.GetValue(SmoothScrollProperty);
        }

        private SmoothScrollViewerHelper(ScrollViewer scrollViewer)
        {
            Debug.Assert(scrollViewer != null);

            scrollViewer.PreviewMouseWheel += HandleMouseWheel;
            ScrollViewer = scrollViewer;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            Timer_Elapsed(null);
        }

        public void Stop()
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            ScrollViewer.PreviewMouseWheel -= HandleMouseWheel;
        }

        private int i = 0;

        private void Timer_Elapsed(object obj)
        {
            if (remainsDelta != 0)
            {
                var target = ScrollViewer.VerticalOffset
                    - (remainsDelta > 0 ? 1 : -1) * Math.Sqrt(Math.Abs(remainsDelta)) / 1.5d //这个控制滑动的距离，值越大距离越短
                    * System.Windows.Forms.SystemInformation.MouseWheelScrollLines;

                ScrollViewer.Dispatcher.Invoke(() => ScrollViewer.ScrollToVerticalOffset(target));
                remainsDelta /= 1.5;//这个控制每一次滑动的时间，值越大时间越短

                //如果到目标距离不到1了，就直接停止滚动，因为不然的话会永远滚下去
                if (Math.Abs(remainsDelta) < 1)
                {
                    remainsDelta = 0;
                }
            }
        }

        private double remainsDelta = 0;

        public ScrollViewer ScrollViewer { get; }

        public void HandleMouseWheel(object scr, MouseWheelEventArgs e)
        {
            Debug.Assert(scr != null);

            remainsDelta = remainsDelta * 1.5 + e.Delta;//乘一个系数，那么滚轮越快页面滑动也将越快
            if (remainsDelta != e.Delta)
            {
                //如果滚动正在进行，那么把滚动交给之前的方法即可
                return;
            }
        }
    }
}