namespace SimpleFFmpegGUI.WebTest;

// public class AppTestSettingsKeys
// {
//     public const string TestOutputVideo10sKey = "TestOutputVideo10s";
//     public const string TestVideo10sKey = "TestVideo10s";
//     public const string TestVideoKey = "TestVideo";
// }

public class AppTestSettings
{
    public string TestVideo { get; set; } = string.Empty;
    public string TestVideo10s { get; set; } = string.Empty;
    public string TestOutputVideo10s { get; set; } = string.Empty;
}