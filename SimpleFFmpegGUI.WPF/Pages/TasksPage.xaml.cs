using Enterwell.Clients.Wpf.Notifications;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using FzLib.WPF;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Dto;
// (usings removed - no longer needed)
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using Size = System.Drawing.Size;

namespace SimpleFFmpegGUI.WPF.Pages
{

    /// <summary>
    /// Interaction logic for TasksPage.xaml
    /// </summary>
    public partial class TasksPage : UserControl
    {
        public TasksPageViewModel ViewModel { get; set; }

        public TasksPage()
        {
            InitializeComponent();
            ViewModel = this.SetDataContext<TasksPageViewModel>();
        }
    }
}