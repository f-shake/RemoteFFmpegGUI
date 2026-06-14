using CommunityToolkit.Mvvm.Messaging;
using FzLib.WPF;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.WPF.Messages;
using SimpleFFmpegGUI.WPF.ViewModels;
using SimpleFFmpegGUI.WPF.Pages;
using System;
using System.Collections;
using System.Collections.Generic;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CommonDialog = iNKORE.Extension.CommonDialog.CommonDialog;
using Path = System.IO.Path;

namespace SimpleFFmpegGUI.WPF.Panels
{
    public partial class FileIOPanel : UserControl
    {
        public FileIOPanel()
        {
            ViewModel = this.SetDataContext<FileIOPanelViewModel>();
            InitializeComponent();
        }

        public FileIOPanelViewModel ViewModel { get; }

        public void Update(TaskType type, List<InputParameters> inputs, string output)
        {
            ViewModel.Update(type, inputs, output);
        }

        public void UpdateType(TaskType type)
        {
            ViewModel.UpdateType(type);
            ViewModel.Reset(true);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];

            foreach (var file in ViewModel.Inputs.Where(p => string.IsNullOrEmpty(p.FilePath)).ToList())
            {
                ViewModel.Inputs.Remove(file);
            }
            foreach (string file in files)
            {
                if (ViewModel.Inputs.Count >= ViewModel.MinInputsCount && !ViewModel.CanChangeInputsCount)
                {
                    break;
                }
                if (File.Exists(file))
                {
                    ViewModel.Inputs.Add(new InputArgumentsViewModel() { FilePath = file });
                }
            }
            while (ViewModel.Inputs.Count < ViewModel.MinInputsCount)
            {
                ViewModel.Inputs.Add(new InputArgumentsViewModel());
            }
        }
    }
}