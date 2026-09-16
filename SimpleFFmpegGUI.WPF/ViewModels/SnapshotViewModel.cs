using CommunityToolkit.Mvvm.ComponentModel;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class SnapshotViewModel : ViewModelBase
    {
        [ObservableProperty]
        private bool displayFrame;

        [ObservableProperty]
        private bool canUpdate;

        [ObservableProperty]
        private Uri source;

        /// <summary>
        /// <see cref="Source"/> 这一帧在源视频里的位置（抓帧时刻）。
        /// <para>
        /// 由 <c>TaskInfoViewModel.UpdateSnapshotAsync</c> 与 Source 一起赋值：预览窗口的标题要用它，
        /// 而不是用点击瞬间的 <c>ProcessStatus.Time</c>——快照最多十秒才刷新一次，用点击时的进度会让标题
        /// 描述的不是窗口里那张图。
        /// </para>
        /// </summary>
        [ObservableProperty]
        private TimeSpan time;
    }
}