using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

    /// <summary>
    /// 上传时因同名文件而重新解析文件名的最大次数（兜住并发上传同名文件的窗口）
    /// </summary>
    private const int MaxUploadAttempts = 10;

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
        // 返回解析后的绝对路径，而不是配置原值：前端拿这两个值给任务里的绝对路径做前缀匹配，
        // 砍掉前缀后只显示相对路径。出厂示例值是相对的（"input"/"output"），直接返回会让前端永远匹配不上。
        return new AppDirDto
        {
            InputDir = filePathHelper.InputDirFullPath,
            OutputDir = filePathHelper.OutputDirFullPath
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
            string desiredPath = filePathHelper.GetFullPath(RootDirType.InputDir, file.FileName);
            // 输入目录可能尚未创建（列表端点对目录不存在返回空列表是正常状态），上传时先确保目录存在
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(desiredPath));

            // 同名文件不覆盖：自动改名（与任务输出路径同一套规则）。
            // CreateNew 兜住"两个请求同时上传同名文件"的并发窗口——查完不存在到写盘之间仍可能被别人抢先
            string name = null;
            FileStream stream = null;
            for (int attempt = 0; attempt < MaxUploadAttempts && stream == null; attempt++)
            {
                string candidate = FileSystemHelper.GetNonDuplicatePath(desiredPath);
                try
                {
                    stream = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    name = candidate;
                }
                catch (IOException) when (System.IO.File.Exists(candidate))
                {
                    // 与并发的同名上传撞车：重新解析文件名后重试
                }
            }

            if (stream == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "无法创建上传文件：同名文件数量过多");
            }

            await using (stream)
            {
                try
                {
                    await file.CopyToAsync(stream);
                }
                catch
                {
                    // 上传中断（客户端断连等）时删除半截文件：否则它会长期占住这个名字，
                    // 使后续同名上传只能一直往后递增
                    TryDeleteUploadedFile(name);
                    throw;
                }
            }
            return Ok(name);
        }

        return BadRequest("文件大小为0");
    }

    /// <summary>
    /// 尽力删除上传失败留下的文件，删除失败不改变要向上抛出的原始异常
    /// </summary>
    private static void TryDeleteUploadedFile(string path)
    {
        try
        {
            System.IO.File.Delete(path);
        }
        catch
        {
            // 忽略：原始异常更重要
        }
    }
}