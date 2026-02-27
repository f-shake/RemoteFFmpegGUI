using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.Dto;

public record AddPresetRequest(
    string Name,
    OutputParameters Parameters,
    TaskType Type
);
public record UpdatePresetRequest(
    string Name,
    OutputParameters Parameters,
    TaskType Type
);