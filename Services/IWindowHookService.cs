using System;

namespace QuickStarted.Services
{
    /// <summary>
    /// 窗口钩子服务接口
    /// </summary>
    public interface IWindowHookService : IDisposable
    {
        /// <summary>
        /// 反引号键按下事件
        /// </summary>
        event EventHandler? WinKeyPressed;

        /// <summary>
        /// 反引号键长按事件
        /// </summary>
        event EventHandler? WinKeyLongPressed;

        /// <summary>
        /// 反引号键释放事件
        /// </summary>
        event EventHandler? WinKeyReleased;

        /// <summary>
        /// 启动钩子
        /// </summary>
        void StartHook();

        /// <summary>
        /// 停止钩子
        /// </summary>
        void StopHook();
    }
}