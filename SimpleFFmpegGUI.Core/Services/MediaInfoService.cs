using FFMpegCore;
using Mapster;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Helpers;
using SimpleFFmpegGUI.Models;
using SimpleFFmpegGUI.Models.MediaInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Extensions;
using SimpleFFmpegGUI.Models.MediaParameters;

namespace SimpleFFmpegGUI.Services
{
    public class MediaInfoService(IFFmpegProcessServiceFactory ffmpegProcessServiceFactory, ConfigService configService)
    {
        public VideoCodecParameters ConvertToVideoArguments(MediaInfoGeneral mediaInfo)
        {
            VideoCodecParameters arguments = new VideoCodecParameters();

            var tracks = JsonNode.Parse(mediaInfo.Raw)["media"]["track"] as JsonArray;
            if (mediaInfo.Videos.Count == 0)
            {
                throw new Exception("源文件不含视频");
            }

            var video = mediaInfo.Videos[0];

            arguments.Codec = video.Format switch
            {
                "AVC" => VideoCodec.X264.Name,
                "HEVC" => VideoCodec.X265.Name,
                _ => throw new Exception("仅支持H264或H265")
            };

            if (video.EncodingSettings != null && video.EncodingSettings.Count > 0)
            {
                var settings = video.EncodingSettings.ToDictionary(p => p.Name, p => p.Value);
                try
                {
                    if (settings.TryGetValue("rc", out var rc) && rc.Equals("crf"))
                    {
                        if (settings.TryGetValue("crf", out var crf))
                        {
                            arguments.Crf = Convert.ToInt32(crf);
                        }
                    }
                    else if (rc?.Equals("abr") == true)
                    {
                        if (settings.TryGetValue("bitrate", out var bitrate))
                        {
                            arguments.AverageBitrate = Convert.ToDouble(bitrate) / 1000;
                        }
                        if (settings.TryGetValue("stats-read", out var statsRead) && Convert.ToDouble(statsRead) > 0)
                        {
                            arguments.TwoPass = true;
                        }
                    }

                    if (settings.TryGetValue("vbv-maxrate", out var vbvMaxrate))
                    {
                        arguments.MaxBitrate = Convert.ToDouble(vbvMaxrate) / 1000;
                        if (settings.TryGetValue("vbv-bufsize", out var vbvBufsize))
                        {
                            arguments.MaxBitrateBuffer =
                                Convert.ToDouble(vbvBufsize) / 1000 / arguments.MaxBitrate;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"读取编码设置失败：{ex.Message}");
                }

                int preset = 0;
                // 编码设置缺键时跳过对应预设推断，不再抛 KeyNotFoundException
                settings.TryGetValue("cabac", out var cabac);
                settings.TryGetValue("subme", out var subme);
                settings.TryGetValue("ref", out var refValue);
                settings.TryGetValue("max-merge", out var maxMerge);

                if (arguments.Codec == VideoCodec.X264.Name)
                {
                    if (cabac?.Equals(0) == true)
                    {
                        preset = 8;
                    }
                    else if (subme?.Equals(1) == true)
                    {
                        preset = 7;
                    }
                    else if (subme?.Equals(2) == true)
                    {
                        preset = 6;
                    }
                    else if (subme?.Equals(4) == true)
                    {
                        preset = 5;
                    }
                    else if (subme?.Equals(6) == true)
                    {
                        preset = 4;
                    }
                    else if (subme?.Equals(7) == true)
                    {
                        preset = 3;
                    }
                    else if (subme?.Equals(8) == true)
                    {
                        preset = 2;
                    }
                    else if (subme?.Equals(9) == true)
                    {
                        preset = 1;
                    }
                }
                else if (arguments.Codec == VideoCodec.X265.Name)
                {
                    if (settings.ContainsKey("no-signhide"))
                    {
                        preset = 8;
                    }
                    else if (settings.ContainsKey("no-sao"))
                    {
                        preset = 7;
                    }
                    else if (subme?.Equals(1) == true)
                    {
                        preset = 6;
                    }
                    else if (refValue?.Equals(2) == true)
                    {
                        preset = 5;
                    }
                    else if (maxMerge?.Equals(2) == true)
                    {
                        preset = 4;
                    }
                    else if (refValue?.Equals(3) == true)
                    {
                        preset = 3;
                    }
                    else if (refValue?.Equals(4) == true)
                    {
                        preset = 2;
                    }
                    else if (maxMerge?.Equals(4) == true)
                    {
                        preset = 1;
                    }
                }

                arguments.Preset = preset;
            }
            else
            {
                throw new Exception("源视频未提供编码设置信息，无法转换为输出参数");
            }

            return arguments;
        }

        public async Task<MediaInfoGeneral> GetMediaInfoAsync(string path)
        {
            MediaInfoGeneral mediaInfo = null;
            await Task.Run(() =>
            {
                var mediaInfoJSON = GetMediaInfoProcessOutput(path);
                mediaInfo = ParseMediaInfoJSON(mediaInfoJSON);
                mediaInfo.Raw = mediaInfoJSON.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                foreach (var video in mediaInfo.Videos)
                {
                    if (!string.IsNullOrEmpty(video.EncodedLibrarySettings))
                    {
                        video.EncodingSettings = ParseEncodingSettings(video.EncodedLibrarySettings);
                    }
                }
            });
            return mediaInfo;
        }

        public async Task<string> GetSnapshotAsync(string path, TimeSpan time, string scale = null,
            string format = "jpg")
        {
            // 未显式指定时使用配置的快照尺寸（P1-12）
            scale ??= configService.SnapshotSize;
            Debug.WriteLine("正在采集截图");
            string tempPath = $"{FileSystemHelper.GetTempFileName("snapshot")}.{format}";

            string args =
                $"-ss {time.TotalSeconds:0.000} " + // 快速 seek（在 -i 前）
                $"-skip_frame nokey " + //只截取关键帧，提升速度，降低内存
                $"-i \"{path}\" " +
                "-vframes 1 " +
                $"-vf \"scale={scale}:flags=fast_bilinear,format=yuvj420p\" " +
                "-threads 1 " + // 限制线程
                "-max_muxing_queue_size 2 " + // 限制缓存
                $"\"{tempPath}\"";

            FFmpegProcessService process = ffmpegProcessServiceFactory.Create(args);
            await process.StartAsync(null, null);

            return tempPath;
        }

        public TimeSpan GetVideoDurationByFFprobe(string path)
        {
            return FFProbe.Analyse(path).Duration;
        }

        public async Task<TimeSpan> GetVideoDurationByFFprobeAsync(string path)
        {
            return (await FFProbe.AnalyseAsync(path)).Duration;
        }

        private static JsonObject GetMediaInfoProcessOutput(string path)
        {
            string tmpFile = FileSystemHelper.GetTempFileName("mediainfo");
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "MediaInfo",
                Arguments = $"--output=JSON --BOM --LogFile=\"{tmpFile}\" \"{path}\"",
                CreateNoWindow = true,
            });
            p.WaitForExit();
            string output = System.IO.File.ReadAllText(tmpFile);
            return JsonNode.Parse(output) as JsonObject;
        }

