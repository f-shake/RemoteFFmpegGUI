using FluentAssertions;
using SimpleFFmpegGUI.Services;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// PowerService 公共入口测试（仅测参数校验，避免真实采样 CPU/关机）
/// </summary>
public class PowerServiceTests
{
    [Fact]
    public async Task GetCpuUsageAsync_BothDefaults_ShouldThrow()
    {
        // 两个参数均为默认值时应拒绝（GetCpuUsageAsync 是静态方法，无需实例化 PowerService）
        var act = () => PowerService.GetCpuUsageAsync();
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
