using FFMpegCore;
using Mapster;
using Newtonsoft.Json.Linq;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Manager
{
    public static class MediaInfoManager
    {
        public static async Task<MediaInfoGeneral> GetMediaInfoAsync(string path)
        {
            MediaInfoGeneral mediaInfo = null;
            await Task.Run(() =>
            {
                var mediaInfoJSON = GetMediaInfoProcessOutput(path);
                mediaInfo = ParseMediaInfoJSON(mediaInfoJSON);
                mediaInfo.Raw = mediaInfoJSON.ToString(Newtonsoft.Json.Formatting.Indented);
                foreach (var video in mediaInfo.Videos)
                {
                    if (!string.IsNullOrEmpty(video.Encoded_Library_Settings))
                    {
                        video.EncodingSettings = ParseEncodingSettings(video.Encoded_Library_Settings);
                    }
                }

            });
            return mediaInfo;
        }

        public static async Task<string> GetSnapshotAsync(string path, TimeSpan time, string scale, string format = "jpg")
        {
            Debug.WriteLine("正在采集截图");
            string tempPath = $"{FileSystemUtility.GetTempFileName("snapshot")}.{format}";

            string args =
                $"-ss {time.TotalSeconds:0.000} " +        // 快速 seek（在 -i 前）
                $"-skip_frame nokey " +            //只截取关键帧，提升速度，降低内存
                $"-i \"{path}\" " +
                "-vframes 1 " +
                $"-vf scale={scale}:flags=fast_bilinear " + // 低内存 scale
                "-threads 1 " +                              // 限制线程
                "-max_muxing_queue_size 2 " +                // 限制缓存
                $"\"{tempPath}\"";

            FFmpegProcess process = new FFmpegProcess(args);
            await process.StartAsync(null, null);

            return tempPath;
        }

        public static TimeSpan GetVideoDurationByFFprobe(string path)
        {
            return FFProbe.Analyse(path).Duration;
        }

        public static async Task<TimeSpan> GetVideoDurationByFFprobeAsync(string path)
        {
            return (await FFProbe.AnalyseAsync(path)).Duration;
        }

        private static JObject GetMediaInfoProcessOutput(string path)
        {
            string tmpFile = FileSystemUtility.GetTempFileName("mediainfo");
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "MediaInfo",
                Arguments = $"--output=JSON --BOM --LogFile=\"{tmpFile}\" \"{path}\"",
                CreateNoWindow = true,
            });
            p.WaitForExit();
            string output = System.IO.File.ReadAllText(tmpFile);
            return JObject.Parse(output);
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

        private static MediaInfoGeneral ParseMediaInfoJSON(JObject json)
        {
            MediaInfoGeneral info = null;
            var tracks = json["media"]["track"] as JArray;
            foreach (JObject track in tracks)
            {
                if (track["@type"].Value<string>() == "General")
                {
                    info = track.ToObject<MediaInfoGeneral>();
                }
                else if (track["@type"].Value<string>() == "Video")
                {
                    Debug.Assert(info != null);
                    info.Videos.Add(track.ToObject<MediaInfoVideo>());
                    info.Videos[^1].Index = info.Videos.Count;
                }
                else if (track["@type"].Value<string>() == "Audio")
                {
                    Debug.Assert(info != null);
                    info.Audios.Add(track.ToObject<MediaInfoAudio>());
                    info.Audios[^1].Index = info.Audios.Count;
                }
                else if (track["@type"].Value<string>() == "Text")
                {
                    Debug.Assert(info != null);
                    info.Texts.Add(track.ToObject<MediaInfoText>());
                    info.Texts[^1].Index = info.Texts.Count;
                }
            }
            return info;
        }

        public static VideoCodeArguments ConvertToVideoArguments(MediaInfoGeneral mediaInfo)
        {
            VideoCodeArguments arguments = new VideoCodeArguments();

            var tracks = JObject.Parse(mediaInfo.Raw)["media"]["track"] as JArray;
            if (mediaInfo.Videos.Count == 0)
            {
                throw new Exception("源文件不含视频");
            }
            var video = mediaInfo.Videos[0];

            arguments.Code = video.Format switch
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
                    if (settings["rc"].Equals("crf"))
                    {
                        if (settings.ContainsKey("crf"))
                        {
                            arguments.Crf = Convert.ToInt32(settings["crf"]);
                        }
                    }
                    else if (settings["rc"].Equals("abr"))
                    {
                        arguments.AverageBitrate = Convert.ToDouble(settings["bitrate"]) / 1000;
                        if (Convert.ToDouble(settings["stats-read"]) > 0)
                        {
                            arguments.TwoPass = true;
                        }
                    }
                    if (settings.ContainsKey("vbv-maxrate"))
                    {
                        arguments.MaxBitrate = Convert.ToDouble(settings["vbv-maxrate"]) / 1000;
                        arguments.MaxBitrateBuffer = Convert.ToDouble(settings["vbv-bufsize"]) / 1000 / arguments.MaxBitrate;
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }

                int preset = 0;

                if (arguments.Code == VideoCodec.X264.Name)
                {
                    if (settings["cabac"].Equals(0))
                    {
                        preset = 8;
                    }
                    else if (settings["subme"].Equals(1))
                    {
                        preset = 7;
                    }
                    else if (settings["subme"].Equals(2))
                    {
                        preset = 6;
                    }
                    else if (settings["subme"].Equals(4))
                    {
                        preset = 5;
                    }
                    else if (settings["subme"].Equals(6))
                    {
                        preset = 4;
                    }
                    else if (settings["subme"].Equals(7))
                    {
                        preset = 3;
                    }
                    else if (settings["subme"].Equals(8))
                    {
                        preset = 2;
                    }
                    else if (settings["subme"].Equals(9))
                    {
                        preset = 1;
                    }
                }
                else if (arguments.Code == VideoCodec.X265.Name)
                {
                    if (settings.ContainsKey("no-signhide"))
                    {
                        preset = 8;
                    }
                    else if (settings.ContainsKey("no-sao"))
                    {
                        preset = 7;
                    }
                    else if (settings["subme"].Equals(1))
                    {
                        preset = 6;
                    }
                    else if (settings["ref"].Equals(2))
                    {
                        preset = 5;
                    }
                    else if (settings["max-merge"].Equals(2))
                    {
                        preset = 4;
                    }
                    else if (settings["ref"].Equals(3))
                    {
                        preset = 3;
                    }
                    else if (settings["ref"].Equals(4))
                    {
                        preset = 2;
                    }
                    else if (settings["max-merge"].Equals(4))
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
    }
}