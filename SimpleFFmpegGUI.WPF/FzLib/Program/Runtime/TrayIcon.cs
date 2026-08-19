using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace SimpleFFmpegGUI.WPF.FzLib.Program.Runtime
{
    public class TrayIcon : IDisposable
    {
        private NotifyIcon trayIcon = new NotifyIcon();

        private TrayIcon()
        {
            trayIcon.MouseClick += (p1, p2) =>
            {
                if (p2.Button == MouseButtons.Left)
                {
                    MouseLeftClick?.Invoke(p1, p2);
                }
                else if (p2.Button == MouseButtons.Right)
                {
                    MouseRightClick?.Invoke(p1, p2);
                }
            };
        }

        public TrayIcon(System.Drawing.Icon icon, string text) : this()
        {
            trayIcon.Text = text;
            trayIcon.Icon = icon;
        }

        public TrayIcon(System.Drawing.Icon icon, string text, MouseEventHandler mouseClick) : this(icon, text)
        {
            if (mouseClick != null)
            {
                trayIcon.MouseClick += mouseClick;
            }
        }

        public TrayIcon(System.Drawing.Icon icon, string text, Action mouseLeftClick, Action mouseRightClick) : this(icon, text)
        {
            if (mouseLeftClick != null || mouseRightClick != null)
            {
                trayIcon.MouseClick += (p1, p2) =>
                {
                    if (p2.Button == MouseButtons.Left)
                    {
                        mouseLeftClick?.Invoke();
                    }
                    else if (p2.Button == MouseButtons.Right)
                    {
                        mouseRightClick?.Invoke();
                    }
                };
            }
        }

        public bool ReShowWhenDisplayChanged
        {
            get => reShowWhenDpiChanged;
            set
            {
                if (value == reShowWhenDpiChanged)
                {
                    return;
                }

                reShowWhenDpiChanged = value;
                if (value)
                {
                    Microsoft.Win32.SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;
                }
                else
                {
                    Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
                }
            }
        }

        private void DisplaySettingsChanged(object sender, EventArgs e)
        {
            Hide();
            Show();
        }

        private bool reShowWhenDpiChanged = false;

        public TrayIcon(System.Drawing.Icon icon, string text, Action mouseLeftClick, Dictionary<string, Action> mouseRightClickMenu) : this(icon, text)
        {
            if (mouseLeftClick != null)
            {
                trayIcon.MouseClick += (p1, p2) =>
                {
                    if (p2.Button == MouseButtons.Left)
                    {
                        mouseLeftClick?.Invoke();
                    }
                };
            }

            if (mouseRightClickMenu != null && mouseRightClickMenu.Count != 0)
            {
                AddContextMenuStripItems(mouseRightClickMenu);
            }
        }

        public void AddContextMenuStripItems(Dictionary<string, Action> mouseRightClickMenu)
        {
            if (mouseRightClickMenu == null)
            {
                throw new ArgumentNullException();
            }
            if (mouseRightClickMenu.Count == 0)
            {
                return;
            }
            if (trayIcon.ContextMenuStrip == null)
            {
                trayIcon.ContextMenuStrip = new ContextMenuStrip();
            }
            foreach (var item in mouseRightClickMenu)
            {
                trayIcon.ContextMenuStrip.Items.Add(item.Key, null, new EventHandler((p1, p2) => item.Value()));
            }
        }

        public void AddContextMenuStripItem(string text, Action action)
        {
            if (text == null || action == null)
            {
                throw new ArgumentNullException();
            }
            if (trayIcon.ContextMenuStrip == null)
            {
                trayIcon.ContextMenuStrip = new ContextMenuStrip();
            }
            trayIcon.ContextMenuStrip.Items.Add(text, null, new EventHandler((p1, p2) => action()));
        }

        public void InsertContextMenuStripItem(string text, Action action, int index)
        {
            if (text == null || action == null)
            {
                throw new ArgumentNullException();
            }
            if (trayIcon.ContextMenuStrip == null)
            {
                trayIcon.ContextMenuStrip = new ContextMenuStrip();
            }
            var item = new ToolStripMenuItem(text, null, new EventHandler((p1, p2) => action()));
            trayIcon.ContextMenuStrip.Items.Insert(index, item);
        }

        public void DeleteContextMenuStripItem(string text)
        {
            trayIcon.ContextMenuStrip.Items.RemoveByKey(text);
        }

        public void ClearContextMenuStripItems()
        {
            if (trayIcon.ContextMenuStrip != null)
            {
                trayIcon.ContextMenuStrip.Items.Clear();
            }
        }

        public ContextMenuStrip ContextMenuStrip => trayIcon.ContextMenuStrip;

        public void ClickToOpenOrHideWindow(Window window)
        {
            MouseLeftClick += (p1, p2) =>
            {
                if (window.Visibility != Visibility.Visible)
                {
                    window.Show();
                    window.Activate();
                }
                else
                {
                    window.Hide();
                }
            };
        }

        public event MouseEventHandler MouseLeftClick;

        public event MouseEventHandler MouseRightClick;

        public void ShowMessage(string message, int ms = 2000)
        {
            trayIcon.BalloonTipText = message;
            trayIcon.ShowBalloonTip(ms);
        }

        public void Show()
        {
            trayIcon.Visible = true;
        }

        public void Hide()
        {
            trayIcon.Visible = false;
        }

        public void Dispose()
        {
            trayIcon.Dispose();
        }
    }
}