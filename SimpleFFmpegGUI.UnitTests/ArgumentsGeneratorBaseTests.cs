using FluentAssertions;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.FFmpegLib;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// ArgumentsGeneratorBase 的扁平化/分组逻辑测试
/// </summary>
public class ArgumentsGeneratorBaseTests
{
    /// <summary>
    /// 测试用生成器：暴露 protected 的 arguments 集合
    /// </summary>
    private class TestGenerator : ArgumentsGeneratorBase
    {
        public void Add(FFmpegArgumentItem item) => arguments.Add(item);
    }

    [Fact]
    public void ParentlessItems_ShouldJoinWithSpace()
    {
        var g = new TestGenerator();
        g.Add(new FFmpegArgumentItem("c:v", "libx264"));
        g.Add(new FFmpegArgumentItem("crf", "23"));
        g.GetArguments().Should().Be("-c:v libx264 -crf 23");
    }

    [Fact]
    public void ParentItems_ShouldGroupAndJoinWithSeparator()
    {
        var g = new TestGenerator();
        g.Add(new FFmpegArgumentItem("pass", "1", "x265-params", ':'));
        g.Add(new FFmpegArgumentItem("crf", "28", "x265-params", ':'));
        g.GetArguments().Should().Be("-x265-params pass=1:crf=28");
    }

    [Fact]
    public void NullItems_ShouldBeFiltered()
    {
        var g = new TestGenerator();
        g.Add(null);
        g.Add(new FFmpegArgumentItem("i", "\"a.mp4\""));
        g.GetArguments().Should().Be("-i \"a.mp4\"");
    }

    [Fact]
    public void Other_ShouldBeFlattened()
    {
        var g = new TestGenerator();
        // SVTAV1 风格：rc 通过 Other 串联 tbr，二者同属父参数 svtav1-params
        var rc = new FFmpegArgumentItem("rc", "1", "svtav1-params", ':')
        {
            Other = new FFmpegArgumentItem("tbr", "1000", "svtav1-params", ':')
        };
        g.Add(rc);
        g.GetArguments().Should().Be("-svtav1-params rc=1:tbr=1000");
    }
}
