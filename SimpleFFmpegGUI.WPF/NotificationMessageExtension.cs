using Enterwell.Clients.Wpf.Notifications;
using Enterwell.Clients.Wpf.Notifications.Controls;
using SimpleFFmpegGUI.WPF.FzLib.WPF;
using iNKORE.Extension.CommonDialog;
using SimpleFFmpegGUI.WPF;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SimpleFFmpegGUI.WPF
{
    public static class NotificationMessageExtension
    {
        public static void QueueSuccess(this NotificationMessageBuilder builder, string message)
        {
            builder.Background(Brushes.Green)
                       .HasMessage(message)
                       .Animates(true)
                       .Dismiss().WithDelay(3000)
                       .Queue();
        }

        public static void QueueError(this NotificationMessageBuilder builder, string message, Exception ex)
        {
            builder.Background("#B71C1C")
                       .HasMessage(message + "：" + ex.Message)
                       .Animates(true)
                       .Dismiss().WithDelay(5000)
                       .Dismiss().WithButton("详情", b => CommonDialog.ShowErrorDialogAsync(ex))
                       .Queue();
        }
        public static void QueueError(this NotificationMessageBuilder builder, string message)
        {
            builder.Background("#B71C1C")
                       .HasMessage(message)
                       .Animates(true)
                       .Dismiss().WithDelay(5000)
                       .Queue();
        }

        public static async Task<NotificationMessageBuilder> CreateMessageAsync(this FrameworkElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            await element.WaitForLoadedAsync();
            return CreateMessage(element);
        }
        public static NotificationMessageBuilder CreateMessage(this FrameworkElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            //if(!element.IsLoaded)
            //{
            //    void ElementLoaded(object sender,EventArgs e)
            //    {
            //        element.Loaded -= ElementLoaded;
            //        CreateMessage(element);
            //    }
            //    element.Loaded += ElementLoaded;
            //    return;
            //}
            var window = Window.GetWindow(element);
            if (window == null)
            {
                throw new Exception("找不到元素的窗口");
            }
            if (!(window.Content is Grid))
            {
                throw new Exception("窗口的内容不是Grid");
            }
            Grid grid = window.Content as Grid;

            NotificationMessageContainer container;
            if (grid.Children.OfType<NotificationMessageContainer>().Any())
            {
                container = grid.Children.OfType<NotificationMessageContainer>().First();
            }
            else
            {
                container = new NotificationMessageContainer
                {
                    Margin = new Thickness(-grid.Margin.Left, -grid.Margin.Top, -grid.Margin.Right, -grid.Margin.Bottom),
                    Width = 360,
                    Manager = new NotificationMessageManager()
                };
                Grid.SetRowSpan(container, int.MaxValue);
                Grid.SetColumnSpan(container, int.MaxValue);
                grid.Children.Add(container);
            }
            return container.Manager.CreateMessage();
        }
    }
}