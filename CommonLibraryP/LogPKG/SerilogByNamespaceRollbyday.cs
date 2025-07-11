using Serilog.Context;
using Serilog;
using Serilog.Configuration;
using Serilog.Expressions;
using Serilog.Events;
using System.Collections.Concurrent;

namespace CommonLibraryP.LogPKG
{
    public static class SerilogByNamespaceRollbyday
    {
        // 用於管理不同命名空間日誌輸出的緩存
        private static readonly ConcurrentDictionary<string, ILogger> LoggerCache = new();

        // 公共接口：記錄信息級別日誌
        public static void Information(string message)
        {
            LogEvent(message, LogEventLevel.Information);
        }

        // 公共接口：記錄警告級別日誌
        public static void Warning(string message)
        {
            LogEvent(message, LogEventLevel.Warning);
        }

        // 公共接口：記錄錯誤級別日誌
        public static void Error(string message, Exception ex)
        {
            LogEvent($"{message} - Exception: {ex.Message}", LogEventLevel.Error);
        }

        // 核心方法：根據命名空間動態記錄日誌
        private static void LogEvent(string message, LogEventLevel level)
        {
            // 獲取調用者的命名空間
            var callerType = new System.Diagnostics.StackTrace().GetFrame(2).GetMethod().DeclaringType;
            var namespaceName = callerType != null ? callerType.Namespace.Split(".").LastOrDefault() : "General";

            // 獲取或創建對應的 Logger
            var logger = LoggerCache.GetOrAdd(namespaceName, ns =>
            {
                var sanitizedNamespace = ns.Replace('.', '_'); // 替換非法字符
                return new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.Async(x=>x.File(
                        path: $"logs/{sanitizedNamespace}/{sanitizedNamespace}_.txt",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
                        retainedFileCountLimit: 7
                    ))
                    .CreateLogger();
            });

            // 實際寫入日誌
            logger.Write(level, message);
        }

        // 確保所有日誌在應用退出時刷新到文件
        public static void Dispose()
        {
            foreach (var logger in LoggerCache.Values)
            {
                (logger as IDisposable)?.Dispose();
            }
        }
    }

}
