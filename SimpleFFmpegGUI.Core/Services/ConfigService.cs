using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using SimpleFFmpegGUI.Extensions;

namespace SimpleFFmpegGUI.Services;

public class ConfigService
{
    private const string path = "config.json";

    private static bool loaded = false;

    public ConfigService()
    {

    }

    /// <summary>
    /// 是否从 config.json 加载了配置（未加载过时各配置项为默认值，应由调用方用 appsettings 默认值填充）
    /// </summary>
    internal bool LoadedFromFile { get; private set; } = false;

    internal static ConfigService Create()
    {

        if (loaded)
        {
            throw new Exception("ConfigService只能被实例化一次");
        }
        loaded = true;

        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var config = json.DeserializeWithDefaultSettings<ConfigService>();
                config.LoadedFromFile = true;
                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new ConfigService();
            }
        }

        return new ConfigService();
    }
    
    // 默认 2（正常），避免 config.json 存在但未写入该键时（如 WPF 端由 Config 类生成的文件）取值 0（空闲）导致 ffmpeg 以 Idle 优先级运行
    public int DefaultProcessPriority { get; set; } = 2;

    /// <summary>
    /// 快照缩放尺寸（v1 的 SnapshotSize 配置，默认 -1:1080）
    /// </summary>
    public string SnapshotSize { get; set; } = "-1:1080";
    
    public async Task SaveAsync()
    {
        Console.WriteLine("尝试保存配置");
        // 与 WPF 端 Config 类共用 config.json：只更新本服务管理的键，保留文件中的其他配置（如 RemoteHosts），
        // 避免整文件覆盖导致 WPF 用户配置丢失
        JsonObject root;
        if (File.Exists(path))
        {
            try
            {
                root = JsonNode.Parse(await File.ReadAllTextAsync(path).ConfigureAwait(false)) as JsonObject ?? new JsonObject();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                root = new JsonObject();
            }
        }
        else
        {
            root = new JsonObject();
        }
        root["DefaultProcessPriority"] = DefaultProcessPriority;
        root["SnapshotSize"] = SnapshotSize;
        await File.WriteAllTextAsync(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true })).ConfigureAwait(false);
    }
}