using Enterwell.Clients.Wpf.Notifications;
using FzLib;
using FzLib.WPF;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using iNKORE.Extension.CommonDialog;
using Newtonsoft.Json;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

namespace SimpleFFmpegGUI.WPF.Pages
{
    public partial class PresetsPage : UserControl
    {
        public PresetsPage()
        {
            ViewModel = this.SetDataContext<PresetsPageViewModel>();
            InitializeComponent();
            ViewModel.CodeArgumentsViewModel = argumentsPanel.ViewModel;
            Loaded += async (s, e) => await ViewModel.FillPresetsAsync();
        }

        public PresetsPageViewModel ViewModel { get; set; }
      
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is PresetEntity preset)
            {
                grd.RowDefinitions[2].Height = new GridLength(48);
                lvw.IsHitTestVisible = false;
                lvw.ScrollIntoView(preset);
                grd.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);
                argumentsPanel.Update(preset.Type, preset.Parameters);
            }
            else
            {
                grd.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
                lvw.IsHitTestVisible = true;
                grd.RowDefinitions[4].Height = new GridLength(0);
            }
        }
    }
}