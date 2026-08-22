using FluentAssertions;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.FFmpegArgument;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// StreamArgumentsGenerator 的流映射逻辑测试
/// </summary>
public class StreamArgumentsGeneratorTests
{
    [Fact]
    public void Map_Video_WithoutIndex_ShouldOnlyUseChannel()
    {
        var sg = new StreamArgumentsGenerator();
        sg.Map(0, StreamChannel.Video, null);
        sg.GetArguments().Should().Be("-map 0:v");
    }

    [Fact]
    public void Map_Audio_WithIndex_ShouldAppendIndex()
    {
        var sg = new StreamArgumentsGenerator();
        sg.Map(1, StreamChannel.Audio, 2);
        sg.GetArguments().Should().Be("-map 1:a:2");
    }

    [Fact]
    public void Map_All_ShouldOmitChannel_KeepIndex()
    {
        var sg = new StreamArgumentsGenerator();
        sg.Map(0, StreamChannel.All, 3);
        sg.GetArguments().Should().Be("-map 0:3");
    }

    [Fact]
    public void Map_UnknownChannel_ShouldThrow()
    {
        var sg = new StreamArgumentsGenerator();
        var act = () => sg.Map(0, (StreamChannel)16, null);
        act.Should().Throw<NotImplementedException>();
    }
}
