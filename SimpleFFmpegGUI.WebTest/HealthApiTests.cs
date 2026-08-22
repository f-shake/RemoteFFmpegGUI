using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SimpleFFmpegGUI.WebTest;

/// <summary>
/// 健康检查与根路由（无鉴权，独立于 Token 配置）
/// </summary>
public class HealthApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestHealthAsync()
    {
        var response = await GetAsync("/health");
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task TestRootAsync()
    {
        var response = await GetAsync("/");
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        // 支持两种部署形态：裸 API 返回横幅 "SimpleFFmpegGUI API is running!"；
        // 托管前端时 / 返回 index.html（标题含 "FFmpeg"）。二者都含 "FFmpeg"，据此断言更稳。
        content.Should().Contain("FFmpeg");
    }

    [Fact]
    public async Task TestUnknownApiPathReturns404Async()
    {
        // /api 前缀下的无效路径必须 404，绝不能被子兜底吞成 index.html/200（掩盖后端路由错误）。
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/NotExistPath");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TestUnknownPageWithoutFrontendReturns404Async()
    {
        // 测试宿主无 wwwroot/index.html：未知非 API 页面回退时不应因缺文件抛 500，而应干净地 404。
        var client = factory.CreateClient();
        var response = await client.GetAsync("/some/random/page");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
