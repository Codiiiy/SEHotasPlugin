using System;
using System.IO;

namespace SEPlugin
{
    class Debug
    {
        public static bool debugMode = true;
        public static void LogToDesktop(string message)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string logFile = Path.Combine(desktopPath, "log.txt");

            File.AppendAllText(logFile, message + Environment.NewLine);
        }
    }
}
