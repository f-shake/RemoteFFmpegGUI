using CommunityToolkit.Mvvm.Input;
using SimpleFFmpegGUI.Logging;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class FFmpegOutputPageViewModel : ViewModelBase
    {
        public FFmpegOutputPageViewModel()
        {
            DbLogger.Log += Logger_Log;
        }
        public ObservableCollection<Log> Outputs { get; } = new ObservableCollection<Log>();

        [RelayCommand]
        private void ClearAll()
        {
            Outputs.Clear();
        }

        [RelayCommand]
        private void CopyAll()
        {
            try
            {
                Clipboard.SetText(string.Join(Environment.NewLine, Outputs.Select(p => p.Message)));
                QueueSuccessMessage("已复制内容到剪贴板");
            }
            catch (Exception ex)
            {
                QueueErrorMessage("复制内容失败", ex);
            }
        }

        private async void Logger_Log(object sender, LogEventArgs e)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (e.Log.Message.StartsWith("frame=")
                && Outputs.Count > 0 && Outputs[^1].Message.StartsWith("frame="))
                {
                    Outputs[^1] = e.Log;
                }
                else
                {
                    Outputs.Add(e.Log);
                }
            });
        }
    }
}