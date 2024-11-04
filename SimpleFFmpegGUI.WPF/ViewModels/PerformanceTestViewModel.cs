using CommunityToolkit.Mvvm.ComponentModel;
using FzLib;
using Newtonsoft.Json.Linq;
using SimpleFFmpegGUI.FFmpegLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WPF.ViewModels
{
    public partial class PerformanceTestCodecParameterViewModel : ViewModelBase
    {
        [ObservableProperty]
        private double bitrateFactor = 1;

        public int CpuSpeed { get; set; }

        public int CRF { get; set; }

        public string ExtraArguments { get; set; }

        public int MaxCpuSpeed { get; set; }

        public int MaxCRF { get; set; }

        public string Name { get; set; }

        partial void OnBitrateFactorChanged(double value)
        {
            if (value < 0.5)
            {
                BitrateFactor = 0.5;
            }
            else if (value > 2)
            {
                BitrateFactor = 2;
            }
        }
    }

    [DebuggerDisplay("{Codec}")]
    public partial class PerformanceTestItemViewModel : ViewModelBase
    {
        [ObservableProperty]
        public string codec;

        [ObservableProperty]
        private double cpuUsage;

        [ObservableProperty]
        private double fps;

        [ObservableProperty]
        private bool isChecked;

        [ObservableProperty]
        private double outputSize;

        [ObservableProperty]
        private TimeSpan processDuration;

        [ObservableProperty]
        private double psnr;

        [ObservableProperty]
        private double ssim;

        [ObservableProperty]
        private double vmaf;

        public PerformanceTestItemViewModel(string codec)
        {
            Codec = codec;
        }

        public PerformanceTestItemViewModel()
        {
        }
        public void Clear()
        {
            Fps = 0;
            Ssim = 0;
            Psnr = 0;
            Vmaf = 0;
            CpuUsage = 0;
            OutputSize = 0;
        }
    }

    [DebuggerDisplay("{Header}")]
    public class PerformanceTestLine
    {
        public const int CodecsCount = 8;
        public PerformanceTestLine()
        {
            if (VideoCodec.VideoCodecs.Length != CodecsCount)
            {
                throw new Exception("编码数量不对应");
            }
        }
        public string Header { get; set; }

        public PerformanceTestItemViewModel[] Items { get; set; }

        public double MBitrate { get; set; }

        public void FillItems()
        {
            Items = new PerformanceTestItemViewModel[CodecsCount];

            for (int i = 0; i < CodecsCount; i++)
            {
                Items[i] = new PerformanceTestItemViewModel(VideoCodec.VideoCodecs[i].Name);
            }
        }
    }
}
