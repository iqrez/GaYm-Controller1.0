using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WootMouseRemap
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    public static class Logger
    {
        private static readonly object _lock = new();
        private static string _logDirectory = "Logs";
        private static string _currentLogFile = "woot.log";
        private const long MaxSize = 5 * 1024 * 1024; // Increased to 5MB
        private const int MaxBackupFiles = 10;
        private static LogLevel _minLogLevel = LogLevel.Info;

        public static void Init(string logDirectory = "Logs", LogLevel minLevel = LogLevel.Info)
        {
            _logDirectory = logDirectory;
            _minLogLevel = minLevel;
            _currentLogFile = Path.Combine(_logDirectory, "woot.log");
            
            try
            {
                Directory.CreateDirectory(_logDirectory);
            }
            catch (Exception ex)
            {
                // Fallback to current directory if we can't create logs directory
                _currentLogFile = "woot.log";
                WriteToEventLog($"Failed to create log directory: {ex.Message}");
            }
        }

        public static void Debug(string msg, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            => WriteWithContext(LogLevel.Debug, msg, caller, file, line);

        public static void Info(string msg, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            => WriteWithContext(LogLevel.Info, msg, caller, file, line);

        public static void Warn(string msg, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
            => WriteWithContext(LogLevel.Warning, msg, caller, file, line);

        public static void Error(string msg, Exception? ex = null, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var fullMsg = ex != null ? $"{msg}\nException: {ex}" : msg;
            WriteWithContext(LogLevel.Error, fullMsg, caller, file, line);
        }

        public static void Critical(string msg, Exception? ex = null, [CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            var fullMsg = ex != null ? $"{msg}\nException: {ex}" : msg;
            WriteWithContext(LogLevel.Critical, fullMsg, caller, file, line);
            
            // Also write to Windows Event Log for critical errors
            WriteToEventLog($"CRITICAL: {fullMsg}");
        }

        private static void WriteWithContext(LogLevel level, string msg, string caller, string file, int line)
        {
            if (level < _minLogLevel) return;

            var fileName = Path.GetFileNameWithoutExtension(file);
            var context = $"{fileName}.{caller}:{line}";
            Write(GetLevelString(level), msg, context);
        }

        private static string GetLevelString(LogLevel level) => level switch
        {
            LogLevel.Debug => "DEBG",
            LogLevel.Info => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERR ",
            LogLevel.Critical => "CRIT",
            _ => "UNKN"
        };

        private static void Write(string lvl, string msg, string context = "")
        {
            lock (_lock)
            {
                try
                {
                    RotateLogIfNeeded();
                    
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var contextStr = !string.IsNullOrEmpty(context) ? $" [{context}]" : "";
                    var logLine = $"{timestamp} [{lvl}]{contextStr} {msg}\n";
                    
                    File.AppendAllText(_currentLogFile, logLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    // Last resort: try to write to event log
                    WriteToEventLog($"Logger failed: {ex.Message}. Original message: {msg}");
                }
            }
        }

        private static void RotateLogIfNeeded()
        {
            if (!File.Exists(_currentLogFile)) return;
            
            var fileInfo = new FileInfo(_currentLogFile);
            if (fileInfo.Length <= MaxSize) return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFile = Path.Combine(_logDirectory, $"woot_{timestamp}.log");
                
                File.Move(_currentLogFile, backupFile);
                CleanupOldLogs();
            }
            catch (Exception ex)
            {
                WriteToEventLog($"Log rotation failed: {ex.Message}");
            }
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "woot_*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(MaxBackupFiles)
                    .ToArray();

                foreach (var file in logFiles)
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                WriteToEventLog($"Log cleanup failed: {ex.Message}");
            }
        }

        private static void WriteToEventLog(string message)
        {
            try
            {
                using var eventLog = new EventLog("Application");
                eventLog.Source = "WootMouseRemap";
                eventLog.WriteEntry(message, EventLogEntryType.Warning);
            }
            catch
            {
                // If we can't even write to event log, there's nothing more we can do
            }
        }

        public static void SetLogLevel(LogLevel level) => _minLogLevel = level;
        
        public static string GetCurrentLogFile() => _currentLogFile;
        
        public static long GetCurrentLogSize()
        {
            try
            {
                return File.Exists(_currentLogFile) ? new FileInfo(_currentLogFile).Length : 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
