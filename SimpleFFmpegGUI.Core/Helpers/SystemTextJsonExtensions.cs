using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using SimpleFFmpegGUI.Converter;

namespace SimpleFFmpegGUI.Extensions;

public static class SystemTextJsonExtensions
{
    static SystemTextJsonExtensions()
    {
        defaultOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        webOptions = new JsonSerializerOptions(defaultOptions)
        {
            Converters =
            {
                new TimeSpanConverter(),
                new DoubleConverter()
            }
        };

        friendlyOptions = new JsonSerializerOptions(defaultOptions)
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = true
        };
    }

    private static JsonSerializerOptions defaultOptions;

    private static JsonSerializerOptions webOptions;

    private static JsonSerializerOptions friendlyOptions;


    extension(string json)
    {
        public T DeserializeWithDefaultSettings<T>()
        {
            return JsonSerializer.Deserialize<T>(json, defaultOptions);
        }

        public T DeserializeWithFriendlySettings<T>()
        {
            return JsonSerializer.Deserialize<T>(json, friendlyOptions);
        }

        public T DeserializeWithWebSettings<T>()
        {
            return JsonSerializer.Deserialize<T>(json, webOptions);
        }
    }

    extension<T>(T obj)
    {
        public string SerializeWithDefaultSettings()
        {
            return JsonSerializer.Serialize(obj, defaultOptions);
        }

        public string SerializeWithFriendlySettings()
        {
            return JsonSerializer.Serialize(obj, friendlyOptions);
        }

        public string SerializeWithWebSettings()
        {
            return JsonSerializer.Serialize(obj, webOptions);
        }
    }

    extension(JsonNode node)
    {
        public T DeserializeWithDefaultSettings<T>()
        {
            return node.Deserialize<T>(defaultOptions);
        }
    }
}