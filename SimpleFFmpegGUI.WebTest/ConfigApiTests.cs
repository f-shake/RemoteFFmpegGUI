using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SimpleFFmpegGUI.WebTest;

public class ConfigApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestProcessPriorityAsync()
    {
        // 获取默认优先级
        var defaultPriority = await GetObjectFromJsonAsync<int>("/Config/ProcessPriority");
        defaultPriority.Should().BeInRange(0, 5);

        // 设置为3
        await PostAsync("/Config/ProcessPriority?priority=3");
        var afterSet = await GetObjectFromJsonAsync<int>("/Config/ProcessPriority");
        afterSet.Should().Be(3);

        // 设回0（Normal）
        await PostAsync("/Config/ProcessPriority?priority=0");
        var afterReset = await GetObjectFromJsonAsync<int>("/Config/ProcessPriority");
        afterReset.Should().Be(0);
    }
}
