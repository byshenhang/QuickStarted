using QuickStarted.ViewModels;
using System.Windows;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace QuickStarted
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;

        /// <summary>
        /// 默认构造函数 - XAML需要
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // 订阅键盘事件（仅保留ESC键）
            KeyDown += MainWindow_KeyDown;
        }

        /// <summary>
        /// 设置视图模型
        /// </summary>
        /// <param name="viewModel">主窗口视图模型</param>
        public void SetViewModel(MainViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // 设置窗口引用并启动服务
            _viewModel.SetMainWindow(this);
            _viewModel.StartServices();
        }



        /// <summary>
        /// 窗口键盘事件处理 - 处理ESC键隐藏窗口
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">键盘事件参数</param>
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            _viewModel?.HandleKeyPress(e.Key);
        }



        /// <summary>
        /// 窗口关闭事件处理 - 隐藏到托盘而不是退出
        /// </summary>
        /// <param name="e">事件参数</param>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // 取消关闭操作，隐藏窗口到托盘
            e.Cancel = true;
            Hide();
            
            base.OnClosing(e);
        }
        
        /// <summary>
        /// 窗口真正关闭时的清理 - 仅在应用程序退出时调用
        /// </summary>
        /// <param name="e">事件参数</param>
        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.StopServices();
            base.OnClosed(e);
        }
    }
}