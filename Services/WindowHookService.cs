using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace QuickStarted.Services
{
    /// <summary>
    /// 窗口钩子服务实现
    /// </summary>
    public class WindowHookService : IWindowHookService, IDisposable
    {
        // Win32 API 声明
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // 常量定义
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int VK_OEM_3 = 0xC0; // 反引号键 (`~)
        private static readonly int LONG_PRESS_DURATION = 500; // 长按时间阈值（毫秒）

        // 委托和变量
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private DispatcherTimer? _winKeyTimer;
        private bool _isWinKeyPressed = false;
        private bool _disposed = false;

        /// <summary>
        /// 反引号键按下事件
        /// </summary>
        public event EventHandler? WinKeyPressed;

        /// <summary>
        /// 反引号键长按事件
        /// </summary>
        public event EventHandler? WinKeyLongPressed;

        /// <summary>
        /// 反引号键释放事件
        /// </summary>
        public event EventHandler? WinKeyReleased;

        /// <summary>
        /// 构造函数 - 初始化钩子服务
        /// </summary>
        public WindowHookService()
        {
            _proc = HookCallback;
            
            // 设置定时器
            _winKeyTimer = new DispatcherTimer();
            _winKeyTimer.Interval = TimeSpan.FromMilliseconds(LONG_PRESS_DURATION);
            _winKeyTimer.Tick += WinKeyTimer_Tick;
        }

        /// <summary>
        /// 启动钩子服务
        /// </summary>
        public void StartHook()
        {
            if (_hookID == IntPtr.Zero)
            {
                _hookID = SetHook(_proc);
            }
        }

        /// <summary>
        /// 停止钩子服务
        /// </summary>
        public void StopHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 设置低级键盘钩子
        /// </summary>
        /// <param name="proc">钩子回调函数</param>
        /// <returns>钩子句柄</returns>
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule?.ModuleName != null)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 键盘钩子回调函数 - 处理全局键盘事件并拦截Win键
        /// </summary>
        /// <param name="nCode">钩子代码</param>
        /// <param name="wParam">消息参数</param>
        /// <param name="lParam">消息参数</param>
        /// <returns>处理结果</returns>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                
                if (vkCode == VK_OEM_3)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN)
                    {
                        // 反引号键按下
                        if (!_isWinKeyPressed)
                        {
                            _isWinKeyPressed = true;
                            _winKeyTimer?.Start();
                            
                            // 立即触发按下事件
                            WinKeyPressed?.Invoke(this, EventArgs.Empty);
                        }
                        // 拦截反引号键事件，阻止其他程序处理
                        return (IntPtr)1;
                    }
                    else if (wParam == (IntPtr)WM_KEYUP)
                    {
                        // 反引号键释放
                        _isWinKeyPressed = false;
                        _winKeyTimer?.Stop();
                        
                        // 触发反引号键释放事件
                        WinKeyReleased?.Invoke(this, EventArgs.Empty);
                        
                        // 拦截反引号键事件，阻止其他程序处理
                        return (IntPtr)1;
                    }
                }
            }
            
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// 反引号键长按定时器事件处理 - 触发长按事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void WinKeyTimer_Tick(object? sender, EventArgs e)
        {
            _winKeyTimer?.Stop();
            
            if (_isWinKeyPressed)
            {
                // 触发反引号键长按事件
                WinKeyLongPressed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的具体实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 清理托管资源
                    _winKeyTimer?.Stop();
                    _winKeyTimer = null;
                }
                
                // 清理非托管资源
                StopHook();
                
                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~WindowHookService()
        {
            Dispose(false);
        }
    }
}