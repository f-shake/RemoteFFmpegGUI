using SimpleFFmpegGUI.FFmpegLib;
using System.Collections.Generic;
using System.Linq;

namespace SimpleFFmpegGUI.FFmpegArgument
{
    public abstract class ArgumentsGeneratorBase
    {
        /// <summary>
        /// 单个参数对的集合
        /// </summary>
        protected List<FFmpegArgumentItem> arguments = new List<FFmpegArgumentItem>();

        /// <summary>
        /// 额外参数
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<FFmpegArgumentItem> ExtraArguments()
        {
            return Enumerable.Empty<FFmpegArgumentItem>();
        }

        /// <summary>
        /// 将<see cref="arguments"/>中的参数连接成为字符串
        /// </summary>
        /// <returns></returns>
        public virtual string GetArguments()
        {
            var args = arguments.Concat(ExtraArguments()).Where(p => p != null).ToList();
            while (args.Any(p => p.Other != null))
            {
                var hasOthers = args.Where(p => p.Other != null).ToList();
                foreach (var item in hasOthers)
                {
                    args.Add(item.Other);
                    item.Other = null;
                }
            }

            List<string> list = new List<string>();
            var groups = args.GroupBy(p => p.Parent).ToList();
            foreach (var group in groups)
            {
                if (group.Key == null)
                {
                    foreach (var arg in group)
                    {
                        list.Add($"-{arg.Key} {arg.Value}");
                    }
                }
                else
                {
                    List<string> subList = new List<string>();
                    foreach (var arg in group)
                    {
                        subList.Add($"{arg.Key}={arg.Value}");
                    }
                    list.Add($"-{group.Key} {string.Join(group.First().Separator, subList)}");
                }
            }

            return string.Join(' ', list);
        }
    }
}