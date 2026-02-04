using CommunityToolkit.Mvvm.Input;
using FzLib;
using Microsoft.Extensions.DependencyInjection;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.WPF.Messages;
using SimpleFFmpegGUI.WPF.Pages;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.Repositories;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public QueueService queue;
        private readonly TaskRepository taskManager;
        private bool isTabControlVisiable = true;

        public MainWindowViewModel(QueueService queue, TaskRepository taskManager)
        {
            this.queue = queue;
            this.taskManager = taskManager;
            queue.TaskManagersChanged += (s, e) => this.Notify(nameof(StartMainQueueButtonVisibility), nameof(StopMainQueueButtonVisibility));
        }

        public Visibility StartMainQueueButtonVisibility => queue.MainQueueTask == null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility StopMainQueueButtonVisibility => queue.MainQueueTask == null ? Visibility.Collapsed : Visibility.Visible;
        public Visibility TabControlVisibility => isTabControlVisiable ? Visibility.Visible : Visibility.Collapsed;
        public Visibility TopTabVisibility => isTabControlVisiable ? Visibility.Collapsed : Visibility.Visible;

        public void SetTabVisiable(bool isTabControlVisiable)
        {
            this.isTabControlVisiable = isTabControlVisiable;
            this.Notify(nameof(TabControlVisibility), nameof(TopTabVisibility));
        }

        [RelayCommand]
        private async Task StartQueueAsync()
        {
            if (!await taskManager.HasQueueTasksAsync())
            {
                QueueErrorMessage("没有排队中的任务");
                return;
            }
            queue.StartQueue();
        }


        [RelayCommand]
        private async Task StopQueueAsync()
        {
            if (!await CommonDialog.ShowYesNoDialogAsync("终止队列", "是否终止队列？"))
            {
                return;
            }
            try
            {
                SendMessage(new WindowEnableMessage(false));
                await queue.CancelAsync();
            }
            catch (Exception ex)
            {
                QueueErrorMessage("终止队列失败", ex);
            }
            finally
            {
                SendMessage(new WindowEnableMessage(true));
            }
        }


        private void ShowPage<T>(bool top = false, bool showWindow = false)
        {
            SendMessage(new AddNewTabMessage(typeof(T), top, showWindow));
        }
        [RelayCommand]
        private void ShowTasks() => ShowPage<TasksPage>();
        [RelayCommand]
        private void ShowPresets() => ShowPage<PresetsPage>();
        [RelayCommand]
        private void ShowSettings() => ShowPage<SettingPage>(true);
        [RelayCommand]
        private void ShowAddTask() => ShowPage<AddTaskPage>();
        [RelayCommand]
        private void ShowFFmpegOutputs() => ShowPage<FFmpegOutputPage>();
        [RelayCommand]
        private void ShowLogs() => ShowPage<LogsPage>();
        [RelayCommand]
        private void ShowMediaInfo() => ShowPage<MediaInfoPage>();
        [RelayCommand]
        private void ShowTests() => ShowPage<TestWindow>(showWindow: true);

    }
}