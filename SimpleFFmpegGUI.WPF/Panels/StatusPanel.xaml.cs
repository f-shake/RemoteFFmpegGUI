using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimpleFFmpegGUI.WPF.Panels
{
    public partial class StatusPanel : UserControl
    {
        public StatusPanel()
        {
            ViewModel = this.SetDataContext<StatusPanelViewModel>();
            InitializeComponent();
        }

        public StatusPanelViewModel ViewModel { get; }

        /// <summary>
        /// 点击任务卡片上的快照缩略图：弹出一个可以缩放/平移的预览窗口看这一帧。
        /// <para>
        /// 快照每次都是新文件名（见 Core 的 FileSystemHelper.GetTempFileName），所以"同一帧"就是"同一个文件路径"——
        /// 去重逻辑在 <see cref="ImagePreviewPanel.ShowAsync"/> 里：同一帧重复点击只把已开的窗口激活。
        /// </para>
        /// </summary>
        private async void Snapshot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 先标记已处理：下面要 await，不能等回来再标
            e.Handled = true;

            // 双击的第二下不必再走一遍（第一下已经把窗口开出来了，这里再跑一次只会白读一次图）
            if (e.ClickCount != 1)
            {
                return;
            }

            try
            {
                // 这个 Border 在 DataTemplate 里，没有 x:Name，只能从 sender 的 DataContext 取视图模型
                if ((sender as FrameworkElement)?.DataContext is not TaskInfoViewModel task)
                {
                    return;
                }

                Uri uri = task.Snapshot.Source;
                if (uri is not { IsFile: true } || task.Inputs == null || task.Inputs.Count == 0)
                {
                    // 还没生成出快照：光标与提示在 XAML 里已是"不可点"的样子，这里静默返回。
                    // 判断条件刻意与 XAML 那个 DataTrigger（只看 Snapshot.Source）保持一致——多一个条件就会出现
                    // "看起来能点、点下去没反应"的撒谎状态
                    return;
                }

                string title = $"快照预览 - {Path.GetFileName(task.Inputs[0].FilePath)} @ {FormatTime(task.Snapshot.Time)}";
                await ImagePreviewPanel.ShowAsync(Window.GetWindow(this), uri, title);
            }
            catch (Exception ex)
            {
                // async void 的异常会直接冒到 Dispatcher（Debug 下没有全局兜底），这里兜住，不让点一下缩略图就崩
                App.AppLog?.Error("打开快照预览失败", ex);
            }
        }

        /// <summary>
        /// 时间点文案（如 01:23:45.6）。注意不能直接用 <c>TimeSpan</c> 的 "hh" 格式——那是 0~23 的小时分量，
        /// 素材位置超过 24 小时会回绕；仓库里 HourMinSecTimeSpanConverter 也是用"天数×24+小时"绕开这个坑的。
        /// </summary>
        private static string FormatTime(TimeSpan time)
        {
            int hours = time.Days * 24 + time.Hours;
            return $"{hours:00}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds / 100}";
        }
    }
}
