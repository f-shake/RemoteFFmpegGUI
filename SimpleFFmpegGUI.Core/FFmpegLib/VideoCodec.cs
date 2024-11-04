using FFMpegCore.Arguments;
using FFMpegCore.Enums;
using FzLib.DataAnalysis;
using SimpleFFmpegGUI.FFmpegArgument;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.FFmpegLib
{
    public abstract class VideoCodec : CodecBase
    {
        public static readonly X264 X264 = new X264();
        public static readonly X265 X265 = new X265();
        public static readonly XVP9 XVP9 = new XVP9();
        public static readonly AomAV1 AomAV1 = new AomAV1();
        public static readonly SVTAV1 SVTAV1 = new SVTAV1();
        public static readonly N_AV1 N_AV1 = new N_AV1();
        public static readonly N_H264 N_H264 = new N_H264();
        public static readonly N_H265 N_H265 = new N_H265();
        public static readonly GeneralVideoCodec General = new GeneralVideoCodec();
        public static readonly VideoCodec[] VideoCodecs = new VideoCodec[]
        {
           X264,
           X265,
           XVP9,
           AomAV1,
           SVTAV1,
           N_H264,
           N_H265,
           N_AV1
        };
        private static Dictionary<string, VideoCodec> name2codec;

        public static VideoCodec GetCodec(string name)
        {
            name2codec ??= VideoCodecs.ToDictionary(p => p.Name, p => p);
            return name == null ? null : name2codec.GetValueOrDefault(name);
        }


        /// <summary>
        /// 默认CRF
        /// </summary>
        public abstract int DefaultCRF { get; }

        /// <summary>
        /// CRF的名字，在硬件编码中没有CRF，只有CQ
        /// </summary>
        public abstract string CRFLabel { get; }

        /// <summary>
        /// 默认速度预设等级
        /// </summary>
        public abstract int DefaultSpeedLevel { get; }

        /// <summary>
        /// 最大CRF
        /// </summary>
        public abstract int MaxCRF { get; }

        /// <summary>
        /// 最大速度预设等级
        /// </summary>
        public abstract int MaxSpeedLevel { get; }

        /// <summary>
        /// 速度预设与编码速度的关系
        /// </summary>
        /// <remarks>
        /// 提供一个数组，其长度=<see cref="MaxSpeedLevel"/>+1，即速度预设的数量。下标值与速度预设值对应。
        /// SpeedFPSRelationship[i]代表速度预设为i时，实际编码速度的一个相对值（帧/秒）。
        /// 其绝对值不具有意义，但一组值之间的相对值代表其相对编码速度的快慢
        /// </remarks>
        public abstract double[] SpeedFPSRelationship { get; }

        /// <summary>
        /// 额外参数
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<FFmpegArgumentItem> ExtraArguments()
        {
            return Enumerable.Empty<FFmpegArgumentItem>();
        }

        public virtual FFmpegArgumentItem AverageBitrate(double mb)
        {
            if (mb < 0)
            {
                throw new FFmpegArgumentException("平均码率超出范围");
            }
            return new FFmpegArgumentItem("b:v", $"{mb}M");
        }

        public virtual FFmpegArgumentItem BufferSize(double mb)
        {
            if (mb < 0)
            {
                throw new FFmpegArgumentException("缓冲大小超出范围");
            }
            return new FFmpegArgumentItem("bufsize", $"{mb}M");
        }

        public virtual FFmpegArgumentItem CRF(int level)
        {
            if (level < 0 || level > MaxCRF)
            {
                throw new FFmpegArgumentException("CRF的值超出范围");
            }
            return new FFmpegArgumentItem("crf", level.ToString());
        }

        public virtual FFmpegArgumentItem FrameRate(double fps)
        {
            if (fps < 0)
            {
                throw new FFmpegArgumentException("帧速率大小超出范围");
            }
            return new FFmpegArgumentItem("r", fps.ToString());
        }

        public virtual FFmpegArgumentItem MaxBitrate(double mb)
        {
            if (mb < 0)
            {
                throw new FFmpegArgumentException("最大码率超出范围");
            }
            return new FFmpegArgumentItem("maxrate", $"{mb}M");
        }

        public virtual FFmpegArgumentItem PixelFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new FFmpegArgumentException("提供的像素格式为空");
            }
            return new FFmpegArgumentItem("pix_fmt", format);
        }

        public virtual FFmpegArgumentItem Pass(int pass)
        {
            if (pass is not (1 or 2 or 3))
            {
                throw new FFmpegArgumentException("参数pass超出范围");
            }
            return new FFmpegArgumentItem("pass", pass.ToString());
        }

        public virtual FFmpegArgumentItem Speed(int speed)
        {
            if (speed > MaxSpeedLevel)
            {
                throw new FFmpegArgumentException("速度值超出范围");
            }
            return null;
        }
    }

}
