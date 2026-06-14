using Mapster;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SimpleFFmpegGUI.WPF.Panels
{
    public partial class CodeArgumentsPanel : UserControl
    {

        public CodeArgumentsPanel()
        {
            DataContext = ViewModel;
            InitializeComponent();
        }
        public CodeArgumentsPanelViewModel ViewModel { get; } = App.ServiceProvider.GetService<CodeArgumentsPanelViewModel>();

        public OutputParameters GetOutputArguments()
        {
            return ViewModel.GetArguments();
        }

        public void SetAsClone()
        {
            ViewModel.CanApplyDefaultPreset = false;
        }

        public  Task UpdateTypeAsync(TaskType type)
        {
          return  ViewModel.UpdateTypeAsync(type);
        }

        public void Update(TaskType type, OutputParameters arguments)
        {
            ViewModel.Update(type, arguments);
        }
        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files.Length == 1 && File.Exists(files[0]))
                {
                    e.Effects = DragDropEffects.Link;
                }
            }
        }

        protected override async void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files.Length == 1 && File.Exists(files[0]))
                {
                    var file = files[0];
                    try
                    {
                        IsEnabled = false;
                        var mediaInfoService = App.ServiceProvider.GetRequiredService<MediaInfoService>();
                        var info = await mediaInfoService.GetMediaInfoAsync(file);
                        var videoArgs = mediaInfoService.ConvertToVideoArguments(info);
                        ViewModel.Video = videoArgs.Adapt<VideoArgumentsViewModel>();
                        if (videoArgs.Crf.HasValue)
                        {
                            ViewModel.Video.EnableCrf = true;
                        }
                        if (videoArgs.AverageBitrate.HasValue)
                        {
                            ViewModel.Video.EnableAverageBitrate = true;
                        }
                        if (videoArgs.MaxBitrate.HasValue)
                        {
                            ViewModel.Video.EnableMaxBitrate = true;
                        }
                        this.CreateMessage().QueueSuccess("已加载指定视频参数");
                    }
                    catch (Exception ex)
                    {
                        this.CreateMessage().QueueError("解析视频编码参数失败", ex);
                    }
                    finally
                    {
                        IsEnabled = true;
                    }

                }
            }

        }
    }
}