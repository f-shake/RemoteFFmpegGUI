namespace SimpleFFmpegGUI.WebAPI.Dto;

public class TaskQueryDto
{
    public int? Status { get; set; }
    public int Page { get; set; } = 1; // 默认第一页
    public int PageSize { get; set; } = 10; // 默认每页10条
}