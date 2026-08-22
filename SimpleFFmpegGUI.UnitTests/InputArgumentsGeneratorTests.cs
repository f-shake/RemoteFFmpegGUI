using FluentAssertions;
using SimpleFFmpegGUI.FFmpegArgument;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// InputArgumentsGenerator 的参数逻辑测试
/// </summary>
public class InputArgumentsGeneratorTests
{
    [Fact]
    public void Duration_ShouldFormatSecondsToThreeDecimals()
    {
        var ig = new InputArgumentsGenerator();
        ig.Duration(TimeSpan.FromSeconds(10.5));
        ig.GetArguments().Should().Be("-t 10.500");
    }

    [Fact]
    public void Seek_ShouldFormatSecondsToThreeDecimals()
    {
        var ig = new InputArgumentsGenerator();
        ig.Seek(TimeSpan.FromSeconds(5));
        ig.GetArguments().Should().Be("-ss 5.000");
    }

    [Fact]
    public void To_ShouldFormatSecondsToThreeDecimals()
    {
        var ig = new InputArgumentsGenerator();
        ig.To(TimeSpan.FromSeconds(2));
        ig.GetArguments().Should().Be("-to 2.000");
    }

    [Fact]
    public void Format_ShouldAddF()
    {
        var ig = new InputArgumentsGenerator();
        ig.Format("mp4");
        ig.GetArguments().Should().Be("-f mp4");
    }

    [Fact]
    public void Framerate_NaN_ShouldBeIgnored()
    {
        var ig = new InputArgumentsGenerator();
        ig.Framerate(double.NaN);
        ig.GetArguments().Should().Be("");
    }

    [Fact]
    public void NullValues_ShouldBeIgnored()
    {
        var ig = new InputArgumentsGenerator();
        ig.Duration(null);
        ig.Seek(null);
        ig.To(null);
        ig.Format(null);
        ig.Framerate(null);
        ig.Input(null);
        ig.GetArguments().Should().Be("");
    }
}
