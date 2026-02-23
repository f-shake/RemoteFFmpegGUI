using FzLib;
using FzLib.Numeric;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Enums;

namespace SimpleFFmpegGUI.Dto
{
    public class FileInfoDto
    {
        public FileInfoDto()
        {
        }

        public FileInfoDto(string path, string rootDir)
        {
            FileInfo file = new FileInfo(path);
            Path = file.FullName;
            RelativePath = System.IO.Path.GetRelativePath(rootDir, Path);
            Name = file.Name;
            Length = file.Length;
            LengthText = NumberConverter.ByteToFitString(Length);
            LastWriteTime = file.LastWriteTime;
        }

        public string Name { get; set; }
        public string Path { get; set; }
        public string RelativePath { get; set; }
        public long Length { get; set; }
        public string LengthText { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}