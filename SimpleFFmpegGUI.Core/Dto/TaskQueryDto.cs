namespace SimpleFFmpegGUI.Dto;

public class TaskQueryDto : PagedListRequest
{
    public int? Status { get; set; }
}