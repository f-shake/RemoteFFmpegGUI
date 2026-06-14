using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.Compatibility;

/// <summary>
/// 预设兼容转换器 — 将旧版（master 分支）预设格式转换为当前版本
/// </summary>
public static class PresetConverter
{
    /// <summary>
    /// 尝试将 JSON 作为旧版预设反序列化并转换。
    /// 若 JSON 不含旧版标志（"Arguments" 键），返回 null。
    /// </summary>
    public static List<PresetEntity>? ConvertJson(string json)
    {
        // 快速检测：旧版预设顶层必须有 "Arguments"
        if (!json.Contains("\"Arguments\"", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var oldPresets = JsonSerializer.Deserialize<List<OldPresetDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (oldPresets == null || oldPresets.Count == 0)
        {
            return null;
        }

        return oldPresets.Select(ConvertToNew).ToList();
    }

    private static PresetEntity ConvertToNew(OldPresetDto old)
    {
        return new PresetEntity
        {
            Name = old.Name,
            Type = old.Type,
            Default = old.Default,
            Parameters = ConvertArguments(old.Arguments)
        };
    }

    /// <summary>
    /// 公开入口：将旧版 v1.1 的 OutputArguments DTO 转换为新版 v2.0 的 OutputParameters。
    /// 供 <see cref="DatabaseMigrator"/> 在数据库迁移时调用。
    /// </summary>
    public static OutputParameters ConvertFromV1_1(OldOutputArgumentsDto old)
        => ConvertArguments(old);

    private static OutputParameters ConvertArguments(OldOutputArgumentsDto old)
    {
        var result = new OutputParameters
        {
            Extra = old.Extra,
            Format = old.Format,
            Video = ConvertVideo(old.Video, old.DisableVideo),
            Audio = ConvertAudio(old.Audio, old.DisableAudio),
            ProcessedOperationParameters = ConvertProcessedOptions(old.ProcessedOptions),
            Stream = ConvertStream(old.Stream),
            Mux = new MuxParameters { Shortest = old.Combine?.Shortest ?? false }
        };
        return result;
    }

    private static VideoCodecParameters ConvertVideo(OldVideoCodeArgumentsDto oldVideo, bool disableVideo)
    {
        var v = new VideoCodecParameters();

        if (oldVideo != null)
        {
            // 旧版有 Video 对象 → 重新编码（Transcode）
            v.Strategy = StreamStrategy.Transcode;
            v.Codec = oldVideo.Code ?? string.Empty;
            v.Preset = oldVideo.Preset;
            v.Crf = oldVideo.Crf;
            v.TwoPass = oldVideo.TwoPass;
            v.Size = oldVideo.Size;
            v.Fps = oldVideo.Fps;
            v.AverageBitrate = oldVideo.AverageBitrate;
            v.MaxBitrate = oldVideo.MaxBitrate;
            v.MaxBitrateBuffer = oldVideo.MaxBitrateBuffer ?? 2.0;
            v.AspectRatio = oldVideo.AspectRatio;
            v.PixelFormat = oldVideo.PixelFormat;
        }
        else if (disableVideo)
        {
            v.Strategy = StreamStrategy.Disable;
        }
        else
        {
            v.Strategy = StreamStrategy.Copy;
        }

        return v;
    }

    private static AudioCodecParameters ConvertAudio(OldAudioCodeArgumentsDto oldAudio, bool disableAudio)
    {
        var a = new AudioCodecParameters();

        if (oldAudio != null)
        {
            a.Strategy = StreamStrategy.Transcode;
            a.Codec = oldAudio.Code ?? string.Empty;
            a.Bitrate = oldAudio.Bitrate;
            a.SamplingRate = oldAudio.SamplingRate;
        }
        else if (disableAudio)
        {
            a.Strategy = StreamStrategy.Disable;
        }
        else
        {
            a.Strategy = StreamStrategy.Copy;
        }

        return a;
    }

    private static ProcessedOperationParameters ConvertProcessedOptions(OldProcessedOptionsDto old)
    {
        if (old == null)
        {
            return new ProcessedOperationParameters();
        }

        return new ProcessedOperationParameters
        {
            SyncModifiedTime = old.SyncModifiedTime,
            DeleteInputFiles = old.DeleteInputFiles
        };
    }

    private static StreamParameters ConvertStream(OldStreamArgumentsDto old)
    {
        var s = new StreamParameters();

        if (old?.Maps != null && old.Maps.Count > 0)
        {
            s.Maps = old.Maps.Select(m => new StreamMapParameters
            {
                InputIndex = m.InputIndex,
                Channel = m.Channel,
                StreamIndex = m.StreamIndex
            }).ToList();
        }

        return s;
    }
}
