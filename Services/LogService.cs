using System;
using System.Diagnostics;

namespace QuickStarted.Services
{
    /// <summary>
    /// 日志服务实现
    /// </summary>
    public class LogService : ILogService
    {
        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void LogInfo(string message)
        {
            var logMessage = $"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void LogWarning(string message)
        {
            var logMessage = $"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息</param>
        public void LogError(string message, Exception? exception = null)
        {
            var logMessage = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            if (exception != null)
            {
                logMessage += $"\n异常详情: {exception}";
            }
            Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void LogDebug(string message)
        {
            var logMessage = $"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            Debug.WriteLine(logMessage);
            Console.WriteLine(logMessage);
        }
    }
}