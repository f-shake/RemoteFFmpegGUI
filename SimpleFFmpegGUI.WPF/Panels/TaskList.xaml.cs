using SimpleFFmpegGUI.WPF.FzLib.WPF;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.WPF;
using SimpleFFmpegGUI.WPF.ViewModels;
using SimpleFFmpegGUI.WPF.Views;
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

        /// <summary>
        /// 是否在任务列表下方显示详情面板。主界面把详情拆成独立列，故设为 false，
        /// “查看所有任务”窗口保持 true（详情仍在列表下方）。
        /// </summary>
        public static readonly DependencyProperty ShowDetailPanelProperty = DependencyProperty.Register(
            nameof(ShowDetailPanel), typeof(bool), typeof(TaskList), new PropertyMetadata(true, OnShowDetailPanelChanged));

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

        public bool ShowDetailPanel
        {
            get => (bool)GetValue(ShowDetailPanelProperty);
            set => SetValue(ShowDetailPanelProperty, value);
        }

        private static void OnShowDetailPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var panel = (TaskList)d;
            // 隐藏详情时把详情行高度压成 0，让列表占满整列，避免底部留白
            panel.rDetailRow.Height = (bool)e.NewValue ? new GridLength(2, GridUnitType.Star) : new GridLength(0);
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
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            bdDetail.Height = double.NaN;
        }
    }
}