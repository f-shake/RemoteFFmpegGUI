using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleFFmpegGUI.WPF.FzLib;
using Mapster;
using Microsoft.Win32;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.Helpers;
using SimpleFFmpegGUI.WPF.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using SimpleFFmpegGUI.Dto;
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

                // 连接与密码校验通过后，再做一次文件可见性测试：验证"源目录对应的本机位置"与远端 InputDir
                // 是否真的对应（远程提交正是靠这个映射把本地文件换算成相对路径）。
                // 没通过就算测试未通过——尤其"没配置本机位置"时不能再报"连接成功"，否则远程提交只能按
                // 旧约定去远端根目录找同名文件，出问题也看不出来
                var visibility = await TestRemoteInputVisibilityAsync(host);
                if (!visibility.Passed)
                {
                    await CommonDialog.ShowErrorDialogAsync("测试未通过：" + visibility.Message);
                    return;
                }
                await CommonDialog.ShowOkDialogAsync("测试连接", "连接成功\n" + visibility.Message);
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
            using var response = await testHttpClient.GetAsync(url);
            // 先校验是接口响应：地址写错时打到前端 SPA 兜底页会拿到 200 + HTML，原来的
            // bool.TryParse 会把"解析不出来"当成 false（即"不需要密码"）→ 测试连接误报成功
            string text = await RemoteApiResponse.ReadAndValidateAsync(response);
            return bool.TryParse(text, out bool result) && result;
        }

        /// <summary>
        /// 文件可见性测试的结论
        /// </summary>
        /// <param name="Passed">是否通过（未配置本机位置一律视为不通过）</param>
        /// <param name="Message">给用户看的一行说明</param>
        public readonly record struct RemoteInputVisibilityResult(bool Passed, string Message);

        /// <summary>
        /// 文件可见性测试：在“源目录对应的本机位置”下建一个临时子目录与文件，再看远端输入文件列表里
        /// 能否找到它对应的相对路径——用来验证该位置与远端 InputDir 是否真的对应（含子目录映射），
        /// 远程提交正是靠这个映射把本地文件换算成相对路径。无论成败都清理临时文件；本方法不抛异常。
        /// </summary>
        public static async Task<RemoteInputVisibilityResult> TestRemoteInputVisibilityAsync(RemoteHost host)
        {
            string baseDir = host.LocalInputDir;
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                return new(false, "未配置“源目录对应的本机位置”");
            }
            if (!Directory.Exists(baseDir))
            {
                return new(false, $"配置的本机位置不存在（{baseDir}）");
            }

            string testDir = Path.Combine(baseDir, "rfg_visibility_" + Guid.NewGuid().ToString("N")[..8]);
            string testFile = Path.Combine(testDir, "probe.tmp");
            try
            {
                Directory.CreateDirectory(testDir);
                await File.WriteAllTextAsync(testFile, "SimpleFFmpegGUI remote input visibility test").ConfigureAwait(false);
                string relative = RemotePathHelper.ToRemoteInputPath(testFile, baseDir);

                // 远端若靠同步/镜像目录，刚放进去可能还没同步过去，重试几次再下结论
                for (int attempt = 1; ; attempt++)
                {
                    var files = await GetRemoteInputFilesAsync(host);
                    if (files.Any(p => string.Equals(p.RelativePath, relative, StringComparison.OrdinalIgnoreCase)))
                    {
                        return new(true, $"目录一致性测试通过");
                    }
                    if (attempt >= 3)
                    {
                        return new(false, $"配置的“源目录对应的本机位置”与远程端不一致");
                    }
                    await Task.Delay(1500);
                }
            }
            catch (Exception ex)
            {
                return new(false, "检查失败：" + ex.Message);
            }
            finally
            {
                // 测试文件用完即删，不留在用户的输入目录里
                try
                {
                    if (File.Exists(testFile))
                    {
                        File.Delete(testFile);
                    }
                    if (Directory.Exists(testDir))
                    {
                        Directory.Delete(testDir);
                    }
                }
                catch
                {
                    // 清理失败不影响测试结论
                }
            }
        }

        /// <summary>
        /// 取远端输入文件列表（相对路径，含子目录）
        /// </summary>
        private static async Task<List<FileInfoDto>> GetRemoteInputFilesAsync(RemoteHost host)
        {
            string url = RemoteApiRequest.BuildUrl(host.Address, "File/List/Input");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            RemoteApiRequest.AddAuthorization(request, host.Token);
            using var response = await testHttpClient.SendAsync(request);
            string json = await RemoteApiResponse.ReadAndValidateAsync(response);
            return JsonConvert.DeserializeObject<List<FileInfoDto>>(json) ?? [];
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

        /// <summary>
        /// 选择某台远程主机的"源目录对应的本机位置"（本机上对应远端输入目录的位置），远程提交时用它求相对路径
        /// </summary>
        [RelayCommand]
        private void BrowseRemoteHostLocalInputDir(RemoteHost host)
        {
            if (host == null)
            {
                return;
            }
            var dialog = new OpenFolderDialog();
            SendMessage(new FileDialogMessage(dialog));
            var path = dialog.FolderName;
            if (!string.IsNullOrEmpty(path))
            {
                host.LocalInputDir = path;
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