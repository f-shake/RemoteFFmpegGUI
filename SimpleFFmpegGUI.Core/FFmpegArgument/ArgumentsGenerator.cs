using SimpleFFmpegGUI.Models;
using System;
using System.Collections.Generic;
using System.Text;
using SimpleFFmpegGUI.Enums;
using SimpleFFmpegGUI.Models.Entities;
using SimpleFFmpegGUI.Models.MediaParameters;

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
        public static string GetArguments(IEnumerable<InputParameters> inputs, string outputArguments,
            string output = null)
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
        /// <param name="p">输出参数</param>
        /// <param name="pass">二次编码时，指定是第几次编码</param>
        /// <returns></returns>
        /// <exception cref="FFmpegArgumentException"></exception>
        public static string GetOutputArguments(OutputParameters p, int pass)
        {
            VideoArgumentsGenerator vg = new VideoArgumentsGenerator();
            AudioArgumentsGenerator ag = new AudioArgumentsGenerator();
            StreamArgumentsGenerator sg = new StreamArgumentsGenerator();
            CheckOutputArguments(p);
            switch (p.Video.Strategy)
            {
                case StreamStrategy.Copy:
                    vg.Copy();
                    break;
                case StreamStrategy.Disable:
                    vg.Disable();
                    break;
                case StreamStrategy.Transcode:
                    vg.Codec(p.Video.Codec);
                    vg.Speed(p.Video.Preset);
                    vg.CRF(p.Video.Crf);
                    vg.AverageBitrate(p.Video.AverageBitrate);
                    vg.MaxBitrate(p.Video.MaxBitrate);
                    if (p.Video.MaxBitrate != null)
                    {
                        vg.BufferRatio(p.Video.MaxBitrateBuffer);
                    }

                    vg.Aspect(p.Video.AspectRatio);
                    vg.PixelFormat(p.Video.PixelFormat);
                    vg.FrameRate(p.Video.Fps);
                    vg.Scale(p.Video.Size);
                    vg.Pass(pass);
                    break;
            }

            switch (p.Audio.Strategy)
            {
                case StreamStrategy.Copy:
                    ag.Copy();
                    break;
                case StreamStrategy.Disable:
                    ag.Disable();
                    break;
                case StreamStrategy.Transcode:
                    ag.Codec(p.Audio.Codec);
                    ag.Bitrate(p.Audio.Bitrate);
                    ag.SamplingRate(p.Audio.SamplingRate);
                    break;
            }


            foreach (var map in p.Stream.Maps)
            {
                sg.Map(map.InputIndex, map.Channel, map.StreamIndex);
            }

            string extra = "";

            if (pass == 1)
            {
                extra = $"-f {p.Format}";
            }

            extra = $"{extra} {p.Extra}";

            return string.Join(' ', vg.GetArguments(), ag.GetArguments(), sg.GetArguments(), extra);
        }

        private static void CheckOutputArguments(OutputParameters p)
        {
            if (p.Video.Strategy == StreamStrategy.Disable && p.Audio.Strategy == StreamStrategy.Disable)
            {
                throw new FFmpegArgumentException("不能同时禁用视频和音频");
            }

            if ((p.Video?.TwoPass ?? false) && string.IsNullOrWhiteSpace(p.Format))
            {
                throw new FFmpegArgumentException("需要二次编码时，必须指定格式（Format）");
            }
        }
    }
}