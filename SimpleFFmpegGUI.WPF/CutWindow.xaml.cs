using CommunityToolkit.Mvvm.Messaging;
using Enterwell.Clients.Wpf.Notifications;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using FzLib.WPF;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Model.MediaInfo;
using SimpleFFmpegGUI.WPF.Messages;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using Size = System.Drawing.Size;
using SimpleFFmpegGUI.Services;

namespace SimpleFFmpegGUI.WPF
{

    /// <summary>
    /// Interaction logic for CutWindow.xaml
    /// </summary>
    public partial class CutWindow : Window
    {
        private readonly string[] args;

        public CutWindowViewModel ViewModel { get; set; }

        public CutWindow(string[] args)
        {
            InitializeComponent();
            ViewModel = this.SetDataContext<CutWindowViewModel>();
            this.args = args;
            Unloaded += (s, e) => media.Close();
            media.MessageLogged += Media_MessageLogged;

            WeakReferenceMessenger.Default.Register<QueueMessagesMessage>(this, (_, m) =>
            {
                switch (m.Type)
                {
                    case 'S':
                        this.CreateMessage().QueueSuccess(m.Message);
                        break;
                    case 'E' when m.Exception == null:
                        this.CreateMessage().QueueError(m.Message);
                        break;
                    case 'E' when m.Exception != null:
                        this.CreateMessage().QueueError(m.Message, m.Exception);
                        break;
                    default:
                        break;
                }
            });
        }

        private async void Media_MessageLogged(object sender, Unosquare.FFME.Common.MediaLogMessageEventArgs e)
        {
            if (e.MessageType == Unosquare.FFME.Common.MediaLogMessageType.Error)
            {
                await Dispatcher.Invoke(async () =>
                {
                    await CommonDialog.ShowErrorDialogAsync("加载视频失败", e.Message);
                    Close();
                });
            }
            Debug.WriteLine(e.Message);
        }


        public async Task SetVideoAsync(string path, TimeSpan? from, TimeSpan? to)
        {
            MediaInfoGeneral mediaInfo;
            ViewModel.FilePath = path;
            try
            {
                mediaInfo = await MediaInfoService.GetMediaInfoAsync(path);
            }
            catch (Exception ex)
            {
                throw new Exception("查询视频信息失败", ex);
            }
            if (mediaInfo.Videos.Count == 0)
            {
                throw new Exception("文件没有视频流");
            }
            ViewModel.Length = mediaInfo.Duration;
            if (from.HasValue)
            {
                ViewModel.From = from.Value;
            }
            if (to.HasValue)
            {
                ViewModel.To = to.Value;
            }
            await media.Open(new Uri(path));
        }

        private async void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            await media.Pause();
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            await media.Play();
        }

        protected override async void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            switch (e.Key)
            {
                case Key.Space:
                    if (media.IsPlaying)
                    {
                        await media.Pause();
                    }
                    else
                    {
                        await media.Play();
                    }
                    break;

                case Key.Left:
                    await media.StepBackward();
                    break;

                case Key.Right:
                    await media.StepForward();
                    break;
            }
        }

        private void Slider_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private async void JumpButton_Click(object sender, RoutedEventArgs e)
        {
            bool shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
            int s = shift ? 30 : (ctrl ? 1 : 5);
            int f = shift ? 10 : (ctrl ? 5 : 1);
            switch ((sender as FrameworkElement).Tag as string)
            {
                case "-2":
                    ViewModel.Current = new TimeSpan(Math.Max(0, ViewModel.Current.Ticks - TimeSpan.FromSeconds(s).Ticks));
                    break;

                case "-1":
                case "1":
                    await media.Pause();
                    for (int i = 0; i < f; i++)
                    {
                        if ((sender as FrameworkElement).Tag.Equals("-1"))
                        {
                            await media.StepBackward();
                        }
                        else
                        {
                            await media.StepForward();
                        }
                    }
                    break;

                case "2":
                    ViewModel.Current = new TimeSpan(Math.Min(ViewModel.Length.Ticks, ViewModel.Current.Ticks + TimeSpan.FromSeconds(s).Ticks));
                    break;
            }
        }

        private void Media_RenderingVideo(object sender, Unosquare.FFME.Common.RenderingVideoEventArgs e)
        {
            ViewModel.Frame = e.PictureNumber;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (args.Length != 3)
            {
                await CommonDialog.ShowErrorDialogAsync("参数不全");
                Application.Current.Shutdown();
                return;
            }
            if (!File.Exists(args[0]))
            {
                await CommonDialog.ShowErrorDialogAsync("文件不存在");
                Application.Current.Shutdown();
                return;
            }
            TimeSpan? from = null;
            TimeSpan? to = null;
            if (args[1] != "-")
            {
                if (TimeSpan.TryParse(args[1], out TimeSpan fromValue))
                {
                    from = fromValue;
                }
                else
                {
                    await CommonDialog.ShowErrorDialogAsync("无法解析开始时间");
                    Application.Current.Shutdown();
                    return;
                }
            }
            if (args[1] != "-")
            {
                if (TimeSpan.TryParse(args[2], out TimeSpan toValue))
                {
                    to = toValue;
                }
                else
                {
                    await CommonDialog.ShowErrorDialogAsync("无法解析结束时间");
                    Application.Current.Shutdown();
                    return;
                }
            }
            try
            {
                await SetVideoAsync(args[0], from, to);
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "加载视频失败");
                Application.Current.Shutdown();
                return;
            }
        }
    }
}