using SimpleFFmpegGUI.Model;

namespace SimpleFFmpegGUI.Dto;

public record AddPresetRequest(
    string Name,
    OutputArguments Arguments,
    TaskType Type
);
public record UpdatePresetRequest(
    string Name,
    OutputArguments Arguments,
    TaskType Type
);