using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Dto;

public class CpuCoreUsageDto
{
    public int CpuIndex { get; set; }
    public int CoreIndex { get; set; }
    public double Usage { get; set; }
}