using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib;
using Microsoft.Win32;
using SimpleFFmpegGUI.FFmpegLib;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WPF.Messages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using static SimpleFFmpegGUI.WPF.ViewModels.PerformanceTestLine;
using CommonDialog = iNKORE.Extension.CommonDialog.CommonDialog;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class TestWindowViewModel : ViewModelBase
    {
        public const int SizesCount = 4;
        public const string TestDir = "test";
        public static readonly string[] sizes = ["-1:720", "-1:1080", "-1:1440", "-1:2160"];
        public static readonly string[] sizeTexts = ["720P", "1080P", "1440P", "2160P"];
        private static readonly int[] bitrates = [2, 4, 8, 16];

        [ObservableProperty]
        private double detailProgress = 0;

        [ObservableProperty]
        private bool isEnabled = true;

        [ObservableProperty]
        private bool isTesting = false;

        [ObservableProperty]
        private double maxProgress = 0;

        [ObservableProperty]
        private string message = "";

        [ObservableProperty]
        private double progress = 0;

        [ObservableProperty]
        private int qCMode;

        private readonly IFFmpegTaskServiceFactory ffmpegFactory;
        private FFmpegTaskService runningFFmpeg = null;

        private bool stopping = false;

        [ObservableProperty]
        private string testVideo = "test.mp4";

        public TestWindowViewModel(IFFmpegTaskServiceFactory ffmpegFactory)
        {
            this.ffmpegFactory = ffmpegFactory;
            if (Config.Instance.TestItems == null || Config.Instance.TestItems.Any(p => p == null) || Config.Instance.TestItems.Length != Tests.Length)
            {
                for (int i = 0; i < SizesCount; i++)
                {
                    Tests[i] = new PerformanceTestLine() { Header = sizeTexts[i], MBitrate = bitrates[i] };
                    Tests[i].FillItems();
                }
            }
            else
            {
                Tests = Config.Instance.TestItems;
                if (Tests[0].Items[0].Codec == null)
                {
                    foreach (var test in Tests)
                    {
                        for (int i = 0; i < CodecsCount; i++)
                        {
                            test.Items[i].Codec = VideoCodec.VideoCodecs[i].Name;
                        }
                    }
                }
            }
            if (Config.Instance.TestCodecs == null || Config.Instance.TestCodecs.Any(p => p == null) || Config.Instance.TestCodecs.Length != Codecs.Length)
            {
                for (int i = 0; i < CodecsCount; i++)
                {
                    Codecs[i] = new PerformanceTestCodecParameterViewModel()
                    {
                        CpuSpeed = VideoCodec.VideoCodecs[i].DefaultSpeedLevel,
                        CRF = VideoCodec.VideoCodecs[i].DefaultCRF,
                        ExtraArguments = ""
                    };
                }
            }
            else
            {
                Codecs = Config.Instance.TestCodecs;
            }
            for (int i = 0; i < CodecsCount; i++)
            {
                Codecs[i].Name = VideoCodec.VideoCodecs[i].Name;
                Codecs[i].MaxCpuSpeed = VideoCodec.VideoCodecs[i].MaxSpeedLevel;
                Codecs[i].MaxCRF = VideoCodec.VideoCodecs[i].MaxCRF;
            }

            if (Config.Instance.TestVideo != null)
            {
                TestVideo = Config.Instance.TestVideo;
            }
            QCMode = Config.Instance.TestQCMode;
        }

        public PerformanceTestCodecParameterViewModel[] Codecs { get; } = new PerformanceTestCodecParameterViewModel[CodecsCount];


        public PerformanceTestLine[] Tests { get; } = new PerformanceTestLine[SizesCount];

        [RelayCommand]
        private void BrowseTestVideo()
        {
            var dialog = new OpenFileDialog().AddFilter("视频", "mp4", "mov", "mkv", "avi");
            SendMessage(new FileDialogMessage(dialog));
            string path = dialog.FileName;
            if (!string.IsNullOrEmpty(path))
            {
                TestVideo = path;
            }
        }

        /// <summary>
        /// 创建各分辨率的
        /// </summary>
        /// <param name="input"></param>
        /// <param name="sizes"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task CreateRefVideosAsync(string input, string[] sizes)
        {
            var task = new TaskEntity()
            {
                Inputs = new List<InputParameters>()
                {
                    new InputParameters()
                    {
                        FilePath = input,
                    }
                },
                Parameters = new OutputParameters()
                {
                    Video = new VideoCodecParameters()
                    {
                        Strategy = StreamStrategy.Transcode,
                        Preset = 7,
                        Codec = "H264",
                        Crf = 13,
                    },
                    Audio = new AudioCodecParameters()
                    {
                        Strategy = StreamStrategy.Disable,
                    },
                    Format = "mp4",
                },
                Type = TaskType.Transcode,
            };
            for (int i = 0; i < sizes.Length; i++)
            {
                var size = sizes[i];

                if (!Tests[i].Items.Any(p => p.IsChecked))
                {
                    continue;
                }
                Message = $"正在准备{size.Split(':')[1]}P素材";
                task.Parameters.Video.Size = size;
                task.Output = Path.GetFullPath($"{TestDir}/{size.Split(':')[1]}P.mp4");

                if (File.Exists(task.Output))
                {
                    continue;
                }
                runningFFmpeg = ffmpegFactory.Create(task);
                runningFFmpeg.StatusChanged += (s, e) =>
                {
                    var status = runningFFmpeg.GetStatus();
                    if (status.HasDetail && status.Progress != null && status.Progress.Percent is >= 0 and <= 1)
                    {
                        DetailProgress = status.Progress.Percent;
                    }
                };
                try
                {
                    await runningFFmpeg.RunAsync();
                }
                catch (TaskCanceledException ex)
                {
                    break;
                }
                catch (Exception ex)
                {
                    throw new Exception("编码失败", ex);
                }
                finally
                {
                    runningFFmpeg = null;
                }
            }
        }

        [RelayCommand]
        private void Export()
        {
            var dialog = new SaveFileDialog().AddFilter("CSV表格", "csv");
            dialog.FileName = "编码测试结果.csv";
            SendMessage(new FileDialogMessage(dialog));
            string path = dialog.FileName;
            if (path == null)
            {
                return;
            }
            try
            {

                StringBuilder str = new StringBuilder();
                if (!File.Exists(path))
                {
                    str.AppendLine("Codec,Resolution,Mbitrate,BitrateFactor,CRF,Speed,Extra,FPS,VMAF,SSIM,PSNR,CPU,Duration,OutputSize,Video");
                }
                foreach (var test in Tests)
                {
                    foreach (var item in test.Items.Where(p => p.Fps > 0))
                    {
                        var codec = Codecs.First(p => p.Name == item.Codec);
                        str.Append(item.Codec ?? "")
                            .Append(',')
                            .Append(test.Header)
                            .Append(',')
                            .Append(QCMode == 0 ? test.MBitrate : "")
                            .Append(',')
                            .Append(QCMode == 0 ? codec.BitrateFactor : "")
                            .Append(',')
                            .Append(QCMode == 1 ? codec.CRF : "")
                            .Append(',')
                            .Append(codec.CpuSpeed)
                            .Append(',')
                            .Append(codec.ExtraArguments ?? "")
                            .Append(',')
                            .Append(item.Fps)
                            .Append(',')
                            .Append(item.Vmaf)
                            .Append(',')
                            .Append(item.Ssim)
                            .Append(',')
                            .Append(item.Psnr)
                            .Append(',')
                            .Append(item.CpuUsage)
                            .Append(',')
                            .Append(item.ProcessDuration.TotalSeconds)
                            .Append(',')
                            .Append(item.OutputSize)
                            .Append(',')
                            .Append('"')
                            .Append(TestVideo)
                            .Append('"')
                            .AppendLine();
                    }
                }

                if (!File.Exists(path))
                {
                    File.WriteAllText(path, str.ToString(), new UTF8Encoding(true));
                }
                else
                {
                    File.AppendAllText(path, str.ToString(), new UTF8Encoding(true));
                }
            }
            catch (Exception ex)
            {
                QueueErrorMessage("导出失败", ex);
            }
        }

        [RelayCommand]
        private void OpenDir()
        {
            if (Directory.Exists(TestDir))
            {
                Process.Start("explorer.exe", Path.GetFullPath(TestDir));
            }
            else
            {
                QueueErrorMessage("测试目录不存在");
            }
        }

        [RelayCommand]
        private void Reset()
        {
            if (Directory.Exists(TestDir))
            {
                try
                {
                    File.Delete(TestDir);
                    QueueSuccessMessage("已删除测试目录");
                }
                catch (Exception ex)
                {
                    QueueErrorMessage("无法删除测试目录", ex);
                }
            }
            else
            {
                QueueErrorMessage("测试目录不存在，无需重置");
            }
        }

        public void SaveConfigs()
        {
            Config.Instance.TestVideo = TestVideo;
            Config.Instance.TestCodecs = Codecs;
            Config.Instance.TestItems = Tests;
            Config.Instance.TestQCMode = QCMode;
            Config.Instance.Save();
        }

        [RelayCommand]
        private void SelectAll()
        {
            Tests.ForEach(p => p.Items.ForEach(q => q.IsChecked = true));
        }

        [RelayCommand]
        private void SelectNone()
        {
            Tests.ForEach(p => p.Items.ForEach(q => q.IsChecked = false));
        }

        [RelayCommand]
        private async Task StartAsync()
        {
            SaveConfigs();
            if (!Tests.Any(p => p.Items.Any(q => q.IsChecked)))
            {
                await CommonDialog.ShowErrorDialogAsync("没有选择任何测试项目");
                return;
            }
            try
            {
                stopping = false;
                Progress = 0;
                IsTesting = true;
                Tests.ForEach(q => q.Items.ForEach(p => p.Clear()));
                if (!Directory.Exists(TestDir))
                {
                    Directory.CreateDirectory(TestDir);
                }
                string input = Path.GetFullPath(TestVideo);
                if (!File.Exists(input))
                {
                    throw new FileNotFoundException("不存在测试文件");
                }
                await TestAsync(input);
                SaveConfigs();
            }
            catch (Exception ex)
            {
                if (stopping)
                {
                    Message = "测试终止";
                }
                else
                {
                    await CommonDialog.ShowErrorDialogAsync(ex);
                    Message = ex.Message;
                }
            }
            finally
            {
                IsTesting = false;
                DetailProgress = 1;
                IsEnabled = true;
            }
        }

        [RelayCommand]
        private void Stop()
        {
            IsEnabled = false;
            stopping = true;
            runningFFmpeg?.Cancel();
        }

        /// <summary>
        /// 测试核心代码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task TestAsync(string input)
        {
            var media = await App.ServiceProvider.GetRequiredService<MediaInfoService>().GetMediaInfoAsync(input);
            var frameCount = media.Videos[0].FrameRate * media.Videos[0].DurationSeconds;
            await CreateRefVideosAsync(input, sizes);
            MaxProgress = Tests.Sum(p => p.Items.Count(q => q.IsChecked));
            if (stopping)
            {
                return;
            }


            var task = new TaskEntity()
            {
                Inputs = new List<InputParameters>(),
                Parameters = new OutputParameters()
                {
                    Video = new VideoCodecParameters() { Strategy = StreamStrategy.Transcode },
                    Audio = new AudioCodecParameters() { Strategy = StreamStrategy.Disable },
                    Format = "mp4"
                },
                Type = TaskType.Transcode,
            };
            for (int i = 0; i < CodecsCount; i++)
            {
                for (int j = 0; j < sizes.Length; j++)
                {
                    var test = Tests[j];
                    var item = test.Items[i];
                    if (!item.IsChecked)
                    {
                        continue;
                    }


                    //编码速度测试
                    var codec = Codecs[i];
                    Message = $"正在编码：{codec.Name}，{sizeTexts[j]}";
                    DetailProgress = 0;
                    task.Inputs.Clear();
                    task.Inputs.Add(new InputParameters() { FilePath = Path.GetFullPath($"test/{sizeTexts[j]}.mp4") });
                    task.Parameters.Video.Codec = codec.Name;
                    task.Parameters.Video.Preset = codec.CpuSpeed;
                    task.Parameters.Video.TwoPass = false;
                    switch (QCMode)//平均码率
                    {
                        case 0:
                            task.Parameters.Video.AverageBitrate = test.MBitrate * codec.BitrateFactor;
                            break;
                        default:
                            task.Parameters.Video.Crf = codec.CRF;
                            break;
                    }
                    task.Parameters.Extra = codec.ExtraArguments;
                    string qctext = QCMode == 0 ? $"bitrate={test.MBitrate}M&factor={codec.BitrateFactor}" : $"crf={codec.CRF}";
                    task.Output = Path.GetFullPath($"{TestDir}/codec={codec.Name}&size={sizeTexts[j]}&speed={codec.CpuSpeed}&{qctext}.mp4");

                    runningFFmpeg = ffmpegFactory.Create(task);
                    runningFFmpeg.StatusChanged += (s, e) =>
                    {
                        var status = runningFFmpeg.GetStatus();
                        if (status.HasDetail && status.Progress != null && status.Progress.Percent is >= 0 and <= 1)
                        {
                            DetailProgress = status.Progress.Percent;
                        }
                    };
                    try
                    {
                        await runningFFmpeg.RunAsync();
                        item.CpuUsage = runningFFmpeg.LastProcess.CpuUsage;
                        item.ProcessDuration = runningFFmpeg.LastProcess.RunningTime;
                        item.Fps = frameCount / item.ProcessDuration.TotalSeconds;
                        item.OutputSize = 1.0 * new FileInfo(task.RealOutput).Length / 1024 / 1024;
                    }
                    catch (TaskCanceledException)
                    {
                        goto end;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("编码失败", ex);
                    }
                    finally
                    {
                        runningFFmpeg = null;
                    }


                    if (stopping)
                    {
                        return;
                    }
                    Progress += 0.5;


                    //编码质量测试
                    var qualityTask = new TaskEntity()
                    {
                        Inputs = new List<InputParameters>()
                        {
                            task.Inputs[0],
                            new InputParameters()
                            {
                                FilePath =task.RealOutput
                            }
                        },
                        Parameters = new OutputParameters(),
                        Type = TaskType.QualityCheck,
                    };
                    Message = $"正在质量测试：{codec.Name}，{sizeTexts[j]}";
                    DetailProgress = 0;


                    runningFFmpeg = ffmpegFactory.Create(qualityTask);
                    runningFFmpeg.StatusChanged += (s, e) =>
                    {
                        var status = runningFFmpeg.GetStatus();
                        if (status.HasDetail && status.Progress != null && status.Progress.Percent is >= 0 and <= 1)
                        {
                            DetailProgress = status.Progress.Percent;
                        }
                    };
                    try
                    {
                        await runningFFmpeg.RunAsync();
                    }
                    catch (TaskCanceledException)
                    {
                        goto end;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("质量检测失败", ex);
                    }
                    finally
                    {
                        runningFFmpeg = null;
                    }
                    Progress += 0.5;
                    if (stopping)
                    {
                        return;
                    }

                    string message = qualityTask.Message;
                    item.Ssim = double.Parse(Regex.Match(message, @"All:[0-9\.]+").Value[4..]);
                    item.Psnr = double.Parse(Regex.Match(message, @"average:[0-9\.]+").Value[8..]);
                    try
                    {
                        item.Vmaf = double.Parse(Regex.Match(message, @"VMAF score: [0-9\.]+").Value[12..]);
                    }
                    catch
                    {
                        throw new Exception("未放置VMAF模型文件");
                    }
                }
            }
        end:
            Message = "";
        }

    }
}