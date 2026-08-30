using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SimpleFFmpegGUI.WPF.FzLib;
using Microsoft.Extensions.DependencyInjection;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.WPF.Messages;
using SimpleFFmpegGUI.WPF.ViewModels;
using SimpleFFmpegGUI.WPF.Views;
using SimpleFFmpegGUI.WPF.Panels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TaskStatus = SimpleFFmpegGUI.Enums.TaskStatus;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.Repositories;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class TaskListViewModel : ViewModelBase
    {
        private readonly QueueService queue;

        private readonly TaskRepository taskManager;

        private readonly CurrentTasksViewModel currentTasks;
        private readonly AllTasksViewModel allTasks;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Tasks))]
        private bool showAllTasks;

        public TaskListViewModel(QueueService queue, CurrentTasksViewModel currentTasks, AllTasksViewModel allTasks, TaskRepository taskManager)
        {
            this.queue = queue;
            this.taskManager = taskManager;
            this.currentTasks = currentTasks;
            this.allTasks = allTasks;
            Tasks.PropertyChanged += Tasks_PropertyChanged;
        }

        private void Tasks_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Tasks.SelectedTask))
            {
                NotifyCanExecute();
            }
        }

        public bool CanCancel => Tasks.SelectedTasks.All(p => p.Status is TaskStatus.Queue or TaskStatus.Processing);

        public bool CanReset => Tasks.SelectedTasks.All(p => p.Status is TaskStatus.Done or TaskStatus.Cancel or TaskStatus.Error);

        public bool CanStart => Tasks.SelectedTasks.All(p => p.Status is TaskStatus.Queue);

        public SelectionMode SelectionMode => ShowAllTasks ? SelectionMode.Single : SelectionMode.Extended;

        public TaskCollectionViewModelBase Tasks => ShowAllTasks ? allTasks : currentTasks;

        public void NotifyCanExecute()
        {
            this.Notify(nameof(CanCancel), nameof(CanReset), nameof(CanStart));
        }
        [RelayCommand]
        private async Task CancelAsync()
        {
            var tasks = Tasks.SelectedTasks;
            Debug.Assert(tasks.Count > 0);
            if (tasks.Any(p => p.Status == TaskStatus.Processing))
            {
                if (!await CommonDialog.ShowYesNoDialogAsync("取消任务", "任务正在执行，是否取消？"))
                {
                    return;
                }
            }
            try
            {
                SendMessage(new WindowEnableMessage(false));
                foreach (var task in tasks)
                {
                    await taskManager.UpdateStatusAsync([task.Id], TaskStatus.Cancel);
                    await task.UpdateSelfAsync();
                }
            }
            catch (Exception ex)
            {
                QueueErrorMessage("取消失败", ex);
            }
            finally
            {
                SendMessage(new WindowEnableMessage(true));
            }

            NotifyCanExecute();
        }

        [RelayCommand]
        private void Clone()
        {
            var task = Tasks.SelectedTask.ToTask();
            SendMessage(new OpenViewMessage(typeof(AddTaskView), initialize: view => ((AddTaskView)view).SetAsClone(task)));
        }

        private void OpenFileOrFolder(string path, bool folder)
        {
            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = true
                }
            };
            if (folder)
            {
                p.StartInfo.FileName = "explorer.exe";
                p.StartInfo.Arguments = @$"/select,""{path}""";
            }
            else
            {
                p.StartInfo.FileName = path;
            }
            p.Start();
        }

        [RelayCommand]
        private void ShowArguments()
        {
            SendMessage(new ShowCodeArgumentsMessage(Tasks.SelectedTask));
        }

        [RelayCommand]
        private void OpenOutputDir()
        {
            var task = Tasks.SelectedTask;
            Debug.Assert(task != null);

            OpenOutputFileOrFolder(task, true);
        }

        [RelayCommand]
        private void OpenOutputFile()
        {
            var task = Tasks.SelectedTask;
            Debug.Assert(task != null);
            OpenOutputFileOrFolder(task, false);
        }

        [RelayCommand]
        private void OpenSource()
        {
            var task = Tasks.SelectedTask;
            Debug.Assert(task != null);
            OpenInputFileOrFolder(task, false);
        }

        [RelayCommand]
        private void OpenSourceDir()
        {
            var task = Tasks.SelectedTask;
            Debug.Assert(task != null);
            OpenInputFileOrFolder(task, true);
        }

        private void OpenInputFileOrFolder(TaskInfoViewModel task, bool folder)
        {
            if (task.Inputs == null || task.Inputs.Count == 0)
            {
                QueueErrorMessage("没有输入文件");
                return;
            }
            OpenFileOrFolder(task.Inputs[0].FilePath, folder);
        }

        private void OpenOutputFileOrFolder(TaskInfoViewModel task, bool folder)
        {
            if (string.IsNullOrWhiteSpace(task.RealOutput))
            {
                QueueErrorMessage("输出为空");
                return;
            }
            if (!File.Exists(task.RealOutput))
            {
                QueueErrorMessage("找不到目标文件：" + task.RealOutput);
                return;
            }
            OpenFileOrFolder(task.RealOutput, folder);
        }

        [RelayCommand]
        private async Task ResetAsync()
        {
            var tasks = Tasks.SelectedTasks;
            Debug.Assert(tasks.Count > 0);
            foreach (var task in tasks)
            {
                await taskManager.UpdateStatusAsync([task.Id], TaskStatus.Queue);
                await task.UpdateSelfAsync();
                currentTasks.NotifyTaskReseted(task);
            }

            NotifyCanExecute();
        }

        [RelayCommand]
        private void ShowLogs()
        {
            var task = Tasks.SelectedTask;
            Debug.Assert(task != null);
            SendMessage(new OpenViewMessage(typeof(LogsView), initialize: view => ((LogsView)view).FillLogs(task.Id)));
        }

        [RelayCommand]
        private async Task StartStandaloneAsync()
        {
            try
            {
                var tasks = currentTasks.SelectedTasks;
                Debug.Assert(tasks.Count > 0);
                foreach (var task in tasks)
                {
                    queue.StartStandalone(task.Id);
                    await task.UpdateSelfAsync();
                }
            }
            catch (Exception ex)
            {
                QueueErrorMessage("启动失败", ex);
            }

            NotifyCanExecute();
        }

    }
}