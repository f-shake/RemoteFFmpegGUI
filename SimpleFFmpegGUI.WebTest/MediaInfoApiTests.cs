using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Models.MediaInfo;
using SimpleFFmpegGUI.WebAPI;

// 建议安装这个包，断言更丝滑

namespace SimpleFFmpegGUI.WebTest;

public class MediaInfoApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestMediaInfoAsync()
    {
        var result = await GetMediaInfoAsync(Path.GetFileName(appTestSettings.TestVideo10s));
        result.Should().NotBeNull();
    }
    
    [Fact]
    public async Task TestSnapshotAsync()
    {
        var result = await GetSnapshotAsync(Path.GetFileName(appTestSettings.TestVideo10s), 1);
        result.Should().NotBeNull();
    }

    private Task<MediaInfoGeneral> GetMediaInfoAsync(string name) =>
        GetObjectFromJsonAsync<MediaInfoGeneral>($"/MediaInfo/{name}");
    
    private Task<HttpResponseMessage> GetSnapshotAsync(string name, double seconds) =>
        GetAsync($"/MediaInfo/Snapshot?videoPath={name}&seconds={seconds}");

}