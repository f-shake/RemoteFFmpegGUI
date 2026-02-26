using SimpleFFmpegGUI.Models;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleFFmpegGUI.Models.Entities;

namespace SimpleFFmpegGUI.FFmpegArgument
{
    /// <summary>
    /// FFmpeg命令参数生成器
    /// </summary>
    public static class ArgumentsGenerator
    {
        /// <summary>
        /// 生成FFmpeg字符串参数
        /// </summary>
        /// <param name="task">任务</param>
        /// <param name="pass">二次编码时，指定是第几次编码</param>
        /// <param name="output">输出路径</param>
        /// <returns></returns>
        public static string GetArguments(TaskEntity task, int pass, string output = null)
        {
            StringBuilder str = new StringBuilder();
            str.Append("-hide_banner ");
            foreach (var input in task.Inputs)
            {
                str.Append(GetInputArguments(input));
                str.Append(' ');
            }
            str.Append(GetOutputArguments(task.Parameters, pass));
            str.Append(" \"");
            str.Append(output ?? task.RealOutput);
            str.Append("\" -y");
            return str.ToString();
        }

        /// <summary>
        /// 生成FFmpeg字符串参数
        /// </summary>
        /// <param name="inputs">输入文件</param>
        /// <param name="outputArguments">输出参数</param>
        /// <param name="output">输出路径</param>
        /// <returns></returns>
        public static string GetArguments(IEnumerable<InputParameters> inputs, string outputArguments, string output = null)
        {
            StringBuilder str = new StringBuilder();
            foreach (var input in inputs)
            {
                str.Append(GetInputArguments(input));
                str.Append(' ');
            }
            str.Append(outputArguments);
            str.Append(' ');
            str.Append(output == null ? "" : $"\"{output}\"");
            str.Append(" -y");
            return str.ToString();
        }

        /// <summary>
        /// 生成输入的字符串参数
        /// </summary>
        /// <param name="ia">输入文件</param>
        /// <returns></returns>
        public static string GetInputArguments(InputParameters ia)
        {
            InputArgumentsGenerator ig = new InputArgumentsGenerator();
            ig.Seek(ia.From);
            ig.To(ia.To);
            ig.Duration(ia.Duration);
            ig.Format(ia.Format);
            ig.Framerate(ia.Framerate);
            ig.Input(ia.FilePath);
            return ia.Extra == null ? ig.GetArguments() : $"{ia.Extra}  {ig.GetArguments()}";
        }

        /// <summary>
        /// 生成输出部分的字符串参数
        /// </summary>
        /// <param name="video">视频参数</param>
        /// <param name="audio">音频参数</param>
        /// <param name="stream">流参数</param>
        /// <returns></returns>
        public static string GetOutputArguments(
            Func<VideoArgumentsGenerator, VideoArgumentsGenerator> video,
            Func<AudioArgumentsGenerator, AudioArgumentsGenerator> audio,
            Func<StreamArgumentsGenerator, StreamArgumentsGenerator> stream)
        {
            VideoArgumentsGenerator vg = new VideoArgumentsGenerator();
            AudioArgumentsGenerator ag = new AudioArgumentsGenerator();
            StreamArgumentsGenerator sg = new StreamArgumentsGenerator();
            vg = video(vg);
            ag = audio(ag);
            sg = stream(sg);

            return string.Join(' ', vg.GetArguments(), ag.GetArguments(), sg.GetArguments());
        }

        /// <summary>
        /// 生成输出部分的字符串参数
        /// </summary>
        /// <param name="oa">输出参数</param>
        /// <param name="pass">二次编码时，指定是第几次编码</param>
        /// <returns></returns>
        /// <exception cref="FFmpegArgumentException"></exception>
        public static string GetOutputArguments(OutputParameters oa, int pass)
        {
            VideoArgumentsGenerator vg = new VideoArgumentsGenerator();
            AudioArgumentsGenerator ag = new AudioArgumentsGenerator();
            StreamArgumentsGenerator sg = new StreamArgumentsGenerator();
            CheckOutputArguments(oa);
            if (oa.DisableVideo)
            {
                vg.Disable();
            }
            else if (oa.Video == null)
            {
                vg.Copy();
            }
            else
            {
                vg.Codec(oa.Video.Code);
                vg.Speed(oa.Video.Preset);
                vg.CRF(oa.Video.Crf);
                vg.AverageBitrate(oa.Video.AverageBitrate);
                vg.MaxBitrate(oa.Video.MaxBitrate);
                if (oa.Video.MaxBitrate != null)
                {
                    vg.BufferRatio(oa.Video.MaxBitrateBuffer);
                }
                vg.Aspect(oa.Video.AspectRatio);
                vg.PixelFormat(oa.Video.PixelFormat);
                vg.FrameRate(oa.Video.Fps);
                vg.Scale(oa.Video.Size);
                vg.Pass(pass);
            }

            if (oa.DisableAudio || pass == 1)
            {
                ag.Disable();
            }
            else if (oa.Audio == null)
            {
                ag.Copy();
            }
            else
            {
                ag.Codec(oa.Audio.Code);
                ag.Bitrate(oa.Audio.Bitrate);
                ag.SamplingRate(oa.Audio.SamplingRate);
            }

            if (oa.Stream != null && oa.Stream.Maps != null && oa.Stream.Maps.Count > 0)
            {
                foreach (var map in oa.Stream.Maps)
                {
                    sg.Map(map.InputIndex, map.Channel, map.StreamIndex);
                }
            }

            string extra = "";
            //取消指定format，因为一些不同的格式可能Format相同，指定后缀名也可以达到相同的效果
            //if (oa.Format != null && pass!=1)
            //{
            //    extra = $"-f {oa.Format}";
            //}
            if (pass == 1)
            {
                extra = $"-f {oa.Format}";
            }
            if (oa.Extra != null)
            {
                extra = $"{extra} {oa.Extra}";
            }

            return string.Join(' ', vg.GetArguments(), ag.GetArguments(), sg.GetArguments(), extra);
        }

        private static void CheckOutputArguments(OutputParameters oa)
        {
            if (oa.DisableVideo && oa.DisableAudio)
            {
                throw new FFmpegArgumentException("不能同时禁用视频和音频");
            }
            if ((oa.Video?.TwoPass ?? false) && string.IsNullOrWhiteSpace(oa.Format))
            {
                throw new FFmpegArgumentException("需要二次编码时，必须指定格式（Format）");
            }
        }
    }
}