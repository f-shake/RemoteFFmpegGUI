using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimpleFFmpegGUI.WPF.Views
{
    public partial class FFmpegOutputView : UserControl, IDisposable
    {
        public FFmpegOutputView()
        {
            ViewModel = this.SetDataContext<FFmpegOutputPageViewModel>();
            InitializeComponent();
            ViewModel.Outputs.CollectionChanged += Outputs_CollectionChanged;
        }

        public FFmpegOutputPageViewModel ViewModel { get; set; }

        /// <summary>
        /// ViewModel 为单例且全局事件通知（输出日志变化），窗口关闭时必须取消订阅，避免残留引用与重复处理
        /// </summary>
        public void Dispose()
        {
            ViewModel.Outputs.CollectionChanged -= Outputs_CollectionChanged;
        }

        private void Outputs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                scr.ScrollToEnd();
            }
        }
        private void TextBlock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Clipboard.SetText((sender as TextBlock).Text);
                this.CreateMessage().QueueSuccess("已复制内容到剪贴板");
            }
            catch(Exception ex)
            {
                this.CreateMessage().QueueError("复制内容失败", ex);
            }
        }
    }
}