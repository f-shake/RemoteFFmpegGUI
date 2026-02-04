using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SimpleFFmpegGUI.Extensions;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SimpleFFmpegGUI.Model;

public class EFJsonConverter<T> : ValueConverter<T, string>
{
    public EFJsonConverter() : base(
        v => ConvertToJson(v),
        v => ConvertFromJson(v))
    {
    }

    private static string ConvertToJson(T value)
    {
        if (value == null)
        {
            return null;
        }
        return value.SerializeWithDefaultSettings();
    }

    private static T ConvertFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;

        try
        {
            return json.DeserializeWithDefaultSettings<T>();
        }
        catch (JsonException)
        {
            // 记录日志
            return default;
        }
    }
}