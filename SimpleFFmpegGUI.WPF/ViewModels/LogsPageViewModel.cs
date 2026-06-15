using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib;
using Mapster;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Repositories;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class LogsPageViewModel : ViewModelBase
    {
        private readonly LogRepository logManager;

        [ObservableProperty]
        private DateTime from = DateTime.Now.AddDays(-1);

        [ObservableProperty]
        private IList<LogEntity> logs;

        [ObservableProperty]
        private TaskInfoViewModel selectedTask;

        [ObservableProperty]
        private List<TaskInfoViewModel> tasks;

        [ObservableProperty]
        private DateTime to = DateTime.Today.AddDays(1);

        [ObservableProperty]
        private int typeIndex;

        public LogsPageViewModel(TaskRepository taskManager, LogRepository logManager)
        {
            LoadTasksAsync(taskManager);
            this.logManager = logManager;
        }

        private async void LoadTasksAsync(TaskRepository taskManager)
        {
            try
            {
                var data = await taskManager.GetTasksAsync(new TaskQueryDto { Page = 1, PageSize = 20 });
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    Tasks = data.List.Adapt<List<TaskInfoViewModel>>();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载任务列表失败：{ex.Message}");
            }
        }
        public char? Type => TypeIndex switch
        {
            0 => null,
            1 => 'E',
            2 => 'W',
            3 => 'I',
            4 => 'O',
            _ => throw new NotImplementedException()
        };

        [RelayCommand]
        public async Task FillLogsAsync()
        {
            Logs = (await logManager.GetLogsAsync(new LogQueryRequest { Type = Type, TaskId = SelectedTask?.Id, From = From, To = To, PageSize = 999999 })).List;
        }
    }
}