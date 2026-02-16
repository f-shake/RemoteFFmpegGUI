using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.WebAPI;
using SQLitePCL;

namespace SimpleFFmpegGUI.WebTest;

public abstract class SimpleFFmpegApiTestsBase : IClassFixture<SimpleFFmpegWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;
    private string token;

    protected abstract string ControllerName { get; }

    protected SimpleFFmpegApiTestsBase(SimpleFFmpegWebApplicationFactory factory)
    {
        this.factory = factory;
        var config = this.factory.Services.GetRequiredService<IConfiguration>();
        token = config.GetValue<string>(AppSettingsKeys.TokenKey);
        client = this.factory.CreateClient();
    }

    protected Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        return SendAsync(HttpMethod.Get, endpoint);
    }

    protected async Task<T> GetObjectFromJsonAsync<T>(string endpoint)
    {
        var response = await SendAsync(HttpMethod.Get, endpoint);
        var content = await response.Content.ReadAsStringAsync();
        T result;
        try
        {
            result = content.DeserializeWithDefaultSettings<T>();
        }
        catch (Exception ex)
        {
            string exceptionContent = content.Length > 100 ? content[..100] : content;
            throw new Exception($"反序列为{typeof(T).Name}失败：{exceptionContent}", ex);
        }

        return result;
    }


    protected async Task<string> GetStringAsync(string endpoint)
    {
        var response = await SendAsync(HttpMethod.Get, endpoint);
        var content = await response.Content.ReadAsStringAsync();
        return content;
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

        var response = await client.SendAsync(request);
        await CheckResponseAsync(response);
        return response;
    }

    private async Task CheckResponseAsync(HttpResponseMessage response)
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