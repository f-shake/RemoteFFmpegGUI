using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SimpleFFmpegGUI.WPF.FzLib;
using SimpleFFmpegGUI.WPF.FzLib.WPF.Converters;
using Mapster;
using Microsoft.Win32;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Helpers;
using SimpleFFmpegGUI.Models.MediaParameters;
using SimpleFFmpegGUI.WPF.Messages;
using SimpleFFmpegGUI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using WinRT;
using CommonDialog = iNKORE.Extension.CommonDialog.CommonDialog;
using Path = System.IO.Path;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class FileIOPanelViewModel : ViewModelBase
    {
        /// <summary>
        /// 是否可以修改输入文件数量
        /// </summary>
        [ObservableProperty]
        private bool canChangeInputsCount;

        /// <summary>
        /// 最多输入文件的个数
        /// </summary>
        [ObservableProperty]
        private int maxInputsCount = int.MaxValue;

        [ObservableProperty]
        private int minInputsCount = 1;

        /// <summary>
        /// 输出目录
        /// </summary>
        [ObservableProperty]
        private string outputDir;

        /// <summary>
        /// 输出文件名
        /// </summary>
        [ObservableProperty]
        private string outputFileName;

        /// <summary>
        /// 是否可用视频分割
        /// </summary>
        [ObservableProperty]
        private bool showTimeClip;

        /// <summary>
        /// 是否显示「更多」面板（图像帧序列 / 输入帧率 / 其他参数）。
        /// 只有会把这些输入参数拼进命令行的类型才显示：转码、混流、质量测试（<c>ArgumentsGenerator.GetInputArguments</c>
        /// 会读 <c>Framerate</c>/<c>Extra</c>）。拼接的执行器自己造输入、只取 <c>FilePath</c>
        /// （<c>FFmpegTaskService.RunConcatProcessAsync</c>），显示出来用户填了也不生效；自定义任务没有输入行。
        /// </summary>
        [ObservableProperty]
        private bool showMoreOptions;

        /// <summary>
        /// 任务类型
        /// </summary>
        [NotifyPropertyChangedFor(nameof(CanSetOutputFileName))]
        [ObservableProperty]
        private TaskType type;

        public FileIOPanelViewModel()
        {
            for (int i = 0; i < MinInputsCount; i++)
            {
                Inputs.Add(new InputArgumentsViewModel() { Index = i + 1, });
            }
            Inputs.CollectionChanged += Inputs_CollectionChanged;
            Config.Instance.PropertyChanged += (s, e) => this.Notify(nameof(OutputDirPlaceholder));
        }

        /// <summary>
        /// 是否可以设置输出文件名
        /// </summary>
        public bool CanSetOutputFileName => !(Type == TaskType.Transcode && Inputs.Count > 1);

        public ObservableCollection<InputArgumentsViewModel> Inputs { get; } = new ObservableCollection<InputArgumentsViewModel>();

        /// <summary>
        /// 输出目录的提示
        /// </summary>
        public string OutputDirPlaceholder => "若为空，则保存到" +
            Config.Instance.DefaultOutputDirType switch
            {
                DefaultOutputDirType.InputDir => DescriptionConverter.GetDescription(DefaultOutputDirType.InputDir),
                DefaultOutputDirType.InputNewDir => $"输入文件同级的{Config.Instance.DefaultOutputDirInputSubDirName}目录",
                DefaultOutputDirType.SpecialDir => Config.Instance.DefaultOutputDirSpecialDirPath,
                _ => throw new NotImplementedException()
            };

        public InputArgumentsViewModel AddInput()
        {
            if (Inputs.Count >= MaxInputsCount)
            {
                throw new ArgumentException("无法继续增加输入文件");
            }
            var input = new InputArgumentsViewModel();
            Inputs.Add(input);
            return input;
        }

        public void BrowseFiles()
        {
            var dialog = new OpenFileDialog().AddAllFilesFilter();
            dialog.Multiselect = true;
            SendMessage(new FileDialogMessage(dialog));
            List<string> paths = dialog.FileNames.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            if (Inputs.Count + paths.Count > MaxInputsCount)
            {
                throw new ArgumentException("欲加入的文件数量超过可加入的文件数");
            }
            foreach (var path in paths)
            {
                Inputs.Add(new InputArgumentsViewModel()
                {
                    FilePath = path,
                });
            }
        }

        public void BrowseFolder()
        {
            var dialog = new OpenFolderDialog();
            dialog.Multiselect = true;
            SendMessage(new FileDialogMessage(dialog));
            if (!string.IsNullOrWhiteSpace(dialog.FolderName))
            {
                var files = Directory.EnumerateFiles(dialog.FolderName, "*", new EnumerationOptions()).ToList();

                if (Inputs.Count + files.Count > MaxInputsCount)
                {
                    throw new ArgumentException("欲加入的文件数量超过可加入的文件数");
                }
                foreach (var path in files)
                {
                    Inputs.Add(new InputArgumentsViewModel()
                    {
                        FilePath = path,
                    });
                }
            }

        }

        public List<InputParameters> GetInputs()
        {
            foreach (var input in Inputs)
            {
                input.Apply();
            }
            var inputs = Inputs.Where(p => !string.IsNullOrEmpty(p.FilePath));
            if (inputs.Count() < MinInputsCount)
            {
                throw new Exception("输入文件少于需要的文件数量");
            }
            return inputs.Adapt<List<InputParameters>>();
        }

        public string GetOutput(InputParameters inputArgs)
        {
            var input = inputArgs.FilePath;
            string dir = OutputDir;
            if (string.IsNullOrWhiteSpace(dir))//没有指定输出位置
            {
                dir = Config.Instance.DefaultOutputDirType switch
                {
                    DefaultOutputDirType.InputDir => Path.GetDirectoryName(input),
                    DefaultOutputDirType.InputNewDir => Path.Combine(Path.GetDirectoryName(input), Config.Instance.DefaultOutputDirInputSubDirName),
                    DefaultOutputDirType.SpecialDir => Config.Instance.DefaultOutputDirSpecialDirPath,
                    _ => throw new NotImplementedException()
                };
            }
            if (CanSetOutputFileName && !string.IsNullOrWhiteSpace(OutputFileName))
            {
                return Path.Combine(dir, OutputFileName);
            }
            return Path.Combine(dir, Path.GetFileName(input));
        }

        /// <summary>
        /// 用于添加到远程主机，获取输出文件名
        /// </summary>
        /// <returns></returns>
        public string GetOutputFileName()
        {
            if (CanSetOutputFileName)//需要可以设置输出文件名
            {
                if (!string.IsNullOrWhiteSpace(OutputFileName))//如果手动指定
                {
                    return OutputFileName;
                }
                if (Inputs.Where(p => !string.IsNullOrEmpty(p.FilePath)).Any())//如果未手动指定并且存在输入文件
                {
                    return Path.GetFileName(Inputs.Where(p => !string.IsNullOrEmpty(p.FilePath)).First().FilePath);
                }
            }
            return null;
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset(bool keepFiles)
        {
            var files = keepFiles ?
                Inputs.Select(p => p.FilePath).ToList() :
                null;
            Inputs.Clear();
            int count = keepFiles ?
                Math.Max(MinInputsCount, Math.Min(files.Count, MaxInputsCount))
                : MinInputsCount;
            while (Inputs.Count < count)
            {
                Inputs.Add(new InputArgumentsViewModel());
            }
            if (keepFiles)
            {
                for (int i = 0; i < Math.Min(files.Count, Inputs.Count); i++)
                {
                    Inputs[i].FilePath = files[i];
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="inputs"></param>
        /// <param name="output"></param>
        /// <returns>若所有文件都被接受，返回True；若文件数量超过允许范围，返回False</returns>
        public void Update(TaskType type, List<InputParameters> inputs, string output)
        {
            UpdateType(type);
            Inputs.Clear();

            foreach (var input in inputs.Take(MaxInputsCount))
            {
                var newInput = input.Adapt<InputArgumentsViewModel>();
                newInput.Update();
                Inputs.Add(newInput);
            }
            while (Inputs.Count < MinInputsCount)
            {
                Inputs.Add(new InputArgumentsViewModel());
            }
            OutputDir = Path.GetDirectoryName(output);
            OutputFileName = Path.GetFileName(output);
            if (inputs.Count > MaxInputsCount)
            {
                QueueErrorMessage("输入文件超过该类型最大数量");
            }
        }

        /// <summary>
        /// 更新任务类型
        /// </summary>
        /// <param name="type"></param>
        public void UpdateType(TaskType type)
        {
            Type = type;
            CanChangeInputsCount = type is TaskType.Transcode or TaskType.Concat;
            MinInputsCount = type switch
            {
                TaskType.Transcode => 1,
                TaskType.Mux or TaskType.Concat or TaskType.QualityCheck => 2,
                _ => 0
            };
            MaxInputsCount = type switch
            {
                TaskType.Transcode or TaskType.Concat => int.MaxValue,
                TaskType.Mux or TaskType.QualityCheck => 2,
                _ => 0
            };
            ShowTimeClip = type switch
            {
                TaskType.Transcode => true,
                _ => false
            };
            ShowMoreOptions = type is TaskType.Transcode or TaskType.Mux or TaskType.QualityCheck;
        }

        [RelayCommand]
        private async Task BrowseFileAsync(InputArgumentsViewModel input)
        {
            var dialog = new OpenFileDialog().AddAllFilesFilter();
            SendMessage(new FileDialogMessage(dialog));
            string path = dialog.FileName;
            if (!string.IsNullOrEmpty(path))
            {
                if (input.Image2)
                {
                    string seqFilename = FileSystemHelper.GetSequence(path);
                    if (seqFilename != null)
                    {
                        bool rename = await CommonDialog.ShowYesNoDialogAsync("图像序列", $"指定的文件可能是图像序列中的一个，是否将输入路径修改为{seqFilename}？");
                        if (rename)
                        {
                            path = seqFilename;
                        }
                    }
                }
                input.FilePath = path;
            }
        }

        [RelayCommand]
        private void BrowseOutputFile()
        {
            var dialog = new OpenFolderDialog();
            SendMessage(new FileDialogMessage(dialog));
            string path = dialog.FolderName;
            if (!string.IsNullOrEmpty(path))
            {
                OutputDir = path;
            }
        }

        [RelayCommand]
        private async Task ClipAsync(InputArgumentsViewModel input)
        {
            // 忙碌状态"发出去"和"收回来"必须配对：下面的 finally 是无条件收的，而两个提前 return 都在
            // 发送之前——直接用 finally 收，就会在"输入框还空着就点裁剪"这类情况下多发一条"不忙碌"，
            // 把别的窗口正在转的环收掉（那个窗口提前变回可点）
            bool busySent = false;
            try
            {
                Debug.Assert(input != null);
                if (string.IsNullOrEmpty(input.FilePath))
                {
                    QueueErrorMessage("请先设置文件地址");
                    return;
                }
                if (!File.Exists(input.FilePath))
                {
                    QueueErrorMessage($"找不到文件{input.FilePath}");
                    return;
                }
                busySent = true;
                SendMessage(new WindowEnableMessage(false, "正在裁剪"));

                var handle = WeakReferenceMessenger.Default.Send(new WindowHandleMessage()).Handle;

                Process p = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = SimpleFFmpegGUI.WPF.FzLib.Program.App.ProgramFilePath,
                        RedirectStandardOutput = true,
                    }
                };
                p.StartInfo.ArgumentList.Add("cut");
                p.StartInfo.ArgumentList.Add(handle.ToString());
                p.StartInfo.ArgumentList.Add(input.FilePath);
                p.StartInfo.ArgumentList.Add(input.From.HasValue ? input.From.Value.ToString() : "-");
                p.StartInfo.ArgumentList.Add(input.To.HasValue ? input.To.Value.ToString() : "-");
                p.Start();
                string output = await p.StandardOutput.ReadToEndAsync();
                // 接不回来时必须说出来：以前是静默什么都不做，用户只看到"点了完成但时间没变"，无从判断是哪一环断了
                if (TryParseCutResult(output, out TimeSpan from, out TimeSpan to))
                {
                    input.From = from;
                    input.To = to;
                    input.Duration = null;
                }
                else
                {
                    App.AppLog?.Warn($"裁剪窗口没有返回可用时间（退出码 {(p.HasExited ? p.ExitCode : -1)}，原始输出 \"{output}\"）");
                    QueueErrorMessage("裁剪窗口没有返回时间，请重试");
                }
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex);
            }
            finally
            {
                // 只收自己发出去的那次（上面两个提前 return 没发过，就不该收）
                if (busySent)
                {
                    SendMessage(new WindowEnableMessage(true));
                }
            }
        }

        /// <summary>
        /// 从裁剪窗口的标准输出里取出"{开始},{结束}"（协议见 <c>CutWindowViewModel.Apply</c>）。
        /// <para>
        /// 按**行**找，而不是把整段按逗号切开：子进程退出时保存配置还会往标准输出再写一句
        /// （Core 的 <c>ConfigService.SaveAsync</c> 里那句"尝试保存配置"），只要它自成一行就不影响解析，
        /// 逐行找还能容忍它出现在结果之前。找不到时返回 false，由调用方记日志并提示用户。
        /// </para>
        /// </summary>
        private static bool TryParseCutResult(string output, out TimeSpan from, out TimeSpan to)
        {
            foreach (string line in output.Split('\n'))
            {
                string[] parts = line.Trim().Split(',');
                if (parts.Length == 2
                    && TimeSpan.TryParse(parts[0], out from)
                    && TimeSpan.TryParse(parts[1], out to))
                {
                    return true;
                }
            }

            from = default;
            to = default;
            return false;
        }

        private void Inputs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            for (int i = 0; i < Inputs.Count; i++)
            {
                Inputs[i].Index = i + 1;
                Inputs[i].CanDelete = Inputs.Count > MinInputsCount;
            }
            this.Notify(nameof(CanSetOutputFileName));
        }

        partial void OnMaxInputsCountChanged(int value)
        {
            while (Inputs.Count > value)
            {
                Inputs.RemoveAt(Inputs.Count - 1);
            }
        }

        partial void OnMinInputsCountChanged(int value)
        {
            while (value > Inputs.Count)
            {
                Inputs.Add(new InputArgumentsViewModel());
            }
        }
        [RelayCommand]
        private void RemoveFile(InputArgumentsViewModel input)
        {
            Inputs.Remove(input);
        }
    }
}