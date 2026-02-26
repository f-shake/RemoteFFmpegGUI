using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Models;
using System.Text;
using SimpleFFmpegGUI.Enums;

namespace SimpleFFmpegGUI.FFmpegArgument
{
    public class StreamArgumentsGenerator : ArgumentsGeneratorBase
    {
        /// <summary>
        /// 设置流映射
        /// </summary>
        /// <param name="inputIndex"></param>
        /// <param name="channel"></param>
        /// <param name="streamIndex"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public StreamArgumentsGenerator Map(int inputIndex, StreamChannel channel, int? streamIndex)
        {
            StringBuilder str = new StringBuilder();
            str.Append(inputIndex);
            if (channel != StreamChannel.All)
            {
                str.Append(':');
                str.Append(channel switch
                {
                    StreamChannel.Video => 'v',
                    StreamChannel.Audio => 'a',
                    StreamChannel.Subtitle => 's',
                    _ => throw new System.NotImplementedException(),
                });
            }
            if (streamIndex.HasValue)
            {
                str.Append(':').Append(streamIndex.Value);
            }
            arguments.Add(new FFmpegArgumentItem("map", str.ToString()));
            return this;
        }
    }
}