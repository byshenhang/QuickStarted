using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuickStarted.Services;
using QuickStarted.ViewModels;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace QuickStarted
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private ITrayService? _trayService;

        /// <summary>
        /// 应用程序启动时的初始化
        /// </summary>
        /// <param name="e">启动事件参数</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // 创建主机并配置依赖注入
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // 注册服务
                    services.AddSingleton<ILogService, LogService>();
                    services.AddSingleton<IWindowHookService, WindowHookService>();
                    services.AddSingleton<IScreenService, ScreenService>();
                    services.AddSingleton<IDataService, DataService>();
                    services.AddSingleton<ITrayService, TrayService>();
                    
                    // 注册视图模型
                    services.AddSingleton<MainViewModel>();
                    
                    // 注册窗口
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            // 启动主机
            _host.Start();

            // 获取主窗口、视图模型和托盘服务
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            var viewModel = _host.Services.GetRequiredService<MainViewModel>();
            _trayService = _host.Services.GetRequiredService<ITrayService>();
            
            // 初始化托盘服务
            _trayService.Initialize();
            _trayService.ShowWindow += (s, e) => 
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            };
            _trayService.ExitApplication += (s, e) => 
            {
                Shutdown();
            };
            
            // 设置视图模型并显示窗口
            mainWindow.SetViewModel(viewModel);
            mainWindow.Show();
            
            // 显示托盘图标
            _trayService.Show();
            
            base.OnStartup(e);
        }

        /// <summary>
        /// 应用程序退出时的清理
        /// </summary>
        /// <param name="e">退出事件参数</param>
        protected override void OnExit(ExitEventArgs e)
        {
            // 清理托盘服务
            _trayService?.Dispose();
            
            // 停止并释放主机
            _host?.StopAsync().Wait();
            _host?.Dispose();
            
            base.OnExit(e);
        }
    }
}
