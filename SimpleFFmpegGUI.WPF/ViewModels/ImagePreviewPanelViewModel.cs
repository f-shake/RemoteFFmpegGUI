using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    /// <summary>
    /// 图片预览面板的视图模型：只负责"显示什么"（图片、标题、两段文字）。
    /// <para>
    /// 缩放与平移**不**放进来：它们要读鼠标坐标、要直接改渲染矩阵，本质是视图状态而不是界面数据，
    /// 留在 <c>ImagePreviewPanel</c> 的 code-behind 里更直接。这里只有两段文本需要通知界面。
    /// </para>
    /// </summary>
    public partial class ImagePreviewPanelViewModel : ViewModelBase
    {
        /// <summary>窗口标题（同时用于面板内的说明文案）</summary>
        [ObservableProperty]
        private string title;

        /// <summary>要显示的图片（已冻结，与来源文件解耦，不跟随后续刷新）</summary>
        [ObservableProperty]
        private ImageSource source;

        /// <summary>当前缩放百分比，如 "50%"</summary>
        [ObservableProperty]
        private string zoomText = "100%";

        /// <summary>图片的像素尺寸，如 "1920×1080"；拿不到尺寸时为空串</summary>
        [ObservableProperty]
        private string sizeText = string.Empty;

        /// <summary>
        /// 载入一张图片。只调用一次——窗口打开后显示的就是这一帧，不再变化。
        /// </summary>
        public void SetImage(ImageSource image, string title)
        {
            Source = image;
            Title = title;
            // 像素尺寸用来回答"放大到 1600% 看到的是原始像素还是插值"，非位图来源时留空
            SizeText = image is BitmapSource bitmap
                ? $"{bitmap.PixelWidth}×{bitmap.PixelHeight}"
                : string.Empty;
        }
    }
}
