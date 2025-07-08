using System;

namespace QuickStarted.Services
{
    /// <summary>
    /// 系统托盘服务接口
    /// </summary>
    public interface ITrayService : IDisposable
    {
        /// <summary>
        /// 显示窗口事件
        /// </summary>
        event EventHandler? ShowWindow;
        
        /// <summary>
        /// 退出应用程序事件
        /// </summary>
        event EventHandler? ExitApplication;
        
        /// <summary>
        /// 初始化托盘图标
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 显示托盘图标
        /// </summary>
        void Show();
        
        /// <summary>
        /// 隐藏托盘图标
        /// </summary>
        void Hide();
        
        /// <summary>
        /// 设置托盘图标提示文本
        /// </summary>
        /// <param name="text">提示文本</param>
        void SetTooltipText(string text);
    }
}