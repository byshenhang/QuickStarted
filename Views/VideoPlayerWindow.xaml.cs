using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using QuickStarted.Models;
using QuickStarted.Services;
using Panel = System.Windows.Controls.Panel;

namespace QuickStarted.Views
{
    /// <summary>
    /// VideoPlayerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class VideoPlayerWindow : Window, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private bool _isUserDragging = false;
        private VideoInfo _videoInfo;
        private readonly ILogService? _logService;
        private bool _wasPlayingBeforeDrag = false;
        public VideoPlayerWindow(VideoInfo videoInfo, ILogService? logService = null)
        {
            _logService = logService;
            _logService?.LogInfo($"开始初始化VideoPlayerWindow，视频: {videoInfo?.Name}");
            
            InitializeComponent();
            DataContext = this;
            
            _videoInfo = videoInfo;
            VideoTitle = $"视频播放器 - {videoInfo.Name}";
            
            _logService?.LogInfo($"设置视频源: {videoInfo.FilePath}");
            VideoSource = new Uri(videoInfo.FilePath);
            Volume = 0.5;
            IsLoading = true;
            
            // 设置窗口置顶
            Topmost = true;
            _logService?.LogInfo("设置窗口置顶");
            
            _logService?.LogInfo("VideoPlayerWindow初始化完成，IsLoading设置为true");

            // 初始化定时器用于更新进度
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _timer.Tick += Timer_Tick;
            _logService?.LogInfo("定时器初始化完成");

            // 设置窗口关闭事件
            Closing += VideoPlayerWindow_Closing;
            _logService?.LogInfo("VideoPlayerWindow构造函数执行完成");


            Loaded += (_, __) =>
            {
                IsPlaying = false;
            };
        }

        #region 属性

        private string _videoTitle = "视频播放器";
        public string VideoTitle
        {
            get => _videoTitle;
            set => SetProperty(ref _videoTitle, value);
        }

        private Uri _videoSource;
        public Uri VideoSource
        {
            get => _videoSource;
            set => SetProperty(ref _videoSource, value);
        }

