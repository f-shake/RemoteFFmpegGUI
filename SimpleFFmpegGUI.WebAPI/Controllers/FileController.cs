using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FzLib.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Helpers;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Services;

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

    [HttpPost]
    [Route("Ftp/Input/Off")]
    public async Task<IActionResult> CloseInputFtp()
    {
        await ftpInput.StopAsync();
        return NoContent();
    }

    [HttpPost]
    [Route("Ftp/Output/Off")]
    public async Task<IActionResult> CloseOutputFtp()
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
    [Route("Dirs")]
    public ActionResult<AppDirDto> GetDirs()
    {
        return new AppDirDto
        {
            InputDir = appSettings.Value.InputDir,
            OutputDir = appSettings.Value.OutputDir
        };
    }

    /// <summary>
    /// 获取FTP状态
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("Ftp")]
    public ActionResult<FtpStatusDto> GetFtpStatus()
    {
        var status = new FtpStatusDto
        {
            InputOn = ftpInput.IsRunning,
            OutputOn = ftpOutput.IsRunning,
            InputPort = ftpInput.Port,
            OutputPort = ftpOutput.Port
        };
        return status;
    }

    [HttpGet]
    [Route("List/Input")]
    public ActionResult<List<FileInfoDto>> GetInputFiles()
    {
        // 目录尚未创建时返回空列表而非 500（P3-2 相对目录出厂值）
        if (!Directory.Exists(appSettings.Value.InputDir))
        {
            return new List<FileInfoDto>();
        }
        return Directory.EnumerateFiles(appSettings.Value.InputDir, "*", SearchOption.AllDirectories)
            .Select(p => new FileInfoDto(p, filePathHelper.InputDir)).ToList();
    }

    [HttpGet]
    [Route("List/Output")]
    public ActionResult<List<FileInfoDto>> GetOutputFiles()
    {
        if (!Directory.Exists(appSettings.Value.OutputDir))
        {
            return new List<FileInfoDto>();
        }
        return Directory.EnumerateFiles(appSettings.Value.OutputDir, "*", SearchOption.AllDirectories)
            .Select(p => new FileInfoDto(p, filePathHelper.OutputDir)).ToList();
    }
    [HttpPost]
    [Route("Ftp/Input/On")]
    public async Task<IActionResult> StartInputFtp()
    {
        await ftpInput.StartAsync(appSettings.Value.InputDir, appSettings.Value.InputFtpPort);
        return NoContent();
    }

    [HttpPost]
    [Route("Ftp/Output/On")]
    public async Task<IActionResult> StartOutputFtp()
    {
        await ftpOutput.StartAsync(appSettings.Value.OutputDir, appSettings.Value.OutputFtpPort);
        return NoContent();
    }

    [HttpPost, HttpOptions]
    [Route("Upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null)
        {
            return BadRequest("文件不能为空");
        }
        if (file.Length > 0)
        {
            string name = filePathHelper.GetFullPath(RootDirType.InputDir, file.FileName);
            name = FileNameHelper.GenerateUniquePath(name, new HashSet<string>());
            // 输入目录可能尚未创建（列表端点对目录不存在返回空列表是正常状态），上传时先确保目录存在
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(name));
            await using var stream = System.IO.File.Create(name);
            await file.CopyToAsync(stream);
            return Ok(name);
        }

        return BadRequest("文件大小为0");
    }
}