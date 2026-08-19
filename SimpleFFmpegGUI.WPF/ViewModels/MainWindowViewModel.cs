using CommunityToolkit.Mvvm.Input;
using SimpleFFmpegGUI.WPF.FzLib;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.WPF.Messages;
using SimpleFFmpegGUI.WPF.Views;
using System;
using System.Threading.Tasks;
using System.Windows;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.Repositories;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public QueueService queue;
        private readonly TaskRepository taskManager;
        public MainWindowViewModel(QueueService queue, TaskRepository taskManager)
        {
            this.queue = queue;
            this.taskManager = taskManager;
            queue.TaskManagersChanged += (s, e) => this.Notify(nameof(StartMainQueueButtonVisibility), nameof(StopMainQueueButtonVisibility));
        }

        public Visibility StartMainQueueButtonVisibility => queue.MainQueueTask == null ? Visibility.Visible : Visibility.Collapsed;
        public Visibility StopMainQueueButtonVisibility => queue.MainQueueTask == null ? Visibility.Collapsed : Visibility.Visible;
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


        private void ShowView<T>(bool modal = false, bool window = false)
        {
            SendMessage(new OpenViewMessage(typeof(T), modal, window));
        }
        [RelayCommand]
        private void ShowTasks() => ShowView<TasksView>();
        [RelayCommand]
        private void ShowPresets() => ShowView<PresetsView>();
        [RelayCommand]
        private void ShowSettings() => ShowView<SettingView>(modal: true);
        [RelayCommand]
        private void ShowAddTask() => ShowView<AddTaskView>();
        [RelayCommand]
        private void ShowFFmpegOutputs() => ShowView<FFmpegOutputView>();
        [RelayCommand]
        private void ShowLogs() => ShowView<LogsView>();
        [RelayCommand]
        private void ShowMediaInfo() => ShowView<MediaInfoView>();
        [RelayCommand]
        private void ShowTests() => ShowView<TestWindow>(window: true);

    }
}