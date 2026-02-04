using FzLib;
using FzLib.IO;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Repositories;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SimpleFFmpegGUI.Services;

public interface IFFmpegTaskServiceFactory
{
    FFmpegTaskService Create(TaskInfo task);
}
public class FFmpegTaskServiceFactory(DbLoggerService logger, LogRepository logRepository,
                               MediaInfoService mediaInfoService, IFFmpegProcessServiceFactory ffmpegProcessServiceFactory) : IFFmpegTaskServiceFactory
{
    public FFmpegTaskService Create(TaskInfo task)
    {
        return new FFmpegTaskService(task, logger, logRepository, mediaInfoService, ffmpegProcessServiceFactory);
    }
}
