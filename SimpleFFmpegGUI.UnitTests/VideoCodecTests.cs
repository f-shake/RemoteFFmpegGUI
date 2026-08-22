using FluentAssertions;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.FFmpegLib;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// VideoCodec 基类与查找逻辑测试
/// </summary>
public class VideoCodecTests
{
    [Fact]
    public void GetCodec_Null_ShouldReturnNull()
    {
        VideoCodec.GetCodec(null).Should().BeNull();
    }

    [Fact]
    public void GetCodec_KnownName_ShouldReturnInstance()
    {
        VideoCodec.GetCodec("H264").Should().BeSameAs(VideoCodec.X264);
    }

    [Fact]
    public void GetCodec_Unknown_ShouldReturnNull()
    {
        VideoCodec.GetCodec("unknown").Should().BeNull();
    }

    [Fact]
    public void GetCodec_NvidiaName_ShouldReturnFixInstance()
    {
        // fix ②：Nvdia→Nvidia 后名称应能正确命中 N_H265
        VideoCodec.GetCodec("H265 (Nvidia)").Should().BeSameAs(VideoCodec.N_H265);
    }

    [Fact]
    public void AverageBitrate_Negative_ShouldThrow()
    {
        var x264 = VideoCodec.GetCodec("H264");
        var act = () => x264.AverageBitrate(-1);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void AverageBitrate_Valid_ShouldReturnBvItem()
    {
        var item = VideoCodec.X264.AverageBitrate(5);
        item.Key.Should().Be("b:v");
        item.Value.Should().Be("5M");
    }

    [Fact]
    public void MaxBitrate_Negative_ShouldThrow()
    {
        var act = () => VideoCodec.X264.MaxBitrate(-1);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void BufferSize_Negative_ShouldThrow()
    {
        var act = () => VideoCodec.X264.BufferSize(-1);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void FrameRate_Negative_ShouldThrow()
    {
        var act = () => VideoCodec.X264.FrameRate(-1);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void PixelFormat_Blank_ShouldThrow()
    {
        var act = () => VideoCodec.X264.PixelFormat(" ");
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void PixelFormat_Valid_ShouldReturnPixFmtItem()
    {
        var item = VideoCodec.X264.PixelFormat("yuv420p");
        item.Key.Should().Be("pix_fmt");
        item.Value.Should().Be("yuv420p");
    }

    [Fact]
    public void Pass_Invalid_ShouldThrow()
    {
        var act = () => VideoCodec.X264.Pass(0);
        act.Should().Throw<FFmpegArgumentException>();
        act = () => VideoCodec.X264.Pass(4);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void Speed_GreaterThanMax_ShouldThrow()
    {
        var act = () => VideoCodec.X264.Speed(9); // X264.MaxSpeedLevel = 8
        act.Should().Throw<FFmpegArgumentException>();
    }
}
