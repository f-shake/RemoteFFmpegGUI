using System.Collections.Generic;
using SimpleFFmpegGUI.Enums;

namespace SimpleFFmpegGUI.Compatibility;

/// <summary>
/// 旧版预设 DTO（匹配 master 分支的 CodePreset + OutputArguments）
/// </summary>
public class OldPresetDto
{
    public OldOutputArgumentsDto Arguments { get; set; }
    public bool Default { get; set; }
    public string Name { get; set; }
    public TaskType Type { get; set; }
    public int Id { get; set; }
}

public class OldOutputArgumentsDto
{
    public OldVideoCodeArgumentsDto Video { get; set; }
    public OldAudioCodeArgumentsDto Audio { get; set; }
    public OldCombineArgumentsDto Combine { get; set; }
    public OldStreamArgumentsDto Stream { get; set; }
    public bool DisableVideo { get; set; }
    public bool DisableAudio { get; set; }
    public string Extra { get; set; }
    public string Format { get; set; }
    public OldProcessedOptionsDto ProcessedOptions { get; set; }
}

public class OldVideoCodeArgumentsDto
{
    public string Code { get; set; }
    public int Preset { get; set; }
    public int? Crf { get; set; }
    public bool TwoPass { get; set; }
    public string Size { get; set; }
    public double? Fps { get; set; }
    public double? AverageBitrate { get; set; }
    public double? MaxBitrate { get; set; }
    public double? MaxBitrateBuffer { get; set; }
    public string AspectRatio { get; set; }
    public string PixelFormat { get; set; }
}

public class OldAudioCodeArgumentsDto
{
    public string Code { get; set; }
    public int? Bitrate { get; set; }
    public int? SamplingRate { get; set; }
}

public class OldCombineArgumentsDto
{
    public bool Shortest { get; set; }
}

public class OldStreamArgumentsDto
{
    public List<OldStreamMapInfoDto> Maps { get; set; }
}

public class OldStreamMapInfoDto
{
    public int InputIndex { get; set; }
    public StreamChannel Channel { get; set; }
    public int? StreamIndex { get; set; }
}

public class OldProcessedOptionsDto
{
    public bool SyncModifiedTime { get; set; }
    public bool DeleteInputFiles { get; set; }
}
