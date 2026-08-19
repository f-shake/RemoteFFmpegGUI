using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SimpleFFmpegGUI.WPF.FzLib.WPF
{
    public static class FrameworkElementExtension
    {
        /// <summary>
        /// 等待元素加载完成
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public async static Task WaitForLoadedAsync(this FrameworkElement element)
        {
            if (element.IsLoaded)
            {
                return;
            }
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            element.Loaded += (p1, p2) =>
            {
                tcs.TrySetResult(0);
            };
            await tcs.Task;
        }
    }
}