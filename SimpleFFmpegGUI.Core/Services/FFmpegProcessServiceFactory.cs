using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;

namespace SimpleFFmpegGUI.Services;

public interface IFFmpegProcessServiceFactory
{
    FFmpegProcessService Create(string argument);
}

public class FFmpegProcessServiceFactory(IOptionsSnapshot<AppSettings> appSettings) : IFFmpegProcessServiceFactory
{
    public FFmpegProcessService Create(string argument)
    {
        return new FFmpegProcessService(appSettings, argument);
    }
}