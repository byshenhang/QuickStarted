using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickStarted.Models;
using QuickStarted.Services;
using QuickStarted.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace QuickStarted.ViewModels
{
    /// <summary>
    /// 视频页 ViewModel
    /// </summary>
    public partial class VideosViewModel : ObservableObject
    {
        private readonly IDataService? _dataService;
        private readonly ILogService? _logService;

        /// <summary>
        /// 视频列表
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<VideoInfo> _videos = new();

        /// <summary>
        /// 当前选中的视频
        /// </summary>
        [ObservableProperty]
        private VideoInfo? _selectedVideo;

        /// <summary>
        /// 是否正在加载
        /// </summary>
        [ObservableProperty]
        private bool _isLoading = false;

        /// <summary>
        /// 是否有视频数据
        /// </summary>
        [ObservableProperty]
        private bool _hasVideos = false;

        /// <summary>
        /// 当前程序名称
        /// </summary>
        [ObservableProperty]
        private string _currentProgram = string.Empty;

        /// <summary>
        /// 状态消息
        /// </summary>
        [ObservableProperty]
        private string _statusMessage = "请选择一个程序查看视频教程";

        /// <summary>
        /// 构造函数（无参数，用于设计时）
        /// </summary>
        public VideosViewModel()
        {
            // 设计时构造函数
        }

        /// <summary>
        /// 构造函数（带依赖注入）
        /// </summary>
        public VideosViewModel(IDataService dataService, ILogService logService)
        {
            _dataService = dataService;
            _logService = logService;
        }

        /// <summary>
        /// 加载指定程序的视频数据
        /// </summary>
        /// <param name="programName">程序名称</param>
        public async Task LoadVideosAsync(string programName)
        {
            if (_dataService == null || _logService == null)
            {
                StatusMessage = "服务未初始化";
                return;
            }

            if (string.IsNullOrEmpty(programName))
            {
                StatusMessage = "程序名称不能为空";
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = "正在加载视频数据...";
                CurrentProgram = programName;

                _logService.LogInfo($"开始加载程序 '{programName}' 的视频数据");

                var videos = await _dataService.LoadProgramVideosAsync(programName);
                
                Videos.Clear();
                foreach (var video in videos)
                {
                    Videos.Add(video);
                }

                HasVideos = Videos.Any();
                
                if (HasVideos)
                {
                    StatusMessage = $"找到 {Videos.Count} 个视频教程";
                    _logService.LogInfo($"成功加载 {Videos.Count} 个视频");
                }
                else
                {
                    StatusMessage = $"程序 '{programName}' 暂无视频教程";
                    _logService.LogWarning($"程序 '{programName}' 没有找到视频文件");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载视频失败: {ex.Message}";
                _logService?.LogError($"加载视频失败: {ex.Message}", ex);
                Videos.Clear();
                HasVideos = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 播放选中的视频
        /// </summary>
        [RelayCommand]
        private async Task PlayVideoAsync(VideoInfo video)
        {
            try
            {
                _logService?.LogInfo($"开始播放视频: {video?.Name ?? "null"}");
                
                if (video == null)
                {
                    _logService?.LogError("视频对象为空");
                    StatusMessage = "视频对象为空";
                    return;
                }
                
                _logService?.LogInfo($"检查视频文件路径: {video.FilePath}");
                
                if (!File.Exists(video.FilePath))
                {
                    _logService?.LogError($"视频文件不存在: {video.FilePath}");
                    StatusMessage = "视频文件不存在";
                    return;
                }
                
                _logService?.LogInfo($"视频文件存在，开始创建播放窗口");

                // 创建并显示视频播放窗口
                var playerWindow = new VideoPlayerWindow(video, _logService);
                _logService?.LogInfo($"视频播放窗口已创建，准备显示");
                
                playerWindow.Show();
                _logService?.LogInfo($"视频播放窗口已显示");

                StatusMessage = $"正在播放: {video.Name}";
            }
            catch (Exception ex)
            {
                _logService?.LogError($"播放视频失败: {ex.Message}", ex);
                StatusMessage = $"播放视频失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 刷新视频列表
        /// </summary>
        [RelayCommand]
        private async Task RefreshVideosAsync()
        {
            if (!string.IsNullOrEmpty(CurrentProgram))
            {
                await LoadVideosAsync(CurrentProgram);
            }
        }

        /// <summary>
        /// 清空视频数据
        /// </summary>
        public void ClearVideos()
        {
            Videos.Clear();
            SelectedVideo = null;
            HasVideos = false;
            CurrentProgram = string.Empty;
            StatusMessage = "请选择一个程序查看视频教程";
        }

        /// <summary>
        /// 视频播放请求事件
        /// </summary>
        public event EventHandler<VideoPlayEventArgs>? VideoPlayRequested;
    }

    /// <summary>
    /// 视频播放事件参数
    /// </summary>
    public class VideoPlayEventArgs : EventArgs
    {
        public VideoInfo Video { get; }

        public VideoPlayEventArgs(VideoInfo video)
        {
            Video = video;
        }
    }
}