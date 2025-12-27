using Newtonsoft.Json;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Manager
{
    public class ConfigManager
    {
        public const string DefaultProcessPriorityKey = "DefaultProcessPriority";
        public const string SnapshotSizeKey = "SnapshotSize";
        private static readonly Dictionary<string, object> cache = new Dictionary<string, object>();

        private readonly FFmpegDbContext db;

        public ConfigManager(FFmpegDbContext db)
        {
            this.db = db;
        }

        public int DefaultProcessPriority
        {
            get => GetConfig(DefaultProcessPriorityKey, 2);
            set => SetConfig(DefaultProcessPriorityKey, value);
        }

        public string SnapshotSize
        {
            get => GetConfig(SnapshotSizeKey, "-1:1080");
            set => SetConfig(SnapshotSizeKey, value);
        }

        public T GetConfig<T>(string key, T defaultValue)
        {
            if (cache.ContainsKey(key))
            {
                return (T)cache[key];
            }
            var item = db.Configs.Where(p => p.Key == key).FirstOrDefault();
            if (item == null)
            {
                return defaultValue;
            }
            T value = Parse<T>(item.Value);
            cache.Add(key, value);

            Logger.Info($"读取配置：[{key}]={value}");
            return value;
        }
        public void SetConfig<T>(string key, T value)
        {
            if (cache.ContainsKey(key))
            {
                cache[key] = value;
            }
            var item = db.Configs.Where(p => p.Key == key).FirstOrDefault();
            if (item == null)
            {
                item = new Config()
                {
                    Key = key,
                    Value = GetString(value)
                };
                db.Configs.Add(item);
            }
            else
            {
                item.Value = GetString(value);
                db.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }

            db.SaveChanges();

            Logger.Info($"写入配置：[{key}]={value}");
        }

        private static string GetString<T>(T data)
        {
            return JsonConvert.SerializeObject(data);
        }

        private static T Parse<T>(string data)
        {
            return JsonConvert.DeserializeObject<T>(data);
        }
    }
}
