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

    /// <summary>
    /// 快照尺寸配置读写（P1-12）
    /// </summary>
    [Fact]
    public async Task TestSnapshotSizeAsync()
    {
        var size = await GetObjectFromJsonAsync<string>("/Config/SnapshotSize");
        size.Should().NotBeNullOrWhiteSpace();

        await PostAsync("/Config/SnapshotSize?snapshotSize=-1:720");
        var afterSet = await GetObjectFromJsonAsync<string>("/Config/SnapshotSize");
        afterSet.Should().Be("-1:720");

        // 恢复默认，避免影响其他用例
        await PostAsync("/Config/SnapshotSize?snapshotSize=-1:1080");
        var afterReset = await GetObjectFromJsonAsync<string>("/Config/SnapshotSize");
        afterReset.Should().Be("-1:1080");
    }

    /// <summary>
    /// 越界的优先级应被拒绝（P3-5 范围校验，随 P1-3 一并落地）
    /// </summary>
    [Fact]
    public async Task TestSetProcessPriorityOutOfRangeAsync()
    {
        var act = async () => await PostAsync("/Config/ProcessPriority?priority=6");
        await act.Should().ThrowAsync<Exception>();

        act = async () => await PostAsync("/Config/ProcessPriority?priority=-1");
        await act.Should().ThrowAsync<Exception>();
    }
}
