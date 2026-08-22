using FluentAssertions;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.FFmpegLib;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// VideoArgumentsGenerator 的参数逻辑测试
/// </summary>
public class VideoArgumentsGeneratorTests
{
    [Fact]
    public void Aspect_ValidRatio_ShouldAddArgument()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Aspect("16:9");
        vg.GetArguments().Should().Be("-aspect 16:9");
    }

    [Fact]
    public void Aspect_Invalid_ShouldThrow()
    {
        var vg = new VideoArgumentsGenerator();
        var act = () => vg.Aspect("abcde");
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void BufferRatio_WithoutMaxBitrate_ShouldThrow()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Codec("H264");
        var act = () => vg.BufferRatio(2.0);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void BufferRatio_SVTAV1_ShouldBeSkipped()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Codec("libsvtav1");
        vg.BufferRatio(2.0); // SVTAV1 不支持缓冲，应跳过而非抛异常
        vg.GetArguments().Should().NotContain("bufsize");
    }

    [Fact]
    public void Codec_Auto_ShouldNotAddCv_AndSetGeneral()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Codec("自动");
        vg.GetArguments().Should().Be("");
        vg.VideoCodec.Should().BeOfType<GeneralVideoCodec>();
    }

    [Fact]
    public void Codec_Unknown_ShouldSetGeneral_AndAddCv()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Codec("mycodec");
        vg.GetArguments().Should().Be("-c:v mycodec");
        vg.VideoCodec.Should().BeOfType<GeneralVideoCodec>();
    }

    [Fact]
    public void Crf_ZeroOrNull_ShouldBeIgnored()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Codec("H264");
        vg.CRF(0);
        vg.CRF(null);
        vg.GetArguments().Should().Be("-c:v libx264");
    }

    [Fact]
    public void Speed_ZeroOrNegative_ShouldBeIgnored()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Codec("H264");
        vg.Speed(0);
        vg.Speed(-1);
        vg.GetArguments().Should().Be("-c:v libx264");
    }

    [Fact]
    public void Speed_GreaterThanMax_ShouldThrow()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Codec("H264"); // MaxSpeedLevel = 8
        var act = () => vg.Speed(9);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void MaxBitrate_NaN_ShouldBeIgnored()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Codec("H264");
        vg.MaxBitrate(double.NaN);
        vg.GetArguments().Should().Be("-c:v libx264");
    }

    [Fact]
    public void FrameRate_NaN_ShouldBeIgnored()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Codec("H264");
        vg.FrameRate(double.NaN);
        vg.GetArguments().Should().Be("-c:v libx264");
    }

    [Fact]
    public void Pass_ZeroOrNull_ShouldBeIgnored()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Codec("H264");
        vg.Pass(0);
        vg.Pass(null);
        vg.GetArguments().Should().Be("-c:v libx264");
    }

    [Fact]
    public void Scale_ShouldUseVfGroup()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Scale("1920x1080");
        vg.GetArguments().Should().Be("-vf scale=1920x1080");
    }

    [Fact]
    public void Disable_ShouldContainVn()
    {
        var vg = new VideoArgumentsGenerator();
        vg.Disable();
        vg.GetArguments().Trim().Should().Be("-vn");
    }
}
