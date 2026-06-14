using Enterwell.Clients.Wpf.Notifications;
using FzLib.WPF;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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

    public partial class SettingPage : UserControl, ICloseablePage
    {
        public SettingPage()
        {
            ViewModel = this.SetDataContext<SettingPageViewModel>();
            InitializeComponent();
        }

        public event EventHandler RequestToClose
        {
            add => ViewModel.RequestToClose += value;
            remove => ViewModel.RequestToClose -= value;
        }

        public SettingPageViewModel ViewModel { get; set; }
        private void CommandBar_MouseEnter(object sender, MouseEventArgs e)
        {
            Keyboard.ClearFocus();
        }
    }
}