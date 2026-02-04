using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SimpleFFmpegGUI.Extensions;

public static class SystemTextJsonExtensions
{
    private static JsonSerializerOptions defaultOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        //PropertyNamingPolicy = null,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        //NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };
    private static JsonSerializerOptions friendlyOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        //PropertyNamingPolicy = null,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };


    public static T DeserializeWithDefaultSettings<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, defaultOptions);
    }

    public static T DeserializeWithFriendlySettings<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, friendlyOptions);
    }

    public static string SerializeWithDefaultSettings<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj, defaultOptions);
    }

    public static string SerializeWithFriendlySettings<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj, friendlyOptions);
    }
}
