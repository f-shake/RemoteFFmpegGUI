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
        var status = await GetObjectFromJsonAsync<FtpStatusDto>("/File/Ftp");
        status.InputOn.Should().BeFalse();
        status.OutputOn.Should().BeFalse();

        await FtpInputOnAsync();
        status = await GetObjectFromJsonAsync<FtpStatusDto>("/File/Ftp");
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
        // 上传一个测试文件
        var content = "test file content for upload";
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        var form = new MultipartFormDataContent
        {
            { fileContent, "file", "test_upload.txt" }
        };
        var response = await PostMultipartAsync("/File/Upload", form);
        var uploadedPath = await response.Content.ReadAsStringAsync();
        uploadedPath.Should().NotBeNullOrEmpty();
        uploadedPath.Should().Contain("test_upload");
        File.Exists(uploadedPath).Should().BeTrue();
        var savedContent = await File.ReadAllTextAsync(uploadedPath);
        savedContent.Should().Be(content);
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
        var act = async () => await PostMultipartAsync("/File/Upload", new MultipartFormDataContent());
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
        var act = async () => await PostMultipartAsync("/File/Upload", form);
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
        var response = await GetAsync($"/File/Download/{fileName}");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/octet-stream");
    }

    private Task<string> DownloadAsync(string name) => GetStringAsync($"/File/Download/{name}");

    private Task FtpInputOffAsync() => PostAsync("/File/Ftp/Input/Off");

    private Task FtpInputOnAsync() => PostAsync("/File/Ftp/Input/On");

    private Task FtpOutputOffAsync() => PostAsync("/File/Ftp/Output/Off");

    private Task FtpOutputOnAsync() => PostAsync("/File/Ftp/Output/On");

    private Task<List<FileInfoDto>> GetInputListAsync() => GetObjectFromJsonAsync<List<FileInfoDto>>("/File/List/Input");

    private Task<List<FileInfoDto>> GetOutputListAsync() =>
        GetObjectFromJsonAsync<List<FileInfoDto>>("/File/List/Output");
}