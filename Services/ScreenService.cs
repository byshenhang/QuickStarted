using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace QuickStarted.Services
{
    /// <summary>
    /// 屏幕服务实现
    /// </summary>
    public class ScreenService : IScreenService
    {
        // Win32 API 声明
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        // 常量定义
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002; // 获取最近的显示器

        // 结构体定义
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        /// <summary>
        /// 获取鼠标所在屏幕的工作区域
        /// </summary>
        /// <returns>工作区域矩形</returns>
        public Rect GetMouseScreenWorkArea()
        {
            // 获取鼠标位置
            GetCursorPos(out POINT mousePos);
            
            // 获取鼠标所在的显示器
            IntPtr hMonitor = MonitorFromPoint(mousePos, MONITOR_DEFAULTTONEAREST);
            
            // 获取显示器信息
            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);
            
            if (GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                var workArea = monitorInfo.rcWork;
                return new Rect(
                    workArea.Left,
                    workArea.Top,
                    workArea.Right - workArea.Left,
                    workArea.Bottom - workArea.Top
                );
            }
            
            // 如果获取失败，返回主屏幕工作区域
            return new Rect(
                SystemParameters.WorkArea.Left,
                SystemParameters.WorkArea.Top,
                SystemParameters.WorkArea.Width,
                SystemParameters.WorkArea.Height
            );
        }

        /// <summary>
        /// 获取当前屏幕信息
        /// </summary>
        /// <returns>屏幕信息</returns>
        public ScreenInfo GetCurrentScreenInfo()
        {
            // 获取鼠标位置
            GetCursorPos(out POINT mousePos);
            
            // 获取鼠标所在的显示器
            IntPtr hMonitor = MonitorFromPoint(mousePos, MONITOR_DEFAULTTONEAREST);
            
            // 获取显示器信息
            MONITORINFO monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);
            
            if (GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                var workArea = monitorInfo.rcWork;
                var bounds = monitorInfo.rcMonitor;
                
                return new ScreenInfo
                {
                    WorkingArea = new Rect(
                        workArea.Left,
                        workArea.Top,
                        workArea.Right - workArea.Left,
                        workArea.Bottom - workArea.Top
                    ),
                    Bounds = new Rect(
                        bounds.Left,
                        bounds.Top,
                        bounds.Right - bounds.Left,
                        bounds.Bottom - bounds.Top
                    )
                };
            }
            
            // 如果获取失败，返回主屏幕信息
            return new ScreenInfo
            {
                WorkingArea = new Rect(
                    SystemParameters.WorkArea.Left,
                    SystemParameters.WorkArea.Top,
                    SystemParameters.WorkArea.Width,
                    SystemParameters.WorkArea.Height
                ),
                Bounds = new Rect(
                    SystemParameters.VirtualScreenLeft,
                    SystemParameters.VirtualScreenTop,
                    SystemParameters.VirtualScreenWidth,
                    SystemParameters.VirtualScreenHeight
                )
            };
        }
    }
}