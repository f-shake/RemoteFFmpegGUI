namespace SimpleFFmpegGUI.Dto;

public class PagedListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}