using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using SimpleFFmpegGUI.Extensions;

namespace SimpleFFmpegGUI.Services;

public class ConfigService
{
    private const string path = "config.json";
    
    private static bool loaded = false;
    
    public ConfigService()
    {
        
    }

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
                return json.DeserializeWithDefaultSettings<ConfigService>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new ConfigService();
            }
        }

        return new ConfigService();
    }
    
    public int DefaultProcessPriority { get; set; }
    
    public async Task SaveAsync()
    {
        Console.WriteLine("尝试保存配置");
        await File.WriteAllTextAsync(path, this.SerializeWithDefaultSettings());
    }
}