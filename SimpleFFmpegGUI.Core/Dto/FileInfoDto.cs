using FzLib;
using FzLib.Numeric;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Dto
{
    public class FileInfoDto
    {
        public FileInfoDto()
        {
        }

        public FileInfoDto(string path)
        {
            FileInfo file = new FileInfo(path);
            Name = file.Name;
            Length = file.Length;
            LengthText = NumberConverter.ByteToFitString(Length);
            LastWriteTime = file.LastWriteTime;
        }

        public string Name { get; set; }
        public long Length { get; set; }
        public string LengthText { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string Id { get; set; }
    }
}