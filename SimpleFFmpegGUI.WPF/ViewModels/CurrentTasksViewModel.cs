using SimpleFFmpegGUI.WPF.FzLib;
using SimpleFFmpegGUI.WPF.FzLib.Collection;
using Mapster;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Models.Entities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System;
using System.Windows;
using System.Windows.Shell;
using System.Threading.Tasks;
using TaskStatus = SimpleFFmpegGUI.Enums.TaskStatus;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using SimpleFFmpegGUI.WPF.Messages;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.Repositories;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class CurrentTasksViewModel : TaskCollectionViewModelBase
    {
        [ObservableProperty]
        private List<TaskInfoViewModel> processingTasks;

        private readonly TaskRepository taskManager;

        public CurrentTasksViewModel(QueueService queue, TaskRepository tm)
        {
            Queue = queue;
            taskManager = tm;
            queue.TaskManagersChanged += Queue_TaskManagersChanged;
            RefreshAsync();
            WeakReferenceMessenger.Default.Register<SnapshotEnabledMessage>(this, async (_, m) =>
            {
                foreach (var task in Tasks.ToList())
                {
                    task.Snapshot.DisplayFrame = m.Options.DisplayFrame;
                    task.Snapshot.CanUpdate = m.Options.CanUpdate;
                    await task.UpdateSnapshotAsync();
                }
            });
        }

        public QueueService Queue { get; }

        public ObservableCollection<StatusDto> Statuses { get; } = new ObservableCollection<StatusDto>();

        public void NotifyTaskReseted(TaskInfoViewModel task)
        {
            if (!Tasks.Any(p => p.Id == task.Id))
            {
                Tasks.Add(task);
            }
        }

        public override async Task RefreshAsync()
        {
            var tasks = await taskManager.GetCurrentTasksAsync();
            Tasks = new ObservableCollection<TaskInfoViewModel>(tasks.Adapt<List<TaskInfoViewModel>>());
        }

        private static void GetMainWindowAnd(Action<MainWindow> action)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = App.ServiceProvider.GetService<MainWindow>();
                Debug.Assert(mainWindow != null);
                action(mainWindow);
            });
        }

        private void Manager_StatusChanged(object sender, EventArgs e)
        {
            var manager = sender as FFmpegTaskService;
            var newStatus = manager.GetStatus();

            var task = Tasks.FirstOrDefault(p => p.Id == newStatus.Task.Id);
            Debug.Assert(task != null);
            Debug.Assert(task.ProcessStatus != null);

            task.ProcessStatus = newStatus;
            if (manager == Queue.MainQueueManager || Queue.Managers.Count == 1)//主队列，或者只有一个任务，则在状态栏上显示进度
            {
                GetMainWindowAnd(mainWindow =>
                {
                    if (newStatus.HasDetail)
                    {
                        mainWindow.TaskbarItemInfo.ProgressState =
                        manager.Paused ? TaskbarItemProgressState.Paused : TaskbarItemProgressState.Normal;
                        mainWindow.TaskbarItemInfo.ProgressValue = newStatus.Progress.Percent;
                    }
                    else
                    {
                        mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                    }
                });
            }
        }

        /// <summary>
        /// 任务队列发生改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Queue_TaskManagersChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // 事件可能在任务线程触发，操作 UI 集合前先编组到 UI 线程（P2-3）
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                await Application.Current.Dispatcher.InvokeAsync(() => Queue_TaskManagersChanged(sender, e));
                return;
            }
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)//新增任务
            {
                var manager = e.NewItems[0] as FFmpegTaskService;
                var unstartStatus = new StatusDto(manager.Task); //先放入一个StatusDto进行占位，因为此时Status还未生成

                var task = Tasks.FirstOrDefault(p => p.Id == manager.Task.Id);//找到对应的TaskInfoViewModel
                Debug.Assert(task != null);
                await task.UpdateSelfAsync(); //用TaskInfo实体更新TaskInfoViewModel
                task.ProcessStatus = unstartStatus;
                task.ProcessManager = manager;
                if (manager == Queue.MainQueueManager)
                {
                    Statuses.Insert(0, unstartStatus);
                }
                else
                {
                    Statuses.Add(unstartStatus);
                }
                manager.StatusChanged += Manager_StatusChanged;
                // 启动后新加入的处理任务也需开启缩略图开关（DisplayFrame）：
                // 否则 UpdateSnapshotAsync 第一步即 return，右侧不显示缩略图。
                // 复用 MainWindow 的广播，以正确处理 CanUpdate（窗口可见性）。
                // 此处已由方法顶部 marshal 到 UI 线程，直接用单例 MainWindow 发广播即可（避免重复 Dispatcher.Invoke）。
                App.ServiceProvider.GetService<MainWindow>()?.SendSnapshotEnabledMessage();
            }
            else
            {
                var manager = e.OldItems[0] as FFmpegTaskService;
                var status = Statuses.FirstOrDefault(p => p.Task.Id == manager.Task.Id);
                Debug.Assert(status != null);

                var task = Tasks.FirstOrDefault(p => p.Id == manager.Task.Id);
                Debug.Assert(task != null);
                task.ProcessManager = null;
                task.ProcessStatus = null;
                await task.UpdateSelfAsync();

                Statuses.Remove(status);
                manager.StatusChanged -= Manager_StatusChanged;
                GetMainWindowAnd(async mainWindow =>
                {
                    if (task.Status is TaskStatus.Error or TaskStatus.Cancel)
                    {
                        mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                        await System.Threading.Tasks.Task.Delay(1000);
                    }
                    mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                    Debug.Write("finish");
                });
            }
            ProcessingTasks = Tasks.Where(p => p.ProcessStatus != null).ToList();
        }
    }
}