        /// <summary>
        /// 解析编码设置（由NewBing生成）
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static List<MediaInfoItem> ParseEncodingSettings(string input)
        {
            List<MediaInfoItem> settings = new List<MediaInfoItem>(); // 创建一个空列表来存储编码设置项
            var parts = input.Split('/').Select(p => p.Trim()); // 用"/"来分割输入字符串为一个字符串数组
            foreach (string part in parts) // 遍历数组中的每个字符串
            {
                MediaInfoItem setting = new MediaInfoItem(); // 创建一个新的编码设置项对象
                if (part.Contains('=')) // 检查字符串是否包含"="
                {
                    string[] pair = part.Split('='); // 用"="来分割字符串为两部分
                    setting.Name = pair[0]; // 把第一部分赋值给编码设置项的名称属性
                    string value = pair[1]; // 把第二部分作为一个字符串
                    if (int.TryParse(value, out int intValue)) // 尝试把值解析为一个整数
                    {
                        setting.Value = intValue; // 把整数值赋值给编码设置项的值属性
                    }
                    else if (double.TryParse(value, out double doubleValue)) // 尝试把值解析为一个双精度浮点数
                    {
                        setting.Value = doubleValue; // 把双精度浮点数赋值给编码设置项的值属性
                    }
                    else // 如果值不是一个数字
                    {
                        setting.Value = value; // 把字符串值赋值给编码设置项的值属性
                    }
                }
                else // 如果字符串不包含"="
                {
                    setting.Name = part; // 把整个字符串赋值给编码设置项的名称属性
                    setting.Value = true; // 把true赋值给编码设置项的值属性
                }

                settings.Add(setting); // 把编码设置项对象添加到列表中
            }

            return settings; // 返回编码设置项列表
        }

        private static MediaInfoGeneral ParseMediaInfoJSON(JsonObject json)
        {
            MediaInfoGeneral info = null;
            var tracks = json["media"]["track"] as JsonArray;
            foreach (JsonObject track in tracks)
            {
                if (track["@type"].GetValue<string>() == "General")
                {
                    info = track.DeserializeWithDefaultSettings<MediaInfoGeneral>();
                }
                else if (track["@type"].GetValue<string>() == "Video")
                {
                    Debug.Assert(info != null);
                    info.Videos.Add(track.DeserializeWithDefaultSettings<MediaInfoVideo>());
                    info.Videos[^1].Index = info.Videos.Count;
                }
                else if (track["@type"].GetValue<string>() == "Audio")
                {
                    Debug.Assert(info != null);
                    info.Audios.Add(track.DeserializeWithDefaultSettings<MediaInfoAudio>());
                    info.Audios[^1].Index = info.Audios.Count;
                }
                else if (track["@type"].GetValue<string>() == "Text")
                {
                    Debug.Assert(info != null);
                    info.Texts.Add(track.DeserializeWithDefaultSettings<MediaInfoText>());
                    info.Texts[^1].Index = info.Texts.Count;
                }
            }

            return info;
        }
    }
}