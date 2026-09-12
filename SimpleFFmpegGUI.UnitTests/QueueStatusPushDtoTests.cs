using FluentAssertions;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// 推送用瘦身状态（<see cref="QueueStatusPushDto"/>）的映射测试。
/// 关键是确认状态栏/任务页读到的字段一个都没被裁掉，以及前端会直接读的
/// <c>task.inputs</c> 永远是数组而不是 null。
/// </summary>
public class QueueStatusPushDtoTests
{
    /// <summary>一行能被 StatusDto 的进度正则识别的 ffmpeg 输出</summary>
    private const string ProgressLine =
        "frame=  100 fps=25.0 q=28.0 size=     256KiB time=00:00:04.00 bitrate= 524.3kbits/s speed=1.0x";

    [Fact]
    public void From_ProcessingStatus_ShouldKeepEveryFieldFrontendUses()
    {
        var task = new TaskEntity
        {
            Id = 7,
            Status = SimpleFFmpegGUI.Enums.TaskStatus.Processing,
            Output = "out.mp4",
            Inputs = [new InputParameters { FilePath = "in.mp4" }],
        };
        var progress = new ProgressDto
        {
            Name = "任务名",
            VideoLength = TimeSpan.FromSeconds(10),
            StartTime = DateTime.Now.AddSeconds(-1),
        };

        var dto = QueueStatusPushDto.From(new StatusDto(task, progress, ProgressLine, paused: false));

        dto.IsProcessing.Should().BeTrue();
        dto.IsPaused.Should().BeFalse();
        dto.HasDetail.Should().BeTrue();
        dto.Frame.Should().Be(100);
        dto.Fps.Should().Be(25.0);
        dto.Q.Should().Be(28.0);
        // 正则捕获组本身包含结尾的空格（"256KiB "），映射必须原样带过去（前端本来就按原样显示）
        dto.Size.Should().Be("256KIB ");
        dto.Time.Should().Be(TimeSpan.FromSeconds(4));
        dto.Bitrate.Should().Be("524.3kbits/s");
        dto.Speed.Should().Be("1.0");
        dto.LastOutput.Should().Be(ProgressLine);
        // 进度对象必须原样带过去：前端读 percent/lastTime/finishTime/name/isIndeterminate
        dto.Progress.Should().BeSameAs(progress);
        dto.Task.Id.Should().Be(7);
        dto.Task.Status.Should().Be(SimpleFFmpegGUI.Enums.TaskStatus.Processing);
        dto.Task.Output.Should().Be("out.mp4");
        dto.Task.Inputs.Should().HaveCount(1);
        dto.Task.Inputs[0].FilePath.Should().Be("in.mp4");
    }

    [Fact]
    public void From_TaskWithoutInputs_ShouldReturnEmptyListInsteadOfNull()
    {
        // 状态栏里是直接写 task.inputs.length 的，返回 null 会让定时器里的代码抛异常、快照再也不更新
        var dto = QueueStatusPushDto.From(new StatusDto(new TaskEntity { Id = 1, Inputs = null }));

        dto.Task.Inputs.Should().NotBeNull();
        dto.Task.Inputs.Should().BeEmpty();
    }

    [Fact]
    public void From_IdleStatus_ShouldBeEmpty()
    {
        var dto = QueueStatusPushDto.From(new StatusDto());

        dto.IsProcessing.Should().BeFalse();
        dto.HasDetail.Should().BeFalse();
        dto.Progress.Should().BeNull();
        dto.Task.Should().BeNull();
        // 序号由 QueueStateProvider 分配，映射本身不碰它
        dto.Seq.Should().Be(0);
    }
}
