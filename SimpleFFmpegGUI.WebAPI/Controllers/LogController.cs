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
    public class LogController(IConfiguration config, LogRepository log) : FFmpegControllerBase(config)
    {
        [HttpGet]
        [Route("List")]
        public async Task<PagedListDto<Log>> GetLogs(char? type = null, int taskId = 0, DateTime? from = null, DateTime? to = null, int skip = 0, int take = 0)
        {
            if (from.HasValue)
            {
                from = from.Value.ToLocalTime();
            }
            if (to.HasValue)
            {
                to = to.Value.ToLocalTime();
            }

            var result = await log.GetLogsAsync(type, taskId, from, to, skip, take);
            return result;
        }
    }
}