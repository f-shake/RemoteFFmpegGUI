using Microsoft.EntityFrameworkCore;
using SimpleFFmpegGUI.Model;
using System.Linq;
using TaskStatus = SimpleFFmpegGUI.Model.TaskStatus;

namespace SimpleFFmpegGUI.Manager
{
    public static class QueueManagerExtensions
    {
        public static IQueryable<TaskInfo> IsQueueing(this DbSet<TaskInfo> tasks)
        {
            return tasks.Where(p => p.IsDeleted == false && p.Status == TaskStatus.Queue);
        }
    }
}