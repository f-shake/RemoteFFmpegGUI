using FzLib.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Services;
using SimpleFFmpegGUI.WebAPI.Dto;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WebAPI.Controllers;

public class FileController(IConfiguration config,
    [FromKeyedServices(FileController.InputFtpKey)] FtpService ftpInput,
    [FromKeyedServices(FileController.OutputFtpKey)] FtpService ftpOutput,
    IWebHostEnvironment hostingEnvironment) : FFmpegControllerBase(config)
{
    public const string InputFtpKey = "input";
    public const string OutputFtpKey = "output";

    public static ConcurrentDictionary<string, string> Guid2File { get; } = new ConcurrentDictionary<string, string>();

    [HttpPost]
    [Route("Ftp/Input/Off")]
    public async Task CloseInput()
    {
        await ftpInput.StopAsync();
    }

    [HttpPost]
    [Route("Ftp/Output/Off")]
    public async Task CloseOutput()
    {
        await ftpOutput.StopAsync();
    }

    [HttpGet]
    [Route("Download")]
    public IActionResult Download(string name)
    {
        string path = Path.Combine(OutputDir, name);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }
        var stream = System.IO.File.OpenRead(path);
        return File(stream, "application/octet-stream", Path.GetFileName(path));
    }

    [HttpGet]
    [Route("Dir")]
    public string GetCurrentDir()
    {
        return hostingEnvironment.ContentRootPath;
    }

    [HttpGet]
    [Route("List/Input")]
    public async Task<List<string>> GetInputFiles()
    {
        return [.. Directory.EnumerateFiles(InputDir, "*", SearchOption.AllDirectories).Select(GetInputRelativePath)];
    }

    [HttpGet]
    [Route("List/Output")]
    public async Task<List<FileInfoDto>> GetOutputFiles()
    {
        return [.. Directory.EnumerateFiles(OutputDir).Select(p => new FileInfoDto(p))];
    }

    /// <summary>
    /// 获取FTP状态
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("Ftp/Status")]
    public async Task<FtpStatusDto> GetStatus()
    {
        var status = new FtpStatusDto()
        {
            InputOn = ftpInput.IsRunning,
            OutputOn = ftpOutput.IsRunning,
            InputPort = ftpInput.Port,
            OutputPort = ftpOutput.Port
        };
        return status;
    }

    [HttpPost]
    [Route("Ftp/Input/On")]
    public Task OpenInput()
    {
        return ftpInput.StartAsync(InputDir, config.GetValue(AppSettingsKeys.InputFtpPortKey, 0));
    }

    [HttpPost]
    [Route("Ftp/Output/On")]
    public Task OpenOutput()
    {
        return ftpOutput.StartAsync(OutputDir, config.GetValue(AppSettingsKeys.OutputFtpPortKey, 0));
    }

    [HttpPost, HttpOptions]
    [Route("Upload")]
    [DisableRequestSizeLimit]
    public async Task UploadFile([FromQuery] IFormFile file)
    {
        if (file.Length > 0)
        {
            string name = Path.Combine(InputDir, file.FileName);
            name = FileNameHelper.GenerateUniquePath(name, new HashSet<string>());
            using var stream = System.IO.File.Create(name);
            await file.CopyToAsync(stream);
        }
    }
}