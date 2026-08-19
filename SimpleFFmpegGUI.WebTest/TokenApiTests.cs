using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SimpleFFmpegGUI.WebTest;

public class TokenApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    /// <summary>
    /// 不带 Authorization 头（未登录）访问受保护接口应 401（P4-3 反向用例）
    /// </summary>
    [Fact]
    public async Task TestNoTokenUnauthorizedAsync()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/Task");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// "Bearer undefined"（前端未登录时误设的无效头）应 401（P1-5 反向用例）
    /// </summary>
    [Fact]
    public async Task TestBearerUndefinedUnauthorizedAsync()
    {
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/Task");
        request.Headers.Add("Authorization", "Bearer undefined");
        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
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
