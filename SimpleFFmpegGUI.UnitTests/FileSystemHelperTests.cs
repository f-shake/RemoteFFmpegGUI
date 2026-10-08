using FluentAssertions;
using SimpleFFmpegGUI.Helpers;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// FileSystemHelper 的序列检测与输出路径生成测试
/// </summary>
public class FileSystemHelperTests : IDisposable
{
    /// <summary>
    /// 本次用例创建的临时目录，测试结束后统一删除（否则每跑一次测试都会在 %TEMP% 留下 rfg_* 垃圾目录）
    /// </summary>
    private readonly List<string> tempDirs = [];

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rfg_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        tempDirs.Add(dir);
        return dir;
    }

    public void Dispose()
    {
        foreach (var dir in tempDirs)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch
            {
                // 清理失败不影响测试结论
            }
        }
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

    /// <summary>
    /// 存量的相对输出路径（v2 早期相对配置下写入的 "output\x.mp4"）必须绝对化：
    /// TwoPass 会把 ffmpeg 子进程的工作目录设成 2pass 临时目录，相对路径会跟着落到那里，
    /// 任务显示成功但输出不在 OutputDir 里
    /// </summary>
    [Fact]
    public void GenerateOutputPath_RelativeOutput_ShouldResolveAgainstCurrentDirectory()
    {
        var relative = Path.Combine("rfg_rel_out", "x.mp4");
        var task = new TaskEntity
        {
            Output = relative,
            Parameters = new OutputParameters(),
            Inputs = new List<InputParameters>(),
        };

        try
        {
            var path = FileSystemHelper.GenerateOutputPath(task);
            Path.IsPathFullyQualified(path).Should().BeTrue();
            path.Should().Be(Path.GetFullPath(relative));
        }
        finally
        {
            // GenerateOutputPath 会创建输出目录，清掉，别把测试输出目录搞脏
            var dir = Path.GetDirectoryName(Path.GetFullPath(relative));
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    /// <summary>
    /// 磁盘状态不变时，同一任务重复解析输出路径必须得到同一结果（解析是纯函数式的，无副作用）
    /// </summary>
    [Fact]
    public void GenerateOutputPath_RepeatedResolveWithUnchangedDisk_ShouldBeStable()
    {
        var dir = CreateTempDir();
        var target = Path.Combine(dir, "out.mp4");
        File.WriteAllText(target, "已有内容");

        var task = new TaskEntity
        {
            Output = target,
            Parameters = new OutputParameters(),
            Inputs = new List<InputParameters>(),
        };

        var first = FileSystemHelper.GenerateOutputPath(task);
        var second = FileSystemHelper.GenerateOutputPath(task);

        first.Should().Be(second);
        first.Should().Be(Path.Combine(dir, "out (2).mp4"));
    }

    /// <summary>
    /// 反向固化：若两次解析之间目标文件才出现，解析结果会变。
    /// 这正是调用方只在第一遍编码前解析一次、两遍共用 RealOutput 的原因——
    /// 若在两遍之间重新解析，第二遍会写到另一个文件上
    /// </summary>
    [Fact]
    public void GenerateOutputPath_ResolveAfterTargetAppears_ShouldChangeResult()
    {
        var dir = CreateTempDir();
        var target = Path.Combine(dir, "out.mp4");

        var task = new TaskEntity
        {
            Output = target,
            Parameters = new OutputParameters(),
            Inputs = new List<InputParameters>(),
        };

        var first = FileSystemHelper.GenerateOutputPath(task);
        first.Should().Be(target);

        File.WriteAllText(first, "第一遍写出的文件"); // 模拟第一遍编码落盘

        var second = FileSystemHelper.GenerateOutputPath(task);
        second.Should().Be(Path.Combine(dir, "out (2).mp4"));
    }

    /// <summary>
    /// 目标文件已存在时必须改名，并通过 out 参数告知调用方（据此记日志）
    /// </summary>
    [Fact]
    public void GenerateOutputPath_ExistingTarget_ShouldRenameAndReportRenamed()
    {
        var dir = CreateTempDir();
        var target = Path.Combine(dir, "out.mp4");
        File.WriteAllText(target, "原有内容");

        var task = new TaskEntity
        {
            Output = target,
            Parameters = new OutputParameters(),
            Inputs = new List<InputParameters>(),
        };

        var path = FileSystemHelper.GenerateOutputPath(task, out bool renamed);

        renamed.Should().BeTrue();
        path.Should().Be(Path.Combine(dir, "out (2).mp4"));
        File.ReadAllText(target).Should().Be("原有内容"); // 原文件不得被覆盖或删除
    }

    /// <summary>
    /// 目标文件不存在时路径必须原样返回，不得凭空改名
    /// </summary>
    [Fact]
    public void GenerateOutputPath_NotExistingTarget_ShouldNotReportRenamed()
    {
        var dir = CreateTempDir();
        var target = Path.Combine(dir, "out.mp4");

        var task = new TaskEntity
        {
            Output = target,
            Parameters = new OutputParameters(),
            Inputs = new List<InputParameters>(),
        };

        var path = FileSystemHelper.GenerateOutputPath(task, out bool renamed);

        renamed.Should().BeFalse();
        path.Should().Be(target);
    }

    [Fact]
    public void GetNonDuplicatePath_NotExisting_ShouldReturnDesiredPath()
    {
        var dir = CreateTempDir();
        var path = Path.Combine(dir, "a.mp4");

        FileSystemHelper.GetNonDuplicatePath(path).Should().Be(path);
    }

    [Fact]
    public void GetNonDuplicatePath_ExistingFile_ShouldAppendCounter()
    {
        var dir = CreateTempDir();
        var path = Path.Combine(dir, "a.mp4");
        File.WriteAllText(path, "x");

        FileSystemHelper.GetNonDuplicatePath(path).Should().Be(Path.Combine(dir, "a (2).mp4"));
    }

    [Fact]
    public void GetNonDuplicatePath_ExistingCounter_ShouldIncreaseCounter()
    {
        var dir = CreateTempDir();
        var path = Path.Combine(dir, "a.mp4");
        File.WriteAllText(path, "x");
        File.WriteAllText(Path.Combine(dir, "a (2).mp4"), "x");

        FileSystemHelper.GetNonDuplicatePath(path).Should().Be(Path.Combine(dir, "a (3).mp4"));
    }

    /// <summary>
    /// 大小写不敏感：期望路径与已占用路径仅大小写不同也算冲突（Windows 上就是同一个文件）
    /// </summary>
    [Fact]
    public void GetNonDuplicatePath_UsedPathDifferentCase_ShouldBeTreatedAsOccupied()
    {
        var dir = CreateTempDir();
        var desired = Path.Combine(dir, "A.MP4");
        var used = Path.Combine(dir, "a.mp4");

        FileSystemHelper.GetNonDuplicatePath(desired, new[] { used })
            .Should().Be(Path.Combine(dir, "A (2).MP4"));
    }

    /// <summary>
    /// 磁盘上确实存在的大小写变体（仅在大小写不敏感的文件系统上成立）
    /// </summary>
    [Fact]
    public void GetNonDuplicatePath_ExistingFileDifferentCase_ShouldBeTreatedAsOccupiedOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var dir = CreateTempDir();
        File.WriteAllText(Path.Combine(dir, "a.MP4"), "x");

        FileSystemHelper.GetNonDuplicatePath(Path.Combine(dir, "a.mp4"))
            .Should().Be(Path.Combine(dir, "a (2).mp4"));
    }

    /// <summary>
    /// 图像序列的通配路径（img%04d.jpg）在磁盘上不存在，但仍必须视为与输入冲突
    /// </summary>
    [Fact]
    public void GetNonDuplicatePath_ImageSequenceWildcard_ShouldBeTreatedAsOccupied()
    {
        var dir = CreateTempDir();
        var wildcard = Path.Combine(dir, "img%04d.jpg");

        FileSystemHelper.GetNonDuplicatePath(wildcard, new[] { wildcard })
            .Should().Be(Path.Combine(dir, "img%04d (2).jpg"));
    }

    [Fact]
    public void GetNonDuplicatePath_UsedPathNotOnDisk_ShouldAppendCounter()
    {
        var dir = CreateTempDir();
        var path = Path.Combine(dir, "a.mp4");

        FileSystemHelper.GetNonDuplicatePath(path, new[] { path })
            .Should().Be(Path.Combine(dir, "a (2).mp4"));
    }

    [Fact]
    public void GetNonDuplicatePath_NoExtension_ShouldNotAddDot()
    {
        var dir = CreateTempDir();
        var path = Path.Combine(dir, "video");
        File.WriteAllText(path, "x");

        FileSystemHelper.GetNonDuplicatePath(path).Should().Be(Path.Combine(dir, "video (2)"));
    }

    /// <summary>
    /// 多段扩展名按最后一个点切分（与 Windows 资源管理器一致）：a.tar.gz → a.tar (2).gz
    /// </summary>
    [Fact]
    public void GetNonDuplicatePath_MultipleExtensions_ShouldInsertBeforeLastExtension()
    {
        var dir = CreateTempDir();
        var path = Path.Combine(dir, "a.tar.gz");
        File.WriteAllText(path, "x");

        FileSystemHelper.GetNonDuplicatePath(path).Should().Be(Path.Combine(dir, "a.tar (2).gz"));
    }

    [Fact]
    public void GetNonDuplicatePath_NullOrEmptyUsedPaths_ShouldNotThrow()
    {
        var dir = CreateTempDir();
        var path = Path.Combine(dir, "a.mp4");

        FileSystemHelper.GetNonDuplicatePath(path, null).Should().Be(path);
        FileSystemHelper.GetNonDuplicatePath(path, new List<string>()).Should().Be(path);
    }

    /// <summary>
    /// 候选名被占满时必须抛异常，而不是退回原路径（那等于静默覆盖）
    /// </summary>
    [Fact]
    public void GetNonDuplicatePath_TooManyDuplicates_ShouldThrowInsteadOfOverwrite()
    {
        var dir = CreateTempDir();
        var path = Path.Combine(dir, "a.mp4");
        // 用集合模拟"候选名全部被占用"，避免真的建 10000 个文件
        var used = new List<string> { path };
        for (int i = 2; i < 10002; i++)
        {
            used.Add(Path.Combine(dir, $"a ({i}).mp4"));
        }

        var act = () => FileSystemHelper.GetNonDuplicatePath(path, used);

        act.Should().Throw<IOException>();
    }
}
