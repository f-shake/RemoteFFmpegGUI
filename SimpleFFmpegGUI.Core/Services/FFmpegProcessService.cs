using SimpleFFmpegGUI.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Models;

namespace SimpleFFmpegGUI.Services;

/// <summary>
/// FFmpeg进程
/// </summary>
public partial class FFmpegProcessService
{
    private readonly Process process = new Process();

    private ProcessPriorityClass priority = default;

    private bool started = false;

    private TaskCompletionSource<bool> tcs;

    private List<string> outputs = new List<string>();

    /// <summary>
    /// 错误信息的识别正则
    /// </summary>
    private static readonly Regex[] ErrorMessageRegexs =
    [
        ErrorMessageRegex1(),
        ErrorMessageRegex2(),
        ErrorMessageRegex3(),
        ErrorMessageRegex4(),
        ErrorMessageRegex5()
    ];


    /// <summary>
    /// 获取错误信息
    /// </summary>
    /// <returns></returns>
    public string GetErrorMessage()
    {
        var errorLogs = outputs
            .AsEnumerable()
            .Reverse()
            .Take(20)
            .Where(p => ErrorMessageRegexs.Any(q => q.IsMatch(p)));
        return string.Join(Environment.NewLine, errorLogs);
    }

    public FFmpegProcessService(IOptionsSnapshot<AppSettings> appSettings, string argument)
    {
        Priority = appSettings.Value.DefaultProcessPriority;
        string ffmpegProgram = "ffmpeg";
        var ffmpegDir = appSettings.Value.FFmpegDir;
        if (!string.IsNullOrEmpty(ffmpegDir))
        {
            var ffmpegProgram1 = Path.Combine(Path.GetFullPath(ffmpegDir), "ffmpeg");
            var ffmpegProgram2 = Path.Combine(Path.GetFullPath(ffmpegDir), "ffmpeg.exe");
            if (File.Exists(ffmpegProgram1))
            {
                ffmpegProgram = ffmpegProgram1;
            }
            else if (File.Exists(ffmpegProgram2))
            {
                ffmpegProgram = ffmpegProgram2;
            }
            else
            {
                throw new Exception($"ffmpeg程序不存在：{ffmpegDir}");
            }
        }

        process.StartInfo = new ProcessStartInfo()
        {
            FileName = ffmpegProgram,
            Arguments = argument,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
        };
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += Process_OutputDataReceived;
        process.ErrorDataReceived += Process_OutputDataReceived;
    }

    public event EventHandler<FFmpegOutputEventArgs> Output;

    /// <summary>
    /// CPU使用率
    /// </summary>
    public double CpuUsage => process.HasExited
        ? process.TotalProcessorTime.TotalSeconds / Environment.ProcessorCount / RunningTime.TotalSeconds
        : throw new Exception("进程还未结束");

    /// <summary>
    /// 进程ID
    /// </summary>
    public int Id => !started ? throw new Exception("进程还未开始运行") : process.Id;

    public int Priority
    {
        get
        {
            if (started && process.HasExited)
            {
                throw new Exception("进程已经退出");
            }

            return priority switch
            {
                ProcessPriorityClass.RealTime => 5,
                ProcessPriorityClass.High => 4,
                ProcessPriorityClass.AboveNormal => 3,
                ProcessPriorityClass.Normal => 2,
                ProcessPriorityClass.BelowNormal => 1,
                ProcessPriorityClass.Idle => 0,
                _ => throw new InvalidEnumArgumentException()
            };
        }
        set
        {
            if (started && process.HasExited)
            {
                throw new Exception("进程已经退出");
            }

            priority = value switch
            {
                5 => ProcessPriorityClass.RealTime,
                4 => ProcessPriorityClass.High,
                3 => ProcessPriorityClass.AboveNormal,
                2 => ProcessPriorityClass.Normal,
                1 => ProcessPriorityClass.BelowNormal,
                0 => ProcessPriorityClass.Idle,
                _ => ProcessPriorityClass.Normal,
            };
            if (started)
            {
                process.PriorityClass = priority;
            }
        }
    }

    /// <summary>
    /// 运行时间
    /// </summary>
    public TimeSpan RunningTime =>
        process.HasExited ? process.ExitTime - process.StartTime : throw new Exception("进程还未结束");

    /// <summary>
    /// 启动进程
    /// </summary>
    /// <param name="workingDir"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Task StartAsync(string workingDir, CancellationToken? cancellationToken)
    {
        if (started)
        {
            throw new Exception("已经开始运行，不可再次运行");
        }

        started = true;

        if (!string.IsNullOrEmpty(workingDir))
        {
            //2Pass时会生成文件名相同的临时文件，如果多个FFmpeg一起运行会冲突，因此需要设置单独的工作目录
            process.StartInfo.WorkingDirectory = workingDir;
        }

        tcs = new TaskCompletionSource<bool>();
        bool exit = false;
        cancellationToken?.Register(() =>
        {
            if (!exit)
            {
                exit = true;
                process.Kill();
            }
        });
        process.Start();
        process.PriorityClass = priority;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.Exited += async (s, e) =>
        {
            var manualExit = exit;
            exit = true;
            try
            {
                await Task.Delay(1000);
                if (process.ExitCode == 0)
                {
                    tcs.SetResult(true);
                }
                else if (manualExit)
                {
                    tcs.SetException(new TaskCanceledException("进程被取消"));
                }
                else
                {
                    var errorLog = GetErrorMessage();
                    tcs.SetException(new Exception($"进程退出：" + (errorLog ?? "未知原因")));
                }

                await Task.Delay(10000);
                process.Dispose();
            }
            catch (Exception ex)
            {
                tcs.SetException(new Exception($"进程处理程序发生错误：" + ex.Message, ex));
            }
        };
        return tcs.Task;
    }

    public Task WaitForExitAsync()
    {
        if (tcs == null)
        {
            throw new Exception("进程还未开始");
        }

        return tcs.Task;
    }

    /// <summary>
    /// 进程输出事件接收
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
        {
            return;
        }

        outputs.Add(e.Data);
        Output?.Invoke(this, new FFmpegOutputEventArgs(e.Data));
    }

    [GeneratedRegex("Error.*", RegexOptions.Compiled)]
    private static partial Regex ErrorMessageRegex1();

    [GeneratedRegex(@"\[.*\] *Unable.*", RegexOptions.Compiled)]
    private static partial Regex ErrorMessageRegex2();

    [GeneratedRegex(@".*Invalid.*", RegexOptions.IgnoreCase | RegexOptions.Compiled, "zh-CN")]
    private static partial Regex ErrorMessageRegex3();

    [GeneratedRegex(@"Could find no file.*", RegexOptions.IgnoreCase | RegexOptions.Compiled, "zh-CN")]
    private static partial Regex ErrorMessageRegex4();

    [GeneratedRegex(@".* error", RegexOptions.IgnoreCase | RegexOptions.Compiled, "zh-CN")]
    private static partial Regex ErrorMessageRegex5();
}