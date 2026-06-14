using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using SimpleFFmpegGUI.Models.MediaInfo;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WPF.Messages;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class MediaInfoPageViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string filePath;

        [ObservableProperty]
        private MediaInfoGeneral mediaInfo;
        public MediaInfoPageViewModel()
        {
        }

        protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(FilePath))
            {
                if (!string.IsNullOrWhiteSpace(FilePath) && System.IO.File.Exists(FilePath))
                {
                    await ShowInfoAsync();
                }
            }
        }

        private async Task ShowInfoAsync()
        {
            SendMessage(new WindowEnableMessage(false));
            try
            {
                MediaInfo = await App.ServiceProvider.GetRequiredService<MediaInfoService>().GetMediaInfoAsync(FilePath);
            }
            catch (Exception ex)
            {
                QueueErrorMessage("读取媒体信息失败", ex);
            }
            finally
            {
                SendMessage(new WindowEnableMessage(true));
            }
        }

        [RelayCommand]
        private void BrowseFile()
        {
            var dialog = new OpenFileDialog().AddAllFilesFilter();
            SendMessage(new FileDialogMessage(dialog));

            string path = dialog.FileName;
            if (!string.IsNullOrEmpty(path))
            {
                FilePath = path;
            }
        }

    }
}