using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.WebAPI;
using SQLitePCL;

namespace SimpleFFmpegGUI.WebTest;

public abstract class SimpleFFmpegApiTestsBase : IClassFixture<SimpleFFmpegWebApplicationFactory>
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;
    private readonly string token;
    protected readonly IConfiguration config;

    protected abstract string ControllerName { get; }

    protected SimpleFFmpegApiTestsBase(SimpleFFmpegWebApplicationFactory factory)
    {
        this.factory = factory;
        config = this.factory.Services.GetRequiredService<IConfiguration>();
        token = config.GetValue<string>(AppSettingsKeys.TokenKey);
        client = this.factory.CreateClient();
        ClearDatabase();
    }

    private void ClearDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FFmpegDbContext>>();
        using var context = dbContextFactory.CreateDbContext();

        context.Tasks.ExecuteDelete();
        context.Logs.ExecuteDelete();
        context.Presets.ExecuteDelete();
    }

    protected Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        return SendAsync(HttpMethod.Get, endpoint);
    }

    protected async Task<T> GetObjectFromJsonAsync<T>(string endpoint)
    {
        var response = await SendAsync(HttpMethod.Get, endpoint);
        var content = await response.Content.ReadAsStringAsync();
        return ParseJson<T>(content);
    }

    protected async Task<T> PostObjectFromJsonAsync<T>(string endpoint, object body = null)
    {
        var response = await SendAsync(HttpMethod.Post, endpoint, body);
        var content = await response.Content.ReadAsStringAsync();
        return ParseJson<T>(content);
    }

    private T ParseJson<T>(string content)
    {
        try
        {
            return content.DeserializeWithDefaultSettings<T>();
        }
        catch (Exception ex)
        {
            string exceptionContent = content.Length > 100 ? content[..100] : content;
            throw new Exception($"反序列为{typeof(T).Name}失败：{exceptionContent}", ex);
        }
    }


    protected async Task<string> GetStringAsync(string endpoint)
    {
        var response = await SendAsync(HttpMethod.Get, endpoint);
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }

    protected Task<HttpResponseMessage> PostAsync(string endpoint, object body = null)
    {
        return SendAsync(HttpMethod.Post, endpoint, body);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string endpoint, object body = null)
    {
        var request = new HttpRequestMessage(method, $"/{ControllerName}/{endpoint}");
        if (token != null)
        {
            request.Headers.Add("Authorization", $"Bearer {token}");
        }

        if (method == HttpMethod.Post && body != null)
        {
            request.Content = JsonContent.Create(body);
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