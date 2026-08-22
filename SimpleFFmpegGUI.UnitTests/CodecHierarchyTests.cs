using FluentAssertions;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.FFmpegLib;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// 软件/硬件编码器的 CRF/CQ 差异测试
/// </summary>
public class CodecHierarchyTests
{
    [Fact]
    public void SoftwareVideoCodec_CrfLabel_ShouldBeCRF()
    {
        VideoCodec.X264.CrfLabel.Should().Be("CRF");
    }

    [Fact]
    public void SoftwareVideoCodec_Crf_WithinRange_ShouldAddCrf()
    {
        var item = VideoCodec.X264.CRF(51); // MaxCRF
        item.Key.Should().Be("crf");
        item.Value.Should().Be("51");
    }

    [Fact]
    public void SoftwareVideoCodec_Crf_AboveMax_ShouldThrow()
    {
        var act = () => VideoCodec.X264.CRF(52);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void SoftwareVideoCodec_Crf_Negative_ShouldThrow()
    {
        var act = () => VideoCodec.X264.CRF(-1);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void HardwareVideoCodec_CrfLabel_ShouldBeCQ()
    {
        VideoCodec.N_H264.CrfLabel.Should().Be("CQ");
    }

    [Fact]
    public void HardwareVideoCodec_Crf_ShouldAddCq()
    {
        var item = VideoCodec.N_H264.CRF(20);
        item.Key.Should().Be("cq");
        item.Value.Should().Be("20");
    }

    [Fact]
    public void HardwareVideoCodec_Crf_AboveMax_ShouldThrow()
    {
        var act = () => VideoCodec.N_H264.CRF(52); // MaxCRF 51
        act.Should().Throw<FFmpegArgumentException>();
    }
}
