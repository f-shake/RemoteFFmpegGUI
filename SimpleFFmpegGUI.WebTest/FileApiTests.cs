using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.WebAPI;

// 建议安装这个包，断言更丝滑

namespace SimpleFFmpegGUI.WebTest;

public class FileApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestDownloadAsync()
    {
        var outputDir = (await GetDirsAsync()).OutputDir;
        var fileName = Path.GetFileName(appTestSettings.TestOutputVideo10s);
        if (fileName == null)
        {
            throw new Exception("测试输出视频不存在");
        }
        var filePath = Uri.EscapeDataString(Path.Combine(outputDir, fileName));
        await DownloadAsync(filePath);
        await DownloadAsync(fileName);
    }

    [Fact]
    public async Task TestFtpAsync()
    {
        await FtpInputOnAsync();
        await FtpInputOffAsync();
        await FtpOutputOnAsync();
        await FtpOutputOffAsync();
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

    private Task<string> DownloadAsync(string name) => GetStringAsync($"/File/Download/{name}");

    private Task FtpInputOffAsync() => PostAsync("/File/Ftp/Input/Off");

    private Task FtpInputOnAsync() => PostAsync("/File/Ftp/Input/On");

    private Task FtpOutputOffAsync() => PostAsync("/File/Ftp/Output/Off");

    private Task FtpOutputOnAsync() => PostAsync("/File/Ftp/Output/On");

    private Task<List<FileInfoDto>> GetInputListAsync() => GetObjectFromJsonAsync<List<FileInfoDto>>("/File/List/Input");

    private Task<List<FileInfoDto>> GetOutputListAsync() =>
        GetObjectFromJsonAsync<List<FileInfoDto>>("/File/List/Output");
}