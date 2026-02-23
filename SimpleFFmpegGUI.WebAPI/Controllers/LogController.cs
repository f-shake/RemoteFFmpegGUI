using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using System;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class LogController(LogRepository log) : FFmpegControllerBase
    {
        [HttpGet]
        [Route("List")]
        public async Task<PagedListResponse<Log>> GetLogs([FromQuery] LogQueryRequest query)
        {
            // if (query.From.HasValue)
            // {
            //     query.From = query.From.Value.ToLocalTime();
            // }
            //
            // if (query.To.HasValue)
            // {
            //     query.To = query.To.Value.ToLocalTime();
            // }
            //
            // int skip = (query.Page - 1) * query.PageSize;
            // var result = await log.GetLogsAsync(query.Type, query.TaskId ?? 0, query.From, query.To, skip, query.PageSize);
            var result = await log.GetLogsAsync(query);
            return result;
        }
    }
}