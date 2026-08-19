using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WPF.FzLib
{
    public static class LoopExtension
    {
        public static void ForEach<T>(this IEnumerable<T> list, Action<T> action)
        {
            foreach (var item in list)
            {
                action(item);
            }
        }

        public async static void ForEachAsync<T>(this IEnumerable<T> list, Func<T, Task> action)
        {
            foreach (var item in list)
            {
                await action(item);
            }
        }

        public static void ForEach(this IEnumerable list, Action<object> action)
        {
            foreach (var item in list)
            {
                action(item);
            }
        }

        public static void ForRange(int start, int smallerThan, Action<int> action)
        {
            for (int i = start; i < smallerThan; i++)
            {
                action(i);
            }
        }

        public static void ForRange(int smallerThan, Action<int> action)
        {
            ForRange(0, smallerThan, action);
        }
    }
}