        private bool _isLoading = true;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _hasError = false;
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private bool _isPlaying = false;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                SetProperty(ref _isPlaying, value);
                OnPropertyChanged(nameof(PlayPauseIcon));
            }
        }

        public string PlayPauseIcon => IsPlaying ? "⏸" : "▶";

        private double _volume = 0.5;
        public double Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        private double _totalSeconds = 0;
        public double TotalSeconds
        {
            get => _totalSeconds;
            set => SetProperty(ref _totalSeconds, value);
        }

        private double _currentSeconds = 0;
        public double CurrentSeconds
        {
            get => _currentSeconds;
            set => SetProperty(ref _currentSeconds, value);
        }

        public string CurrentTimeText => TimeSpan.FromSeconds(CurrentSeconds).ToString(@"mm\:ss");
        public string TotalTimeText => TimeSpan.FromSeconds(TotalSeconds).ToString(@"mm\:ss");

        #endregion

        #region 事件处理

        private async void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            IsLoading = false;
            HasError = false;

            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                TotalSeconds = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                OnPropertyChanged(nameof(TotalTimeText));
            }

            VideoPlayer.Volume = Volume;

            // 对于短视频（小于5秒）跳过预热，避免播放错误
            if (TotalSeconds >= 5.0)
            {
                // —— 预热开始（静音+极短播放）——
                double keepVolume = VideoPlayer.Volume;
                VideoPlayer.Volume = 0;              // 防止短促的出声
                VideoPlayer.Position = TimeSpan.FromMilliseconds(1); // 踢到首关键帧附近，避免0帧卡顿
                VideoPlayer.Play();
                await System.Threading.Tasks.Task.Delay(120); // 100~200ms 视情况调整
                VideoPlayer.Pause();
                VideoPlayer.Position = TimeSpan.Zero; // 复位到 0
                VideoPlayer.Volume = keepVolume;
                // —— 预热结束 ——
                
                _logService?.LogInfo($"视频预热完成，时长: {TotalSeconds:F1}秒");
            }
            else
            {
                // 短视频直接设置到开始位置，不进行预热
                VideoPlayer.Position = TimeSpan.Zero;
                _logService?.LogInfo($"短视频跳过预热，时长: {TotalSeconds:F1}秒");
            }

            IsPlaying = false;   // 保持“未播放”状态
            _timer.Stop();       // 直到用户点击才启动计时器
        }


        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _logService?.LogInfo("VideoPlayer_MediaEnded事件触发，视频播放结束");
            
            IsPlaying = false;
            _timer.Stop();
            CurrentSeconds = 0;
            VideoPlayer.Position = TimeSpan.Zero;
            
            _logService?.LogInfo("视频播放结束，已重置播放状态");
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _logService?.LogError($"VideoPlayer_MediaFailed事件触发: {e.ErrorException?.Message ?? "未知错误"}", e.ErrorException);
            
            IsLoading = false;
            HasError = true;
            ErrorMessage = $"视频加载失败: {e.ErrorException?.Message ?? "未知错误"}";
            _timer.Stop();
            
            _logService?.LogError($"视频加载失败，错误信息: {ErrorMessage}");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_isUserDragging && VideoPlayer.Position.TotalSeconds > 0)
            {
                CurrentSeconds = VideoPlayer.Position.TotalSeconds;
                OnPropertyChanged(nameof(CurrentTimeText));
                
                // 每10秒记录一次播放进度
                if ((int)CurrentSeconds % 10 == 0)
                {
                    _logService?.LogInfo($"视频播放进度: {CurrentSeconds:F1}/{TotalSeconds:F1}秒");
                }
            }
        }


        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (HasError) 
            {
                _logService?.LogWarning("播放/暂停按钮点击，但视频有错误，忽略操作");
                return;
            }

            if (IsPlaying)
            {
                _logService?.LogInfo("暂停视频播放");
                VideoPlayer.Pause();
                IsPlaying = false;
                _timer.Stop();
            }
            else
            {
                _logService?.LogInfo("恢复视频播放");
                VideoPlayer.Play();
                IsPlaying = true;
                _timer.Start();
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _logService?.LogInfo("停止视频播放");
            VideoPlayer.Stop();
            IsPlaying = false;
            _timer.Stop();
            CurrentSeconds = 0;
            VideoPlayer.Position = TimeSpan.Zero;
        }

        private void ProgressSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isUserDragging = true;
            _wasPlayingBeforeDrag = IsPlaying;
            if (!_hasError && IsPlaying)
            {
                VideoPlayer.Pause();
                IsPlaying = false;
                _timer.Stop();
            }
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUserDragging && !HasError)
            {
                VideoPlayer.Position = TimeSpan.FromSeconds(e.NewValue);
                OnPropertyChanged(nameof(CurrentTimeText)); // 立刻刷新时间文本
                                                            // ScrubbingEnabled=True 会让画面也立刻更新
            }
        }

        private void ProgressSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isUserDragging = false;
            if (!HasError)
            {
                VideoPlayer.Position = TimeSpan.FromSeconds(ProgressSlider.Value);
                if (_wasPlayingBeforeDrag)
                {
                    VideoPlayer.Play();
                    IsPlaying = true;
                    _timer.Start();
                }
            }
        }


        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VideoPlayer != null)
            {
                VideoPlayer.Volume = e.NewValue;
            }
        }

        private void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
            }
            else
            {
                WindowState = WindowState.Maximized;
                WindowStyle = WindowStyle.None;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void VideoPlayerWindow_Closing(object sender, CancelEventArgs e)
        {
            _logService?.LogInfo("VideoPlayerWindow正在关闭");
            
            _timer?.Stop();
            _logService?.LogInfo("定时器已停止");
            
            VideoPlayer?.Stop();
            _logService?.LogInfo("视频播放器已停止");
            
            VideoPlayer?.Close();
            _logService?.LogInfo("视频播放器已关闭，VideoPlayerWindow关闭完成");


            if (VideoPlayer.Parent is Panel p) p.Children.Remove(VideoPlayer);
        }

        // 标题栏拖拽事件
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        // 全屏按钮点击事件
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                _logService?.LogInfo("窗口已最大化");
            }
            else
            {
                this.WindowState = WindowState.Normal;
                _logService?.LogInfo("窗口已还原");
            }
        }

        // 关闭按钮点击事件
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _logService?.LogInfo("用户点击关闭按钮");
            this.Close();
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}