using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib;
using Mapster;
using Microsoft.Win32;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WPF.Messages;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class SettingPageViewModel : ViewModelBase
    {
        private readonly ConfigService configManager;

        [ObservableProperty]
        private Config configs;

        public SettingPageViewModel(ConfigService configManager)
        {
            configs = Config.Instance.DeepCopy();
            this.configManager = configManager;
            ObservableRemoteHosts = new ObservableCollection<RemoteHost>(Configs.RemoteHosts);
            this.Notify(nameof(ObservableRemoteHosts));
        }

        public event EventHandler RequestToClose;

        public IEnumerable DefaultOutputDirTypes => Enum.GetValues<DefaultOutputDirType>();

        public int DefaultProcessPriority
        {
            get => configManager.DefaultProcessPriority;
            set => configManager.DefaultProcessPriority = value;
        }

        public ObservableCollection<RemoteHost> ObservableRemoteHosts { get; set; }
        [RelayCommand]
        private void AddRemoteHost()
        {
            ObservableRemoteHosts.Add(new RemoteHost());
        }

        [RelayCommand]
        private void BrowseSpecialDirPath()
        {
            var dialog = new OpenFolderDialog();
            SendMessage(new FileDialogMessage(dialog));
            var path = dialog.FolderName;
            if (!string.IsNullOrEmpty(path))
            {
                Configs.DefaultOutputDirSpecialDirPath = path;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestToClose?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void Save()
        {
            Configs.RemoteHosts = ObservableRemoteHosts.ToList();
            Configs.Adapt(Config.Instance);
            Config.Instance.Save();
            RequestToClose?.Invoke(this, EventArgs.Empty);
        }
    }
}