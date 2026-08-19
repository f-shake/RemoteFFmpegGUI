using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleFFmpegGUI.WPF.FzLib.Collection
{
    public static class DictionaryExtension
    {
        /// <summary>
        /// 如果存在Key，那么重新设置值；否则调用Add方法
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>如果是true，表示增加了值；否则表示值已存在并重新赋值</returns>
        public static bool AddOrSetValue<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue value)
        {
            if (!dic.ContainsKey(key))
            {
                dic.Add(key, value);
                return true;
            }
            else
            {
                dic[key] = value;
                return false;
            }
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key)
        {
            if (!dic.ContainsKey(key))
            {
                return default;
            }
            return dic[key];
        }
    }
}