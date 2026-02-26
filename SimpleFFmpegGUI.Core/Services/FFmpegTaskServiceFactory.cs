using FzLib;
using FzLib.IO;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Repositories;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SimpleFFmpegGUI.Models.Entities;

namespace SimpleFFmpegGUI.Services;

public interface IFFmpegTaskServiceFactory
{
    FFmpegTaskService Create(TaskEntity task);
}
public class FFmpegTaskServiceFactory(DbLoggerService logger, LogRepository logRepository,
                               MediaInfoService mediaInfoService, IFFmpegProcessServiceFactory ffmpegProcessServiceFactory) : IFFmpegTaskServiceFactory
{
    public FFmpegTaskService Create(TaskEntity task)
    {
        return new FFmpegTaskService(task, logger, logRepository, mediaInfoService, ffmpegProcessServiceFactory);
    }
}
