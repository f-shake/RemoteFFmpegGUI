using FluentAssertions;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// FFmpeg 参数生成器单元测试（Core 纯函数）
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
        // 非二次编码（pass=0）不应输出 -pass；TwoPass 字段为 false 时不应触发二次编码参数
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

    [Fact]
    public void GetArguments_OutputNull_FallsBackToRealOutput()
    {
        var task = CreateTask(new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Copy },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Disable },
        }, "input.mp4");
        task.RealOutput = "real.mp4";

        var args = ArgumentsGenerator.GetArguments(task, 0);
        args.Should().Contain("\"real.mp4\" -y");
    }

    [Fact]
    public void GetArguments_MultipleInputs_ShouldIncludeAllInputs()
    {
        var task = CreateTask(new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Copy },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Copy },
        }, "a.mp4", "b.mp4");

        var args = ArgumentsGenerator.GetArguments(task, 0, "out.mp4");
        args.Should().Contain("-i \"a.mp4\"");
        args.Should().Contain("-i \"b.mp4\"");
        args.Should().Contain("\"out.mp4\" -y");
    }

    [Fact]
    public void GetInputArguments_Extra_ShouldConcatenateWithTwoSpaces()
    {
        var input = new InputParameters { FilePath = "input.mp4", Extra = "-threads 4" };
        var args = ArgumentsGenerator.GetInputArguments(input);
        args.Should().Be("-threads 4  -i \"input.mp4\"");
    }

    [Fact]
    public void GetOutputArguments_VideoDisable_ShouldContainVn()
    {
        var p = new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Disable },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Copy },
        };
        var args = ArgumentsGenerator.GetOutputArguments(p, 0);
        args.Should().Contain("-vn");
        args.Should().Contain("-c:a copy");
    }

    [Fact]
    public void GetOutputArguments_AudioDisable_ShouldContainAn()
    {
        var p = new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Copy },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Disable },
        };
        var args = ArgumentsGenerator.GetOutputArguments(p, 0);
        args.Should().Contain("-an");
        args.Should().Contain("-c:v copy");
    }

    [Fact]
    public void GetOutputArguments_TranscodeBitrate_ShouldContainMaxAndBuffer()
    {
        var p = new OutputParameters
        {
            Video = new VideoCodecParameters
            {
                Strategy = StreamStrategy.Transcode,
                Codec = "H264",
                AverageBitrate = 100,
                MaxBitrate = 200,
                MaxBitrateBuffer = 2.0,
            },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Disable },
        };
        var args = ArgumentsGenerator.GetOutputArguments(p, 0);
        args.Should().Contain("-c:v libx264");
        args.Should().Contain("-b:v 100M");
        args.Should().Contain("-maxrate 200M");
        args.Should().Contain("-bufsize 400M"); // 2.0 × 200
    }

    [Fact]
    public void GetOutputArguments_AspectPixelFpsScale_ShouldContain()
    {
        var p = new OutputParameters
        {
            Video = new VideoCodecParameters
            {
                Strategy = StreamStrategy.Transcode,
                Codec = "H264",
                AspectRatio = "16:9",
                PixelFormat = "yuv420p",
                Fps = 30,
                Size = "1920x1080",
            },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Disable },
        };
        var args = ArgumentsGenerator.GetOutputArguments(p, 0);
        args.Should().Contain("-aspect 16:9");
        args.Should().Contain("-pix_fmt yuv420p");
        args.Should().Contain("-r 30");
        args.Should().Contain("-vf scale=1920x1080");
    }

    [Fact]
    public void GetOutputArguments_Speed_ShouldContainPreset()
    {
        var p = new OutputParameters
        {
            Video = new VideoCodecParameters
            {
                Strategy = StreamStrategy.Transcode,
                Codec = "H264",
                Preset = 3,
            },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Disable },
        };
        var args = ArgumentsGenerator.GetOutputArguments(p, 0);
        args.Should().Contain("-preset medium");
    }

    [Fact]
    public void GetOutputArguments_StreamMaps_ShouldContainMap()
    {
        var p = new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Copy },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Copy },
            Stream = new StreamParameters
            {
                Maps =
                {
                    new StreamMapParameters { InputIndex = 0, Channel = StreamChannel.Video },
                    new StreamMapParameters { InputIndex = 1, Channel = StreamChannel.Audio, StreamIndex = 2 },
                }
            },
        };
        var args = ArgumentsGenerator.GetOutputArguments(p, 0);
        args.Should().Contain("-map 0:v");
        args.Should().Contain("-map 1:a:2");
    }

    [Fact]
    public void CheckOutputArguments_DoubleDisable_ShouldThrow()
    {
        var p = new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Disable },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Disable },
        };
        var act = () => ArgumentsGenerator.GetOutputArguments(p, 0);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void CheckOutputArguments_TwoPassWithBlankFormat_ShouldThrow()
    {
        var p = new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Transcode, TwoPass = true },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Disable },
            Format = null,
        };
        var act = () => ArgumentsGenerator.GetOutputArguments(p, 0);
        act.Should().Throw<FFmpegArgumentException>();
    }

    [Fact]
    public void Pass1WithoutFormat_ShouldNotWriteBareDashF()
    {
        // pass==1 且 Format 为空：TwoPass=false 不触发 CheckOutputArguments 抛异常，
        // 但旧代码会生成裸 "-f "，此处断言修复后不会出现（fix ④）
        var p = new OutputParameters
        {
            Video = new VideoCodecParameters { Strategy = StreamStrategy.Transcode, Codec = "H264", TwoPass = false },
            Audio = new AudioCodecParameters { Strategy = StreamStrategy.Disable },
            Format = null,
        };
        var args = ArgumentsGenerator.GetOutputArguments(p, 1);
        args.Should().Contain("-pass 1");
        args.Should().NotContain("-f");
    }
}
