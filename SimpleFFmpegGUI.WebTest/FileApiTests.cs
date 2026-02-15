using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;

// 建议安装这个包，断言更丝滑

namespace SimpleFFmpegGUI.WebTest;

public class FileApiTests(WebApplicationFactory<Program> factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestFtpAsync()
    {
        var inputOnResponse = await PostAsync("/File/Ftp/Input/On");
        await CheckResponseAsync(inputOnResponse);
        await Task.Delay(TimeSpan.FromSeconds(2));
        var inputOffResponse = await PostAsync("/File/Ftp/Input/Off");
        await CheckResponseAsync(inputOffResponse);
        await Task.Delay(TimeSpan.FromSeconds(2));
        var outputOnResponse = await PostAsync("/File/Ftp/Output/On");
        await CheckResponseAsync(outputOnResponse);
        await Task.Delay(TimeSpan.FromSeconds(2));
        var outputOffResponse = await PostAsync("/File/Ftp/Output/Off");
        await CheckResponseAsync(outputOffResponse);
    }
}