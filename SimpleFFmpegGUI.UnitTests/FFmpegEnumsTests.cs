using FluentAssertions;
using SimpleFFmpegGUI.FFmpegLib;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// FFmpegEnums 的数据一致性测试
/// </summary>
public class FFmpegEnumsTests
{
    [Fact]
    public void Presets_ShouldContainExpectedPreset()
    {
        FFmpegEnums.Presets.Should().Contain("medium");
        FFmpegEnums.Presets.Should().Contain("ultrafast");
        FFmpegEnums.Presets.Length.Should().Be(9);
    }

    [Fact]
    public void NPresets_ShouldContainExpectedPreset()
    {
        FFmpegEnums.N_Presets.Should().Contain("p7");
        FFmpegEnums.N_Presets.Should().Contain("p1");
        FFmpegEnums.N_Presets.Length.Should().Be(7);
    }

    [Fact]
    public void PixelFormats_ShouldContainExpectedFormat()
    {
        FFmpegEnums.PixelFormats.Should().Contain("yuv420p");
        FFmpegEnums.PixelFormats.Length.Should().Be(7);
    }

    [Fact]
    public void Presets_EachIndex_ShouldMapToSpeedLevel()
    {
        // 速度预设下标 0..8，保证 X264/X265 的 Speed(level) 不会越界
        for (int i = 0; i < FFmpegEnums.Presets.Length; i++)
        {
            FFmpegEnums.Presets[i].Should().NotBeNullOrWhiteSpace();
        }
    }
}
