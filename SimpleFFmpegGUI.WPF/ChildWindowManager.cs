using Microsoft.Extensions.DependencyInjection;
using SimpleFFmpegGUI.WPF.Views;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SimpleFFmpegGUI.WPF
{
    public sealed class ChildWindowManager
    {
        private readonly Window owner;
        private readonly IServiceProvider serviceProvider;
        private readonly Dictionary<Type, Window> modelessWindows = new();

        public ChildWindowManager(Window owner, IServiceProvider serviceProvider)
        {
            this.owner = owner;
            this.serviceProvider = serviceProvider;
        }

        public TView Show<TView>(Action<TView> initialize = null)
            where TView : UserControl
        {
            return (TView)Show(typeof(TView), initialize == null ? null : view => initialize((TView)view), false);
        }

        public UserControl Show(Type viewType, Action<UserControl> initialize = null, bool modal = false)
        {
            if (modal)
            {
                return ShowModal(viewType, initialize);
            }

            if (modelessWindows.TryGetValue(viewType, out Window existingWindow))
            {
                var existing = (ViewWindow)existingWindow;
                initialize?.Invoke(existing.View);
                existingWindow.Activate();
                return existing.View;
            }

            var view = serviceProvider.GetRequiredService(viewType) as UserControl;
            initialize?.Invoke(view);
            var window = new ViewWindow(view, ViewHelper.GetTitle(viewType), owner, 1000, 720);
            modelessWindows.Add(viewType, window);
            window.Closed += (_, _) => modelessWindows.Remove(viewType);
            window.Show();
            return view;
        }

        public void ShowWindow<TWindow>()
            where TWindow : Window
        {
            ShowWindow(typeof(TWindow));
        }

        public void ShowWindow(Type type)
        {
            if (modelessWindows.TryGetValue(type, out Window existingWindow))
            {
                existingWindow.Activate();
                return;
            }

            var window = serviceProvider.GetRequiredService(type) as Window;
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ViewWindow.LimitInitialSize(window, owner);
            modelessWindows.Add(type, window);
            window.Closed += (_, _) => modelessWindows.Remove(type);
            window.Show();
        }

        public TView ShowModal<TView>(Action<TView> initialize = null)
            where TView : UserControl
        {
            return (TView)ShowModal(typeof(TView), initialize == null ? null : view => initialize((TView)view));
        }

        public UserControl ShowModal(Type viewType, Action<UserControl> initialize = null)
        {
            var view = serviceProvider.GetRequiredService(viewType) as UserControl;
            initialize?.Invoke(view);
            var window = new ViewWindow(view, ViewHelper.GetTitle(viewType), owner, 1000, 720);
            window.ShowDialog();
            return view;
        }

        public void CloseAll()
        {
            foreach (Window window in new List<Window>(modelessWindows.Values))
            {
                window.Close();
            }
            modelessWindows.Clear();
        }
    }
}
