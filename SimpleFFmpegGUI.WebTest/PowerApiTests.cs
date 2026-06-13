using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SimpleFFmpegGUI.Dto;

namespace SimpleFFmpegGUI.WebTest;

public class PowerApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestCpuUsageAsync()
    {
        var cpuUsages = await GetObjectFromJsonAsync<CpuCoreUsageDto[]>("/Power/Cpu");
        cpuUsages.Should().NotBeNull();
        cpuUsages.Length.Should().BeGreaterThan(0);
        cpuUsages.Should().AllSatisfy(c =>
        {
            c.Usage.Should().BeInRange(0, 100);
        });
        // CpuIndex/CoreIndex 在某些环境中可能为 -1，不强制断言
    }

    [Fact]
    public async Task TestShutdownQueueToggleAsync()
    {
        // GET默认值应该是false
        var initial = await GetObjectFromJsonAsync<bool>("/Power/ShutdownQueue");
        initial.Should().BeFalse();

        // POST设为true（模拟前端通过FormData发送）
        var formOn = new MultipartFormDataContent
        {
            { new StringContent("true"), "on" }
        };
        await PostMultipartAsync("/Power/ShutdownQueue", formOn);

        var afterOn = await GetObjectFromJsonAsync<bool>("/Power/ShutdownQueue");
        afterOn.Should().BeTrue();

        // POST设为false
        var formOff = new MultipartFormDataContent
        {
            { new StringContent("false"), "on" }
        };
        await PostMultipartAsync("/Power/ShutdownQueue", formOff);

        var afterOff = await GetObjectFromJsonAsync<bool>("/Power/ShutdownQueue");
        afterOff.Should().BeFalse();
    }
}
