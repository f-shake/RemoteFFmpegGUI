using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.WebAPI;

namespace SimpleFFmpegGUI.WebTest;

public abstract class SimpleFFmpegApiTestsBase : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;
    private string token;

    protected abstract string ControllerName { get; }

    protected SimpleFFmpegApiTestsBase(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(ConfigureAppConfiguration);
        });
        var config = factory.Services.GetRequiredService<IConfiguration>();
        token = config.GetValue<string>(AppSettingsKeys.TokenKey);
        client = factory.CreateClient();
    }

    private void ConfigureAppConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder)
    {
        builder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Token"] = "Test_Token_123",
            ["Database:ConnectionString"] = "DataSource=:memory:"
        });
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
        var request = new HttpRequestMessage(method, $"/{ControllerName}/{endpoint}");
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