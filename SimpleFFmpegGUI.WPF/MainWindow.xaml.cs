using CommunityToolkit.Mvvm.Messaging;
using SimpleFFmpegGUI.WPF.FzLib.WPF;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.Attributes;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.WPF.Converters;
using SimpleFFmpegGUI.WPF.Messages;
using SimpleFFmpegGUI.WPF.ViewModels;
using SimpleFFmpegGUI.WPF.Views;
using SimpleFFmpegGUI.WPF.Panels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Task = System.Threading.Tasks.Task;
using SimpleFFmpegGUI.Services;

namespace SimpleFFmpegGUI.WPF
{
    public partial class MainWindow : Window
    {
        private readonly QueueService queue;
        private bool hasShownTrayMessage = false;
        private bool isShuttingDown;
        private SimpleFFmpegGUI.WPF.FzLib.Program.Runtime.TrayIcon tray;
        private readonly ChildWindowManager childWindows;
        private TaskList taskPanel;
        private StatusPanel statusPanel;

        public MainWindow(QueueService queue)
        {
            if (Config.Instance.WindowMaximum)
            {
                WindowState = WindowState.Maximized;
            }
            ViewModel = this.SetDataContext<MainWindowViewModel>();
            InitializeComponent();
            RegisterMessages();
            this.queue = queue;
            childWindows = new ChildWindowManager(this, App.ServiceProvider);
            WindowBusyOverlay.Register(this, ring);
            Closed += (_, _) => WindowBusyOverlay.Unregister(this);
        }

        public MainWindowViewModel ViewModel { get; set; }


        protected override void OnClosing(CancelEventArgs e)
        {
            if (!isShuttingDown && queue.Managers.Any(manager => manager.Process?.IsRunning == true))
            {
                e.Cancel = true;
                ShowTrayAndHide();
                return;
            }

            isShuttingDown = true;
            Config.Instance.Save();
            childWindows.CloseAll();
            base.OnClosing(e);
        }

        private void ShowTrayAndHide()
        {
            if (tray == null)
            {
                using var bmp = Bitmap.FromFile("icon.png");
                using var thumb = (Bitmap)bmp.GetThumbnailImage(64, 64, null, IntPtr.Zero);
                thumb.MakeTransparent();
                var icon = System.Drawing.Icon.FromHandle(thumb.GetHicon());
                tray = new SimpleFFmpegGUI.WPF.FzLib.Program.Runtime.TrayIcon(icon, SimpleFFmpegGUI.WPF.FzLib.Program.App.ProgramName);

                tray.MouseLeftClick += (s, e) =>
                {
                    Show();
                    tray.Hide();
                };
                tray.ReShowWhenDisplayChanged = true;
                Closed += (s, e) => tray.Dispose();
            }

            tray.Show();
            Hide();
            if (!hasShownTrayMessage)
            {
                hasShownTrayMessage = true;
                tray.ShowMessage("任务将在后台继续执行");
            }
        }

        protected override async void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            //await Task.Yield();
            //string[] files =
            //[
            //    "ffmpeg.exe",
            //    "ffprobe.exe",
            //    "ffplay.exe",
            //    "mediainfo.exe"
            //];

