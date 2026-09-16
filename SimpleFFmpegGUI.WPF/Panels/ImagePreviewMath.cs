using System;
using System.Windows;

namespace SimpleFFmpegGUI.WPF.Panels
{
    /// <summary>
    /// 图片预览面板的缩放/平移数学。
    /// <para>
    /// 单独抽出来一个静态类，而不是散在 <see cref="ImagePreviewPanel"/> 的事件处理器里：这里的坐标换算是整个功能中
    /// 唯一有算法味道、也最容易算错的部分，而且算错了仅凭肉眼很难判定（画面只是"有点飘"）。集中一处便于复核，
    /// 将来若要补单元测试也有明确落点。本类不引用任何控件，只做数字。
    /// </para>
    /// <para>
    /// 坐标系约定：一律使用**视口坐标系**（即 <c>e.GetPosition(viewport)</c> 的结果，单位为 DIP）。
    /// 变换把「图像空间」映射到「视口空间」：图像左上角 (0,0) 映射到 (OffsetX, OffsetY)，缩放系数为 Scale。
    /// 这要求 <c>Image</c> 在视口里左上对齐，否则它自身的布局原点还会带一段居中偏移（见 ImagePreviewPanel.xaml 的注释）。
    /// </para>
    /// </summary>
    public static class ImagePreviewMath
    {
        /// <summary>缩放下限（5%）：用来纵览整张图的构图</summary>
        public const double MinScale = 0.05;

        /// <summary>缩放上限（1600%）：足以看清 1080P 快照的单个像素</summary>
        public const double MaxScale = 16;

        /// <summary>滚轮一格的缩放倍率（比按钮细一点，免得"一滚就过头"）</summary>
        public const double WheelZoomStep = 1.15;

        /// <summary>工具栏放大/缩小按钮点一次的缩放倍率</summary>
        public const double ButtonZoomStep = 1.25;

        /// <summary>
        /// 一次变换的全部状态：缩放系数，以及图像左上角在视口里的位置。
        /// </summary>
        public readonly record struct Transform(double Scale, double OffsetX, double OffsetY);

        /// <summary>
        /// 适应窗口：缩放到正好放得下，但**最多 100%**——图比窗口小时按原始像素显示、不放大（放大会糊），
        /// 并让图在视口里居中。
        /// </summary>
        public static Transform CreateFit(double viewWidth, double viewHeight, double imageWidth, double imageHeight)
        {
            double scale = GetFitScale(viewWidth, viewHeight, imageWidth, imageHeight);
            return ClampToView(new Transform(scale, 0, 0), viewWidth, viewHeight, imageWidth, imageHeight);
        }

        /// <summary>适应窗口所需的缩放系数（小于 1 或等于 1，且图/视口尺寸非法时退化为 1）</summary>
        public static double GetFitScale(double viewWidth, double viewHeight, double imageWidth, double imageHeight)
        {
            if (viewWidth <= 0 || viewHeight <= 0 || imageWidth <= 0 || imageHeight <= 0)
            {
                return 1;
            }

            return Math.Min(Math.Min(viewWidth / imageWidth, viewHeight / imageHeight), 1);
        }

        /// <summary>
        /// 以 <paramref name="anchor"/>（视口坐标）为不动点缩放：光标底下的那个图像点在缩放前后位置完全不变。
        /// <para>
        /// 推导：设光标下的图像点为 p = M⁻¹(v)，要求缩放后它仍映射到 v。因为变换只有等比缩放与平移，
        /// 这等价于「围绕 v 把整体按 ratio 放大」：新偏移 = v - ratio * (v - 旧偏移)。
        /// </para>
        /// 缩放越界（<see cref="MinScale"/>~<see cref="MaxScale"/>）时**原样返回**，不做"夹逼后再补一次位移"——
        /// 那样会让光标下的点漂走。
        /// </summary>
        public static Transform ZoomAt(Transform current, double factor, Point anchor,
            double viewWidth, double viewHeight, double imageWidth, double imageHeight)
        {
            if (current.Scale <= 0 || factor <= 0)
            {
                return current;
            }

            double target = current.Scale * factor;
            if (target < MinScale || target > MaxScale)
            {
                return current;
            }

            return ZoomTo(current, target, anchor, viewWidth, viewHeight, imageWidth, imageHeight);
        }

        /// <summary>把缩放系数变为 <paramref name="targetScale"/>，并保持 <paramref name="anchor"/> 处的图像点不动</summary>
        public static Transform ZoomTo(Transform current, double targetScale, Point anchor,
            double viewWidth, double viewHeight, double imageWidth, double imageHeight)
        {
            if (current.Scale <= 0 || targetScale <= 0)
            {
                return current;
            }

            double ratio = targetScale / current.Scale;
            return ClampToView(new Transform(targetScale,
                anchor.X - ratio * (anchor.X - current.OffsetX),
                anchor.Y - ratio * (anchor.Y - current.OffsetY)),
                viewWidth, viewHeight, imageWidth, imageHeight);
        }

        /// <summary>1:1（1 图像像素 = 1 DIP）。以视口中心为锚，这样从"适应窗口"切过去时视觉上是连续的</summary>
        public static Transform CreateActualSize(Transform current, double viewWidth, double viewHeight,
            double imageWidth, double imageHeight)
        {
            return ZoomTo(current, 1, new Point(viewWidth / 2, viewHeight / 2),
                viewWidth, viewHeight, imageWidth, imageHeight);
        }

        /// <summary>平移（<paramref name="delta"/> 为本次鼠标移动的位移）</summary>
        public static Transform Pan(Transform current, Vector delta,
            double viewWidth, double viewHeight, double imageWidth, double imageHeight)
        {
            return ClampToView(current with
            {
                OffsetX = current.OffsetX + delta.X,
                OffsetY = current.OffsetY + delta.Y,
            }, viewWidth, viewHeight, imageWidth, imageHeight);
        }

        /// <summary>
        /// 钳制偏移：某个方向上图比视口小时强制居中（不可拖），比视口大时把偏移夹在
        /// [视口尺寸 - 图尺寸, 0]——即"图的边缘最多贴到视口边缘"，既能浏览每一个角落，又不会把图拖丢。
        /// </summary>
        public static Transform ClampToView(Transform current, double viewWidth, double viewHeight,
            double imageWidth, double imageHeight)
        {
            double scaledWidth = imageWidth * current.Scale;
            double scaledHeight = imageHeight * current.Scale;

            double offsetX = scaledWidth <= viewWidth
                ? (viewWidth - scaledWidth) / 2
                : Math.Clamp(current.OffsetX, viewWidth - scaledWidth, 0);
            double offsetY = scaledHeight <= viewHeight
                ? (viewHeight - scaledHeight) / 2
                : Math.Clamp(current.OffsetY, viewHeight - scaledHeight, 0);

            return current with { OffsetX = offsetX, OffsetY = offsetY };
        }

        /// <summary>缩放百分比文案（1.0 → "100%"）</summary>
        public static string ToPercentText(double scale)
        {
            return (scale * 100).ToString("0") + "%";
        }
    }
}
