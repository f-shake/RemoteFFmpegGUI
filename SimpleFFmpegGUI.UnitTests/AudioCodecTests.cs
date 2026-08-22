using FluentAssertions;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.FFmpegLib;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// AudioCodec 基类测试
/// </summary>
public class AudioCodecTests
{
    [Fact]
    public void Bitrate_Negative_ShouldThrow()
    {
        var act = () => AudioCodec.AudioCodecs[0].Bitrate(-1);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void Bitrate_Valid_ShouldReturnBaItem()
    {
        var item = new AAC().Bitrate(128);
        item.Key.Should().Be("b:a");
        item.Value.Should().Be("128K");
    }

    [Fact]
    public void SamplingRate_BelowMinimum_ShouldThrow()
    {
        var act = () => new AAC().SamplingRate(8000);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void SamplingRate_Valid_ShouldReturnArItem()
    {
        var item = new AAC().SamplingRate(48000);
        item.Key.Should().Be("ar");
        item.Value.Should().Be("48000");
    }
}
