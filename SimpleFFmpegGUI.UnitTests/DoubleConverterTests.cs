using System.Text;
using System.Text.Json;
using FluentAssertions;
using SimpleFFmpegGUI.Converters;

namespace SimpleFFmpegGUI.UnitTests;

/// <summary>
/// DoubleConverter 的读写逻辑测试（含 NaN 双 token bug 回归）
/// </summary>
public class DoubleConverterTests
{
    private static double ReadDouble(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        reader.Read();
        return new DoubleConverter().Read(ref reader, typeof(double), new JsonSerializerOptions());
    }

    private static string WriteDouble(double value)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            new DoubleConverter().Write(writer, value, new JsonSerializerOptions());
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [Fact]
    public void Read_StringNaN_ShouldReturnNaN()
    {
        ReadDouble("\"NaN\"").Should().Be(double.NaN);
    }

    [Fact]
    public void Read_Number_ShouldReturnValue()
    {
        ReadDouble("5.5").Should().Be(5.5);
    }

    [Fact]
    public void Write_NaN_ShouldWriteSingleNull()
    {
        // fix：NaN 只应写一个 null，而非继续 WriteNumberValue(NaN) 造成双 token
        WriteDouble(double.NaN).Should().Be("null");
    }

    [Fact]
    public void Write_Infinity_ShouldWriteNull()
    {
        WriteDouble(double.PositiveInfinity).Should().Be("null");
        WriteDouble(double.NegativeInfinity).Should().Be("null");
    }

    [Fact]
    public void Write_OrdinaryValue_ShouldWriteNumber()
    {
        WriteDouble(5.5).Should().Be("5.5");
    }
}
