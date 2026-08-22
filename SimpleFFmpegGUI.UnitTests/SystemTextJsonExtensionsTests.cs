using FluentAssertions;
using SimpleFFmpegGUI.Extensions;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// SystemTextJsonExtensions 的序列化选项与扩展测试
/// </summary>
public class SystemTextJsonExtensionsTests
{
    private class PlainDto
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    private class TimeSpanDto
    {
        public TimeSpan Duration { get; set; }
        public double Ratio { get; set; }
    }

    [Fact]
    public void Default_ShouldRoundtrip_AndKeepChinese()
    {
        var obj = new PlainDto { Name = "测试", Count = 3 };
        var json = obj.SerializeWithDefaultSettings();
        json.Should().Contain("测试"); // UnsafeRelaxedJsonEscaping 保留中文

        var back = json.DeserializeWithDefaultSettings<PlainDto>();
        back.Name.Should().Be("测试");
        back.Count.Should().Be(3);
    }

    [Fact]
    public void Web_ShouldUseTimeSpanConverter()
    {
        var obj = new TimeSpanDto { Duration = TimeSpan.FromSeconds(5), Ratio = 1.5 };
        var json = obj.SerializeWithWebSettings();
        // TimeSpanConverter 将 5 秒序列化为数字 5，而非 ISO 字符串
        json.Should().Contain("\"Duration\":5");

        var back = json.DeserializeWithWebSettings<TimeSpanDto>();
        back.Duration.Should().Be(TimeSpan.FromSeconds(5));
        back.Ratio.Should().Be(1.5);
    }

    [Fact]
    public void Friendly_ShouldBeIndented()
    {
        var obj = new PlainDto { Name = "友好", Count = 7 };
        var json = obj.SerializeWithFriendlySettings();
        json.Should().Contain("\n");

        var back = json.DeserializeWithFriendlySettings<PlainDto>();
        back.Name.Should().Be("友好");
        back.Count.Should().Be(7);
    }

    [Fact]
    public void StringExtension_ShouldDeserializeWithWebSettings()
    {
        const string json = """{"Duration":5}""";
        var back = json.DeserializeWithWebSettings<TimeSpanDto>();
        back.Duration.Should().Be(TimeSpan.FromSeconds(5));
    }
}
