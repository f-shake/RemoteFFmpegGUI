using FzLib;
using FzLib.IO;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SimpleFFmpegGUI.Services;

public interface IFFmpegTaskServiceFactory
{
	FFmpegTaskService Create(TaskInfo task);
}
public class FFmpegTaskServiceFactory(DbLoggerService logger, IServiceProvider services) : IFFmpegTaskServiceFactory
{
	public FFmpegTaskService Create(TaskInfo task)
	{
		return new FFmpegTaskService(task, logger, services);
	}
}
