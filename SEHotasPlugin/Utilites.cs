using System;
using System.IO;


namespace SEHotasPlugin
{
    class Logger
    {
        public static void LogToDesktop(string message)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string logFile = Path.Combine(desktopPath, "log.txt");

            File.AppendAllText(logFile, message + Environment.NewLine);
        }
    }
}
