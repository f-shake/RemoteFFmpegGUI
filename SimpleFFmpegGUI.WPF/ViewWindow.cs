using iNKORE.UI.WPF.Modern.Controls.Helpers;
using SimpleFFmpegGUI.WPF.Views;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SimpleFFmpegGUI.WPF
{
    public sealed class ViewWindow : Window
    {
        private const double OwnerMargin = 32;
        private readonly UserControl view;
        private readonly ICloseableView closeableView;

        public ViewWindow(UserControl view, string title, Window owner, double width, double height)
        {
            this.view = view;
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
            Title = title;
            Width = width;
            Height = height;
            LimitInitialSize(this, owner, 640, 400);
            Content = view;

            closeableView = view as ICloseableView;
            if (closeableView != null)
            {
                closeableView.RequestToClose += CloseableView_RequestToClose;
            }

            Closed += (_, _) =>
            {
                if (closeableView != null)
                {
                    closeableView.RequestToClose -= CloseableView_RequestToClose;
                }
                (view as IDisposable)?.Dispose();
            };

            WindowHelper.SetUseModernWindowStyle(this, true);
        }

        /// <summary>
        /// 将窗口初始尺寸限制在属主窗口范围内（含最小尺寸下限），避免窗口比属主窗口还大
        /// </summary>
        public static void LimitInitialSize(Window window, Window owner, double minWidth = 0, double minHeight = 0)
        {
            double ownerWidth = owner.ActualWidth > 0 ? owner.ActualWidth : owner.Width;
            double ownerHeight = owner.ActualHeight > 0 ? owner.ActualHeight : owner.Height;
            double availableWidth = Math.Max(1, ownerWidth - OwnerMargin * 2);
            double availableHeight = Math.Max(1, ownerHeight - OwnerMargin * 2);

            window.Width = Math.Min(window.Width, availableWidth);
            window.Height = Math.Min(window.Height, availableHeight);
            window.MinWidth = Math.Min(Math.Max(window.MinWidth, minWidth), window.Width);
            window.MinHeight = Math.Min(Math.Max(window.MinHeight, minHeight), window.Height);
        }

        private void CloseableView_RequestToClose(object sender, EventArgs e)
        {
            Close();
        }
    }
}
