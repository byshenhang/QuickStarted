using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Reflection;

namespace QuickStarted.Services
{
    /// <summary>
    /// 系统托盘服务实现
    /// </summary>
    public class TrayService : ITrayService
    {
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private bool _disposed = false;

        /// <summary>
        /// 显示窗口事件
        /// </summary>
        public event EventHandler? ShowWindow;
        
        /// <summary>
        /// 退出应用程序事件
        /// </summary>
        public event EventHandler? ExitApplication;

        /// <summary>
        /// 初始化托盘图标
        /// </summary>
        public void Initialize()
        {
            if (_notifyIcon != null)
                return;

            // 创建托盘图标
            _notifyIcon = new NotifyIcon
            {
                Icon = GetApplicationIcon(),
                Text = "QuickStarted - 快速启动工具",
                Visible = false
            };

            // 创建右键菜单
            CreateContextMenu();
            
            // 绑定事件
            _notifyIcon.DoubleClick += OnTrayIconDoubleClick;
            _notifyIcon.ContextMenuStrip = _contextMenu;
        }

        /// <summary>
        /// 显示托盘图标
        /// </summary>
        public void Show()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
            }
        }

        /// <summary>
        /// 隐藏托盘图标
        /// </summary>
        public void Hide()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        /// <summary>
        /// 设置托盘图标提示文本
        /// </summary>
        /// <param name="text">提示文本</param>
        public void SetTooltipText(string text)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = text;
            }
        }

        /// <summary>
        /// 创建右键菜单
        /// </summary>
        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            
            // 显示窗口菜单项
            var showMenuItem = new ToolStripMenuItem("显示窗口")
            {
                Font = new Font(_contextMenu.Font, FontStyle.Bold)
            };
            showMenuItem.Click += OnShowMenuItemClick;
            _contextMenu.Items.Add(showMenuItem);
            
            // 分隔线
            _contextMenu.Items.Add(new ToolStripSeparator());
            
            // 退出菜单项
            var exitMenuItem = new ToolStripMenuItem("退出")
            {
                Image = SystemIcons.Application.ToBitmap()
            };
            exitMenuItem.Click += OnExitMenuItemClick;
            _contextMenu.Items.Add(exitMenuItem);
        }

        /// <summary>
        /// 获取应用程序图标
        /// </summary>
        /// <returns>应用程序图标</returns>
        private Icon GetApplicationIcon()
        {
            try
            {
                // 尝试从程序集获取图标
                var assembly = Assembly.GetExecutingAssembly();
                var iconStream = assembly.GetManifestResourceStream("QuickStarted.icon.ico");
                
                if (iconStream != null)
                {
                    return new Icon(iconStream);
                }
            }
            catch
            {
                // 忽略错误，使用默认图标
            }
            
            // 使用系统默认应用程序图标
            return SystemIcons.Application;
        }

        /// <summary>
        /// 托盘图标双击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void OnTrayIconDoubleClick(object? sender, EventArgs e)
        {
            ShowWindow?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 显示窗口菜单项点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void OnShowMenuItemClick(object? sender, EventArgs e)
        {
            ShowWindow?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 退出菜单项点击事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void OnExitMenuItemClick(object? sender, EventArgs e)
        {
            ExitApplication?.Invoke(this, EventArgs.Empty);
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
            if (!_disposed && disposing)
            {
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~TrayService()
        {
            Dispose(false);
        }
    }
}