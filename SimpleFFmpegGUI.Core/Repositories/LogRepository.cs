using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Repositories
{
    public class LogRepository(FFmpegDbContext db, DbLoggerService logger)
    {
        public async Task<PagedListDto<Log>> GetLogsAsync(char? type = null,
            int taskId = 0,
            DateTime? from = null,
            DateTime? to = null,
            int skip = 0,
            int take = 0)
        {
            await logger.SaveAllAsync();

            IQueryable<Log> logs = db.Logs;
            if (type.HasValue)
            {
                logs = logs.Where(p => p.Type == type.Value);
            }
            if (from.HasValue)
            {
                logs = logs.Where(p => p.Time > from.Value);
            }
            if (to.HasValue)
            {
                logs = logs.Where(p => p.Time < to.Value);
            }
            if (taskId != 0)
            {
                logs = logs.Where(p => p.TaskId == taskId);
            }
            logs = logs.OrderByDescending(p => p.Time);
            int count = await logs.CountAsync();
            if (skip > 0)
            {
                logs = logs.Skip(skip);
            }
            if (take > 0)
            {
                logs = logs.Take(take);
            }
            return new PagedListDto<Log>(await logs.ToListAsync(), count);
        }
    }
}