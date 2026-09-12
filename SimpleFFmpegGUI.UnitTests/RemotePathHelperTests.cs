using System.IO;
using FluentAssertions;
using SimpleFFmpegGUI.Helpers;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// 远程提交路径映射（RemotePathHelper）：本地路径 → 远端输入目录下的相对路径
/// </summary>
public class RemotePathHelperTests
{
    private static readonly string Base = Path.Combine(Path.GetTempPath(), "rfg_base");

    [Fact]
    public void Inside_SubDirectory_KeepsRelativePath()
    {
        RemotePathHelper.ToRemoteInputPath(Path.Combine(Base, "temp", "a.mp4"), Base)
            .Should().Be(@"temp\a.mp4");
    }

    [Fact]
    public void Inside_Root_ReturnsFileName()
    {
        RemotePathHelper.ToRemoteInputPath(Path.Combine(Base, "b.mp4"), Base).Should().Be("b.mp4");
    }

    [Fact]
    public void BaseWithTrailingSeparator_StillRelative()
    {
        RemotePathHelper.ToRemoteInputPath(Path.Combine(Base, "temp", "c.mp4"), Base + Path.DirectorySeparatorChar)
            .Should().Be(@"temp\c.mp4");
    }

    [Fact]
    public void BaseCaseDiffers_StillRelative()
    {
        RemotePathHelper.ToRemoteInputPath(Path.Combine(Base.ToLowerInvariant(), "Temp", "D.MP4"), Base)
            .Should().Be(@"Temp\D.MP4");
    }

    [Fact]
    public void ForwardSlashes_AreAccepted()
    {
        RemotePathHelper.ToRemoteInputPath(Base.Replace('\\', '/') + "/temp/e.mp4", Base)
            .Should().Be(@"temp\e.mp4");
    }

    [Fact]
    public void ChildDirStartingWithDots_IsInside_KeepsDotsInName()
    {
        // 名字以两个点开头的合法子目录（如 ..backup）：它确实在基准目录内，应保留相对路径。
        // 若用 StartsWith("..") 判逃逸会误判，静默退化成文件名，可能让远端处理根目录下的同名文件
        RemotePathHelper.ToRemoteInputPath(Path.Combine(Base, "..backup", "a.mp4"), Base)
            .Should().Be(@"..backup\a.mp4");
    }

    [Theory]
    [InlineData(@"D:\elsewhere\x.mp4", "x.mp4")]        // 跨盘符
    [InlineData(@"C:\other\y.mp4", "y.mp4")]            // 同盘但不在基准目录下
    [InlineData(@"..\escape\z.mp4", "z.mp4")]           // 相对形态的逃逸
    public void Outside_ReturnsFileName(string localPath, string expected)
    {
        RemotePathHelper.ToRemoteInputPath(localPath, Base).Should().Be(expected);
    }

    [Fact]
    public void SiblingDirOfBase_IsOutside()
    {
        // 与基准目录"前缀相同"的兄弟目录（rfg_base2）不能被当成基准目录内
        RemotePathHelper.ToRemoteInputPath(Base + "2\\f.mp4", Base).Should().Be("f.mp4");
    }

    [Fact]
    public void UnknownBase_ReturnsFileName()
    {
        RemotePathHelper.ToRemoteInputPath(@"D:\x\h.mp4", null).Should().Be("h.mp4");
        RemotePathHelper.ToRemoteInputPath(@"D:\x\h.mp4", "").Should().Be("h.mp4");
    }

    [Fact]
    public void EmptyLocalPath_ReturnsEmpty()
    {
        RemotePathHelper.ToRemoteInputPath("", Base).Should().BeEmpty();
        RemotePathHelper.ToRemoteInputPath(null, Base).Should().BeEmpty();
    }
}
