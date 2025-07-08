using System;

namespace QuickStarted.Services
{
    /// <summary>
    /// 日志服务接口
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void LogInfo(string message);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void LogWarning(string message);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常信息</param>
        void LogError(string message, Exception? exception = null);

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void LogDebug(string message);
    }
}