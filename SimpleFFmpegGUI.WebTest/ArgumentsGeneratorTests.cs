using FluentAssertions;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.WebTest;

/// <summary>
/// FFmpeg 参数生成器单元测试（Core 纯函数，不经 HTTP）
/// </summary>
public class ArgumentsGeneratorTests
{
    private static TaskEntity CreateTask(OutputParameters parameters, params string[] inputs)
    {
        return new TaskEntity
        {
            Type = TaskType.Transcode,
            Inputs = inputs.Select(p => new InputParameters { FilePath = p }).ToList(),
            Parameters = parameters,
        };
    }

    [Fact]
    public void TranscodeWithCrf_ShouldContainCrfAndCodec()
    {
        var task = CreateTask(new OutputParameters
        {
            Video = new VideoCodecParameters
            {
                Strategy = StreamStrategy.Transcode,
                Codec = "H264",
                Crf = 23,
            },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Copy },
        }, "input.mp4");

        var args = ArgumentsGenerator.GetArguments(task, 0, "output.mp4");
        args.Should().Contain("-i \"input.mp4\"");
        args.Should().Contain("libx264");
        args.Should().Contain("-crf 23");
        args.Should().Contain("\"output.mp4\" -y");
    }

    [Fact]
    public void MuxShortest_ShouldContainShortest()
    {
        var task = CreateTask(new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Copy },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Copy },
            Mux = new MuxParameters { Shortest = true },
        }, "video.mp4", "audio.mp4");
        task.Type = TaskType.Mux;

        var args = ArgumentsGenerator.GetArguments(task, 0, "muxed.mp4");
        args.Should().Contain("-shortest");
    }

    [Fact]
    public void CopyStrategy_ShouldNotContainCodecArguments()
    {
        var task = CreateTask(new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Copy },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Copy },
        }, "input.mp4");

        var args = ArgumentsGenerator.GetArguments(task, 0, "output.mp4");
        args.Should().Contain("-c:v copy");
        args.Should().Contain("-c:a copy");
        args.Should().NotContain("libx264");
    }

    [Fact]
    public void TwoPass_FirstPass_ShouldContainFormat()
    {
        var task = CreateTask(new OutputParameters
        {
            Video = new VideoCodecParameters
            {
                Strategy = StreamStrategy.Transcode,
                Codec = "H264",
                TwoPass = true,
                Crf = 20,
            },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Disable },
            Format = "mp4",
        }, "input.mp4");

        var args = ArgumentsGenerator.GetArguments(task, 1, "output.mp4");
        args.Should().Contain("-f mp4");
        args.Should().Contain("-pass 1");
    }

    [Fact]
    public void NonTwoPass_ShouldNotContainPassArgument()
    {
        // 非二次编码（执行路径传 pass=0）不应输出 -pass；TwoPass 字段为 false 时不应触发二次编码参数
        var task = CreateTask(new OutputParameters
        {
            Video = new VideoCodecParameters
            {
                Strategy = StreamStrategy.Transcode,
                Codec = "H264",
                TwoPass = false,
                Crf = 20,
            },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Disable },
            Format = "mp4",
        }, "input.mp4");

        var args = ArgumentsGenerator.GetArguments(task, 0, "output.mp4");
        args.Should().NotContain("-pass");
    }

    [Fact]
    public void ExtraParameters_ShouldBeAppended()
    {
        var task = CreateTask(new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Copy },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Copy },
            Extra = "-threads 4",
        }, "input.mp4");

        var args = ArgumentsGenerator.GetArguments(task, 0, "output.mp4");
        args.Should().Contain("-threads 4");
    }
}
