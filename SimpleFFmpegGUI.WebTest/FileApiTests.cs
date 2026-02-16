using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;

// 建议安装这个包，断言更丝滑

namespace SimpleFFmpegGUI.WebTest;

public class FileApiTests(WebApplicationFactory<Program> factory) : SimpleFFmpegApiTestsBase(factory)
{
    protected override string ControllerName => "File";

    [Fact]
    public async Task TestFtpAsync()
    {
        var inputOnResponse = await PostAsync("Ftp/Input/On");
        await CheckResponseAsync(inputOnResponse);
        await Task.Delay(TimeSpan.FromSeconds(2));
        var inputOffResponse = await PostAsync("Ftp/Input/Off");
        await CheckResponseAsync(inputOffResponse);
        await Task.Delay(TimeSpan.FromSeconds(2));
        var outputOnResponse = await PostAsync("Ftp/Output/On");
        await CheckResponseAsync(outputOnResponse);
        await Task.Delay(TimeSpan.FromSeconds(2));
        var outputOffResponse = await PostAsync("Ftp/Output/Off");
        await CheckResponseAsync(outputOffResponse);
    }


    [Fact]
    public async Task TestListAsync()
    {
        var dirResponse = await GetAsync("Dir");
        await CheckResponseAsync(dirResponse);
    }
}