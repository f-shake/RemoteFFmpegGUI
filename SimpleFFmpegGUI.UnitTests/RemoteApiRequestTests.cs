using System.Linq;
using System.Net.Http;
using FluentAssertions;
using SimpleFFmpegGUI.Helpers;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// 远程主机请求构造（RemoteApiRequest）：URL 拼接与 Bearer 头
/// </summary>
public class RemoteApiRequestTests
{
    [Theory]
    [InlineData("http://host:5001/api/", "File/Dirs", "http://host:5001/api/File/Dirs")]
    [InlineData("http://host:5001/api", "File/Dirs", "http://host:5001/api/File/Dirs")]
    [InlineData("http://host:5001/api", "/File/Dirs", "http://host:5001/api/File/Dirs")]
    [InlineData("http://host:5001/api/", "Task/Mux", "http://host:5001/api/Task/Mux")]
    [InlineData("https://fshake.com/ffmpeg/api/", "Token/Need", "https://fshake.com/ffmpeg/api/Token/Need")]
    public void BuildUrl_ShouldNormalizeSlashes(string address, string subUrl, string expected)
    {
        RemoteApiRequest.BuildUrl(address, subUrl).Should().Be(expected);
    }

    [Fact]
    public void AddAuthorization_EmptyToken_ShouldNotAddHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://host/api/Token/Need");
        RemoteApiRequest.AddAuthorization(request, "");
        request.Headers.Contains("Authorization").Should().BeFalse();
        RemoteApiRequest.AddAuthorization(request, null);
        request.Headers.Contains("Authorization").Should().BeFalse();
    }

    [Fact]
    public void AddAuthorization_ShouldAddBearerHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://host/api/File/List/Input");
        RemoteApiRequest.AddAuthorization(request, "1234qwerASDF");
        request.Headers.GetValues("Authorization").Single().Should().Be("Bearer 1234qwerASDF");
    }

    [Fact]
    public void AddAuthorization_BearerPrefixInToken_ShouldNotDoubleIt()
    {
        // 兼容 v1 已保存带前缀的令牌（大小写不敏感）
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://host/api/File/List/Input");
        RemoteApiRequest.AddAuthorization(request, "bearer 1234qwerASDF");
        request.Headers.GetValues("Authorization").Single().Should().Be("Bearer 1234qwerASDF");
    }
}
