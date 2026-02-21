using System.Collections.Generic;

namespace SimpleFFmpegGUI.Dto;

public class TaskStatusChangeResult
{
    public bool Success => FailedIds.Count == 0 && NotFoundIds.Count == 0;
    
    public int AffectedRows { get; set; }

    public List<int> NotFoundIds { get; set; } = new();

    public Dictionary<int, string> FailedIds { get; set; } = new();
}