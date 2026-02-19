using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SimpleFFmpegGUI.Services;

public interface IFFmpegProcessServiceFactory
{
    FFmpegProcessService Create(string argument);
}

public class FFmpegProcessServiceFactory(IConfiguration config) : IFFmpegProcessServiceFactory
{
    public FFmpegProcessService Create(string argument)
    {
        return new FFmpegProcessService(config, argument);
    }
}