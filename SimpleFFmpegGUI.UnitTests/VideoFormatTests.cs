using FluentAssertions;
using SimpleFFmpegGUI.FFmpegLib;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// VideoFormat 容器格式测试
/// </summary>
public class VideoFormatTests
{
    [Fact]
    public void Constructor_NullName_ShouldThrow()
    {
        var act = () => new VideoFormat(null, "mp4");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullExtension_ShouldThrow()
    {
        var act = () => new VideoFormat("mp4", null);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AudioOnly_ShouldBeTrueForMp3()
    {
        VideoFormat.Formats.Single(p => p.Extension == "mp3").AudioOnly.Should().BeTrue();
    }

    [Fact]
    public void ImageOnly_ShouldBeTrueForPng()
    {
        VideoFormat.Formats.Single(p => p.Extension == "png").ImageOnly.Should().BeTrue();
    }

    [Fact]
    public void Formats_Count_ShouldBeSeventeen()
    {
        VideoFormat.Formats.Length.Should().Be(17);
    }

    [Fact]
    public void Formats_ShouldContainMainMp4()
    {
        var mp4 = VideoFormat.Formats.Single(p => p.Extension == "mp4");
        mp4.Name.Should().Be("mp4");
        mp4.Main.Should().BeTrue();
    }
}
