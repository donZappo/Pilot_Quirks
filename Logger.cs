using HBS.Logging;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Pilot_Quirks
{
    public class Logger {

        private static StreamWriter LogStream;
        private static string LogFile;
        private readonly ILog HBSLogger;

        public Logger(string modDir, string logName) {
            if (LogFile == null) {
                LogFile = Path.Combine(modDir, $"{logName}.log");
            }
            if (File.Exists(LogFile)) {
                File.Delete(LogFile);
            }

            LogStream = File.AppendText(LogFile);

            HBSLogger = HBS.Logging.Logger.GetLogger(Pre_Control.ModId);
        }

        public Logger() { }

        public void Debug(string message, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            if (Pre_Control.settings.Debug)
            {
                Log($"{memberName}:{sourceFilePath}:{sourceLineNumber} - {message}");
            }
        }

        public void Info(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            Log($"{memberName}:{sourceFilePath}:{sourceLineNumber} - {message}");
        }

        public void Warn(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            Log($"WARNING! {memberName}:{sourceFilePath}:{sourceLineNumber} - {message}");
            HBSLogger.LogAtLevel(LogLevel.Warning, $"<PILOTQUIRKS>: {message}");
        }

        public void Error(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            Log($"ERROR! {memberName}:{sourceFilePath}:{sourceLineNumber} - {message}");

            HBSLogger.LogAtLevel(LogLevel.Error, "<PILOTQUIRKS>: " + message);
        }

        public void Error(Exception e,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            Log($"ERROR! {memberName}:{sourceFilePath}:{sourceLineNumber} - {e.Message}\n{e}");

            HBSLogger.LogAtLevel(LogLevel.Error, "<PILOTQUIRKS>: " + e.Message, e);
        }

        private void Log(string message)
        {
            string now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            LogStream.WriteLine($"{now} - {message}");
            LogStream.Flush();
        }

    }
}
