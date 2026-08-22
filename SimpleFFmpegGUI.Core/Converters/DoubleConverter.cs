using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleFFmpegGUI.Converters
{
    public class DoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 处理字符串数字（配合 AllowReadingFromString），如 MediaInfo 输出的 "Duration":"10.5"
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (str == "NaN")
                {
                    return double.NaN;
                }
                if (double.TryParse(str, out var parsed))
                {
                    return parsed;
                }
            }

            return reader.GetDouble(); // JsonException thrown if reader.TokenType != JsonTokenType.Number
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue(value);
            }
        }
    }
}