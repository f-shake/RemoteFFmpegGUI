using FluentAssertions;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Models.MediaInfo;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// MediaInfo 各轨道类与 JSON 属性名映射、Duration 计算测试
/// </summary>
public class MediaInfoJsonTests
{
    [Fact]
    public void MediaInfoGeneral_MapsJsonPropertyNames_AndComputesDuration()
    {
        const string json = """{"CodecID":"avc1","Duration":"10.5","FileSize":123,"Format":"MPEG-4","OverallBitRate":1000000}""";
        var general = json.DeserializeWithWebSettings<MediaInfoGeneral>();

        general.CodecId.Should().Be("avc1");
        general.DurationSeconds.Should().Be(10.5); // AllowReadingFromString 解析字符串数字
        general.Duration.Should().Be(TimeSpan.FromSeconds(10.5));
        general.FileSize.Should().Be(123);
        general.Format.Should().Be("MPEG-4");
    }

    [Fact]
    public void MediaInfoVideo_MapsSpecificProperties()
    {
        const string json = """{"Width":1920,"Height":1080,"FrameRate":"29.97","BitRate_Maximum":5000,"Format_Profile":"High"}""";
        var video = json.DeserializeWithWebSettings<MediaInfoVideo>();

        video.Width.Should().Be(1920);
        video.Height.Should().Be(1080);
        video.FrameRate.Should().Be(29.97);
        video.BitRateMaximum.Should().Be(5000);
        video.FormatProfile.Should().Be("High");
    }

    [Fact]
    public void MediaInfoAudio_MapsSpecificProperties()
    {
        const string json = """{"Channels":2,"SamplingRate":48000,"BitRate_Mode":"CBR"}""";
        var audio = json.DeserializeWithWebSettings<MediaInfoAudio>();

        audio.Channels.Should().Be(2);
        audio.SamplingRate.Should().Be(48000);
        audio.BitRateMode.Should().Be("CBR");
    }

    [Fact]
    public void MediaInfoText_MapsUniqueId()
    {
        const string json = """{"Language":"chi","UniqueID":100}""";
        var text = json.DeserializeWithWebSettings<MediaInfoText>();

        text.Language.Should().Be("chi");
        text.UniqueId.Should().Be(100);
    }

    [Fact]
    public void MediaInfoImage_MapsProperties()
    {
        const string json = """{"Width":800,"Height":600,"Compression_Mode":"Lossy"}""";
        var image = json.DeserializeWithWebSettings<MediaInfoImage>();

        image.Width.Should().Be(800);
        image.Height.Should().Be(600);
        image.CompressionMode.Should().Be("Lossy");
    }
}
