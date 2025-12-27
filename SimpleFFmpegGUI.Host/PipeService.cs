using FFMpegCore;
using FzLib.Collection;
using Mapster;
using Newtonsoft.Json;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SimpleFFmpegGUI.Model.MediaInfo;
using System.Threading.Tasks;
using TaskStatus = SimpleFFmpegGUI.Model.TaskStatus;

namespace SimpleFFmpegGUI
{
    public class PipeService : IPipeService
    {
        private readonly TaskManager taskManager;
        private readonly QueueManager queueManager;
        private readonly PresetManager presetManager;
        private readonly ConfigManager configManager;
        private readonly LogManager logManager;

        public PipeService(TaskManager taskManager,
                           QueueManager queueManager,
                           PresetManager presetManager,
                           ConfigManager configManager,
                           LogManager logManager)
        {
            Logger.Info("已建立与客户端的连接");
            this.taskManager = taskManager;
            this.queueManager = queueManager;
            this.presetManager = presetManager;
            this.configManager = configManager;
            this.logManager = logManager;
        }

        public void StartQueue()
        {
            queueManager.StartQueue();
        }

        public void PauseQueue()
        {
            queueManager.SuspendMainQueue();
        }

        public void ResumeQueue()
        {
            queueManager.ResumeMainQueue();
        }

        public async Task<int> AddTaskAsync(TaskType type, List<InputArguments> inputs, string outputPath, OutputArguments arg)
        {
            int id = (await taskManager.AddTaskAsync(type, inputs, outputPath, arg)).Id;
            return id;
        }

        public Task<MediaInfoGeneral> GetInfoAsync(string path)
        {
            return MediaInfoManager.GetMediaInfoAsync(path);
        }

        public void Join(IEnumerable<string> path)
        {
            throw new NotImplementedException();
        }

        public void CancelQueue()
        {
            queueManager.Cancel();
        }

        public Task<PagedListDto<TaskInfo>> GetTasksAsync(TaskStatus? status = null, int skip = 0, int take = 0)
        {
            return taskManager.GetTasksAsync(status, skip, take);
        }

        public Task<TaskInfo> GetTaskAsync(int id)
        {
            return taskManager.GetTaskAsync(id);
        }

        public StatusDto GetStatus()
        {
            StatusDto status = queueManager.MainQueueManager == null
                ? new StatusDto()
                : queueManager.MainQueueManager.GetStatus();

            return status;
        }

        public Task ResetTaskAsync(int id)
        {
            return taskManager.ResetTaskAsync(id);
        }

        public Task CancelTaskAsync(int id)
        {
            return taskManager.CancelTaskAsync(id);
        }

        public Task ResetTasksAsync(IEnumerable<int> ids)
        {
            return taskManager.TryResetTasksAsync(ids);
        }

        public Task CancelTasksAsync(IEnumerable<int> ids)
        {
            return taskManager.TryCancelTasksAsync(ids);
        }

        public Task DeleteTaskAsync(int id)
        {
            return taskManager.DeleteTaskAsync(id);
        }

        public Task DeleteTasksAsync(IEnumerable<int> ids)
        {
            return taskManager.TryDeleteTasks(ids);
        }

        public Task<int> AddOrUpdatePresetAsync(string name, TaskType type, OutputArguments arguments)
        {
            return presetManager.AddOrUpdatePresetAsync(name, type, arguments);
        }

        public Task DeletePresetAsync(int id)
        {
            return presetManager.DeletePresetAsync(id);
        }

        public Task<List<CodePreset>> GetPresetsAsync()
        {
            return presetManager.GetPresetsAsync();
        }

        public Task<PagedListDto<Log>> GetLogsAsync(char? type = null, int taskId = 0, DateTime? from = null, DateTime? to = null, int skip = 0, int take = 0)
        {
            return logManager.GetLogsAsync(type, taskId, from, to, skip, take);
        }

        public List<string> GetFiles(string dir)
        {
            return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).ToList();
        }

        public List<FileInfoDto> GetFileDetails(string dir)
        {
            return Directory.EnumerateFiles(dir).Select(p => new FileInfoDto(p)).ToList();
        }

        private ConcurrentDictionary<int, FtpManager> ftps = new ConcurrentDictionary<int, FtpManager>();

        public void OpenFtp(int id, string path, int port)
        {
            if (ftps.ContainsKey(id))
            {
                return;
            }
            FtpManager manager = new FtpManager(path, port);
            if (ftps.TryAdd(id, manager))
            {
                manager.StartAsync().Wait();
            }
        }

        public void CloseFtp(int id)
        {
            if (ftps.ContainsKey(id))
            {
                ftps[id].StopAsync().Wait();
                ftps.TryRemove(id, out _);
            }
        }

        public int? GetFtpPort(int id)
        {
            return ftps.GetOrDefault(id)?.Port;
        }

        public bool IsFileExist(string path)
        {
            return File.Exists(path);
        }

        public VideoFormat[] GetSuggestedFormats()
        {
            return VideoFormat.Formats;
        }

        public Task SetDefaultPresetAsync(int id)
        {
            return presetManager.SetDefaultPresetAsync(id);
        }

        public Task<CodePreset> GetDefaultPresetAsync(TaskType type)
        {
            return presetManager.GetDefaultPresetAsync(type);
        }

        public Task ImportPresetsAsync(string json)
        {
            return presetManager.ImportAsync(json);
        }

        public Task<string> ExportPresetsAsync()
        {
            return presetManager.ExportAsync();
        }

        public void SetShutdownAfterQueueFinished(bool v)
        {
            queueManager.PowerManager.ShutdownAfterQueueFinished = v;
        }
        public bool IsShutdownAfterQueueFinished()
        {
            return queueManager.PowerManager.ShutdownAfterQueueFinished;
        }

        public void Shutdown()
        {
            queueManager.PowerManager.Shutdown();
        }
        public void AbortShutdown()
        {
            queueManager.PowerManager.AbortShutdown();
        }

        public CpuCoreUsageDto[] GetCpuUsage(TimeSpan sampleSpan)
        {
            return PowerManager.GetCpuUsageAsync(sampleSpan).Result;
        }

        public void ScheduleQueue(DateTime time)
        {
            queueManager.ScheduleQueue(time);
        }

        public void CancelQueueSchedule()
        {
            queueManager.CancelQueueSchedule();
        }

        public DateTime? GetQueueScheduleTime()
        {
            return queueManager.GetQueueScheduleTime();
        }

        public string GetSingleFileInDir(string dir, string name)
        {
            var files = Directory.EnumerateFiles(dir, name, SearchOption.AllDirectories);
            if (!files.Any())
            {
                throw new Exception("不存在文件" + name);
            }
            if (files.Count() > 2)
            {
                throw new Exception($"存在多个文件名为{name}的文件");
            }
            return files.First();
        }

        public int GetDefaultProcessPriority()
        {
            return configManager.DefaultProcessPriority;
        }

        public void SetDefaultProcessPriority(int priority)
        {
            configManager.DefaultProcessPriority = priority;
            foreach (var task in queueManager.Managers)
            {
                task.Process.Priority = priority;
            }
        }

        public string GetSnapshot(string path, double seconds)
        {
            return MediaInfoManager.GetSnapshotAsync(path, TimeSpan.FromSeconds(seconds), configManager.SnapshotSize).Result;
        }

        public byte[] ReadFiles(string path)
        {
            return File.ReadAllBytes(path);
        }

        public async Task TestAsync()
        {
            await Task.Yield();
        }
    }
}