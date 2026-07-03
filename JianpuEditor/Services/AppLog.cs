using System;
using System.IO;
using System.Text;

namespace JianpuEditor.Services
{
    public static class AppLog
    {
        private static readonly object SyncRoot = new object();
        private static string _logFilePath;

        public static string LogFilePath
        {
            get
            {
                EnsureLogPath();
                return _logFilePath;
            }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Exception(string context, Exception ex)
        {
            if (ex == null)
            {
                Write("ERROR", context);
                return;
            }

            var builder = new StringBuilder();
            builder.Append(context);
            builder.Append(": ");
            builder.Append(ex.GetType().FullName);
            builder.Append(" - ");
            builder.Append(ex.Message);
            builder.AppendLine();
            builder.Append(ex.StackTrace);
            if (ex.InnerException != null)
            {
                builder.AppendLine("--- Inner Exception ---");
                builder.Append(ex.InnerException.GetType().FullName);
                builder.Append(" - ");
                builder.AppendLine(ex.InnerException.Message);
                builder.Append(ex.InnerException.StackTrace);
            }

            Write("ERROR", builder.ToString());
        }

        private static void Write(string level, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            try
            {
                EnsureLogPath();
                var line = string.Format(
                    "[{0:yyyy-MM-dd HH:mm:ss.fff}] [{1}] {2}{3}",
                    DateTime.Now,
                    level,
                    message,
                    Environment.NewLine);
                lock (SyncRoot)
                {
                    File.AppendAllText(_logFilePath, line, Encoding.UTF8);
                }

                System.Diagnostics.Trace.WriteLine(line);
            }
            catch
            {
            }
        }

        private static void EnsureLogPath()
        {
            if (!string.IsNullOrWhiteSpace(_logFilePath))
            {
                return;
            }

            var baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JianpuEditor",
                "logs");
            Directory.CreateDirectory(baseDir);
            _logFilePath = Path.Combine(baseDir, "jianpu-editor.log");
        }
    }
}
