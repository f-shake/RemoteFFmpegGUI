using FFMpegCore;
using FzLib.Application;
using FzLib.IO;
using FzLib.Programming;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Events;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Repositories;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;
using static SimpleFFmpegGUI.Helpers.FileSystemHelper;
using Task = System.Threading.Tasks.Task;
using TaskStatus = SimpleFFmpegGUI.Enums.TaskStatus;

namespace SimpleFFmpegGUI.Services;

/// <summary>
/// 单个FFmpeg任务管理器
/// </summary>
/// <param name="task">
/// 当前任务
/// </param>
public class FFmpegTaskService(TaskEntity task,
                               DbLoggerService logger,
                               LogRepository logRepository,
                               MediaInfoService mediaInfoService,
                               IFFmpegProcessServiceFactory ffmpegProcessServiceFactory) : INotifyPropertyChanged
{
  
    /// <summary>
    /// 用于识别PSNR的正则
    /// </summary>
    private static readonly Regex rPSNR = new Regex(@"PSNR (([yuvaverageminmax]+:[0-9\. ]+)+)", RegexOptions.Compiled);

    /// <summary>
    /// 用于识别SSIM的正则
    /// </summary>
    private static readonly Regex rSSIM = new Regex(@"SSIM ([YUVAll]+:[0-9\.\(\) ]+)+", RegexOptions.Compiled);

    /// <summary>
    /// 用于识别VMAF的正则
    /// </summary>
    private static readonly Regex rVMAF = new Regex(@"VMAF score: [0-9\.]+", RegexOptions.Compiled);

    /// <summary>
    /// 用于取消任务的token源
    /// </summary>
    private CancellationTokenSource cancel;

    /// <summary>
    /// 任务是否已经开始运行或已经完成
    /// </summary>
    private bool hasRun = false;

    /// <summary>
    /// 最后一个输出
    /// </summary>
    private string lastOutput;

    /// <summary>
    /// 任务是否被暂停
    /// </summary>
    private bool paused;

    /// <summary>
    /// 暂停时，暂停开始的时间
    /// </summary>
    private DateTime pauseStartTime;
    private FFmpegProcessService process;

    /// <summary>
    /// 进程输出事件
    /// </summary>
    public event EventHandler<FFmpegOutputEventArgs> FFmpegOutput;

    public event EventHandler<ProcessChangedEventArgs> ProcessChanged;

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// 任务状态改变事件
    /// </summary>
    public event EventHandler StatusChanged;

    /// <summary>
    /// 用于在任务结束后，保留该实例对应的FFmpeg进程
    /// </summary>
    public FFmpegProcessService LastProcess { get; private set; }

    /// <summary>
    /// 任务是否暂停中
    /// </summary>
    public bool Paused
    {
        get => paused;
        set => this.SetValueAndNotify(ref paused, value, nameof(Paused));
    }

    /// <summary>
    /// FFmpeg进程
    /// </summary>
    public FFmpegProcessService Process
    {
        get => process;
        set
        {
            var old = process;
            process = value;
            ProcessChanged?.Invoke(this, new ProcessChangedEventArgs(old, value));
        }
    }
    /// <summary>
    /// 进度相关属性
    /// </summary>
    public ProgressDto Progress { get; private set; }

    /// <summary>
    /// FFmpeg任务
    /// </summary>
    public TaskEntity Task => task;

    /// <summary>
    /// 测试输出参数是否合法
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static string TestOutputArguments(OutputParameters parameters)
    {
        return ArgumentsGenerator.GetOutputArguments(parameters, parameters.Video?.TwoPass == true ? 2 : 0);
    }

    /// <summary>
    /// 取消任务
    /// </summary>
    public void Cancel()
    {
        logger.Info(task, "取消当前任务");
        task.Status = TaskStatus.Cancel;
        cancel.Cancel();
    }

    /// <summary>
    /// 取消任务
    /// </summary>
    public async Task CancelAsync()
    {
        logger.Info(task, "取消当前任务");
        task.Status = TaskStatus.Cancel;
        cancel.Cancel();
        try
        {
            await Process.WaitForExitAsync();
        }
        catch (TaskCanceledException)
        {

        }
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    /// <returns></returns>
    public StatusDto GetStatus()
    {
        if (Process == null)
        {
            return new StatusDto(task);
        }
        return new StatusDto(task, Progress, lastOutput, paused);
    }

    /// <summary>
    /// 暂停后恢复
    /// </summary>
    /// <exception cref="PlatformNotSupportedException"></exception>
    /// <exception cref="Exception"></exception>
    public void Resume()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("暂停和恢复功能仅支持Windows");
        }
        if (Process == null)
        {
            throw new Exception("进程还未启动，不可暂停或恢复");
        }
        Paused = false;
        Progress.PauseTime += DateTime.Now - pauseStartTime;
        logger.Info(task, "恢复队列");
        ProcessSuspensionHelper.ResumeProcess(Process.Id);
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 执行任务
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    /// <exception cref="NotSupportedException"></exception>
    public async Task RunAsync()
    {
        if (hasRun)
        {
            throw new Exception("一个实例只可运行一次");
        }
        hasRun = true;
        cancel = new CancellationTokenSource();
        try
        {
            logger.Info(task, "开始任务");
            await (task.Type switch
            {
                TaskType.Transcode => RunCodeProcessAsync(cancel.Token),
                TaskType.Mux => RunCombineProcessAsync(cancel.Token),
                TaskType.QualityCheck => RunCompareProcessAsync(cancel.Token),
                TaskType.Custom => RunCustomProcessAsync(cancel.Token),
                TaskType.Concat => RunConcatProcessAsync(cancel.Token),
                _ => throw new NotSupportedException("不支持的任务类型：" + task.Type),
            });

            if (task.Status == TaskStatus.Error)
            {
                Debug.Assert(false);
            }

            if (task.RealOutput != null && File.Exists(task.RealOutput) && task.Parameters?.ProcessedOperationParameters?.SyncModifiedTime == true)
            {
                try
                {
                    var time = File.GetLastWriteTime(task.Inputs[^1].FilePath);
                    File.SetLastWriteTime(task.RealOutput, time);
                    logger.Info(task, $"已设置输出文件的修改时间为{time}");
                }
                catch (Exception ex)
                {
                    logger.Error(task, "修改输出文件的修改时间失败：" + ex.Message);
                }
            }

            if (task.Inputs.Count > 0 && task.Parameters?.ProcessedOperationParameters?.DeleteInputFiles == true)
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (var file in task.Inputs)
                {
                    if (File.Exists(file.FilePath))
                    {
                        try
                        {
                            bool canDeleteToRecycleBin = false;
                            bool deleted = false;
                            if (OperatingSystem.IsWindows())
                            {
                                var root = Path.GetPathRoot(Path.GetFullPath(file.FilePath));
                                canDeleteToRecycleBin = allDrives.First(p => p.Name == root).DriveType == DriveType.Fixed;
                            }
                            if (canDeleteToRecycleBin)
                            {
                                try
                                {
                                    FileDeleteHelper.DeleteToRecycleBin(file.FilePath);
                                    deleted = true;
                                    logger.Info(task, $"已将输入文件{file.FilePath}移至回收站");
                                }
                                catch (Exception ex)
                                {
                                    logger.Error(task, $"将输入文件{file.FilePath}移至回收站失败：{ex.Message}");
                                }
                            }
                            if (!deleted)
                            {
                                File.Delete(file.FilePath);
                                logger.Info(task, $"已彻底删除输入文件{file.FilePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(task, $"删除输入文件{file.FilePath}失败：{ex.Message}");
                        }
                    }
                }
            }

            logger.Info(task, "完成任务");
        }
        finally
        {
            Progress = null;
        }
    }

    /// <summary>
    /// 暂停任务
    /// </summary>
    /// <exception cref="PlatformNotSupportedException"></exception>
    /// <exception cref="Exception"></exception>
    public void Suspend()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("暂停和恢复功能仅支持Windows");
        }
        if (Process == null)
        {
            throw new Exception("进程还未启动或该任务不允许暂停");
        }
        Paused = true;
        logger.Info(task, "暂停队列");
        pauseStartTime = DateTime.Now;
        ProcessSuspensionHelper.SuspendProcess(Process.Id);
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 获取进度信息
    /// </summary>
    /// <param name="onlyCalcFirstVideoDuration"></param>
    /// <returns></returns>
    private ProgressDto GetProgress(bool onlyCalcFirstVideoDuration = false)
    {
        var p = new ProgressDto();
        if (task.Inputs.Count == 1 || onlyCalcFirstVideoDuration)
        {
            p.VideoLength = GetVideoDuration(task.Inputs[0]);
        }
        else
        {
            var durations = task.Inputs.Select(p => GetVideoDuration(p));
            p.VideoLength = durations.All(p => p.HasValue) ? TimeSpan.FromTicks(durations.Select(p => p.Value.Ticks).Sum()) : null;
        }
        p.StartTime = DateTime.Now;
        return p;
    }

    /// <summary>
    /// 获取视频的长度
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private TimeSpan? GetVideoDuration(InputParameters arg)
    {
        var path = arg.FilePath;
        TimeSpan realLength;
        try
        {
            realLength = mediaInfoService.GetVideoDurationByFFprobe(path);

            logger.Info($"视频的长度为：{realLength}");
        }
        catch (Exception ex)
        {
            logger.Info($"获取视频长度失败：{ex}");
            return null;
        }
        if (arg == null || arg.From == null && arg.To == null && arg.Duration == null)
        {
            return realLength;
        }

        TimeSpan start = TimeSpan.Zero;
        if (arg.From.HasValue)
        {
            start = arg.From.Value;
        }
        if (arg.To == null && arg.Duration == null)
        {
            if (realLength <= arg.From.Value)
            {
                throw new FFmpegArgumentException("开始时间在视频结束时间之后");
            }
            return realLength - arg.From.Value;
        }
        else if (arg.Duration.HasValue)
        {
            TimeSpan endTime = (arg.From.HasValue ? arg.From.Value : TimeSpan.Zero) + arg.Duration.Value;
            if (endTime > realLength)
            {
                throw new FFmpegArgumentException("裁剪后的结束时间在视频结束时间之后");
            }
            return arg.Duration.Value;
        }
        else if (arg.To.HasValue)
        {
            if (arg.To.Value > realLength)
            {
                throw new FFmpegArgumentException("裁剪后的结束时间在视频结束时间之后");
            }
            return arg.To.Value - start;
        }

        throw new Exception("未知情况");
    }
    /// <summary>
    /// FFmpeg进程输出
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Output(object sender, FFmpegOutputEventArgs e)
    {
        lastOutput = e.Data;
        logger.Output(task, e.Data);
        FFmpegOutput?.Invoke(this, new FFmpegOutputEventArgs(e.Data));
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 执行任务
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="desc"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="workingDir"></param>
    /// <returns></returns>
    private async Task RunAsync(string arguments, string desc, CancellationToken cancellationToken, string workingDir = null)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            logger.Info(task, "进程启动前就被要求取消");
            return;
        }
        logger.Info(task, "FFmpeg参数为：" + arguments);
        task.FFmpegArguments = string.IsNullOrEmpty(task.FFmpegArguments) ? arguments : task.FFmpegArguments + ";" + arguments;
        if (Progress != null)
        {
            Progress.Name = desc;
        }

        Process = ffmpegProcessServiceFactory.Create(arguments);
        Process.Output += Output;
        try
        {
            await Process.StartAsync(workingDir, cancellationToken);
        }
        finally
        {
            LastProcess = Process;
            Process = null;
        }
    }

    /// <summary>
    /// 执行编码任务
    /// </summary>
    /// <param name="tempDir"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private async Task RunCodeProcessAsync(CancellationToken cancellationToken)
    {
        if (task.Inputs.Count != 1)
        {
            throw new ArgumentException("普通编码，输入文件必须为1个");
        }
        if (task.Parameters == null)
        {
            throw new InvalidOperationException("转码任务缺少参数");
        }
        //处理图像序列名
        if (task.Inputs.Count == 1 && task.Inputs[0].Image2)
        {
            string seq = GetSequence(task.Inputs[0].FilePath);
            if (seq != null)
            {
                task.Inputs[0].FilePath = seq;
            }
        }
        GenerateOutputPath(task);
        string message;
        if (task.Parameters.Video == null || !task.Parameters.Video.TwoPass)
        {
            Progress = GetProgress();
            message = $"正在转码：{Path.GetFileName(task.Inputs[0].FilePath)}";
            string arg = ArgumentsGenerator.GetArguments(task, 0);
            await RunAsync(arg, message, cancellationToken);
        }
        else
        {
            string tempDirectory = GetTempDir("2pass");
            Directory.CreateDirectory(tempDirectory);

            VideoArgumentsGenerator vag = new VideoArgumentsGenerator();
            vag.Codec(task.Parameters.Video.Codec);

            Progress = GetProgress();
            Progress.VideoLength *= 2;

            message = $"正在转码（Pass=1）：{Path.GetFileName(task.Inputs[0].FilePath)}";
            string arg = ArgumentsGenerator.GetArguments(task, 1, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "NUL" : "/dev/null");
            await RunAsync(arg, message, cancellationToken, tempDirectory);

            Progress.BasePercent = 0.5;
            message = $"正在转码（Pass=2）：{Path.GetFileName(task.Inputs[0].FilePath)}";
            arg = ArgumentsGenerator.GetArguments(task, 2);
            await RunAsync(arg, message, cancellationToken, tempDirectory);
        }

    }

    /// <summary>
    /// 执行音视频合并任务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private async Task RunCombineProcessAsync(CancellationToken cancellationToken)
    {
        if (task.Inputs.Count != 2)
        {
            throw new ArgumentException("合并音视频操作，输入文件必须为2个");
        }
        var video = FFProbe.Analyse(task.Inputs[0].FilePath);
        var audio = FFProbe.Analyse(task.Inputs[1].FilePath);
        if (video.VideoStreams.Count == 0)
        {
            throw new ArgumentException("输入1不含视频");
        }
        if (audio.AudioStreams.Count == 0)
        {
            throw new ArgumentException("输入2不含音频");
        }

        Progress = GetProgress(true);
        GenerateOutputPath(task);

        var outputArgs = ArgumentsGenerator.GetOutputArguments(v => v.Copy(), a => a.Copy(),
            s => (video.AudioStreams.Count != 0 || audio.VideoStreams.Count != 0) ? s.Map(0, StreamChannel.Video, 0).Map(0, StreamChannel.Audio, 0) : s);
        string arg = ArgumentsGenerator.GetArguments(task.Inputs, outputArgs, task.RealOutput);

        await RunAsync(arg, "正在合并音视频", cancellationToken);
    }

    /// <summary>
    /// 执行视频对比任务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="FFmpegArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    private async Task RunCompareProcessAsync(CancellationToken cancellationToken)
    {
        if (task.Inputs.Count != 2)
        {
            throw new FFmpegArgumentException("视频比较，输入文件必须为2个");
        }
        var v1 = FFProbe.Analyse(task.Inputs[0].FilePath);
        var v2 = FFProbe.Analyse(task.Inputs[1].FilePath);
        if (v1.VideoStreams.Count == 0)
        {
            throw new FFmpegArgumentException("输入1不含视频");
        }
        if (v2.VideoStreams.Count == 0)
        {
            throw new FFmpegArgumentException("输入2不含视频");
        }
        Progress = GetProgress(true);
        string argument = "-lavfi \"ssim;[0:v][1:v]psnr\" -f null -";
        string vmafModel = Directory.EnumerateFiles(ApplicationInfo.ProgramDirectoryPath, "vmaf*.json").FirstOrDefault();
        if (vmafModel != null)
        {
            vmafModel = Path.GetFileName(vmafModel);
            argument = @$"-lavfi ""ssim;[0:v][1:v]psnr;[0:v]setpts=PTS-STARTPTS[reference]; [1:v]setpts=PTS-STARTPTS[distorted]; [distorted][reference]libvmaf=model=version=vmaf_v0.6.1:n_threads={Environment.ProcessorCount}""  -f null -";
        }
        var arg = ArgumentsGenerator.GetArguments(task.Inputs, argument);
        FFmpegOutput += CheckOutput;
        string ssim = null;
        string psnr = null;
        string vmaf = null;
        try
        {
            await RunAsync(arg, $"正在对比 {Path.GetFileName(task.Inputs[0].FilePath)} 和 {Path.GetFileName(task.Inputs[1].FilePath)}", cancellationToken);
            if (ssim == null || psnr == null)
            {
                throw new Exception("对比视频失败，未识别到对比结果");
            }
            task.Message = ssim + Environment.NewLine + psnr + (vmaf == null ? "" : (Environment.NewLine + vmaf));
        }
        finally
        {
            FFmpegOutput -= CheckOutput;
        }

        void CheckOutput(object sender, FFmpegOutputEventArgs e)
        {
            if (!e.Data.StartsWith('['))
            {
                return;
            }
            if (rSSIM.IsMatch(e.Data))
            {
                var match = rSSIM.Match(e.Data);
                ssim = match.Value;
                logger.Info(task, "对比结果（SSIM）：" + match.Value);
            }
            if (rPSNR.IsMatch(e.Data))
            {
                var match = rPSNR.Match(e.Data);
                psnr = match.Value;
                logger.Info(task, "对比结果（PSNR）：" + match.Value);
            }
            if (rVMAF.IsMatch(e.Data))
            {
                var match = rVMAF.Match(e.Data);
                vmaf = match.Value;
                logger.Info(task, "对比结果（VMAF）：" + match.Value);
            }
        }
    }

    /// <summary>
    /// 执行视频拼接任务
    /// </summary>
    /// <param name="tempDir"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    private async Task RunConcatProcessAsync(CancellationToken cancellationToken)
    {
        if (task.Inputs.Count < 2)
        {
            throw new ArgumentException("拼接视频，输入文件必须为2个或更多");
        }
        string message = $"正在拼接：{task.Inputs.Count}个文件";

        string tempPath = GetTempFileName("concat") + ".txt";
        using (var stream = File.CreateText(tempPath))
        {
            foreach (var file in task.Inputs)
            {
                stream.WriteLine($"file '{file.FilePath}'");
            }
        }
        Progress = GetProgress();
        GenerateOutputPath(task);
        var input = new InputParameters()
        {
            FilePath = tempPath,
            Format = "concat",
            Extra = "-safe 0"
        };

        string arg = ArgumentsGenerator.GetArguments(new InputParameters[] { input }, "-c copy", task.RealOutput);

        await RunAsync(arg, message, cancellationToken);
    }

    /// <summary>
    /// 执行自定义任务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task RunCustomProcessAsync(CancellationToken cancellationToken)
    {
        await RunAsync(task.Parameters.Extra, null, cancellationToken);
    }
}
