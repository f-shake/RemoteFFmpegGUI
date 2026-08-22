using FluentAssertions;
using SimpleFFmpegGUI.Compatibility;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// PresetConverter 的 v1→v2 预设转换测试
/// </summary>
public class PresetConverterTests
{
    [Fact]
    public void ConvertJson_EmptyString_ShouldReturnNull()
    {
        PresetConverter.ConvertJson("").Should().BeNull();
    }

    [Fact]
    public void ConvertJson_NoArguments_ShouldReturnNull()
    {
        const string json = """[{"Name":"p","Type":0,"Default":false}]""";
        PresetConverter.ConvertJson(json).Should().BeNull();
    }

    [Fact]
    public void ConvertJson_WithArguments_ShouldConvert()
    {
        const string json = """[{"Name":"p","Type":0,"Default":false,"Arguments":{"Extra":"-threads 4","Format":"mp4"}}]""";
        var presets = PresetConverter.ConvertJson(json);

        presets.Should().HaveCount(1);
        presets[0].Name.Should().Be("p");
        presets[0].Type.Should().Be(TaskType.Transcode);
        presets[0].Parameters.Extra.Should().Be("-threads 4");
        presets[0].Parameters.Format.Should().Be("mp4");
    }

    [Fact]
    public void ConvertJson_Type3_ShouldConvertToCustom()
    {
        const string json = """[{"Name":"v1_custom","Type":3,"Default":false,"Arguments":{}}]""";
        var presets = PresetConverter.ConvertJson(json);
        presets[0].Type.Should().Be(TaskType.Custom); // v2 中 Custom = 99
    }

    [Fact]
    public void ConvertFromV1_1_VideoTranscode_MaxBitrateBufferDefaults()
    {
        var old = new OldOutputArgumentsDto
        {
            Video = new OldVideoCodeArgumentsDto { Code = "H264", Crf = 23, MaxBitrate = null },
            DisableAudio = true,
            Format = "mp4",
        };
        var result = PresetConverter.ConvertFromV1_1(old);

        result.Video.Strategy.Should().Be(StreamStrategy.Transcode);
        result.Video.Codec.Should().Be("H264");
        result.Video.Crf.Should().Be(23);
        result.Video.MaxBitrateBuffer.Should().Be(2.0); // MaxBitrateBuffer ?? 2.0
        result.Audio.Strategy.Should().Be(StreamStrategy.Disable);
    }

    [Fact]
    public void ConvertFromV1_1_VideoCopy_WhenNoVideoAndNotDisabled()
    {
        var old = new OldOutputArgumentsDto { DisableVideo = false, DisableAudio = false };
        var result = PresetConverter.ConvertFromV1_1(old);

        result.Video.Strategy.Should().Be(StreamStrategy.Copy);
        result.Audio.Strategy.Should().Be(StreamStrategy.Copy);
    }

    [Fact]
    public void ConvertFromV1_1_AudioTranscode()
    {
        var old = new OldOutputArgumentsDto
        {
            Audio = new OldAudioCodeArgumentsDto { Code = "AAC", Bitrate = 128, SamplingRate = 48000 },
        };
        var result = PresetConverter.ConvertFromV1_1(old);

        result.Audio.Strategy.Should().Be(StreamStrategy.Transcode);
        result.Audio.Codec.Should().Be("AAC");
        result.Audio.Bitrate.Should().Be(128);
        result.Audio.SamplingRate.Should().Be(48000);
    }

    [Fact]
    public void ConvertFromV1_1_VideoDisabled_ShouldDisableVideo()
    {
        var old = new OldOutputArgumentsDto { DisableVideo = true, DisableAudio = false };
        var result = PresetConverter.ConvertFromV1_1(old);

        result.Video.Strategy.Should().Be(StreamStrategy.Disable);
        result.Audio.Strategy.Should().Be(StreamStrategy.Copy);
    }

    [Fact]
    public void ConvertFromV1_1_StreamMaps_ShouldBeCarriedOver()
    {
        var old = new OldOutputArgumentsDto
        {
            Stream = new OldStreamArgumentsDto
            {
                Maps = new List<OldStreamMapInfoDto>
                {
                    new() { InputIndex = 0, Channel = StreamChannel.Video, StreamIndex = null },
                }
            },
        };
        var result = PresetConverter.ConvertFromV1_1(old);

        result.Stream.Maps.Should().ContainSingle();
        result.Stream.Maps[0].InputIndex.Should().Be(0);
        result.Stream.Maps[0].Channel.Should().Be(StreamChannel.Video);
    }
}
