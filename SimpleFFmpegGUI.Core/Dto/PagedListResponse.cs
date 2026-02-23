using System;
using System.Collections.Generic;

namespace SimpleFFmpegGUI.Dto
{
    public class PagedListResponse<T>(IList<T> list, int totalCount, int page, int pageSize)
    {
        public IList<T> List { get; set; } = list;

        public int TotalCount { get; set; } = totalCount;

        public int Page { get; set; } = page;

        public int PageSize { get; set; } = pageSize;

        public int TotalPage { get; set; } = (int)Math.Ceiling((double)totalCount / pageSize);
    }
}