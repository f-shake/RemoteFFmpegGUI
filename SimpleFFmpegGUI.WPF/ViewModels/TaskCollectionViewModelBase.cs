using CommunityToolkit.Mvvm.ComponentModel;
using SimpleFFmpegGUI.WPF.FzLib;
using SimpleFFmpegGUI.WPF.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public abstract partial class TaskCollectionViewModelBase : ViewModelBase
    {
        public abstract Task RefreshAsync();

        [ObservableProperty]
        private ObservableCollection<TaskInfoViewModel> tasks;

        [ObservableProperty]
        private TaskInfoViewModel selectedTask;

        public IList<TaskInfoViewModel> SelectedTasks=>Tasks.Where(p=>p.IsSelected).ToList();
    }
}