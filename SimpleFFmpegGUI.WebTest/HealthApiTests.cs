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
        content.Should().Contain("SimpleFFmpegGUI");
    }
}
