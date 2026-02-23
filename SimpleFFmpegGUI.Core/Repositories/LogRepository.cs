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
        public async Task<PagedListResponse<Log>> GetLogsAsync(LogQueryRequest request)
        {
            await logger.SaveAllAsync();

            IQueryable<Log> logs = db.Logs;
            if (request.Type.HasValue)
            {
                logs = logs.Where(p => p.Type == request.Type.Value);
            }

            if (request.From.HasValue)
            {
                logs = logs.Where(p => p.Time > request.From.Value);
            }

            if (request.To.HasValue)
            {
                logs = logs.Where(p => p.Time < request.To.Value);
            }

            if (request.TaskId.HasValue)
            {
                logs = logs.Where(p => p.TaskId == request.TaskId.Value);
            }

            logs = logs.OrderByDescending(p => p.Time);
            int count = await logs.CountAsync();
            var skip = (request.Page - 1) * request.PageSize;
            var take = request.PageSize;
            if (skip > 0)
            {
                logs = logs.Skip(skip);
            }

            if (take > 0)
            {
                logs = logs.Take(take);
            }

            return new PagedListResponse<Log>(await logs.ToListAsync(), count, request.Page, request.PageSize);
        }
    }
}