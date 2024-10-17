using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppWrapper
{
    public static class DebugLogger
    {
        private static object _fileLock = new object();

        public static bool IsLogging { get; set; }

        public static bool IsDetailedLogging { get; set; }

        public static string PathToLogFile { get; set; }

        private static StreamWriter LogFile { get; set; }

        public static void Init(string path)
        {
            PathToLogFile = path;
            IsLogging = !string.IsNullOrEmpty(path);

            CloseLogFile(); // make sure existing log file is closed before initializing new streamwriter
            if (IsLogging)
            {
                LogFile = File.AppendText(path);
            }
        }

        public static void CloseLogFile()
        {
            try
            {
                if (LogFile != null)
                {
                    LogFile.Close();
                }

                LogFile = null;
            }
            catch (Exception)
            {
            }
        }

        public static void WriteLine(string message)
        {
            try
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(message); // always print message to debug console if compiled for debugging
#endif

                if (!IsLogging || string.IsNullOrEmpty(PathToLogFile) || LogFile == null)
                {
                    return;
                }

                lock (_fileLock)
                {
                    LogFile.WriteLine(message);
                    LogFile.Flush();
                }
            }
            catch (Exception)
            {
            }
        }

        public static void DetailedWriteLine(string message)
        {
            if (!IsDetailedLogging)
            {
                return;
            }

            WriteLine(message);
        }
    }
}
