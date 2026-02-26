using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Models;

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