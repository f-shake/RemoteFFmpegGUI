using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SimpleFFmpegGUI.Models.Entities;

namespace SimpleFFmpegGUI.Helpers
{
    public static class FileSystemHelper
    {
        private const string TempDirKey = "TempDir";

        /// <summary>
        /// 自动改名时尝试的最大次数，超过则抛异常（避免病态目录下无限循环）
        /// </summary>
        private const int MaxDuplicateAttempts = 10000;

        /// <summary>
        /// 不合法的文件名字符集合
        /// </summary>
        private static HashSet<char> invalidFileNameChars = Path.GetInvalidFileNameChars().ToHashSet();

        public static string GetSequence(string sampleFilePath)
        {
            string dir = Path.GetDirectoryName(sampleFilePath);
            string filename = Path.GetFileNameWithoutExtension(sampleFilePath);
            string ext = Path.GetExtension(sampleFilePath);
            var filesInDir = Directory.EnumerateFiles(dir, $"*{ext}").ToList();
            for (int i = filename.Length - 1; i >= 0; i--)
            {
                var thisChar = filename[i];
                var rightChar = i == filename.Length - 1 ? '\0' : filename[i + 1];
                if (thisChar is '0' or '1' && rightChar is < '0' or > '9') //当前字符是0或1，右侧是非数字
                {
                    int indexLength = 1;
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (filename[j] == '0')
                        {
                            indexLength++;
                        }
                    }
                    string leftPart = filename[..(i - indexLength + 1)];
                    string rightPart = filename[(i + 1)..];
                    int indexFrom = thisChar - '0';
                    string nextFileName = leftPart + (indexFrom + 1).ToString().PadLeft(indexLength, '0') + rightPart;
                    nextFileName = Path.Combine(dir, nextFileName + ext);
                    if (filesInDir.Contains(nextFileName))
                    {
                        if (indexLength == 1)
                        {
                            return Path.Combine(dir, $"{leftPart}%d{rightPart}{ext}");
                        }
                        return Path.Combine(dir, $"{leftPart}%0{indexLength}d{rightPart}{ext}");
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取名称随机的临时文件。得到的文件为：%TEMP%/<see cref="SimpleFFmpegGUI"/>/{type}/{随机名称}
        /// </summary>
        /// <returns></returns>
        public static string GetTempFileName(string type)
        {
            return Path.Combine(GetTempDir(type,""), Guid.NewGuid().ToString());
        }

        /// <summary>
        /// 获取临时目录。得到的目录为：%TEMP%/<see cref="SimpleFFmpegGUI"/>/{type}/{随机名称}
        /// </summary>
        /// <returns></returns>
        public static string GetTempDir(string type)
        {
            string str = Guid.NewGuid().ToString();
            return GetTempDir(type, str);
        }

        private static string GetTempDir(string type, string subName)
        {
            string path = Path.Combine(Path.GetTempPath(), nameof(SimpleFFmpegGUI), type, subName);
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// 生成输出路径
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GenerateOutputPath(TaskEntity task)
        {
            return GenerateOutputPath(task, out _);
        }

        /// <summary>
        /// 生成输出路径
        /// </summary>
        /// <param name="task"></param>
        /// <param name="renamed">输出是否因与输入文件或已存在的文件冲突而被自动改名</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GenerateOutputPath(TaskEntity task, out bool renamed)
        {
            string output = task.Output.Trim();
            var a = task.Parameters;
            if (string.IsNullOrEmpty(output))
            {
                if (task.Inputs.Count == 0)
                {
                    throw new Exception("没有指定输出路径，且输入文件为空");
                }
                output = task.Inputs[0].FilePath;
            }

            // 库里存的输出本应是绝对路径（TaskService 入库时已解析），但 v2 早期用相对配置写入的
            // "output\x.mp4" 这类存量值仍是相对形态：TwoPass 会把 ffmpeg 子进程的工作目录设成 2pass
            // 临时目录（FFmpegProcessService），相对路径会跟着落到那里——任务显示成功，输出却不在 OutputDir 里。
            // 这里按当前工作目录绝对化一次（与该值当初的写入基准一致，也是非 2Pass 时子进程继承的目录），
            // 之后就不再依赖运行目录了。
            if (!Path.IsPathFullyQualified(output))
            {
                output = Path.GetFullPath(output);
            }

            //删除非法字符
            string dir = Path.GetDirectoryName(output);
            string filename = Path.GetFileName(output);
            if (filename.Any(p => invalidFileNameChars.Contains(p)))
            {
                foreach (var c in invalidFileNameChars)
                {
                    filename = filename.Replace(c.ToString(), "");
                }
                output = Path.Combine(dir, filename);
            }


            //修改扩展名
            if (!string.IsNullOrEmpty(a?.Format))
            {
                VideoFormat format = VideoFormat.Formats.Where(p => p.Name == a.Format || p.Extension == a.Format).FirstOrDefault();
                if (format != null)
                {
                    string name = Path.GetFileNameWithoutExtension(output);
                    string extension = format.Extension;
                    output = Path.Combine(dir, name + "." + extension);
                }
            }

            //获取非重复文件名：既不能和任何输入文件冲突，也不能覆盖磁盘上已经存在的文件
            //使用 OrdinalIgnoreCase 防止 Windows 大小写不敏感导致同名覆盖
            var usedPaths = new HashSet<string>(task.Inputs?.Select(p => p.FilePath) ?? [], StringComparer.OrdinalIgnoreCase);
            task.RealOutput = GetNonDuplicatePath(output, usedPaths);
            renamed = !string.Equals(task.RealOutput, output, StringComparison.OrdinalIgnoreCase);

            //创建目录
            if (!new FileInfo(task.RealOutput).Directory.Exists)
            {
                new FileInfo(task.RealOutput).Directory.Create();
            }
            return task.RealOutput;
        }

        /// <summary>
        /// 获取一个既不在 <paramref name="usedPaths"/> 中、磁盘上也不存在的文件路径。
        /// 冲突时按"名称 (2).扩展名"递增（与 v1 的 FileSystem.GetNoDuplicateFile 命名格式一致）。
        /// 注意：FzLib 3.x 的 FileNameHelper.GenerateUniquePath 只比对传入集合、不检查磁盘，
        /// 因此这里自行实现，避免 v2 曾出现的"目标文件已存在却被 ffmpeg -y 静默覆盖"。
        /// </summary>
        /// <param name="desiredPath">期望的路径</param>
        /// <param name="usedPaths">视为已占用的路径集合，可为null</param>
        /// <returns></returns>
        /// <exception cref="IOException">连续冲突次数超过上限</exception>
        public static string GetNonDuplicatePath(string desiredPath, IEnumerable<string> usedPaths = null)
        {
            var used = new HashSet<string>(usedPaths ?? [], StringComparer.OrdinalIgnoreCase);
            if (!IsOccupied(desiredPath, used))
            {
                return desiredPath;
            }

            string dir = Path.GetDirectoryName(desiredPath) ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(desiredPath);
            string ext = Path.GetExtension(desiredPath);
            for (int i = 2; i < MaxDuplicateAttempts + 2; i++)
            {
                string candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                if (!IsOccupied(candidate, used))
                {
                    return candidate;
                }
            }

            throw new IOException($"无法生成不重复的文件名：{desiredPath}");
        }

        /// <summary>
        /// 路径是否已被占用：在 <paramref name="usedPaths"/> 中，或磁盘上已存在
        /// </summary>
        private static bool IsOccupied(string path, HashSet<string> usedPaths)
        {
            return usedPaths.Contains(path) || File.Exists(path);
        }
    }
}