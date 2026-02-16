using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.WebAPI;

// 建议安装这个包，断言更丝滑

namespace SimpleFFmpegGUI.WebTest;

public class FileApiTests : SimpleFFmpegApiTestsBase
{
    private string testVideo10s;
    private string outputTestVideo10s;

    public FileApiTests(SimpleFFmpegWebApplicationFactory factory) : base(factory)
    {
        testVideo10s = factory.Services.GetRequiredService<IConfiguration>()
            .GetValue<string>(AppTestSettingsKeys.TestVideo10sKey);
        outputTestVideo10s = factory.Services.GetRequiredService<IConfiguration>()
            .GetValue<string>(AppTestSettingsKeys.TestOutputVideo10sKey);
    }

    protected override string ControllerName => "File";

    [Fact]
    public async Task TestFtpAsync()
    {
        var inputOnResponse = await PostAsync("Ftp/Input/On");
        var inputOffResponse = await PostAsync("Ftp/Input/Off");
        var outputOnResponse = await PostAsync("Ftp/Output/On");
        var outputOffResponse = await PostAsync("Ftp/Output/Off");
    }


    [Fact]
    public async Task TestListAsync()
    {
        var dir = await GetStringAsync("Dir");
        dir.Should().NotBeNullOrEmpty();

        var inputs = await GetObjectFromJsonAsync<List<string>>("List/Input");
        inputs.Count.Should().BeGreaterThanOrEqualTo(1);
        inputs.Should().Contain(Path.GetFileName(testVideo10s));

        var outputs = await GetObjectFromJsonAsync<List<FileInfoDto>>("List/Output");
        outputs.Count.Should().BeGreaterThanOrEqualTo(1);
        outputs.Should().Contain(p => p.Name == Path.GetFileName(outputTestVideo10s));
    }

    [Fact]
    public async Task TestDownloadAsync()
    {
        var dirResponse = await GetAsync($"Download?name={Path.GetFileName(outputTestVideo10s)}");
    }
}