using FluentAssertions;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.FFmpegLib;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// AudioArgumentsGenerator 的参数逻辑测试
/// </summary>
public class AudioArgumentsGeneratorTests
{
    [Fact]
    public void Codec_Known_ShouldAddCa()
    {
        var ag = new AudioArgumentsGenerator();
        ag.Codec("AAC");
        ag.GetArguments().Should().Be("-c:a aac");
    }

    [Fact]
    public void Codec_Unknown_ShouldSetGeneral_AndAddCa()
    {
        var ag = new AudioArgumentsGenerator();
        ag.Codec("mycodec");
        ag.GetArguments().Should().Be("-c:a mycodec");
        ag.AudioCodec.Should().BeOfType<GeneralAudioCodec>();
    }

    [Fact]
    public void Codec_Auto_ShouldNotAddCa()
    {
        var ag = new AudioArgumentsGenerator();
        ag.Codec("自动");
        ag.GetArguments().Should().Be("");
    }

    [Fact]
    public void Bitrate_ShouldAddB()
    {
        var ag = new AudioArgumentsGenerator();
        ag.Codec("AAC");
        ag.Bitrate(128);
        ag.GetArguments().Should().Contain("-b:a 128K");
    }

    [Fact]
    public void SamplingRate_ShouldAddAr()
    {
        var ag = new AudioArgumentsGenerator();
        ag.Codec("AAC");
        ag.SamplingRate(48000);
        ag.GetArguments().Should().Contain("-ar 48000");
    }
}
