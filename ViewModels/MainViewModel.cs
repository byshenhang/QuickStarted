using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickStarted.Services;
using QuickStarted.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Markdig;
using Application = System.Windows.Application;

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

        [ObservableProperty]
        private string _currentProgram = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ShortcutKey> _shortcutKeys = new();

        [ObservableProperty]
        private ObservableCollection<ShortcutKeyPage> _shortcutKeyPages = new();

        [ObservableProperty]
        private ShortcutKeyPage? _currentShortcutKeyPage;

        [ObservableProperty]
        private int _currentPageIndex = 0;

        [ObservableProperty]
        private string _currentPageName = string.Empty;

        [ObservableProperty]
        private string _currentPageProgress = string.Empty;

        /// <summary>
        /// 动画方向：true为向右滑入，false为向左滑入
        /// </summary>
        private bool _isSlideFromRight = true;

        [ObservableProperty]
        private ObservableCollection<NoteCategory> _noteCategories = new();

        [ObservableProperty]
        private NoteCategory? _selectedCategory;

        [ObservableProperty]
        private NoteFile? _selectedNoteFile;

        [ObservableProperty]
        private string _markdownContent = string.Empty;

        [ObservableProperty]
        private bool _isShortcutKeysVisible = true;

        [ObservableProperty]
        private string _currentHookWindowTitle = string.Empty;

        [ObservableProperty]
        private string _currentHookProcessName = string.Empty;

        /// <summary>
        /// 页面标签是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isPageTabsVisible = true;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="windowHookService">窗口钩子服务</param>
        /// <param name="screenService">屏幕服务</param>
        /// <param name="dataService">数据服务</param>
        /// <param name="logService">日志服务</param>
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
        /// <param name="mainWindow">主窗口</param>
        public void SetMainWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        public void StartServices()
        {
            var markdown = @"
```
// 简单的摆动动画
sin(T*2*pi) * 50

// 基于其他对象的位置
$Box01.pos.x * 2
```
";

            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            var html = Markdown.ToHtml(markdown, pipeline);

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
        /// 反引号键长按事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private async void OnWinKeyLongPressed(object? sender, EventArgs e)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadCurrentProgramDataAsync();
                ShowWindowWithFadeIn();
            });
        }

        /// <summary>
        /// 加载当前程序数据
        /// </summary>
        private async Task LoadCurrentProgramDataAsync()
        {
            _logService.LogInfo("开始加载当前程序数据");
            
            try
            {
                // 首先确保程序映射配置已加载
                await _dataService.LoadProgramMappingAsync();
                
                // 获取当前活动窗口的进程名称
                var processName = GetActiveWindowTitle();
                _logService.LogInfo($"获取到当前进程名称: '{processName}'");
                
                // 根据进程名称匹配程序
                var matchedProgram = _dataService.GetMatchingProgram(processName);
                _logService.LogInfo($"匹配结果: '{matchedProgram}'");
                
                if (string.IsNullOrEmpty(matchedProgram))
                {
                    // 如果没有匹配到程序，使用默认程序（如3dMax）
                    matchedProgram = "3dMax";
                    _logService.LogWarning($"未匹配到程序，使用默认程序: '{matchedProgram}'");
                }

                CurrentProgram = matchedProgram;
                _logService.LogInfo($"设置当前程序为: '{matchedProgram}'");
                
                // 加载程序数据
                var programNotes = await _dataService.LoadProgramNotesAsync(matchedProgram);
                if (programNotes != null)
                {
                    _logService.LogInfo("程序笔记数据加载成功，开始更新UI");
                    
                    // 更新快捷键页面
                    ShortcutKeyPages.Clear();
                    ShortcutKeys.Clear();
                    
                    if (programNotes.ShortcutKeys?.Pages != null && programNotes.ShortcutKeys.Pages.Any())
                    {
                        foreach (var page in programNotes.ShortcutKeys.Pages)
                        {
                            ShortcutKeyPages.Add(page);
                        }
                        
                        // 设置当前页面为第一页
                        CurrentPageIndex = 0;
                        SetCurrentPage(0);
                        
                        // 始终显示页面标签，方便用户切换页面
                        IsPageTabsVisible = true;
                        
                        _logService.LogInfo($"已加载 {ShortcutKeyPages.Count} 个快捷键页面，页面标签可见性: {IsPageTabsVisible}");
                    }
                    else if (programNotes.ShortcutKeys?.Data != null)
                    {
                        // 兼容旧版本数据格式
                        var defaultPage = new ShortcutKeyPage
                        {
                            Index = 1,
                            Name = "Default",
                            Data = programNotes.ShortcutKeys.Data.ToList()
                        };
                        ShortcutKeyPages.Add(defaultPage);
                        CurrentPageIndex = 0;
                        SetCurrentPage(0);
                        
                        // 始终显示页面标签，方便用户切换页面
                        IsPageTabsVisible = true;
                        
                        _logService.LogInfo($"已转换旧版本数据为分页格式，页面标签可见性: {IsPageTabsVisible}");
                    }
                    else
                    {
                        _logService.LogWarning("没有找到快捷键数据");
                    }

                    // 更新笔记分类
                    NoteCategories.Clear();
                    foreach (var category in programNotes.NoteCategories)
                    {
                        NoteCategories.Add(category);
                    }
                    _logService.LogInfo($"已添加 {NoteCategories.Count} 个笔记分类到UI");

                    // 默认选择第一个分类和第一个文件
                    if (NoteCategories.Any())
                    {
                        SelectedCategory = NoteCategories.First();
                        _logService.LogInfo($"选择默认分类: '{SelectedCategory.CategoryName}'");
                        
                        if (SelectedCategory.NoteFiles.Any())
                        {
                            SelectedNoteFile = SelectedCategory.NoteFiles.First();
                            MarkdownContent = SelectedNoteFile.Content;
                            _logService.LogInfo($"选择默认笔记文件: '{SelectedNoteFile.FileName}'");
                        }
                        else
                        {
                            _logService.LogWarning($"分类 '{SelectedCategory.CategoryName}' 中没有笔记文件");
                            SelectedNoteFile = null;
                            MarkdownContent = string.Empty;
                        }
                    }
                    else
                    {
                        _logService.LogWarning("没有找到任何笔记分类");
                        SelectedCategory = null;
                        SelectedNoteFile = null;
                        MarkdownContent = string.Empty;
                    }
                }
                else
                {
                    _logService.LogError($"无法加载程序 '{matchedProgram}' 的笔记数据");
                    // 清空笔记相关数据
                    NoteCategories.Clear();
                    SelectedCategory = null;
                    SelectedNoteFile = null;
                    MarkdownContent = string.Empty;
                }
                
                _logService.LogInfo("程序数据加载完成");
            }
            catch (Exception ex)
            {
                _logService.LogError($"加载程序数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取当前活动窗口标题
        /// </summary>
        /// <returns>窗口标题</returns>
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
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>进程名称</returns>
        private string GetActiveWindowProcessName(IntPtr hWnd)
        {
            try
            {
                // 获取窗口所属的进程ID
                GetWindowThreadProcessId(hWnd, out uint processId);
                
                // 根据进程ID获取进程对象
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

        /// <summary>
        /// Win32 API - 获取前台窗口句柄
        /// </summary>
        /// <returns>窗口句柄</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Win32 API - 获取窗口标题长度
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>标题长度</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// Win32 API - 获取窗口标题
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="text">标题缓冲区</param>
        /// <param name="count">缓冲区大小</param>
        /// <returns>实际获取的字符数</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        /// <summary>
        /// Win32 API - 获取窗口所属的线程ID和进程ID
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="lpdwProcessId">进程ID输出参数</param>
        /// <returns>线程ID</returns>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// 显示窗口并淡入
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
            
            // 显示窗口
            _mainWindow.Show();
            _mainWindow.Activate();
            _mainWindow.Focus();
            
            // 执行淡入动画
            var storyboard = _mainWindow.FindResource("FadeInStoryboard") as Storyboard;
            storyboard?.Begin();
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public void HideWindow()
        {
            if (_mainWindow == null) return;

            // 执行淡出动画
            var storyboard = _mainWindow.FindResource("FadeOutStoryboard") as Storyboard;
            if (storyboard != null)
            {
                storyboard.Completed += (s, e) => _mainWindow.Hide();
                storyboard.Begin();
            }
            else
            {
                _mainWindow.Hide();
            }
        }

        /// <summary>
        /// 切换到快捷键视图
        /// </summary>
        [RelayCommand]
        private void ShowShortcutKeys()
        {
            IsShortcutKeysVisible = true;
        }

        /// <summary>
        /// 切换到笔记视图
        /// </summary>
        [RelayCommand]
        private void ShowNotes()
        {
            IsShortcutKeysVisible = false;
        }

        /// <summary>
        /// 选择笔记分类
        /// </summary>
        /// <param name="category">笔记分类</param>
        [RelayCommand]
        private void SelectCategory(NoteCategory category)
        {
            SelectedCategory = category;
            if (category.NoteFiles.Any())
            {
                SelectedNoteFile = category.NoteFiles.First();
                MarkdownContent = SelectedNoteFile.Content;
            }
        }

        /// <summary>
        /// 选择笔记文件
        /// </summary>
        /// <param name="noteFile">笔记文件</param>
        [RelayCommand]
        private void SelectNoteFile(NoteFile noteFile)
        {
            SelectedNoteFile = noteFile;
            MarkdownContent = noteFile.Content;
        }

        /// <summary>
        /// 设置当前页面
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="withAnimation">是否播放动画</param>
        private void SetCurrentPage(int pageIndex, bool withAnimation = false)
        {
            if (pageIndex < 0 || pageIndex >= ShortcutKeyPages.Count)
                return;

            CurrentPageIndex = pageIndex;
            CurrentShortcutKeyPage = ShortcutKeyPages[pageIndex];
            CurrentPageName = CurrentShortcutKeyPage.Name;
            CurrentPageProgress = $"第{pageIndex + 1}页 / 共{ShortcutKeyPages.Count}页";

            // 更新当前显示的快捷键列表
            ShortcutKeys.Clear();
            foreach (var shortcut in CurrentShortcutKeyPage.Data)
            {
                ShortcutKeys.Add(shortcut);
            }

            // 播放翻页动画
            if (withAnimation && _mainWindow != null)
            {
                TriggerPageAnimation();
            }

            _logService.LogInfo($"切换到页面: {CurrentPageName} (索引: {pageIndex})");
        }

        /// <summary>
        /// 触发页面切换动画
        /// </summary>
        private void TriggerPageAnimation()
        {
            if (_mainWindow == null) return;

            try
            {
                var container = _mainWindow.FindName("ShortcutKeysContainer") as FrameworkElement;
                if (container != null)
                {
                    var animationKey = _isSlideFromRight ? "SlideInFromRight" : "SlideInFromLeft";
                    var storyboard = container.FindResource(animationKey) as System.Windows.Media.Animation.Storyboard;
                    storyboard?.Begin();
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"播放翻页动画失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 切换到上一页
        /// </summary>
        [RelayCommand]
        public void PreviousPage()
        {
            if (ShortcutKeyPages.Count <= 1) return;

            int newIndex = CurrentPageIndex - 1;
            if (newIndex < 0)
                newIndex = ShortcutKeyPages.Count - 1; // 循环到最后一页

            _isSlideFromRight = false; // 上一页从左侧滑入
            SetCurrentPage(newIndex, true);
        }

        /// <summary>
        /// 切换到下一页
        /// </summary>
        [RelayCommand]
        public void NextPage()
        {
            if (ShortcutKeyPages.Count <= 1) return;

            int newIndex = CurrentPageIndex + 1;
            if (newIndex >= ShortcutKeyPages.Count)
                newIndex = 0; // 循环到第一页

            _isSlideFromRight = true; // 下一页从右侧滑入
            SetCurrentPage(newIndex, true);
        }

        /// <summary>
        /// 处理键盘按键事件
        /// </summary>
        /// <param name="key">按键</param>
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
        /// 全局鼠标滚轮事件处理 - 处理翻页
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标滚轮事件参数</param>
        private void OnMouseWheel(object? sender, MouseHookService.MouseWheelEventArgs e)
        {
            // 只有在快捷键视图可见时才处理滚轮事件
            if (IsShortcutKeysVisible)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    HandleMouseWheel(e.Delta);
                });
            }
        }

        /// <summary>
        /// 处理鼠标滚轮事件
        /// </summary>
        /// <param name="delta">滚轮滚动增量</param>
        public void HandleMouseWheel(int delta)
        {
            if (delta > 0)
            {
                // 向上滚动，切换到上一页
                PreviousPage();
            }
            else if (delta < 0)
            {
                // 向下滚动，切换到下一页
                NextPage();
            }
        }

        /// <summary>
        /// 直接跳转到指定页面
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        [RelayCommand]
        public void JumpToPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= ShortcutKeyPages.Count || pageIndex == CurrentPageIndex)
                return;

            // 根据跳转方向设置动画方向
            _isSlideFromRight = pageIndex > CurrentPageIndex;
            SetCurrentPage(pageIndex, true);
        }
    }
}