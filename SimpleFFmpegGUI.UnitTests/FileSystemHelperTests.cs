using FluentAssertions;
using SimpleFFmpegGUI.Helpers;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// FileSystemHelper 的序列检测与输出路径生成测试
/// </summary>
public class FileSystemHelperTests
{
    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rfg_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void GetSequence_ZeroPadded_ShouldReturnPercentWidth()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "img0001.jpg"), "a");
        File.WriteAllText(Path.Combine(dir, "img0002.jpg"), "a");

        var result = FileSystemHelper.GetSequence(Path.Combine(dir, "img0001.jpg"));
        result.Should().Be(Path.Combine(dir, "img%04d.jpg"));
    }

    [Fact]
    public void GetSequence_NonPadded_ShouldReturnPercentD()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "img1.jpg"), "a");
        File.WriteAllText(Path.Combine(dir, "img2.jpg"), "a");

        var result = FileSystemHelper.GetSequence(Path.Combine(dir, "img1.jpg"));
        result.Should().Be(Path.Combine(dir, "img%d.jpg"));
    }

    [Fact]
    public void GetSequence_NoSequence_ShouldReturnNull()
    {
        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "random.jpg"), "a");

        FileSystemHelper.GetSequence(Path.Combine(dir, "random.jpg")).Should().BeNull();
    }

    [Fact]
    public void GenerateOutputPath_EmptyOutputNoInputs_ShouldThrow()
    {
        var task = new TaskEntity
        {
            Output = "",
            Parameters = new OutputParameters(),
            Inputs = new List<InputParameters>(),
        };
        var act = () => FileSystemHelper.GenerateOutputPath(task);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void GenerateOutputPath_Format_ShouldChangeExtension()
    {
        var dir = CreateTempDir();
        var task = new TaskEntity
        {
            Output = Path.Combine(dir, "out.mp3"),
            Parameters = new OutputParameters { Format = "mp4" },
            Inputs = new List<InputParameters>(),
        };
        var path = FileSystemHelper.GenerateOutputPath(task);
        path.Should().EndWith(".mp4");
        Path.GetFileNameWithoutExtension(path).Should().StartWith("out");
    }

    [Fact]
    public void GenerateOutputPath_InvalidChars_ShouldBeRemoved()
    {
        var dir = CreateTempDir();
        var task = new TaskEntity
        {
            Output = Path.Combine(dir, "a<b>.mp4"),
            Parameters = new OutputParameters(),
            Inputs = new List<InputParameters>(),
        };
        var path = FileSystemHelper.GenerateOutputPath(task);
        path.Should().NotContain("<");
        path.Should().NotContain(">");
    }

    [Fact]
    public void GenerateOutputPath_UniqueNaming_ShouldAvoidInputCollision()
    {
        var dir = CreateTempDir();
        var input = Path.Combine(dir, "out.mp4");
        var task = new TaskEntity
        {
            Output = Path.Combine(dir, "out.mp4"),
            Parameters = new OutputParameters { Format = "mp4" },
            Inputs = new List<InputParameters> { new() { FilePath = input } },
        };
        var path = FileSystemHelper.GenerateOutputPath(task);
        path.Should().NotBe(input);
        Path.GetFileNameWithoutExtension(path).Should().StartWith("out");
    }

    [Fact]
    public void GenerateOutputPath_EmptyOutput_ShouldFallBackToInputBase()
    {
        var dir = CreateTempDir();
        var input = Path.Combine(dir, "in.mp4");
        var task = new TaskEntity
        {
            Output = "",
            Parameters = new OutputParameters(),
            Inputs = new List<InputParameters> { new() { FilePath = input } },
        };
        var path = FileSystemHelper.GenerateOutputPath(task);
        Path.GetFileNameWithoutExtension(path).Should().StartWith("in");
    }
}