            //foreach (var file in files)
            //{
            //    if (!File.Exists(file))
            //    {
            //        this.CreateMessage().QueueError("程序目录中缺少文件，将无法正确运行：" + file);
            //        //Close();
            //        return;
            //    }
            //}
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
            }
        }

        protected override async void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            var droppedFiles = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (droppedFiles == null)
            {
                return;
            }
            IEnumerable<string> files = droppedFiles;
            files = files.Where(File.Exists);
            if (files.Any())
            {
                List<SelectDialogItem> items = Enum
                    .GetValues<TaskType>()
                    .Select(p => new SelectDialogItem(AttributeHelper.GetAttributeValue<NameDescriptionAttribute, string>(p, p => p.Name),
                        AttributeHelper.GetAttributeValue<NameDescriptionAttribute, string>(p, p => p.Description)))
                    .ToList();
                items.Add(new SelectDialogItem("查询信息", "查看媒体的元数据信息"));
                int typeCount = Enum.GetValues(typeof(TaskType)).Length;
                Activate();
                var index = await CommonDialog.ShowSelectItemDialogAsync("选择操作", items);
                if (index == -1)
                {
                    return;
                }
                if (index < typeCount)
                {
                    childWindows.Show<AddTaskView>(view => view.SetFiles(files, (TaskType)index));
                }
                else if (index == typeCount)
                {
                    childWindows.Show<MediaInfoView>(view => view.SetFile(files.First()));
                }
            }
        }

        private void RegisterMessages()
        {
            WeakReferenceMessenger.Default.Register<FileDialogMessage>(this, (_, m) =>
            {
                switch (m.Dialog)
                {
                    case OpenFileDialog ofd:
                        m.Result = ofd.ShowDialog(this);
                        break;
                    case SaveFileDialog sfd:
                        m.Result = sfd.ShowDialog(this);
                        break;
                    case OpenFolderDialog ofod:
                        m.Result = ofod.ShowDialog(this);
                        break;
                    default:
                        break;
                }
            });

            WeakReferenceMessenger.Default.Register<WindowHandleMessage>(this, (_, m) =>
            {
                m.Handle = new WindowInteropHelper(this).Handle;
            });

            WeakReferenceMessenger.Default.Register<WindowEnableMessage>(this, (_, m) =>
            {
                // 不再只切换主窗口的加载环，而是把“忙碌”状态交给 WindowBusyOverlay，
                // 由它决定显示在哪个窗口（当前正在操作的那个窗口）上。
                WindowBusyOverlay.SetBusy(!m.IsEnabled);
            });


            WeakReferenceMessenger.Default.Register<OpenViewMessage>(this, (_, m) =>
            {
                if (m.Window)
                {
                    childWindows.ShowWindow(m.Type);
                }
                else if (m.Modal)
                {
                    childWindows.ShowModal(m.Type, m.Initialize);
                }
                else
                {
                    childWindows.Show(m.Type, m.Initialize);
                }
            });

            WeakReferenceMessenger.Default.Register<ShowCodeArgumentsMessage>(this, (_, m) =>
            {
                var task = m.Task;
                Debug.Assert(task != null);
                var panel = new CodeArgumentsPanel
                {
                    IsHitTestVisible = false
                };
                panel.ViewModel.Update(task.Type, task.Arguments);

                // 用普通外层 ScrollViewer 承载滚动：CodeArgumentsPanel 内部的 SmoothScroll 滚动在
                // 独立窗口里不生效（被内部 catch 吞掉），外层普通 ScrollViewer 交由 ViewWindow 限高、滚动可靠。
                // 同时用 ViewWindow（而非 new Window()）避免 iNKORE backdrop 递归导致 StackOverflow。
                ScrollViewer scr = new ScrollViewer
                {
                    Content = panel,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                };
                Window win = new ViewWindow(scr, "详细参数 - FFmpeg工具箱", this, 600, 800);
                win.Show();
            });

            WeakReferenceMessenger.Default.Register<QueueMessagesMessage>(this, (_, m) =>
            {
                switch (m.Type)
                {
                    case 'S':
                        this.CreateMessage().QueueSuccess(m.Message);
                        break;
                    case 'E' when m.Exception == null:
                        this.CreateMessage().QueueError(m.Message);
                        break;
                    case 'E' when m.Exception != null:
                        this.CreateMessage().QueueError(m.Message, m.Exception);
                        break;
                    default:
                        break;
                }
            });
        }

        private void SendSnapshotEnabledMessage()
        {
            WeakReferenceMessenger.Default.Send(new SnapshotEnabledMessage(
                new SnapshotViewModel
                {
                    DisplayFrame = true,
                    CanUpdate = WindowState is WindowState.Maximized or WindowState.Normal
                                    && Visibility == Visibility.Visible
                }));
        }

        /// <summary>
        /// 点击详情浮层外的遮罩：取消选中任务，浮层随之收起（不依赖独立 Popup Hwnd，失激活不崩溃）
        /// </summary>
        private void DetailOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            taskPanel.ViewModel.Tasks.SelectedTask = null;
        }

        private void DetailOverlay_Close(object sender, RoutedEventArgs e)
        {
            taskPanel.ViewModel.Tasks.SelectedTask = null;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            Config.Instance.WindowMaximum = WindowState == WindowState.Maximized;
            SendSnapshotEnabledMessage();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            taskPanel = new TaskList { ShowAllTasks = false, ShowDetailPanel = false };
            statusPanel = new StatusPanel();
            taskHost.Content = taskPanel;
            statusHost.Content = statusPanel;
            SendSnapshotEnabledMessage();
        }
    }
}