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
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Helpers;

namespace SimpleFFmpegGUI.WebAPI.Controllers;

public class FileController(
    IOptionsSnapshot<AppSettings> appSettings,
    [FromKeyedServices(FileController.InputFtpKey)]
    FtpService ftpInput,
    [FromKeyedServices(FileController.OutputFtpKey)]
    FtpService ftpOutput,
    IWebHostEnvironment hostingEnvironment,
    FilePathHelper filePathHelper) : FFmpegControllerBase()
{
    public const string InputFtpKey = "input";
    public const string OutputFtpKey = "output";

    public static ConcurrentDictionary<string, string> Guid2File { get; } = new ConcurrentDictionary<string, string>();

    [HttpPost]
    [Route("Ftp/Input/Off")]
    public async Task<IActionResult> CloseInput()
    {
        await ftpInput.StopAsync();
        return NoContent();
    }

    [HttpPost]
    [Route("Ftp/Output/Off")]
    public async Task<IActionResult> CloseOutput()
    {
        await ftpOutput.StopAsync();
        return NoContent();
    }

    [HttpGet]
    [Route("Download/{name}")]
    public IActionResult Download(string name)
    {
        string path = filePathHelper.GetFullPath(RootDirType.OutputDir, name);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var stream = System.IO.File.OpenRead(path);
        return File(stream, "application/octet-stream", Path.GetFileName(path));
    }

    [HttpGet]
    [Route("Dir")]
    public ActionResult<string> GetCurrentDir()
    {
        return hostingEnvironment.ContentRootPath;
    }

    [HttpGet]
    [Route("List/Input")]
    public ActionResult<List<FileInfoDto>> GetInputFiles()
    {
        return Directory.EnumerateFiles(appSettings.Value.InputDir, "*", SearchOption.AllDirectories)
            .Select(p => new FileInfoDto(p)).ToList();
    }

    [HttpGet]
    [Route("List/Output")]
    public ActionResult<List<FileInfoDto>> GetOutputFiles()
    {
        return Directory.EnumerateFiles(appSettings.Value.OutputDir, "*", SearchOption.AllDirectories)
            .Select(p => new FileInfoDto(p)).ToList();
    }

    /// <summary>
    /// 获取FTP状态
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("Ftp/Status")]
    public ActionResult<FtpStatusDto> GetStatus()
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
    public async Task<IActionResult> OpenInput()
    {
        await ftpInput.StartAsync(appSettings.Value.InputDir, appSettings.Value.InputFtpPort);
        return NoContent();
    }

    [HttpPost]
    [Route("Ftp/Output/On")]
    public async Task<IActionResult> OpenOutput()
    {
        await ftpOutput.StartAsync(appSettings.Value.OutputDir, appSettings.Value.OutputFtpPort);
        return NoContent();
    }

    [HttpPost, HttpOptions]
    [Route("Upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadFile([FromQuery] IFormFile file)
    {
        if (file.Length > 0)
        {
            string name = filePathHelper.GetFullPath(RootDirType.InputDir, file.FileName);
            name = FileNameHelper.GenerateUniquePath(name, new HashSet<string>());
            await using var stream = System.IO.File.Create(name);
            await file.CopyToAsync(stream);
            return Ok(name);
        }

        return BadRequest("文件大小为0");
    }
}