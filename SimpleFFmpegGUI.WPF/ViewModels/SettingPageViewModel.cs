using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleFFmpegGUI.WPF.FzLib;
using Mapster;
using Microsoft.Win32;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WPF.Messages;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using CommonDialog = iNKORE.Extension.CommonDialog.CommonDialog;

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

        /// <summary>
        /// 快照尺寸（v1 的 SnapshotSize 配置，P1-12）
        /// </summary>
        public string SnapshotSize
        {
            get => configManager.SnapshotSize;
            set => configManager.SnapshotSize = value;
        }

        public ObservableCollection<RemoteHost> ObservableRemoteHosts { get; set; }
        [RelayCommand]
        private void AddRemoteHost()
        {
            ObservableRemoteHosts.Add(new RemoteHost());
        }

        /// <summary>
        /// 测试连接专用的 HttpClient：测试连接需要较短的超时，避免地址填错时长时间卡住
        /// </summary>
        private static readonly HttpClient testHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        [RelayCommand]
        private async Task TestRemoteHostAsync(RemoteHost host)
        {
            SendMessage(new WindowEnableMessage(false));
            try
            {
                string baseUrl = (host.Address ?? "").TrimEnd('/');
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    await CommonDialog.ShowOkDialogAsync("测试连接", "请先填写主机地址");
                    return;
                }

                // 1. 远程是否需要鉴权（Token/Need 与 Token/Check 都豁免全局鉴权，可直接访问）
                bool need = await GetBoolAsync(baseUrl + "/Token/Need");
                // 2. 若需要鉴权，再用当前填写的密码校验是否匹配
                if (need)
                {
                    string token = (host.Token ?? "").Trim();
                    // 兼容 v1 已保存带 "Bearer " 前缀的 Token
                    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        token = token["Bearer ".Length..];
                    }
                    if (string.IsNullOrEmpty(token))
                    {
                        await CommonDialog.ShowOkDialogAsync("测试连接", "该远程主机需要连接密码，请填写后再测试");
                        return;
                    }
                    bool ok = await GetBoolAsync(baseUrl + "/Token/Check/" + Uri.EscapeDataString(token));
                    if (!ok)
                    {
                        await CommonDialog.ShowErrorDialogAsync("连接密码不正确");
                        return;
                    }
                }

                await CommonDialog.ShowOkDialogAsync("测试连接", "连接成功");
            }
            catch (TaskCanceledException)
            {
                await CommonDialog.ShowErrorDialogAsync("连接超时");
            }
            catch (HttpRequestException ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "连接失败");
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "测试连接失败");
            }
            finally
            {
                SendMessage(new WindowEnableMessage(true));
            }
        }

        private static async Task<bool> GetBoolAsync(string url)
        {
            string text = await testHttpClient.GetStringAsync(url);
            return bool.TryParse(text, out bool result) && result;
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
        private async Task Save()
        {
            Configs.RemoteHosts = ObservableRemoteHosts.ToList();
            Configs.Adapt(Config.Instance);
            Config.Instance.Save();
            // 持久化 ConfigService 配置（默认进程优先级、快照尺寸）
            await configManager.SaveAsync();
            RequestToClose?.Invoke(this, EventArgs.Empty);
        }
    }
}