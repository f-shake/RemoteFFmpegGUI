using FzLib.WPF;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.WPF;
using SimpleFFmpegGUI.WPF.ViewModels;
using SimpleFFmpegGUI.WPF.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using TaskStatus = SimpleFFmpegGUI.Enums.TaskStatus;

namespace SimpleFFmpegGUI.WPF.Panels
{
    public partial class TaskList : UserControl
    {
        public static readonly DependencyProperty ShowAllTasksProperty = DependencyProperty.Register(
            nameof(ShowAllTasks), typeof(bool), typeof(TaskList));

        public TaskList()
        {
            InitializeComponent();
            ViewModel = this.SetDataContext<TaskListViewModel>();
        }
        public bool ShowAllTasks
        {
            get => (bool)GetValue(ShowAllTasksProperty);
            set => SetValue(ShowAllTasksProperty, value);
        }

        public TaskListViewModel ViewModel { get; }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ShowAllTasksProperty)
            {
                ViewModel.ShowAllTasks = (bool)e.NewValue;
            }
        }
        private void UpdateDetailHeight()
        {
            bdDetail.Height = App.ServiceProvider.GetService<MainWindow>().IsUiCompressMode && !ShowAllTasks ? 108 : double.NaN;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDetailHeight();
            this.GetWindow<MainWindow>().IsUiCompressModeChanged += (s, e) => UpdateDetailHeight();
        }
    }
}