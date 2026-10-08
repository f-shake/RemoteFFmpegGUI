using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.WebAPI;

// 建议安装这个包，断言更丝滑

namespace SimpleFFmpegGUI.WebTest;

public class FileApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestDownloadAsync()
    {
        var fileName = Path.GetFileName(appTestSettings.TestOutputVideo10s);
        if (fileName == null)
        {
            throw new Exception("测试输出视频不存在");
        }
        await DownloadAsync(fileName);
    }

    /// <summary>
    /// 路径穿越（..\）与绝对路径访问应被拒绝（P3-1 回归）
    /// </summary>
    [Fact]
    public async Task TestPathTraversalRejectedAsync()
    {
        var act = async () => await DownloadAsync(Uri.EscapeDataString("..\\..\\Windows\\win.ini"));
        await act.Should().ThrowAsync<Exception>();

        var outputDir = (await GetDirsAsync()).OutputDir;
        var absolutePath = Path.Combine(outputDir, "any.mp4");
        act = async () => await DownloadAsync(Uri.EscapeDataString(absolutePath));
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task TestFtpAsync()
    {
        await FtpInputOnAsync();
        await FtpInputOffAsync();
        await FtpOutputOnAsync();
        await FtpOutputOffAsync();
    }

    /// <summary>
    /// FTP 状态查询接口
    /// </summary>
    [Fact]
    public async Task TestFtpStatusAsync()
    {
        var status = await GetObjectFromJsonAsync<FtpStatusDto>("/api/File/Ftp");
        status.InputOn.Should().BeFalse();
        status.OutputOn.Should().BeFalse();

        await FtpInputOnAsync();
        status = await GetObjectFromJsonAsync<FtpStatusDto>("/api/File/Ftp");
        status.InputOn.Should().BeTrue();
        status.InputPort.Should().BeGreaterThan(0);
        await FtpInputOffAsync();
    }

    /// <summary>
    /// FTP 边界：重复启动 / 未启动时关闭应报错而非静默（P4-3）
    /// </summary>
    [Fact]
    public async Task TestFtpEdgeCasesAsync()
    {
        // 重复启动应失败
        await FtpInputOnAsync();
        var act = async () => await FtpInputOnAsync();
        await act.Should().ThrowAsync<Exception>();
        await FtpInputOffAsync();

        // 未启动时关闭应失败（Input/Output 都处于关闭状态时）
        var act2 = async () => await FtpInputOffAsync();
        await act2.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task TestListAsync()
    {
        var inputs = await GetInputListAsync();
        inputs.Count.Should().BeGreaterThanOrEqualTo(1);
        inputs.Should().Contain(p => p.Name == Path.GetFileName(appTestSettings.TestVideo10s));
        
        var outputs = await GetOutputListAsync();
        outputs.Count.Should().BeGreaterThanOrEqualTo(1);
        outputs.Should().Contain(p => p.Name == Path.GetFileName(appTestSettings.TestOutputVideo10s));
    }

    [Fact]
    public async Task TestUploadAsync()
    {
        // 上传一个测试文件。用随机文件名：本次改动后同名文件不再被覆盖，
        // 若沿用固定名字，每跑一次测试就会在输入目录里多留一个 "test_upload (n).txt"
        string fileName = $"test_upload_{Guid.NewGuid():N}.txt";
        var content = "test file content for upload";

        var uploadedPath = await UploadTextFileAsync(fileName, content);

        uploadedPath.Should().Contain("test_upload");
        File.Exists(uploadedPath).Should().BeTrue();
        (await File.ReadAllTextAsync(uploadedPath)).Should().Be(content);

        File.Delete(uploadedPath);
    }

    /// <summary>
    /// 上传同名文件不得覆盖已存在的文件：第二次应自动改名为"xxx (2).ext"并返回该路径
    /// （回归：v2 曾给 FileNameHelper.GenerateUniquePath 传空集合，等于完全不防重）
    /// </summary>
    [Fact]
    public async Task TestUploadSameNameShouldNotOverwriteAsync()
    {
        string fileName = $"dup_upload_{Guid.NewGuid():N}.txt";
        string firstContent = "第一次上传的内容";
        string secondContent = "第二次上传的内容";

        string firstPath = await UploadTextFileAsync(fileName, firstContent);
        string secondPath = await UploadTextFileAsync(fileName, secondContent);

        secondPath.Should().NotBe(firstPath);
        // 改名格式必须与既有 " ({n})" 一致（单元测试覆盖了 helper，这里守住端到端返回给前端的名字）
        secondPath.Should().Be(Path.Combine(Path.GetDirectoryName(firstPath)!,
            $"{Path.GetFileNameWithoutExtension(fileName)} (2){Path.GetExtension(fileName)}"));
        File.Exists(firstPath).Should().BeTrue("第一次上传的文件必须还在");
        File.Exists(secondPath).Should().BeTrue();
        (await File.ReadAllTextAsync(firstPath)).Should().Be(firstContent);
        (await File.ReadAllTextAsync(secondPath)).Should().Be(secondContent);

        File.Delete(firstPath);
        File.Delete(secondPath);
    }

    /// <summary>
    /// 上传一个文本文件，返回服务端实际落盘路径
    /// </summary>
    private async Task<string> UploadTextFileAsync(string fileName, string content)
    {
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        var form = new MultipartFormDataContent { { fileContent, "file", fileName } };
        var response = await PostMultipartAsync("/api/File/Upload", form);
        var path = await response.Content.ReadAsStringAsync();
        path.Should().NotBeNullOrEmpty();
        return path;
    }

    /// <summary>
    /// 下载不存在的文件应返回 404
    /// </summary>
    [Fact]
    public async Task TestDownloadNonExistentAsync()
    {
        var act = async () => await DownloadAsync("no_such_file.mp4");
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 空表单上传（无 file 字段）应返回 400
    /// </summary>
    [Fact]
    public async Task TestUploadEmptyFormAsync()
    {
        var act = async () => await PostMultipartAsync("/api/File/Upload", new MultipartFormDataContent());
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 0 字节文件上传应返回 400
    /// </summary>
    [Fact]
    public async Task TestUploadZeroByteAsync()
    {
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        var form = new MultipartFormDataContent { { fileContent, "file", "empty.txt" } };
        var act = async () => await PostMultipartAsync("/api/File/Upload", form);
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// Dirs 接口返回输入/输出目录
    /// </summary>
    [Fact]
    public async Task TestDirsAsync()
    {
        var dirs = await GetDirsAsync();
        dirs.InputDir.Should().NotBeNullOrWhiteSpace();
        dirs.OutputDir.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// 下载响应的 Content-Type 应为 octet-stream
    /// </summary>
    [Fact]
    public async Task TestDownloadContentTypeAsync()
    {
        var fileName = Path.GetFileName(appTestSettings.TestOutputVideo10s);
        var response = await GetAsync($"/api/File/Download/{fileName}");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/octet-stream");
    }

    private Task<string> DownloadAsync(string name) => GetStringAsync($"/api/File/Download/{name}");

    private Task FtpInputOffAsync() => PostAsync("/api/File/Ftp/Input/Off");

    private Task FtpInputOnAsync() => PostAsync("/api/File/Ftp/Input/On");

    private Task FtpOutputOffAsync() => PostAsync("/api/File/Ftp/Output/Off");

    private Task FtpOutputOnAsync() => PostAsync("/api/File/Ftp/Output/On");

    private Task<List<FileInfoDto>> GetInputListAsync() => GetObjectFromJsonAsync<List<FileInfoDto>>("/api/File/List/Input");

    private Task<List<FileInfoDto>> GetOutputListAsync() =>
        GetObjectFromJsonAsync<List<FileInfoDto>>("/api/File/List/Output");
}