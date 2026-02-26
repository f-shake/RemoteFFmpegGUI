using System.IO;

namespace SimpleFFmpegGUI.Configurations;

public class AppSettings
{
    public const string SectionName = "";

    public string InputDir { get; set; } = string.Empty;
    public string OutputDir { get; set; } = string.Empty;
    public int InputFtpPort { get; set; }
    public int OutputFtpPort { get; set; }
    public string Token { get; set; } = string.Empty;

    public string LocalSqlite { get; set; } = string.Empty;
    public int DefaultProcessPriority { get; set; } = 2;
    public string FFmpegDir { get; set; } = "ffmpeg";
    public string FFmpegFfmeDir { get; set; } = "ffmpeg_FFME";

    public string GetFullFFmpegPath() =>
        Path.IsPathFullyQualified(FFmpegDir)
            ? FFmpegDir
            : Path.Combine(Directory.GetCurrentDirectory(), FFmpegDir);
}