using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Repositories;
using System;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Models.Entities;

namespace SimpleFFmpegGUI.WebAPI.Controllers
{
    public class LogController(LogRepository log) : FFmpegControllerBase
    {
        [HttpGet]
        [Route("List")]
        public async Task<PagedListResponse<LogEntity>> GetLogs([FromQuery] LogQueryRequest query)
        {
            var result = await log.GetLogsAsync(query);
            return result;
        }
    }
}