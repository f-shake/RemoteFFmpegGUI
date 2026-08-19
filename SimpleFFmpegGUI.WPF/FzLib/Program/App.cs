using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleFFmpegGUI.WPF.FzLib.Program
{
    public static class App
    {
        static App()
        {
            ProgramFilePath = Process.GetCurrentProcess().MainModule.FileName;
            ProgramDirectoryPath = Path.GetDirectoryName(ProgramFilePath);
            ProgramName = AppDomain.CurrentDomain.FriendlyName.Split('.')[0];
        }
        public static string WorkingDirectory
        {
            get => Directory.GetCurrentDirectory();
            set => Directory.SetCurrentDirectory(value);
        }

        public static void SetWorkingDirectoryToAppPath()
        {

            WorkingDirectory = ProgramDirectoryPath;
        }

        public static string ProgramFilePath { get; }
        public static string ProgramDirectoryPath { get; }

        public static string ProgramName { get; }

        public static void Restart(Action shutdown, int delaySeconds = 1)
        {
            ProcessStartInfo Info = new ProcessStartInfo();
            Info.Arguments = $"/C choice /C Y /N /D Y /T {delaySeconds.ToString()} & START \"\" \"{ ProgramFilePath}\"";
            Info.WindowStyle = ProcessWindowStyle.Hidden;
            Info.CreateNoWindow = true;
            Info.FileName = "cmd.exe";
            Process.Start(Info);
            shutdown();
        }
    }
}
