using CommunityToolkit.Mvvm.ComponentModel;
using FzLib;
using Newtonsoft.Json.Linq;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.ComponentModel;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class InputArgumentsViewModel : ViewModelBase, IArgumentVideModel
    {
        [ObservableProperty]
        private bool canDelete;

        [ObservableProperty]
        private TimeSpan? duration;

        [ObservableProperty]
        private bool enableDuration;

        [ObservableProperty]
        private bool enableFrom;

        [ObservableProperty]
        private bool enableTo;

        [ObservableProperty]
        private string extra;

        [ObservableProperty]
        private string filePath;

        [ObservableProperty]
        private string format;

        [ObservableProperty]
        private double? framerate;

        [ObservableProperty]
        private TimeSpan? from;

        [ObservableProperty]
        private bool image2;
        [ObservableProperty]
        private int index;

        [ObservableProperty]
        private TimeSpan? to;

        public void Apply()
        {
            From = EnableFrom ? From : null;
            To = EnableTo ? To : null;
            Duration = EnableDuration ? Duration : null;
        }

        public void Update()
        {
            EnableFrom = From.HasValue;
            EnableTo = To.HasValue;
            EnableDuration = Duration.HasValue;
        }
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            switch (e.PropertyName)
            {
                case nameof(Image2):
                    if (Image2 && !Framerate.HasValue)
                    {
                        Framerate = 30;
                    }
                    break;
                case nameof(From):
                    EnableFrom = From.HasValue;
                    break;
                case nameof(To):
                    EnableTo = To.HasValue;
                    break;
                case nameof(Duration):
                    EnableDuration = Duration.HasValue;
                    break;
                case nameof(EnableFrom):
                    if (!EnableFrom && From.HasValue)
                    {
                        From = null;
                    }
                    break;
                case nameof(EnableTo):
                    if (!EnableTo && To.HasValue)
                    {
                        To = null;
                    }
                    break;
                case nameof(EnableDuration):
                    if (!EnableDuration && Duration.HasValue)
                    {
                        Duration = null;
                    }
                    break;
            }
        }
    }
}