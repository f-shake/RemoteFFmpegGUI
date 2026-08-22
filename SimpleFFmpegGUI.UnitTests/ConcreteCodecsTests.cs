using FluentAssertions;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.FFmpegLib;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// 具体编码器的名称/库/参数项测试
/// </summary>
public class ConcreteCodecsTests
{
    [Fact]
    public void X264_ShouldHaveNameAndLib()
    {
        VideoCodec.X264.Name.Should().Be("H264");
        VideoCodec.X264.Lib.Should().Be("libx264");
        VideoCodec.X264.DefaultCRF.Should().Be(23);
        VideoCodec.X264.MaxCRF.Should().Be(51);
    }

    [Fact]
    public void X264_Speed_ShouldReturnPreset()
    {
        var item = VideoCodec.X264.Speed(3);
        item.Key.Should().Be("preset");
        item.Value.Should().Be("medium"); // FFmpegEnums.Presets[3]
    }

    [Fact]
    public void X265_Pass_ShouldUseX265ParamsParent()
    {
        var item = VideoCodec.X265.Pass(1);
        item.Key.Should().Be("pass");
        item.Value.Should().Be("1");
        item.Parent.Should().Be("x265-params");
        item.Separator.Should().Be(':');
    }

    [Fact]
    public void X265_ShouldHaveNameAndLib()
    {
        VideoCodec.X265.Name.Should().Be("H265");
        VideoCodec.X265.Lib.Should().Be("libx265");
    }

    [Fact]
    public void XVP9_Speed_ShouldReturnCpuUsed()
    {
        var item = VideoCodec.XVP9.Speed(3);
        item.Key.Should().Be("cpu-used");
        item.Value.Should().Be("3");
    }

    [Fact]
    public void XVP9_ExtraArguments_ShouldContainRowMt()
    {
        VideoCodec.XVP9.ExtraArguments().Should().ContainSingle(p => p.Key == "row-mt" && p.Value == "1");
    }

    [Fact]
    public void SVTAV1_AverageBitrate_ShouldChainTbrViaOther()
    {
        var item = VideoCodec.SVTAV1.AverageBitrate(5);
        item.Key.Should().Be("rc");
        item.Value.Should().Be("1");
        item.Parent.Should().Be("svtav1-params");
        item.Separator.Should().Be(':');
        item.Other.Should().NotBeNull();
        item.Other!.Key.Should().Be("tbr");
        item.Other.Value.Should().Be("5000"); // 5 * 1000
        item.Other.Parent.Should().Be("svtav1-params");
    }

    [Fact]
    public void SVTAV1_BufferSize_ShouldThrow()
    {
        var act = () => VideoCodec.SVTAV1.BufferSize(5);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void SVTAV1_Pass_ShouldThrow()
    {
        var act = () => VideoCodec.SVTAV1.Pass(1);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void GeneralVideoCodec_ShouldHaveNullNameAndLib()
    {
        VideoCodec.General.Name.Should().BeNull();
        VideoCodec.General.Lib.Should().BeNull();
    }

    [Fact]
    public void GeneralVideoCodec_Speed_AtMax_ShouldReturnUltrafast()
    {
        var item = VideoCodec.General.Speed(8); // MaxSpeedLevel = Presets.Length - 1 = 8
        item.Key.Should().Be("preset");
        item.Value.Should().Be("ultrafast");
    }

    [Fact]
    public void GeneralVideoCodec_Speed_AboveMax_ShouldThrow()
    {
        var act = () => VideoCodec.General.Speed(9);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void GeneralAudioCodec_ShouldHaveNullNameAndLib()
    {
        var codec = new GeneralAudioCodec();
        codec.Name.Should().BeNull();
        codec.Lib.Should().BeNull();
    }

    [Fact]
    public void AAC_ShouldHaveNameAndLib()
    {
        var codec = new AAC();
        codec.Name.Should().Be("AAC");
        codec.Lib.Should().Be("aac");
    }

    [Fact]
    public void OPUS_ShouldHaveNameAndLib()
    {
        var codec = new OPUS();
        codec.Name.Should().Be("OPUS");
        codec.Lib.Should().Be("libopus");
    }
}
