using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleFFmpegGUI.Converters
{
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.GetDouble() > 0)
            {
                return TimeSpan.FromSeconds(reader.GetDouble());
            }
            if (reader.TokenType == JsonTokenType.String && TimeSpan.TryParse(reader.GetString(), out TimeSpan ts))
            {
                return ts;
            }
            return TimeSpan.Zero;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.TotalSeconds);
        }
    }
}