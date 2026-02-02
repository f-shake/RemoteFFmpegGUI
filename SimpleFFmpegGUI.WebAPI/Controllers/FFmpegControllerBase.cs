using FzLib.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleFFmpegGUI.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class FFmpegControllerBase : ControllerBase
{
    public readonly string InputDir = null;
    public readonly string OutputDir = null;
    protected readonly IConfiguration config;

    public FFmpegControllerBase(IConfiguration config)
    {
        this.config = config;
        InputDir = config.GetValue<string>(AppSettingsKeys.InputDirKey) ?? throw new HttpStatusCodeException("没有配置输入文件夹", System.Net.HttpStatusCode.InternalServerError);
        OutputDir = config.GetValue<string>(AppSettingsKeys.OutputDirKey) ?? throw new HttpStatusCodeException("没有配置输出文件夹", System.Net.HttpStatusCode.InternalServerError);
    }

    protected async Task<string> CheckAndGetInputFilePathAsync(string name)
    {
        if (name.StartsWith(':'))
        {
            name = name[1..];
            var files = Directory.EnumerateFiles(InputDir, name, SearchOption.AllDirectories);
            if (!files.Any())
            {
                throw new HttpStatusCodeException("找不到文件" + name, System.Net.HttpStatusCode.NotFound);
            }
            if (files.Count() > 2)
            {
                throw new HttpStatusCodeException($"存在多个文件名为{name}的文件", System.Net.HttpStatusCode.Conflict);
            }
            return files.First();
        }
        else
        {
            string path = Path.Combine(InputDir, name);
            if (System.IO.File.Exists(path))
            {
                throw new HttpStatusCodeException($"不存在文件{path}", System.Net.HttpStatusCode.NotFound);
            }
            return path;
        }
    }

    protected void CheckFileNameNull(string path)
    {
        if (path == null || path is string s && s == "")
        {
            throw new HttpStatusCodeException("文件名为空", System.Net.HttpStatusCode.BadRequest);
        }
    }

    protected void CheckNull(object obj, string objName)
    {
        if (obj == null || obj is string s && s == "")
        {
            throw new HttpStatusCodeException($"{objName}为空", System.Net.HttpStatusCode.BadRequest);
        }
    }
    protected string GetInputRelativePath(string path)
    {
        return path.StartsWith(InputDir) ?
            path.Substring(InputDir.Length).Replace('\\', '/').TrimStart('/')
            : path;
    }

    protected string GetOutputRelativePath(string path)
    {
        return path.StartsWith(OutputDir) ?
            path.Substring(OutputDir.Length).Replace('\\', '/').TrimStart('/')
            : path;
    }

    protected TaskInfo HideAbsolutePath(TaskInfo task)
    {
        if (task == null)
        {
            return null;
        }
        if (task.Inputs != null)
        {
            foreach (var input in task.Inputs)
            {
                input.FilePath = GetInputRelativePath(input.FilePath);
            }
        }
        if (task.Output != null)
        {
            task.Output = GetOutputRelativePath(task.Output);
        }
        return task;
    }
}