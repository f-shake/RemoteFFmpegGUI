using Enterwell.Clients.Wpf.Notifications;
using FFMpegCore.Exceptions;
using FzLib;
using FzLib.Collection;
using FzLib.WPF;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleFFmpegGUI.WPF.Pages
{

    /// <summary>
    /// Interaction logic for AddTaskPage.xaml
    /// </summary>
    public partial class AddTaskPage : UserControl
    {
        private bool canInitializeType = true;
        public AddTaskPage()
        {
            DataContext = ViewModel;
            ViewModel = this.SetDataContext<AddTaskPageViewModel>();
            InitializeComponent();
            presetsPanel.ViewModel.CodeArgumentsViewModel = argumentsPanel.ViewModel;
            ViewModel.CodeArgumentsViewModel = argumentsPanel.ViewModel;
            ViewModel.PresetsViewModel = presetsPanel.ViewModel;
            ViewModel.FileIOViewModel = fileIOPanel.ViewModel;
        }

        public AddTaskPageViewModel ViewModel { get; set; }
        public void SetAsClone(TaskEntity task)
        {
            canInitializeType = false;
            ViewModel.SetAndNotify(nameof(ViewModel.Type), task.Type);
            fileIOPanel.Update(task.Type, task.Inputs, task.Output);
            argumentsPanel.Update(task.Type, task.Parameters);
        }

        public void SetFiles(IEnumerable<string> files, TaskType type)
        {
            canInitializeType = false;
            ViewModel.SetAndNotify(nameof(ViewModel.Type), type);
            fileIOPanel.Update(type, files.Select(p => new InputParameters() { FilePath = p }).ToList(), null);
        }

        private void CommandBar_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as FrameworkElement).Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (canInitializeType)
            {
                ViewModel.SetAndNotify(nameof(ViewModel.Type),ViewModel.Type);
                canInitializeType = false;
            }
        }
    }
}