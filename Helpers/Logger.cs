using System;
using System.IO;
using System.Diagnostics;

namespace DeskWarrior.Helpers
{
    public static class Logger
    {
        public static bool IsEnabled { get; set; } = true; // 기본값 활성화 (디버깅용)

        private static readonly string _logPath;
        private static readonly Stopwatch _sw = Stopwatch.StartNew();

        static Logger()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logDir = Path.Combine(appData, "DeskWarrior");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            
            _logPath = Path.Combine(logDir, "debug.log");
        }

        public static void Log(string message)
        {
            if (!IsEnabled) return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var elapsed = _sw.ElapsedMilliseconds;
                var logLine = $"[{timestamp}] (+{elapsed}ms) {message}{Environment.NewLine}";
                
                File.AppendAllText(_logPath, logLine);
            }
            catch
            {
                // 로깅 실패는 무시 (앱 동작에 영향 주지 않도록)
            }
        }

        public static void LogError(string message, Exception ex)
        {
            if (!IsEnabled) return;
            Log($"[ERROR] {message}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
