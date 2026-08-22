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

    /// <summary>
    /// 读取不存在的媒体文件应返回 404
    /// </summary>
    [Fact]
    public async Task TestMediaInfoNonExistentAsync()
    {
        var act = async () => await GetMediaInfoAsync("no_such.mp4");
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 快照的视频路径不存在应返回 404
    /// </summary>
    [Fact]
    public async Task TestSnapshotNonExistentAsync()
    {
        var act = async () => await GetSnapshotAsync("no_such.mp4", 1);
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 快照 seconds 为负/NaN/+∞ 应被拒绝（P3-5 边界校验）
    /// </summary>
    [Fact]
    public async Task TestSnapshotInvalidSecondsAsync()
    {
        var name = Path.GetFileName(appTestSettings.TestVideo10s);

        // 负值
        var act = async () => await GetSnapshotAsync(name, -1);
        await act.Should().ThrowAsync<Exception>();

        // NaN：double.NaN.ToString() == "NaN"，可被模型绑定解析为 NaN
        act = async () => await GetSnapshotAsync(name, double.NaN);
        await act.Should().ThrowAsync<Exception>();

        // +∞：double.PositiveInfinity.ToString() 是 "∞" 而非 "Infinity"，绑定会失败，
        // 改用字面量 "Infinity"（double.TryParse 可解析为 +∞），以触发控制的 IsInfinity 校验
        act = async () => await GetAsync($"/api/MediaInfo/Snapshot?videoPath={name}&seconds=Infinity");
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 快照响应的 Content-Type 应为 image/jpeg
    /// </summary>
    [Fact]
    public async Task TestSnapshotContentTypeAsync()
    {
        var name = Path.GetFileName(appTestSettings.TestVideo10s);
        var response = await GetSnapshotAsync(name, 1);
        response.Content.Headers.ContentType?.MediaType.Should().Be("image/jpeg");
    }

    /// <summary>
    /// 快照秒数非数值（绑定失败）应返回 400（窄选项下值类型绑定失败仍自动 400）
    /// </summary>
    [Fact]
    public async Task TestSnapshotNonNumericSecondsAsync()
    {
        var name = Path.GetFileName(appTestSettings.TestVideo10s);
        var act = async () => await GetAsync($"/api/MediaInfo/Snapshot?videoPath={name}&seconds=abc");
        await act.Should().ThrowAsync<Exception>();
    }

    private Task<MediaInfoGeneral> GetMediaInfoAsync(string name) =>
        GetObjectFromJsonAsync<MediaInfoGeneral>($"/api/MediaInfo/{name}");
    
    private Task<HttpResponseMessage> GetSnapshotAsync(string name, double seconds) =>
        GetAsync($"/api/MediaInfo/Snapshot?videoPath={name}&seconds={seconds}");

}