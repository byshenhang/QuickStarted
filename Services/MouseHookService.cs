using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace QuickStarted.Services
{
    /// <summary>
    /// 全局鼠标钩子服务实现
    /// </summary>
    public class MouseHookService : IDisposable
    {
        // Win32 API 声明
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        // 常量定义
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;

        // 委托和变量
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly LowLevelMouseProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private bool _disposed = false;
        private uint _targetProcessId;
        private DateTime _lastWheelTime = DateTime.MinValue;
        private const int WHEEL_DEBOUNCE_MS = 150; // 防抖动间隔（毫秒）

        /// <summary>
        /// 鼠标滚轮事件
        /// </summary>
        public event EventHandler<MouseWheelEventArgs>? MouseWheel;

        /// <summary>
        /// 鼠标滚轮事件参数
        /// </summary>
        public class MouseWheelEventArgs : EventArgs
        {
            public int Delta { get; }
            
            public MouseWheelEventArgs(int delta)
            {
                Delta = delta;
            }
        }

        /// <summary>
        /// 构造函数 - 初始化鼠标钩子服务
        /// </summary>
        public MouseHookService()
        {
            _proc = HookCallback;
            _targetProcessId = (uint)Process.GetCurrentProcess().Id;
        }

        /// <summary>
        /// 启动鼠标钩子服务
        /// </summary>
        public void StartHook()
        {
            if (_hookID == IntPtr.Zero)
            {
                _hookID = SetHook(_proc);
            }
        }

        /// <summary>
        /// 停止鼠标钩子服务
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
        /// 设置低级鼠标钩子
        /// </summary>
        /// <param name="proc">钩子回调函数</param>
        /// <returns>钩子句柄</returns>
        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule?.ModuleName != null)
                {
                    return SetWindowsHookEx(WH_MOUSE_LL, proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 鼠标钩子回调函数 - 处理全局鼠标滚轮事件
        /// </summary>
        /// <param name="nCode">钩子代码</param>
        /// <param name="wParam">消息参数</param>
        /// <param name="lParam">消息参数</param>
        /// <returns>处理结果</returns>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEWHEEL)
            {
                // 检查当前前台窗口是否属于我们的进程
                IntPtr foregroundWindow = GetForegroundWindow();
                GetWindowThreadProcessId(foregroundWindow, out uint foregroundProcessId);
                
                if (foregroundProcessId == _targetProcessId)
                {
                    // 防抖动检查
                    DateTime currentTime = DateTime.Now;
                    if ((currentTime - _lastWheelTime).TotalMilliseconds >= WHEEL_DEBOUNCE_MS)
                    {
                        _lastWheelTime = currentTime;
                        
                        // 解析滚轮数据
                        var mouseData = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        int delta = (short)((mouseData.mouseData >> 16) & 0xFFFF);
                        
                        // 触发滚轮事件
                        MouseWheel?.Invoke(this, new MouseWheelEventArgs(delta));
                    }
                }
            }
            
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// 鼠标钩子数据结构
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        /// <summary>
        /// 点坐标结构
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
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
                // 清理非托管资源
                StopHook();
                
                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~MouseHookService()
        {
            Dispose(false);
        }
    }
}