using CommunityToolkit.Mvvm.ComponentModel;
using FzLib;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;
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
using FzLib.Collection;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using iNKORE.Extension.CommonDialog;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using Mapster;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class AddTaskPageViewModel : ViewModelBase
    {
        private readonly TaskManager taskManager;
        private readonly CurrentTasksViewModel tasks;
        [ObservableProperty]
        private bool allowChangeType = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanAddFile))]
        private TaskType type;

        public AddTaskPageViewModel(TaskManager taskManager, CurrentTasksViewModel tasks)
        {
            this.taskManager = taskManager;
            this.tasks = tasks;
        }

        public bool CanAddFile => Type is TaskType.Code or TaskType.Concat;
        public CodeArgumentsPanelViewModel CodeArgumentsViewModel { get; set; }
        public FileIOPanelViewModel FileIOViewModel { get; set; }
        public PresetsPanelViewModel PresetsViewModel { get; set; }
        public IEnumerable TaskTypes => Enum.GetValues(typeof(TaskType));
        public static async Task PostAsync(RemoteHost host, string subUrl, object data)
        {
            HttpClient client = new HttpClient();
            string str = JsonConvert.SerializeObject(data);
            var content = new StringContent(str, Encoding.UTF8, "application/json");
            string url = host.Address.TrimEnd('/') + "/" + subUrl.TrimStart('/');
            if (!string.IsNullOrEmpty(host.Token))
            {
                client.DefaultRequestHeaders.Add("Authorization", host.Token);
            }
            var response = await client.PostAsync(url, content);

            var responseString = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (string.IsNullOrWhiteSpace(responseString))
                {
                    throw new HttpRequestException($"{response.StatusCode}");
                }
                else
                {
                    throw new HttpRequestException($"{response.StatusCode}：{responseString}");
                }
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
                if (Type is TaskType.Code)
                {
                    FFmpegManager.TestOutputArguments(args);
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
                List<InputArguments> inputs = FileIOViewModel.GetInputs();

                List<TaskInfo> createdTasks = new List<TaskInfo>();
                switch (Type)
                {
                    case TaskType.Code://需要将输入文件单独加入任务
                        foreach (var input in inputs)
                        {
                            TaskInfo task = await taskManager.AddTaskAsync(TaskType.Code, new List<InputArguments>() { input }, FileIOViewModel.GetOutput(input), args);
                            tasks.Tasks.Insert(0, TaskInfoViewModel.FromTask(task));
                            createdTasks.Add(task);
                        }
                        QueueSuccessMessage($"已加入{inputs.Count}个任务队列");
                        break;
                    case TaskType.Custom or TaskType.Compare://不存在文件输出
                        {
                            TaskInfo task = await taskManager.AddTaskAsync(Type, inputs, null, args);
                            tasks.Tasks.Insert(0, TaskInfoViewModel.FromTask(task));
                            createdTasks.Add(task);
                            QueueSuccessMessage("已加入队列");
                        }
                        break;
                    default:
                        {
                            TaskInfo task = await taskManager.AddTaskAsync(Type, inputs, FileIOViewModel.GetOutput(inputs[0]), args);
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
                        await Task.Run(() => App.ServiceProvider.GetService<QueueManager>().StartQueue());
                        QueueSuccessMessage("已开始队列");
                        break;
                    case TaskEnqueueStrategy.RunIndependently:
                        if (createdTasks.Count == 1)
                        {
                            await Task.Run(() => App.ServiceProvider.GetService<QueueManager>().StartStandalone(createdTasks[0].Id));
                            QueueSuccessMessage("已开始独立执行");
                        }
                        else if (createdTasks.Count < 5)
                        {
                            foreach (var t in createdTasks)
                            {
                                await Task.Run(() => App.ServiceProvider.GetService<QueueManager>().StartStandalone(t.Id));

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
                if (Type is TaskType.Code)
                {
                    FFmpegManager.TestOutputArguments(args);
                }
            }
            catch (FFmpegArgumentException ex)
            {
                QueueErrorMessage("参数错误", ex);
                return;
            }

            var items = Config.Instance.RemoteHosts.Select(p =>
            new SelectDialogItem(p.Name, p.Address));
            var index = await CommonDialog.ShowSelectItemDialogAsync("请确保在远程主机的输入文件夹中有同名文件", items);
            if (index < 0)
            {
                return;
            }
            SendMessage(new WindowEnableMessage(false));
            try
            {
                var host = Config.Instance.RemoteHosts[index];
                List<InputArguments> inputs = FileIOViewModel.GetInputs().Adapt<List<InputArguments>>();
                foreach (var i in inputs)
                {
                    //绝对路径，仅保留文件名
                    if (i.FilePath.Contains(':'))
                    {
                        i.FilePath = System.IO.Path.GetFileName(i.FilePath);
                    }
                    i.FilePath = ":" + i.FilePath;
                }
                string output = FileIOViewModel.GetOutputFileName();
                var data = new
                {
                    Inputs = inputs,
                    Output = output,
                    Argument = args,
                    Start = addToQueue
                };
                await PostAsync(host, "Task/Add/" + Type.ToString(), data);

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
                OutputArguments args = CodeArgumentsViewModel.GetArguments();
                await CommonDialog.ShowOkDialogAsync("输出参数", FFmpegManager.TestOutputArguments(args));
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

        private void SaveAsLastOutputArguments(OutputArguments arguments)
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