using SimpleFFmpegGUI.WPF.ViewModels;
using SimpleFFmpegGUI.WPF.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimpleFFmpegGUI.WPF.Panels
{
    /// <summary>
    /// 通用图片预览面板：把一张图铺在一个可缩放、可平移的视口里，下面配一条工具栏。
    /// <para>
    /// 面板本身是通用的——只依赖"一张位图 + 一个标题"，不关心图片从哪来（目前的调用方是任务卡片上的快照缩略图）。
    /// 宿主用 <see cref="ViewWindow"/> 包成独立窗口（Esc 关闭靠 <see cref="ICloseableView"/>），也可以塞进任何容器。
    /// </para>
    /// <para>
    /// 缩放/平移的数学在 <see cref="ImagePreviewMath"/> 里，本文件只负责把鼠标键盘事件翻译成数学调用。
    /// </para>
    /// </summary>
    public partial class ImagePreviewPanel : UserControl, ICloseableView
    {
        /// <summary>
        /// 按来源文件路径去重的已开窗口表：同一帧重复点击只把已经开着的窗口激活，不再开一个内容一模一样的。
        /// （快照每次都是新文件名，所以"同一个文件路径"就等于"同一帧"。）
        /// </summary>
        private static readonly Dictionary<string, Window> openedWindows = new();

        /// <summary>当前变换（初值取单位矩阵：首次适应由 SetImage 或视口的 SizeChanged 完成）</summary>
        private ImagePreviewMath.Transform transform = new(1, 0, 0);

        /// <summary>
        /// 是否处于"适应窗口"状态。为真时窗口尺寸一变就重新适应；用户一旦自己滚轮缩放或切到 1:1，
        /// 就进入"自由"状态——此时改窗口大小不该动他正在看的缩放与位置。
        /// </summary>
        private bool isFitMode = true;

        private bool dragging;
        private Point lastPosition;

        public ImagePreviewPanel()
        {
            ViewModel = this.SetDataContext<ImagePreviewPanelViewModel>();
            InitializeComponent();
        }

        public ImagePreviewPanelViewModel ViewModel { get; }

        /// <summary>请求关闭所在窗口（<see cref="ViewWindow"/> 订阅后转成 Close()）</summary>
        public event EventHandler RequestToClose;

        /// <summary>
        /// 打开（或激活）一张图片的预览窗口。
        /// <para>
        /// 读文件与解码在这里做完（放后台线程），失败就**不**开窗口、只在 <paramref name="owner"/> 上弹一条错误：
        /// 与其弹一个空窗口让用户对着黑框发呆，不如直接说清楚。
        /// </para>
        /// <para>
        /// <b>必须在 UI 线程调用</b>：await 之后要建窗并 <c>Show()</c>，靠的是 WPF 的同步上下文把续体带回 UI 线程
        /// （因此下面没有、也不能加 <c>ConfigureAwait(false)</c>）。从别的线程调用会在开窗处抛异常，然后被这里
        /// 兜住只记一条日志——表现为"点了没反应"。
        /// </para>
        /// </summary>
        public static async Task ShowAsync(Window owner, Uri uri, string title)
        {
            if (owner == null || uri == null || !uri.IsFile)
            {
                return;
            }

            string key = uri.LocalPath;
            if (ActivateOpenedWindow(key))
            {
                return;
            }

            BitmapSource image;
            try
            {
                // 解码放后台线程：耗时取决于"快照尺寸"设置（1080P 约几十毫秒，但设置页那个输入框没有范围校验，
                // 填成 2160P 就是几百毫秒），同步做会让点击之后界面僵一下。BitmapImage 在后台线程解码、
                // Freeze 之后交给 UI 线程使用是标准做法。
                // 这里**不要**加 ConfigureAwait(false)：await 之后要对 UI 线程亲和的对象动手（建窗、Show）
                image = await Task.Run(() => LoadImage(uri));
            }
            catch (Exception ex)
            {
                ShowError(owner, $"读取快照失败：{key}", ex);
                return;
            }

            // 解码期间可能已经有人把同一帧开出来了（例如双击的第二下），再查一次
            if (ActivateOpenedWindow(key))
            {
                return;
            }

            ImagePreviewPanel panel = new ImagePreviewPanel();
            panel.SetImage(image, title);

            Window window = null;
            try
            {
                // 用 ViewWindow 而不是 new Window()：iNKORE 主题下后者会因 backdrop 递归栈溢出
                Window created = new ViewWindow(panel, title, owner, 1100, 680);
                window = created;
                created.Closed += (_, _) => RemoveOpenedWindow(key, created);
                lock (openedWindows)
                {
                    openedWindows[key] = created;
                }
                created.Show();
            }
            catch (Exception ex)
            {
                // 开窗失败时 Closed 不会触发，得在这里手工收拾。window 仍为 null 只有一种可能：构造函数就抛了
                // ——那种情形这里连实例都拿不到（而加载环已在那之前的 ViewWindow 构造函数里登记过），管不了，认了
                if (window != null)
                {
                    // 去重表里"确实登记过的那一个"
                    RemoveOpenedWindow(key, window);
                    // 加载环登记在 ViewWindow 构造函数里（静态表），而摘除只挂在 Closed 上；窗口既然没显示成功，
                    // 就永远不会走到 Closed。先摘条目，再关一次窗——Close 会走完整套收尾（退订、Dispose、摘条目），
                    // 顺带把窗口从 Application.Windows 里去掉（只摘 rings 的话窗口仍被 Application.Windows 钉着）
                    WindowBusyOverlay.Unregister(window);
                    try
                    {
                        window.Close();
                    }
                    catch (Exception closeException)
                    {
                        App.AppLog?.Error($"收拾开窗失败的预览窗口时出错：{key}", closeException);
                    }
                }
                ShowError(owner, $"打开快照预览失败：{key}", ex);
            }
        }

        /// <summary>同一帧的窗口已经开着就把它激活（从最小化恢复），返回是否已处理</summary>
        private static bool ActivateOpenedWindow(string key)
        {
            lock (openedWindows)
            {
                if (openedWindows.TryGetValue(key, out Window opened) && opened.IsVisible)
                {
                    if (opened.WindowState == WindowState.Minimized)
                    {
                        opened.WindowState = WindowState.Normal;
                    }
                    opened.Activate();
                    return true;
                }
            }

            return false;
        }

        /// <summary>把条目从去重表里移除（只移除自己那一条，避免误删别人刚换上的）</summary>
        private static void RemoveOpenedWindow(string key, Window window)
        {
            lock (openedWindows)
            {
                if (openedWindows.TryGetValue(key, out Window current) && ReferenceEquals(current, window))
                {
                    openedWindows.Remove(key);
                }
            }
        }

        /// <summary>
        /// 在 <paramref name="owner"/> 上弹一条错误提示。
        /// <para>
        /// 提示本身也可能抛（<c>CreateMessage</c> 要求窗口根是 Grid，见 NotificationMessageExtension.cs:63-71），
        /// 这里必须再兜一层：点击处理器里逃出去的异常在 Debug 下没有全局兜底（App 只在 Release 注册未处理异常捕获），
        /// 会直接崩掉进程。
        /// </para>
        /// </summary>
        private static void ShowError(Window owner, string message, Exception ex)
        {
            try
            {
                owner.CreateMessage().QueueError(message, ex);
            }
            catch (Exception notifyException)
            {
                App.AppLog?.Error($"提示失败（{message}）时又出错", notifyException);
            }
        }

        /// <summary>载入要显示的图片。只调用一次——窗口打开后显示的就是这一帧，不再跟随刷新</summary>
        public void SetImage(BitmapSource image, string title)
        {
            ViewModel.SetImage(image, title);
            // 显式把元素的布局尺寸设为图片像素尺寸：这样 k=1 严格等于"1 图像像素 = 1 DIP"，
            // 不受 jpg 内嵌 DPI 元数据影响（若用 Stretch=None，元素自然尺寸是 像素×96/dpi，非 96dpi 的图会错位）
            img.Width = image.PixelWidth;
            img.Height = image.PixelHeight;
            ResetToFit(); // 此刻可能还没布局（视口尺寸为 0），那就交给 Viewport_SizeChanged 兜底
        }

        /// <summary>
        /// 解码成一张与文件解耦的位图。
        /// <para>
        /// 关键在 <see cref="BitmapCacheOption.OnLoad"/> + <see cref="Freeze"/>：默认的 OnDemand 会一直持有文件流，
        /// 而主界面那张缩略图本来就锁着同一个文件；OnLoad 在 EndInit 时就解码完，冻结之后这张图与文件再无关系
        /// （临时文件将来被清理也不会影响已打开的窗口）。
        /// </para>
        /// <para>
        /// <b>不要</b>在这里设 <see cref="BitmapCreateOptions.IgnoreImageCache"/>：它只在 UriSource 路径下有意义
        /// （而本方法的来源是每次都不重名的临时文件，根本不需要绕 WPF 内部缓存），配 StreamSource 时会让
        /// EndInit() 必定抛 ArgumentNullException——StreamSource 路径下内部的 _uri 是 null，而 IgnoreImageCache
        /// 会无条件拿它去 RemoveFromCache。已在 .NET 10 / PresentationCore 10.0 上实测：加这一行每次都抛。
        /// </para>
        /// </summary>
        private static BitmapSource LoadImage(Uri uri)
        {
            byte[] bytes = File.ReadAllBytes(uri.LocalPath);
            using MemoryStream stream = new MemoryStream(bytes);
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// 应用一次变换：更新渲染矩阵与缩放百分比，并返回<span>是否真的发生了变化</span>。
        /// <para>
        /// 返回值的用途：缩放到上下限时 <see cref="ImagePreviewMath.ZoomAt"/> 会原样返回当前变换，
        /// 调用方要靠这个值决定要不要退出"适应窗口"状态——否则会出现"变换还是适应窗口那一套、
        /// 状态却已经不是适应"的不一致（此后改窗口大小不再重新适应）。
        /// </para>
        /// </summary>
        private bool Apply(ImagePreviewMath.Transform next)
        {
            bool changed = next != transform;
            transform = next;
            imgTransform.Matrix = new Matrix(next.Scale, 0, 0, next.Scale, next.OffsetX, next.OffsetY);
            ViewModel.ZoomText = ImagePreviewMath.ToPercentText(next.Scale);
            return changed;
        }

        /// <summary>回到"适应窗口"：铺满视口（最多 100%，小图不放大）并居中，之后窗口尺寸变化会继续重新适应</summary>
        private void ResetToFit()
        {
            if (ViewModel.Source == null || viewport.ActualWidth <= 0 || viewport.ActualHeight <= 0)
            {
                return;
            }

            isFitMode = true;
            Apply(ImagePreviewMath.CreateFit(viewport.ActualWidth, viewport.ActualHeight, img.Width, img.Height));
        }

        /// <summary>切到 1:1（真实像素）。以视口中心为锚，从"适应"切过去视觉上是连续的</summary>
        private void ZoomToActualSize()
        {
            if (ViewModel.Source == null)
            {
                return;
            }

            if (Apply(ImagePreviewMath.CreateActualSize(transform,
                viewport.ActualWidth, viewport.ActualHeight, img.Width, img.Height)))
            {
                isFitMode = false;
            }
        }

        /// <summary>双击：在"适应窗口"与"1:1"之间切换</summary>
        private void ToggleFitAndActualSize()
        {
            if (isFitMode)
            {
                ZoomToActualSize();
            }
            else
            {
                ResetToFit();
            }
        }

        private void EndDrag()
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;
            viewport.ReleaseMouseCapture();
            viewport.Cursor = Cursors.Hand;
        }

        /// <summary>
        /// 捕获被别的东西抢走时（Alt+Tab、系统菜单、UAC 提示等）把拖动状态收干净。
        /// <para>
        /// 不收的话：窗口外的移动与那次松开都不会再送到视口（鼠标仍在窗口内时 MouseMove 倒还是会来，
        /// 但那种情况本来就该结束拖动了），<c>dragging</c> 会一直停在 true、光标停在四向箭头，只能等鼠标
        /// 再次移进视口时靠 <see cref="Viewport_MouseMove"/> 里的自愈分支清掉。
        /// </para>
        /// </summary>
        private void Viewport_LostMouseCapture(object sender, MouseEventArgs e)
        {
            EndDrag();
        }

        private void Viewport_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ViewModel.Source == null)
            {
                return;
            }

            // 用 Pow 而不是"每格固定倍数"：触控板与高精度滚轮的 delta 未必是 120 的整数倍
            double factor = Math.Pow(ImagePreviewMath.WheelZoomStep, e.Delta / 120.0);
            if (Apply(ImagePreviewMath.ZoomAt(transform, factor, e.GetPosition(viewport),
                viewport.ActualWidth, viewport.ActualHeight, img.Width, img.Height)))
            {
                // 只有真的变了才退出"适应"模式（到达缩放上下限时 ZoomAt 原样返回，状态不该跟着变）
                isFitMode = false;
            }
            e.Handled = true; // 别让外层的 ScrollViewer 之类把滚轮吞走
        }

        private void Viewport_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.Source == null)
            {
                return;
            }

            Focus(); // WPF 不会因为点了空白区域就把焦点交给 UserControl，这里手工给，保证 Esc 一定生效

            if (e.ClickCount == 2)
            {
                // 双击的这一次按下可能还带着上一次的拖动状态，先收干净再切模式
                EndDrag();
                ToggleFitAndActualSize();
                e.Handled = true;
                return;
            }

            dragging = true;
            lastPosition = e.GetPosition(viewport);
            viewport.Cursor = Cursors.SizeAll;
            viewport.CaptureMouse();
            e.Handled = true;
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!dragging)
            {
                return;
            }
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                // 窗口失去激活等情况可能收不到 up 事件，这里自愈，免得鼠标一动就继续拖
                EndDrag();
                return;
            }

            Point position = e.GetPosition(viewport);
            Vector delta = position - lastPosition;
            lastPosition = position;
            Apply(ImagePreviewMath.Pan(transform, delta,
                viewport.ActualWidth, viewport.ActualHeight, img.Width, img.Height));
        }

        private void Viewport_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            EndDrag();
        }

        private void Viewport_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (isFitMode)
            {
                ResetToFit(); // 适应模式下窗口变大变小都继续铺满
            }
            else
            {
                // 自由模式保持缩放不变，只重跑钳制（某个方向已经比视口小了会自动回到居中）
                Apply(ImagePreviewMath.ClampToView(transform,
                    viewport.ActualWidth, viewport.ActualHeight, img.Width, img.Height));
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
        }

        private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                return;
            }

            e.Handled = true; // 不标记的话系统会"哔"一声
            RequestToClose?.Invoke(this, EventArgs.Empty);
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomFromToolbar(ImagePreviewMath.ButtonZoomStep);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomFromToolbar(1 / ImagePreviewMath.ButtonZoomStep);
        }

        private void ActuallySize_Click(object sender, RoutedEventArgs e)
        {
            ZoomToActualSize();
        }

        private void Fit_Click(object sender, RoutedEventArgs e)
        {
            ResetToFit();
        }

        /// <summary>工具栏的放大/缩小：以视口中心为锚</summary>
        private void ZoomFromToolbar(double factor)
        {
            if (ViewModel.Source == null)
            {
                return;
            }

            if (Apply(ImagePreviewMath.ZoomAt(transform, factor,
                new Point(viewport.ActualWidth / 2, viewport.ActualHeight / 2),
                viewport.ActualWidth, viewport.ActualHeight, img.Width, img.Height)))
            {
                isFitMode = false;
            }
        }
    }
}
