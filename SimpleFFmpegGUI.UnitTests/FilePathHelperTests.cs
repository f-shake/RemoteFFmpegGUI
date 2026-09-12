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

    /// <summary>
    /// 配置写相对路径时（出厂示例值 "input"/"output"），对外暴露的目录也必须解析成绝对路径：
    /// File/Dirs 返回它给前端做前缀匹配，砍掉前缀后任务列表才显示相对路径；返回配置原值的话
    /// 前端永远匹配不上，只能显示完整绝对路径。原始配置值保持原样，供文件列表等按原形式拼接的调用方使用。
    /// </summary>
    [Fact]
    public void DirFullPath_RelativeConfig_ShouldResolveToAbsolute()
    {
        var helper = new FilePathHelper(new FakeOptions<AppSettings>(CreateSettings("input", "output")));

        Path.IsPathFullyQualified(helper.InputDirFullPath).Should().BeTrue();
        Path.IsPathFullyQualified(helper.OutputDirFullPath).Should().BeTrue();
        helper.InputDirFullPath.Should().Be(Path.GetFullPath("input"));
        helper.OutputDirFullPath.Should().Be(Path.GetFullPath("output"));
        helper.InputDir.Should().Be("input");
        helper.OutputDir.Should().Be("output");
    }

    /// <summary>
    /// 配置写绝对路径时（如部署在 NAS 的 C:\共享\待处理），解析结果原样保留
    /// </summary>
    [Fact]
    public void DirFullPath_AbsoluteConfig_ShouldKeepAbsolute()
    {
        var input = TempDir();
        var output = TempDir();
        var helper = new FilePathHelper(new FakeOptions<AppSettings>(CreateSettings(input, output)));

        helper.InputDirFullPath.Should().Be(Path.GetFullPath(input));
        helper.OutputDirFullPath.Should().Be(Path.GetFullPath(output));
    }
}
