using System.Net;
using FluentAssertions;
using FzLib.Web;
using Microsoft.Extensions.Options;
using SimpleFFmpegGUI.Configurations;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Helpers;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// FilePathHelper 的路径安全校验逻辑测试
/// </summary>
public class FilePathHelperTests
{
    /// <summary>
    /// 测试用 IOptionsSnapshot：避免依赖完整 Options 管线
    /// </summary>
    private sealed class FakeOptions<T>(T value) : IOptionsSnapshot<T> where T : class
    {
        public T Value { get; } = value;
        public T Get(string name) => Value;
    }

    private static AppSettings CreateSettings(string inputDir, string outputDir) =>
        new()
        {
            InputDir = inputDir,
            OutputDir = outputDir,
        };

    private static string TempDir() => Path.Combine(Path.GetTempPath(), "rfg_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Constructor_InputDirNull_ShouldThrow500()
    {
        var settings = CreateSettings(null, TempDir());
        var act = () => new FilePathHelper(new FakeOptions<AppSettings>(settings));
        act.Should().Throw<HttpStatusCodeException>()
            .Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void Constructor_OutputDirNull_ShouldThrow500()
    {
        var settings = CreateSettings(TempDir(), null);
        var act = () => new FilePathHelper(new FakeOptions<AppSettings>(settings));
        act.Should().Throw<HttpStatusCodeException>()
            .Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void GetFullPath_Absolute_NotAllowed_ShouldThrow400()
    {
        var dir = TempDir();
        var helper = new FilePathHelper(new FakeOptions<AppSettings>(CreateSettings(dir, dir)));
        var act = () => helper.GetFullPath(RootDirType.InputDir, Path.Combine(dir, "x.mp4"));
        act.Should().Throw<HttpStatusCodeException>()
            .Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void GetFullPath_Absolute_Allowed_WithinRoot_ShouldNormalize()
    {
        var dir = TempDir();
        var helper = new FilePathHelper(new FakeOptions<AppSettings>(CreateSettings(dir, dir)));
        var abs = Path.Combine(dir, "x.mp4");
        helper.GetFullPath(RootDirType.InputDir, abs, true).Should().Be(Path.GetFullPath(abs));
    }

    [Fact]
    public void GetFullPath_Relative_ShouldCombineWithRoot()
    {
        var dir = TempDir();
        var helper = new FilePathHelper(new FakeOptions<AppSettings>(CreateSettings(dir, dir)));
        var full = helper.GetFullPath(RootDirType.InputDir, "sub\\a.mp4");
        Path.IsPathFullyQualified(full).Should().BeTrue();
        full.Should().StartWith(Path.GetFullPath(dir));
        full.Should().EndWith("sub\\a.mp4");
    }

    [Fact]
    public void GetFullPath_ParentTraversal_ShouldThrow400()
    {
        var dir = TempDir();
        var helper = new FilePathHelper(new FakeOptions<AppSettings>(CreateSettings(dir, dir)));
        var act = () => helper.GetFullPath(RootDirType.InputDir, "..\\escape.mp4");
        act.Should().Throw<HttpStatusCodeException>()
            .Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void GetFullPath_OutputDir_UsesOutputRoot()
    {
        var input = TempDir();
        var output = TempDir();
        var helper = new FilePathHelper(new FakeOptions<AppSettings>(CreateSettings(input, output)));
        var full = helper.GetFullPath(RootDirType.OutputDir, "out.mp4");
        full.Should().StartWith(Path.GetFullPath(output));
    }
}
