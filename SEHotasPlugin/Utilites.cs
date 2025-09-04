using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SEHotasPlugin
{
    class Debug
    {
        public static bool debugMode = false;
        public static void Log(string message)
        {
            if (debugMode)
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logDir = Path.Combine(appDataPath, @"SpaceEngineers\Plugins\SEHotasPlugin\Log");

                Directory.CreateDirectory(logDir);

                string logFile = Path.Combine(logDir, "log.txt");

                File.AppendAllText(logFile, message + Environment.NewLine);
            }
        }
        
    }
}