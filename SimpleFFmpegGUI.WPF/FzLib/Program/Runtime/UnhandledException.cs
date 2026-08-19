using SimpleFFmpegGUI.WPF.FzLib.Program;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleFFmpegGUI.WPF.FzLib.Program.Runtime
{
    public class WPFUnhandledExceptionCatcher : UnhandledExceptionCatcher
    {
        public static WPFUnhandledExceptionCatcher RegistAll()
        {
            WPFUnhandledExceptionCatcher catcher = new WPFUnhandledExceptionCatcher();
            catcher.RegisterTaskCatcher();
            catcher.RegisterThreadsCatcher();
            catcher.RegisterUICatcher();
            return catcher;
        }

        public void RegisterUICatcher()
        {
            Application.Current.DispatcherUnhandledException += (p1, p2) =>//UI
            {
                RaiseEvent(p1, p2.Exception, ExceptionSource.UI);
                p2.Handled = true;
            };
        }

    }

}