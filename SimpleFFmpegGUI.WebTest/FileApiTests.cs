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
        var content = await DownloadAsync(Path.GetFileName(appTestSettings.TestOutputVideo10s));
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
        // var dir = await GetStringAsync("Dir");
        var dir = await GetDirAsync();
        dir.Should().NotBeNullOrEmpty();

        var inputs = await GetInputListAsync();
        inputs.Count.Should().BeGreaterThanOrEqualTo(1);
        inputs.Should().Contain(p => p == Path.GetFileName(appTestSettings.TestVideo10s));

        var outputs = await GetOutputListAsync();
        outputs.Count.Should().BeGreaterThanOrEqualTo(1);
        outputs.Should().Contain(p => p.Name == Path.GetFileName(appTestSettings.TestOutputVideo10s));
    }

    private Task<string> DownloadAsync(string name) => GetStringAsync($"/File/Download?name={name}");

    private Task FtpInputOffAsync() => PostAsync("/File/Ftp/Input/Off");

    private Task FtpInputOnAsync() => PostAsync("/File/Ftp/Input/On");

    private Task FtpOutputOffAsync() => PostAsync("/File/Ftp/Output/Off");

    private Task FtpOutputOnAsync() => PostAsync("/File/Ftp/Output/On");

    private Task<string> GetDirAsync() => GetStringAsync("/File/Dir");

    private Task<List<string>> GetInputListAsync() => GetObjectFromJsonAsync<List<string>>("/File/List/Input");

    private Task<List<FileInfoDto>> GetOutputListAsync() =>
        GetObjectFromJsonAsync<List<FileInfoDto>>("/File/List/Output");
}