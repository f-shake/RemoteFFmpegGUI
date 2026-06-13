using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SimpleFFmpegGUI.WebTest;

public class TokenApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestTokenNeedAsync()
    {
        // 测试配置中设置了Token，所以Need应返回true
        var need = await GetObjectFromJsonAsync<bool>("/Token/Need");
        need.Should().BeTrue();
    }

    [Fact]
    public async Task TestTokenCheckAsync()
    {
        // 有效的Token（明文）
        var valid = await GetObjectFromJsonAsync<bool>("/Token/Check/Test_Token_123");
        valid.Should().BeTrue();

        // 无效的Token
        var invalid = await GetObjectFromJsonAsync<bool>("/Token/Check/wrong_token");
        invalid.Should().BeFalse();
    }
}
