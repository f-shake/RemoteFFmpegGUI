using CommunityToolkit.Mvvm.ComponentModel;
using SimpleFFmpegGUI.WPF.FzLib;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.WPF.Enums;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.WPF.ViewModels;
using SimpleFFmpegGUI.WPF.Panels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.WPF.Messages;
using SimpleFFmpegGUI.WPF.FzLib.Collection;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using iNKORE.Extension.CommonDialog;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Mapster;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.Helpers;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class AddTaskPageViewModel : ViewModelBase
    {
        private readonly TaskRepository taskManager;
        private readonly CurrentTasksViewModel tasks;
        private readonly QueueService queueService;
        [ObservableProperty]
        private bool allowChangeType = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanAddFile))]
        private TaskType type;

        public AddTaskPageViewModel(TaskRepository taskManager, CurrentTasksViewModel tasks, QueueService queueService)
        {
            this.taskManager = taskManager;
            this.tasks = tasks;
            this.queueService = queueService;
        }

        public bool CanAddFile => Type is TaskType.Transcode or TaskType.Concat;
        public CodeArgumentsPanelViewModel CodeArgumentsViewModel { get; set; }
        public FileIOPanelViewModel FileIOViewModel { get; set; }
        public PresetsPanelViewModel PresetsViewModel { get; set; }
        public IEnumerable TaskTypes => Enum.GetValues(typeof(TaskType));
        private static readonly HttpClient httpClient = new HttpClient();

        /// <summary>
        /// 探测远端目录专用的 HttpClient：提交用的 httpClient 是 100 秒默认超时，主机不可达
        /// （丢包/黑洞而非 refuse）时会让用户在窗口禁用状态下先白等一份完整超时，这里缩短
        /// </summary>
        private static readonly HttpClient probeHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public static async Task PostAsync(RemoteHost host, string subUrl, object data)
        {
            string str = JsonConvert.SerializeObject(data);
            var content = new StringContent(str, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, RemoteApiRequest.BuildUrl(host.Address, subUrl))
            {
                Content = content
            };
            RemoteApiRequest.AddAuthorization(request, host.Token);
            using var response = await httpClient.SendAsync(request);
            // 校验拿到的是接口响应而不是网页：地址写错时（如漏了 v2 的 /api 前缀）会打到前端 SPA
            // 兜底页并拿到 200 + HTML，只看状态码会误判成提交成功
            await RemoteApiResponse.ReadAndValidateAsync(response);
        }

        /// <summary>
        /// 把本地输入文件映射成远端可用的路径（提交远程任务前调用）：
        /// 本地文件若位于远端 InputDir 之内（同机运行，或共享目录挂载成相同路径），发【相对 InputDir 的路径】
        /// 并保留子目录；否则退回只发文件名（旧约定：由用户保证远端 InputDir 根目录下有同名文件）。
        /// 远端目录信息取不到时一律按文件名处理，不因为这次查询失败而阻断提交。
        /// </summary>
        public static async Task<List<InputParameters>> MapInputsForRemoteAsync(RemoteHost host, List<InputParameters> inputs)
        {
            // 优先用该主机配置的"源目录对应的本机位置"（本机对应远端 InputDir 的位置，可跨盘符或走共享映射）；
            // 没配置时才去问远端要 InputDir（同机运行、或共享目录挂载成相同路径时能对上）。
            // 两者都取不到就按文件名处理，不因为这次查询失败而阻断提交。
            string localBaseDir = string.IsNullOrWhiteSpace(host.LocalInputDir)
                ? await TryGetRemoteInputDirAsync(host)
                : host.LocalInputDir;
            foreach (var i in inputs)
            {
                i.FilePath = RemotePathHelper.ToRemoteInputPath(i.FilePath, localBaseDir);
            }
            return inputs;
        }

        /// <summary>
        /// 取远端 InputDir；失败（网络、地址不对、旧版本没有该接口等）返回 null，由调用方退回按文件名提交
        /// </summary>
        private static async Task<string> TryGetRemoteInputDirAsync(RemoteHost host)
        {
            try
            {
                string url = RemoteApiRequest.BuildUrl(host.Address, "File/Dirs");
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                RemoteApiRequest.AddAuthorization(request, host.Token);
                using var response = await probeHttpClient.SendAsync(request);
                string json = await RemoteApiResponse.ReadAndValidateAsync(response);
                return JsonConvert.DeserializeObject<SimpleFFmpegGUI.Dto.AppDirDto>(json)?.InputDir;
            }
            catch (Exception ex)
            {
                // 拿不到就退回按文件名提交（旧约定）。这里记一条日志：否则"子目录映射被静默丢掉、
                // 远端报输入文件不存在"这种情况无从排查。AppLog 在 OnStartup 里才赋值，故用 ?.
                App.AppLog?.Info($"无法获取远程主机 {host.Name} 的输入目录，本次按文件名提交：{ex.Message}");
                return null;
            }
        }

        [RelayCommand]
        private async Task AddInputAsync()
        {
            try
            {
                FileIOViewModel.AddInput();
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex);
            }
        }

        [RelayCommand]
        private async Task AddInputFilesAsync()
        {
            try
            {
                FileIOViewModel.BrowseFiles();
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex);
            }
        }

        [RelayCommand]
        private async Task AddInputFolderAsync()
        {
            try
            {
                FileIOViewModel.BrowseFolder();
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex);
            }
        }

        [RelayCommand]
        private async Task AddToQueueAsync(TaskEnqueueStrategy strategy)
        {
            var args = CodeArgumentsViewModel.GetArguments();
            try
            {
                if (Type is TaskType.Transcode)
                {
                FFmpegTaskService.TestOutputArguments(args);
                }
            }
            catch (FFmpegArgumentException ex)
            {
                QueueErrorMessage("参数错误", ex);
                return;
            }

            SendMessage(new WindowEnableMessage(false));
            try
            {
                List<InputParameters> inputs = FileIOViewModel.GetInputs();

                List<TaskEntity> createdTasks = new List<TaskEntity>();
                switch (Type)
                {
                    case TaskType.Transcode://需要将输入文件单独加入任务
                        foreach (var input in inputs)
                        {
                            TaskEntity task = await taskManager.AddTaskAsync(TaskType.Transcode, new List<InputParameters>() { input }, FileIOViewModel.GetOutput(input), args);
                            tasks.Tasks.Insert(0, TaskInfoViewModel.FromTask(task));
                            createdTasks.Add(task);
                        }
                        QueueSuccessMessage($"已加入{inputs.Count}个任务队列");
                        break;
                    case TaskType.Custom or TaskType.QualityCheck://不存在文件输出
                        {
                            TaskEntity task = await taskManager.AddTaskAsync(Type, inputs, null, args);
                            tasks.Tasks.Insert(0, TaskInfoViewModel.FromTask(task));
                            createdTasks.Add(task);
                            QueueSuccessMessage("已加入队列");
                        }
                        break;
                    default:
                        {
                            TaskEntity task = await taskManager.AddTaskAsync(Type, inputs, FileIOViewModel.GetOutput(inputs[0]), args);
                            tasks.Tasks.Insert(0, TaskInfoViewModel.FromTask(task));
                            createdTasks.Add(task);
                            QueueSuccessMessage("已加入队列");
                        }
                        break;
                }
                if (Config.Instance.ClearFilesAfterAddTask)
                {
                    FileIOViewModel.Reset(false);
                }
                switch (strategy)
                {
                    case TaskEnqueueStrategy.EnqueueOnly:
                        break;
                    case TaskEnqueueStrategy.EnqueueAndRun:
                        await Task.Run(() => queueService.StartQueue());
                        QueueSuccessMessage("已开始队列");
                        break;
                    case TaskEnqueueStrategy.RunIndependently:
                        if (createdTasks.Count == 1)
                        {
                            await Task.Run(() => queueService.StartStandalone(createdTasks[0].Id));
                            QueueSuccessMessage("已开始独立执行");
                        }
                        else if (createdTasks.Count < 5)
                        {
                            foreach (var t in createdTasks)
                            {
                                await Task.Run(() => queueService.StartStandalone(t.Id));

                            }
                            QueueSuccessMessage($"已开始{createdTasks.Count}个任务的独立执行");
                        }
                        else
                        {
                            QueueSuccessMessage($"同时加入的任务过多，请手动启动任务");
                        }
                        break;
                }
                SaveAsLastOutputArguments(args);
            }
            catch (Exception ex)
            {
                QueueErrorMessage("加入队列失败", ex);
            }
            finally
            {
                SendMessage(new WindowEnableMessage(true));
            }
        }

        [RelayCommand]
        private async Task AddToRemoteHost(bool addToQueue)
        {
            var args = CodeArgumentsViewModel.GetArguments();
            try
            {
                if (Type is TaskType.Transcode)
                {
                FFmpegTaskService.TestOutputArguments(args);
                }
            }
            catch (FFmpegArgumentException ex)
            {
                QueueErrorMessage("参数错误", ex);
                return;
            }

            var items = Config.Instance.RemoteHosts.Select(p =>
            new SelectDialogItem(p.Name, p.Address));
            var index = await CommonDialog.ShowSelectItemDialogAsync(
                "请选择远程主机：输入文件按“源目录对应的本机位置”映射为相对路径提交，子目录一并保留；未配置该位置时需远端输入文件夹根下有同名文件", items);
            if (index < 0)
            {
                return;
            }
            SendMessage(new WindowEnableMessage(false));
            try
            {
                var host = Config.Instance.RemoteHosts[index];
                List<InputParameters> inputs = FileIOViewModel.GetInputs().Adapt<List<InputParameters>>();
                // 映射成远端 InputDir 下的路径：本地文件在远端 InputDir 内时保留子目录（发相对路径），
                // 否则退回只发文件名（约定：远端 InputDir 根目录下有同名文件）
                await MapInputsForRemoteAsync(host, inputs);
                string output = FileIOViewModel.GetOutputFileName();
                var data = new
                {
                    Inputs = inputs,
                    Output = output,
                    Parameter = args,
                };
                await PostAsync(host, "Task/" + Type.ToString(), data);

                if (addToQueue)
                {
                    await PostAsync(host, "Queue/Start", new { });
                }

                if (Config.Instance.ClearFilesAfterAddTask)
                {
                    FileIOViewModel.Reset(false);
                }
                SaveAsLastOutputArguments(args);
                QueueSuccessMessage("已加入到远程主机" + host.Name);
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "加入远程主机失败");
            }
            finally
            {
                SendMessage(new WindowEnableMessage(true));
            }
        }

        [RelayCommand]
        private void ClearInputs()
        {
            FileIOViewModel.Reset(false);
        }

        [RelayCommand]
        private async Task FFmpegArgs()
        {
            try
            {
                OutputParameters args = CodeArgumentsViewModel.GetArguments();
                await CommonDialog.ShowOkDialogAsync("输出参数", FFmpegTaskService.TestOutputArguments(args));
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "获取参数失败");
            }
        }

        async partial void OnTypeChanged(TaskType value)
        {
            FileIOViewModel.UpdateType(value);
            await CodeArgumentsViewModel.UpdateTypeAsync(value);
            await PresetsViewModel.UpdateTypeAsync(value);
        }

        private void SaveAsLastOutputArguments(OutputParameters arguments)
        {
            if (!Config.Instance.RememberLastArguments)
            {
                return;
            }
            Config.Instance.LastOutputArguments.AddOrSetValue(Type, arguments);
            Config.Instance.Save();
        }
    }
}