using Enterwell.Clients.Wpf.Notifications;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleFFmpegGUI.WPF.Views
{
    public partial class LogsView : UserControl
    {
        public LogsPageViewModel ViewModel { get; set; }

        public LogsView()
        {
            ViewModel = this.SetDataContext<LogsPageViewModel>();
            InitializeComponent();
        }
        public async void FillLogs(int taskID)
        {
            var task = ViewModel.Tasks.FirstOrDefault(p => p.Id == taskID);
            Debug.Assert(task != null);
            ViewModel.SelectedTask = task;
            ViewModel.From = DateTime.MinValue;
            await ViewModel.FillLogsAsync();
        }
    }
}