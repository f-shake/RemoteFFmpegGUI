using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Models;

namespace SimpleFFmpegGUI.WebTest;

[Collection("FFmpegWebCollection")]
public abstract class SimpleFFmpegApiTestsBase //: IClassFixture<SimpleFFmpegWebApplicationFactory>
{
    protected readonly AppSettings appSettings;

    protected readonly AppTestSettings appTestSettings;

    private readonly HttpClient client;

    private readonly WebApplicationFactory<Program> factory;

    private readonly string token;

    protected SimpleFFmpegApiTestsBase(SimpleFFmpegWebApplicationFactory factory)
    {
        this.factory = factory;
        appSettings = this.factory.Services.GetRequiredService<IOptions<AppSettings>>().Value;
        appTestSettings = this.factory.Services.GetRequiredService<IOptions<AppTestSettings>>().Value;
        token = appSettings.Token;
        client = this.factory.CreateClient();
        ClearDatabase();
    }

    protected Task<HttpResponseMessage> DeleteAsync(string endpoint)
    {
        return SendAsync(HttpMethod.Delete, endpoint);
    }

    protected Task<HttpResponseMessage> GetAsync(string endpoint)
    {
        return SendAsync(HttpMethod.Get, endpoint);
    }

    protected Task<AppDirDto> GetDirsAsync() => GetObjectFromJsonAsync<AppDirDto>("/File/Dirs");
    protected async Task<T> GetObjectFromJsonAsync<T>(string endpoint)
    {
        var response = await SendAsync(HttpMethod.Get, endpoint);
        var content = await response.Content.ReadAsStringAsync();
        return ParseJson<T>(content);
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

    protected async Task<T> PostObjectFromJsonAsync<T>(string endpoint, object body = null)
    {
        var response = await SendAsync(HttpMethod.Post, endpoint, body);
        var content = await response.Content.ReadAsStringAsync();
        return ParseJson<T>(content);
    }

    private async Task CheckResponseAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var reqMsg = response.RequestMessage;
            var method = reqMsg?.Method;
            var uri = reqMsg?.RequestUri?.AbsolutePath;
            throw new Exception($"调用 {method} {uri} 失败（{(int)response.StatusCode}）：{content}");
        }
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

    private T ParseJson<T>(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return default;
        }

        try
        {
            return content.DeserializeWithWebSettings<T>();
        }
        catch (Exception ex)
        {
            string exceptionContent = content.Length > 100 ? content[..100] : content;
            throw new Exception($"反序列为{typeof(T).Name}失败：{exceptionContent}", ex);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string endpoint, object body = null)
    {
        var request = new HttpRequestMessage(method, endpoint);
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
}