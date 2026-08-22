using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SimpleFFmpegGUI.WebTest;

public class ConfigApiTests(SimpleFFmpegWebApplicationFactory factory) : SimpleFFmpegApiTestsBase(factory)
{
    [Fact]
    public async Task TestProcessPriorityAsync()
    {
        // 获取默认优先级
        var defaultPriority = await GetObjectFromJsonAsync<int>("/api/Config/ProcessPriority");
        defaultPriority.Should().BeInRange(0, 5);

        // 设置为3
        await PostAsync("/api/Config/ProcessPriority?priority=3");
        var afterSet = await GetObjectFromJsonAsync<int>("/api/Config/ProcessPriority");
        afterSet.Should().Be(3);

        // 设回0（Normal）
        await PostAsync("/api/Config/ProcessPriority?priority=0");
        var afterReset = await GetObjectFromJsonAsync<int>("/api/Config/ProcessPriority");
        afterReset.Should().Be(0);
    }

    /// <summary>
    /// 快照尺寸配置读写（P1-12）
    /// </summary>
    [Fact]
    public async Task TestSnapshotSizeAsync()
    {
        var size = await GetObjectFromJsonAsync<string>("/api/Config/SnapshotSize");
        size.Should().NotBeNullOrWhiteSpace();

        await PostAsync("/api/Config/SnapshotSize?snapshotSize=-1:720");
        var afterSet = await GetObjectFromJsonAsync<string>("/api/Config/SnapshotSize");
        afterSet.Should().Be("-1:720");

        // 恢复默认，避免影响其他用例
        await PostAsync("/api/Config/SnapshotSize?snapshotSize=-1:1080");
        var afterReset = await GetObjectFromJsonAsync<string>("/api/Config/SnapshotSize");
        afterReset.Should().Be("-1:1080");
    }

    /// <summary>
    /// 越界的优先级应被拒绝（P3-5 范围校验，随 P1-3 一并落地）
    /// </summary>
    [Fact]
    public async Task TestSetProcessPriorityOutOfRangeAsync()
    {
        var act = async () => await PostAsync("/api/Config/ProcessPriority?priority=6");
        await act.Should().ThrowAsync<Exception>();

        act = async () => await PostAsync("/api/Config/ProcessPriority?priority=-1");
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 非法快照尺寸应被拒绝（P3-5 格式校验：宽度:高度，负值仅允许 -1 开头）
    /// </summary>
    [Fact]
    public async Task TestSnapshotSizeInvalidValuesRejectedAsync()
    {
        var act = async () => await PostAsync("/api/Config/SnapshotSize?snapshotSize=abc");
        await act.Should().ThrowAsync<Exception>();

        act = async () => await PostAsync("/api/Config/SnapshotSize?snapshotSize=1920");
        await act.Should().ThrowAsync<Exception>();

        act = async () => await PostAsync("/api/Config/SnapshotSize?snapshotSize=1920:1080:1");
        await act.Should().ThrowAsync<Exception>();

        act = async () => await PostAsync("/api/Config/SnapshotSize?snapshotSize=");
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    /// 优先级非数值（绑定失败）应返回 400（窄选项下值类型绑定失败仍自动 400）
    /// </summary>
    [Fact]
    public async Task TestNonNumericPriorityRejectedAsync()
    {
        var act = async () => await PostAsync("/api/Config/ProcessPriority?priority=abc");
        await act.Should().ThrowAsync<Exception>();
    }
}
