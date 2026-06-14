using FzLib;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.DependencyInjection;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleFFmpegGUI.WPF.Panels
{

    public partial class PresetsPanel : UserControl
    {
        public PresetsPanel()
        {
            ViewModel = this.SetDataContext<PresetsPanelViewModel>();
            InitializeComponent();
        }

        public PresetsPanelViewModel ViewModel { get; }

        public Task UpdateTypeAsync(TaskType type)
        {
            return ViewModel.UpdateTypeAsync(type);
        }
    }
}