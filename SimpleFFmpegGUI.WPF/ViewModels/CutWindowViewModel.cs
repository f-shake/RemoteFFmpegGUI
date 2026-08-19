using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleFFmpegGUI.WPF.FzLib;
using System;
using System.ComponentModel;
using System.Windows;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class CutWindowViewModel : ViewModelBase
    {
        private TimeSpan current = TimeSpan.Zero;

        private double currentP;

        [ObservableProperty]
        private string filePath;

        [ObservableProperty]
        private long frame;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(FromP))]
        private TimeSpan from = TimeSpan.Zero;

        [ObservableProperty]
        private bool isBarEnabled = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(To), nameof(From), nameof(ToP), nameof(FromP))]
        private TimeSpan length;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ToP))]
        private TimeSpan to = TimeSpan.Zero;

        public CutWindowViewModel()
        {
        }
        public TimeSpan Current
        {
            get => current;
            set
            {
                current = value;
                currentP = value / Length;
                OnPropertyChanged(nameof(CurrentP));
                OnPropertyChanged(nameof(Current));
            }
        }

        public double CurrentP
        {
            get => currentP;
            set
            {
                currentP = value;
                current = Length * value;
                OnPropertyChanged(nameof(CurrentP));
                OnPropertyChanged(nameof(Current));
            }
        }
        public double FromP => From / Length;
        public double ToP => To / Length;
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(Length))
            {
                To = Length;
            }
        }

        [RelayCommand]
        private void JumpToFrom()
        {
            Current = From;
        }

        [RelayCommand]
        private void JumpToTo()
        {
            Current = To;
        }

        [RelayCommand]
        private void SetFrom()
        {
            if (Current >= To)
            {
                QueueErrorMessage("开始时间不可晚于结束时间");
                return;
            }
            From = Current;
            QueueSuccessMessage("已将开始时间设置为" + From.ToString(App.Current.FindResource("TimeSpanFormat") as string));
        }

        [RelayCommand]
        private void SetTo()
        {
            if (Current <= From)
            {
                QueueErrorMessage("结束时间不可早于开始时间");
                return;
            }
            To = Current;
            QueueSuccessMessage("已将结束时间设置为" + To.ToString(App.Current.FindResource("TimeSpanFormat") as string));
        }

        [RelayCommand]
        private void Cancel()
        {
            Application.Current.Shutdown();
        }
        [RelayCommand]
        private void Apply()
        {
            Console.Write("{0},{1}", From, To);
            Application.Current.Shutdown();
        }

    }
}