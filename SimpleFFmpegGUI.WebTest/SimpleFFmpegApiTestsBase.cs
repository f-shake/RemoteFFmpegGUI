using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.WebAPI;

namespace SimpleFFmpegGUI.WebTest;

public class SimpleFFmpegApiTestsBase : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    protected readonly HttpClient client;
    private string token;

    public SimpleFFmpegApiTestsBase(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
        var config = factory.Services.GetRequiredService<IConfiguration>();
        token = config.GetValue<string>(AppSettingsKeys.TokenKey);

        client = factory.CreateClient();
    }

    protected Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        return SendAsync(HttpMethod.Get, endpoint);
    }

    protected Task<HttpResponseMessage> PostAsync(string endpoint)
    {
        return SendAsync(HttpMethod.Post, endpoint);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, endpoint);
        if (token != null)
        {
            request.Headers.Add("Authorization", $"Bearer {token}");
        }

        return await client.SendAsync(request);
    }

    protected async Task CheckResponseAsync(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            var reqMsg = response.RequestMessage;
            var method = reqMsg?.Method;
            var uri = reqMsg?.RequestUri?.AbsolutePath;
            throw new Exception($"调用 {method} {uri} 失败（{(int)response.StatusCode}）：{content}");
        }
    }
}