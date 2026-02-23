using System;

namespace SimpleFFmpegGUI.Dto;

public class LogQueryRequest : PagedListRequest
{
    public char? Type { get; set; }
    public int? TaskId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}