using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimpleFFmpegGUI.WPF.FzLib;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class TasksPageViewModel : ViewModelBase
    {
        public TasksPageViewModel(AllTasksViewModel allTasks)
        {
            AllTasks = allTasks;
            AllTasks.PropertyChanged += AllTasks_PropertyChanged;
            RefreshPages();
        }

        private void AllTasks_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RefreshPages();
        }

        [RelayCommand]
        private void RefreshPages()
        {
            var pages = Enumerable.Range(0, AllTasks.PageCount)
                 .Select(p => new
                 {
                     Label = "第" + (p + 1) + "页",
                     Value = p,
                     Value1 = p + 1
                 });
            if (Pages == null || pages.Count() != Pages.Cast<object>().Count())
            {
                Pages = pages.ToList();
            }
        }

        [ObservableProperty]
        private IEnumerable pages;

        public AllTasksViewModel AllTasks { get; }
    }
}