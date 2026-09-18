using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace iNKORE.Extension
{
    /// <summary>
    /// ProgressRingOverlay.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressRingOverlay : UserControl, INotifyPropertyChanged
    {
        public ProgressRingOverlay()
        {
            TaskArgs = new ProgressRingOverlayArgs(this);
            InitializeComponent();
            grd.DataContext = this;
        }

        private bool showing = false;

        public void Show(int delay = 0)
        {
            grd.Visibility = Visibility.Visible;
            grd.IsHitTestVisible = true;

            showing = true;
            if (delay > 0)
            {
                Task.Delay(delay).ContinueWith(t =>
                {
                    if (showing)
                    {
                        Dispatcher.Invoke(ShowIt);
                    }
                });
            }
            else
            {
                ShowIt();
            }
            void ShowIt()
            {
                DoubleAnimation ani = new DoubleAnimation(1, TimeSpan.FromMilliseconds(500));
                ani.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut };
                grd.BeginAnimation(OpacityProperty, ani);
            }
        }

        public void Hide()
        {
            if (!showing)
            {
                grd.Visibility = Visibility.Collapsed;
                grd.IsHitTestVisible = false;
                return;
            }
            showing = false;
            DoubleAnimation ani = new DoubleAnimation(0, TimeSpan.FromMilliseconds(500));
            ani.EasingFunction = new CubicEase() { EasingMode = EasingMode.EaseInOut };
            grd.IsHitTestVisible = false;
            ani.Completed += (p1, p2) =>
            {
                // 淡出的这 500ms 里又 Show() 了（两个忙碌操作挨着来）时，这条已经作废的动画不该把环收掉：
                // 只有"最后一次调用确实是 Hide"（showing 仍为 false）才折叠，否则环会在忙碌中途凭空消失、
                // 窗口提前变回可点
                if (!showing)
                {
                    grd.Visibility = Visibility.Collapsed;
                }
            };
            grd.BeginAnimation(OpacityProperty, ani);
        }

        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
              nameof(Message),
              typeof(string),
              typeof(ProgressRingOverlay));

        //public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        //      nameof(IsOpen),
        //      typeof(bool),
        //      typeof(ProgressRingOverlay),
        //      new PropertyMetadata(false, new PropertyChangedCallback((s, e) =>
        //      {
        //          if (e.NewValue == e.OldValue)
        //          {
        //              return;
        //          }
        //          var obj = s as ProgressRingOverlay;
        //          if (true.Equals(e.NewValue))
        //          {
        //              obj.Show();
        //          }
        //          else
        //          {
        //              obj.Hide();
        //          }
        //      }))
        //    );

        //public bool IsOpen
        //{
        //    get => (bool)GetValue(IsOpenProperty);
        //    set => SetValue(IsOpenProperty, value);
        //}

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ProgressRingOverlayArgs TaskArgs { get; }
    }

    public class ProgressRingOverlayArgs
    {
        public ProgressRingOverlayArgs(ProgressRingOverlay ui)
        {
            this.ui = ui;
        }

        private ProgressRingOverlay ui;

        public void SetMessage(string message)
        {
            ui.Message = message;
        }
    }
}