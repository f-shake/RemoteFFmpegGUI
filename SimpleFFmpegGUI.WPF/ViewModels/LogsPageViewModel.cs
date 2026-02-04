using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib;
using Mapster;
using SimpleFFmpegGUI.Model;
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
        private IList<Log> logs;

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
            taskManager.GetTasksAsync(take: 20).ContinueWith(data =>
              {
                  Tasks = data.Result.List.Adapt<List<TaskInfoViewModel>>();
              });
            this.logManager = logManager;
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
            Logs = (await logManager.GetLogsAsync(type: Type, taskId: SelectedTask?.Id ?? 0, from: From, to: To)).List;
        }
    }
}