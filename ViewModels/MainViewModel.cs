using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickStarted.Services;
using QuickStarted.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using System.Windows.Media.Animation; // 新增：Storyboard 控制
using System.Threading;               // 新增：用于轻量并发控制

namespace QuickStarted.ViewModels
{
    /// <summary>
    /// 主窗口视图模型
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly IWindowHookService _windowHookService;
        private readonly IScreenService _screenService;
        private readonly IDataService _dataService;
        private readonly ILogService _logService;
        private readonly MouseHookService _mouseHookService;
        private MainWindow? _mainWindow;

        /// <summary>
        /// 子 ViewModel
        /// </summary>
        public ShortcutKeysViewModel ShortcutKeysVM { get; } = new();
        public NotesViewModel NotesVM { get; } = new();
        public VideosViewModel VideosVM { get; } = new();

        [ObservableProperty]
        private string _currentProgram = string.Empty;

        [ObservableProperty]
        private bool _isShortcutKeysVisible = true;

        [ObservableProperty]
        private bool _isNotesVisible = false;

        [ObservableProperty]
        private bool _isVideosVisible = false;

        [ObservableProperty]
        private string _currentHookWindowTitle = string.Empty;

        [ObservableProperty]
        private string _currentHookProcessName = string.Empty;

        /// <summary>
        /// 页面标签是否可见（由子 VM 控制，此处保留开关入口）
        /// </summary>
        [ObservableProperty]
        private bool _isPageTabsVisible = true;

        /// <summary>
        /// 数据是否已加载标志位
        /// </summary>
        private bool _isDataLoaded = false;

        /// <summary>
        /// 上次加载数据的程序名称
        /// </summary>
        private string _lastLoadedProgram = string.Empty;

        // ================== 新增：节流与并发控制 ==================
        /// <summary>
        /// 距离上次显示的最小间隔，避免频繁触发导致动画抖动
        /// </summary>
        private readonly TimeSpan _activationThrottle = TimeSpan.FromMilliseconds(400);

        /// <summary>
        /// 最近一次尝试显示的时间（UTC）
        /// </summary>
        private DateTime _lastShowAttemptUtc = DateTime.MinValue;

        /// <summary>
        /// 避免 OnWinKeyLongPressed 并发重入的简单互斥
        /// </summary>
        private int _activationHandling = 0;
        // =======================================================

        /// <summary>
        /// 构造函数
        /// </summary>
        public MainViewModel(IWindowHookService windowHookService, IScreenService screenService, IDataService dataService, ILogService logService)
        {
            _windowHookService = windowHookService;
            _screenService = screenService;
            _dataService = dataService;
            _logService = logService;
            _mouseHookService = new MouseHookService();
            
            // 订阅鼠标滚轮事件
            _mouseHookService.MouseWheel += OnMouseWheel;
            
            _logService.LogInfo("MainViewModel 初始化完成");
        }

