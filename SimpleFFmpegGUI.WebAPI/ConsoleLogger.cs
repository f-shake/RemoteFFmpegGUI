using System;

namespace SimpleFFmpegGUI.WebAPI
{
    public class ConsoleLogger
    {
        private static ConsoleLogger instance;

        private ConsoleLogger()
        {
            Logger.Log += Logger_Log;
        }

        public static void StartListen()
        {
            if (instance == null)
            {
                instance = new ConsoleLogger();
            }
        }

        private string lastLogContent = null;
        private void Logger_Log(object sender, LogEventArgs e)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            ConsoleColor color = e.Log.Type switch
            {
                'E' => ConsoleColor.Red,
                'D' => defaultColor,
                'I' => defaultColor,
                'W' => ConsoleColor.Yellow,
                'O' => ConsoleColor.Gray,
                _ => defaultColor
            };
            string type = e.Log.Type switch
            {
                'E' => "错误",
                'D' => "调试",
                'I' => "信息",
                'W' => "警告",
                'O' => "输出",
                _ => e.Log.Type.ToString().PadLeft(2)
            };
            string time = e.Log.Time.ToString("yyyy-MM-dd HH:mm:ss");

            if (lastLogContent != null && lastLogContent.StartsWith("frame=") && e.Log.Message.StartsWith("frame="))
            {
                ClearLine();
            }

            Console.Write(time);
            Console.Write("    \t");
            Console.ForegroundColor = color;
            Console.Write(type);
            Console.ForegroundColor = defaultColor;
            Console.Write("    \t");
            Console.Write(e.Log.Message);
            Console.WriteLine();


            lastLogContent = e.Log.Message;
        }
        static void ClearLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }
    }
}