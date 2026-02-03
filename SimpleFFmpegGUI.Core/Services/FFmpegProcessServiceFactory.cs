using SimpleFFmpegGUI.Manager;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleFFmpegGUI.Services;

public interface IFFmpegProcessServiceFactory
{
    FFmpegProcessService Create(string argument);
}
public class FFmpegProcessServiceFactory(ConfigManager configManager, DbLoggerService logger) : IFFmpegProcessServiceFactory
{
    public FFmpegProcessService Create(string argument)
    {
        return new FFmpegProcessService(configManager, argument);
    }
}