        /// <summary>
        /// 设置主窗口引用
        /// </summary>
        public void SetMainWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        public void StartServices()
        {
            _windowHookService.WinKeyLongPressed += OnWinKeyLongPressed;
            _windowHookService.StartHook();
            
            // 启动全局鼠标钩子
            _mouseHookService.StartHook();
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public void StopServices()
        {
            _windowHookService.WinKeyLongPressed -= OnWinKeyLongPressed;
            _windowHookService.StopHook();
            
            // 停止全局鼠标钩子
            _mouseHookService.MouseWheel -= OnMouseWheel;
            _mouseHookService.StopHook();
            _mouseHookService.Dispose();
        }

        /// <summary>
        /// 反引号键长按事件处理（含节流与并发保护）
        /// </summary>
        private async void OnWinKeyLongPressed(object? sender, EventArgs e)
        {
            // 简单节流：避免在极短时间内反复触发
            var nowUtc = DateTime.UtcNow;
            if (nowUtc - _lastShowAttemptUtc < _activationThrottle)
            {
                _logService.LogDebug("触发被节流，忽略本次显示请求。");
                return;
            }
            _lastShowAttemptUtc = nowUtc;

            // 防并发重入：仅允许一个处理流程执行
            if (Interlocked.Exchange(ref _activationHandling, 1) == 1)
            {
                _logService.LogDebug("已在处理显示请求，忽略并发触发。");
                return;
            }

            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    // 获取当前程序名称
                    var currentProgram = await GetCurrentProgramNameAsync();

                    // 只在第一次打开或程序切换时加载数据
                    if (!_isDataLoaded || _lastLoadedProgram != currentProgram)
                    {
                        await LoadCurrentProgramDataAsync();
                        _isDataLoaded = true;
                        _lastLoadedProgram = currentProgram;
                    }
                    ShowWindowWithFadeIn();
                });
            }
            catch (Exception ex)
            {
                _logService.LogError($"OnWinKeyLongPressed 处理异常: {ex.Message}", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _activationHandling, 0);
            }
        }

        /// <summary>
        /// 获取当前程序名称
        /// </summary>
        private async Task<string> GetCurrentProgramNameAsync()
        {
            try
            {
                // 确保程序映射配置已加载
                await _dataService.LoadProgramMappingAsync();
                
                // 获取当前活动窗口的进程名称
                var processName = GetActiveWindowTitle();
                
                // 根据进程名称匹配程序
                var matchedProgram = _dataService.GetMatchingProgram(processName);
                
                if (string.IsNullOrEmpty(matchedProgram))
                {
                    matchedProgram = "3dMax"; // 默认程序
                }
                
                return matchedProgram;
            }
            catch (Exception ex)
            {
                _logService.LogError($"获取当前程序名称失败: {ex.Message}", ex);
                return "3dMax"; // 返回默认程序
            }
        }

        /// <summary>
        /// 加载当前程序数据
        /// </summary>
        private async Task LoadCurrentProgramDataAsync()
        {
            _log_service_loginfo_safe("开始加载当前程序数据");
            
            try
            {
                // 首先确保程序映射配置已加载
                await _data_service_load_program_mapping_safe();
                
                // 获取当前活动窗口的进程名称
                var processName = GetActiveWindowTitle();
                _log_service_loginfo_safe($"获取到当前进程名称: '{processName}'");
                
                // 根据进程名称匹配程序
                var matchedProgram = _dataService.GetMatchingProgram(processName);
                _log_service_loginfo_safe($"匹配结果: '{matchedProgram}'");
                
                if (string.IsNullOrEmpty(matchedProgram))
                {
                    // 如果没有匹配到程序，使用默认程序（如3dMax）
                    matchedProgram = "3dMax";
                    _log_service_logwarning_safe($"未匹配到程序，使用默认程序: '{matchedProgram}'");
                }

                CurrentProgram = matchedProgram;
                _log_service_loginfo_safe($"设置当前程序为: '{matchedProgram}'");
                
                // 加载程序数据
                var programNotes = await _dataService.LoadProgramNotesAsync(matchedProgram);
                if (programNotes != null)
                {
                    _log_service_loginfo_safe("程序笔记数据加载成功，开始更新子视图模型");

                    // 更新快捷键页面
                    if (programNotes.ShortcutKeys?.Pages != null && programNotes.ShortcutKeys.Pages.Any())
                    {
                        ShortcutKeysVM.SetPages(programNotes.ShortcutKeys.Pages);
                        IsPageTabsVisible = true;
                        _log_service_loginfo_safe($"已加载 {programNotes.ShortcutKeys.Pages.Count} 个快捷键页面");
                    }
                    else if (programNotes.ShortcutKeys != null)
                    {
                        // 兼容旧版本数据格式：将 Data 视为默认页
                        var defaultPage = new ShortcutKeyPage
                        {
                            Index = 1,
                            Name = "Default",
                            Data = programNotes.ShortcutKeys.Data.ToList()
                        };
                        ShortcutKeysVM.SetPages(new[] { defaultPage });
                        IsPageTabsVisible = true;
                        _log_service_loginfo_safe("已转换旧版快捷键数据为分页格式");
                    }
                    else
                    {
                        // 无快捷键信息
                        ShortcutKeysVM.SetPages(Array.Empty<ShortcutKeyPage>());
                        IsPageTabsVisible = false;
                        _log_service_logwarning_safe("没有找到快捷键数据");
                    }

                    // 更新笔记分类（内部默认选择第一类与其第一个文件）
                    NotesVM.SetCategories(programNotes.NoteCategories);
                    _log_service_loginfo_safe($"已添加 {programNotes.NoteCategories.Count} 个笔记分类到笔记视图");
                }
                else
                {
                    _log_service_logerror_safe($"无法加载程序 '{matchedProgram}' 的笔记数据");
                    ShortcutKeysVM.SetPages(Array.Empty<ShortcutKeyPage>());
                    NotesVM.SetCategories(Array.Empty<NoteCategory>());
                }
                
                _log_service_loginfo_safe("程序数据加载完成");
            }
            catch (Exception ex)
            {
                _logService.LogError($"加载程序数据失败: {ex.Message}", ex);
                ShortcutKeysVM.SetPages(Array.Empty<ShortcutKeyPage>());
                NotesVM.SetCategories(Array.Empty<NoteCategory>());
            }
        }

        // 封装一下日志调用，防止空引用影响主流程（稳健性）
        private void _log_service_loginfo_safe(string msg) { try { _logService.LogInfo(msg); } catch { } }
        private void _log_service_logwarning_safe(string msg) { try { _logService.LogWarning(msg); } catch { } }
        private void _log_service_logerror_safe(string msg) { try { _logService.LogError(msg); } catch { } }
        private async Task _data_service_load_program_mapping_safe() { try { await _dataService.LoadProgramMappingAsync(); } catch (Exception ex) { _logService.LogError($"LoadProgramMapping 异常: {ex.Message}", ex); } }

        /// <summary>
        /// 获取当前活动窗口标题（返回进程名用于匹配）
        /// </summary>
        private string GetActiveWindowTitle()
        {
            _logService.LogDebug("开始获取当前活动窗口信息");
            
            try
            {
                // 获取当前前台窗口句柄
                IntPtr foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                {
                    _logService.LogWarning("无法获取前台窗口句柄");
                    return string.Empty;
                }
                
                _logService.LogDebug($"获取到前台窗口句柄: {foregroundWindow}");

                // 获取窗口标题长度
                int length = GetWindowTextLength(foregroundWindow);
                if (length == 0)
                {
                    _logService.LogWarning("窗口标题长度为0");
                    return string.Empty;
                }
                
                _logService.LogDebug($"窗口标题长度: {length}");

                // 获取窗口标题
                var builder = new System.Text.StringBuilder(length + 1);
                GetWindowText(foregroundWindow, builder, builder.Capacity);
                
                var title = builder.ToString();
                CurrentHookWindowTitle = title; // 更新当前Hook窗口标题
                _logService.LogDebug($"获取到窗口标题: '{title}'");
                
                // 获取进程名称
                var processName = GetActiveWindowProcessName(foregroundWindow);
                CurrentHookProcessName = processName;
                _logService.LogInfo($"获取到进程名称: '{processName}'");
                
                return processName; // 返回进程名称而不是窗口标题
            }
            catch (Exception ex)
            {
                _logService.LogError($"获取窗口信息失败: {ex.Message}", ex);
                CurrentHookWindowTitle = "获取失败";
                CurrentHookProcessName = "获取失败";
                return string.Empty;
            }
        }

        /// <summary>
        /// 获取指定窗口的进程名称
        /// </summary>
        private string GetActiveWindowProcessName(IntPtr hWnd)
        {
            try
            {
                GetWindowThreadProcessId(hWnd, out uint processId);
                using (var process = System.Diagnostics.Process.GetProcessById((int)processId))
                {
                    return process.ProcessName;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取进程名称失败: {ex.Message}");
                return "Unknown";
            }
        }

        // Win32 API
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// 显示窗口并淡入（含动画互斥与可控）
        /// </summary>
        private void ShowWindowWithFadeIn()
        {
            if (_mainWindow == null) return;

            // 获取当前屏幕信息
            var screenInfo = _screenService.GetCurrentScreenInfo();
            
            // 设置窗口位置和尺寸为工作区域（排除任务栏）
            _mainWindow.Left = screenInfo.WorkingArea.Left;
            _mainWindow.Top = screenInfo.WorkingArea.Top;
            _mainWindow.Width = screenInfo.WorkingArea.Width;
            _mainWindow.Height = screenInfo.WorkingArea.Height;

            // 若已可见且几乎不透明，则不重复播放淡入动画，避免跳动
            if (_mainWindow.IsVisible && _mainWindow.Opacity >= 0.99)
            {
                _logService.LogDebug("窗口已可见且处于不透明状态，仅激活窗口。");
                _mainWindow.Activate();
                _mainWindow.Focus();
                return;
            }

            // 先停止可能存在的淡出动画
            var fadeOut = _mainWindow.FindResource("FadeOutStoryboard") as Storyboard;
            fadeOut?.Stop(_mainWindow);

            // 确保起始透明度为 0，再开始淡入
            _mainWindow.Opacity = 0;
            _mainWindow.Show();
            _mainWindow.Activate();
            _mainWindow.Focus();

            // 以可控方式启动淡入，并替换现有动画时钟，防止跳帧
            var fadeIn = _mainWindow.FindResource("FadeInStoryboard") as Storyboard;
            if (fadeIn != null)
            {
                fadeIn.Stop(_mainWindow);
                fadeIn.Begin(_mainWindow, true); // isControllable = true
            }
            else
            {
                // 兜底：若动画资源缺失，直接设为不透明
                _mainWindow.Opacity = 1.0;
            }
        }

        /// <summary>
        /// 隐藏窗口（互斥淡入/淡出，确保完成后隐藏）
        /// </summary>
        public void HideWindow()
        {
            if (_mainWindow == null) return;

            // 停止可能存在的淡入动画
            var fadeIn = _mainWindow.FindResource("FadeInStoryboard") as Storyboard;
            fadeIn?.Stop(_mainWindow);

            var fadeOut = _mainWindow.FindResource("FadeOutStoryboard") as Storyboard;
            if (fadeOut != null)
            {
                fadeOut.Stop(_mainWindow);
                // 以可控方式开始淡出
                fadeOut.Completed += FadeOut_Completed_Once;
                fadeOut.Begin(_mainWindow, true);
            }
            else
            {
                _mainWindow.Hide();
            }
        }

        // 确保只处理一次 Completed，避免多次订阅导致的重复 Hide
        private void FadeOut_Completed_Once(object? sender, EventArgs e)
        {
            try
            {
                if (_mainWindow != null)
                {
                    _mainWindow.Hide();
                    _mainWindow.Opacity = 0; // 复位不透明度，下一次淡入更平滑
                }
            }
            finally
            {
                if (sender is Storyboard sb)
                {
                    sb.Completed -= FadeOut_Completed_Once;
                }
            }
        }

        /// <summary>
        /// 切换到快捷键视图
        /// </summary>
        [RelayCommand]
        private void ShowShortcutKeys()
        {
            IsShortcutKeysVisible = true;
            IsNotesVisible = false;
            IsVideosVisible = false;
        }

        /// <summary>
        /// 切换到笔记视图
        /// </summary>
        [RelayCommand]
        private void ShowNotes()
        {
            IsShortcutKeysVisible = false;
            IsNotesVisible = true;
            IsVideosVisible = false;
        }

        /// <summary>
        /// 切换到视频视图
        /// </summary>
        [RelayCommand]
        private void ShowVideos()
        {
            IsShortcutKeysVisible = false;
            IsNotesVisible = false;
            IsVideosVisible = true;
        }

        /// <summary>
        /// 处理键盘按键事件
        /// </summary>
        public void HandleKeyPress(System.Windows.Input.Key key)
        {
            switch (key)
            {
                case System.Windows.Input.Key.Escape:
                    HideWindow();
                    break;
            }
        }

        /// <summary>
        /// 全局鼠标滚轮事件处理 - 仅在快捷键视图可见时翻页
        /// </summary>
        private void OnMouseWheel(object? sender, MouseHookService.MouseWheelEventArgs e)
        {
            if (IsShortcutKeysVisible)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ShortcutKeysVM.HandleMouseWheel(e.Delta);
                });
            }
        }
    }
}