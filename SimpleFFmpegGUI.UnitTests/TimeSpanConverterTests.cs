using System.Text;
using System.Text.Json;
using FluentAssertions;
using SimpleFFmpegGUI.Converters;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// TimeSpanConverter 的读写逻辑测试
/// </summary>
public class TimeSpanConverterTests
{
    private static TimeSpan ReadTimeSpan(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        return new TimeSpanConverter().Read(ref reader, typeof(TimeSpan), new JsonSerializerOptions());
    }

    private static string WriteTimeSpan(TimeSpan value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            new TimeSpanConverter().Write(writer, value, new JsonSerializerOptions());
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [Fact]
    public void Read_PositiveNumber_ShouldReturnSeconds()
    {
        ReadTimeSpan("5").Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Read_String_ShouldTryParse()
    {
        ReadTimeSpan("\"00:00:05\"").Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Read_Zero_ShouldReturnZero()
    {
        ReadTimeSpan("0").Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Read_Negative_ShouldReturnZero()
    {
        ReadTimeSpan("-5").Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Read_InvalidString_ShouldReturnZero()
    {
        ReadTimeSpan("\"abc\"").Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Write_ShouldReturnTotalSeconds()
    {
        WriteTimeSpan(TimeSpan.FromSeconds(5)).Should().Be("5");
    }
}
