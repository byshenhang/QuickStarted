using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using QuickStarted.Models;

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

        public VideoPlayerWindow(VideoInfo videoInfo)
        {
            InitializeComponent();
            DataContext = this;
            
            _videoInfo = videoInfo;
            VideoTitle = $"视频播放器 - {videoInfo.Name}";
            VideoSource = new Uri(videoInfo.FilePath);
            Volume = 0.5;
            IsLoading = true;

            // 初始化定时器用于更新进度
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += Timer_Tick;

            // 设置窗口关闭事件
            Closing += VideoPlayerWindow_Closing;
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

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            IsLoading = false;
            HasError = false;
            
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                TotalSeconds = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                OnPropertyChanged(nameof(TotalTimeText));
            }

            VideoPlayer.Volume = Volume;
            VideoPlayer.Play();
            IsPlaying = true;
            _timer.Start();
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            IsPlaying = false;
            _timer.Stop();
            CurrentSeconds = 0;
            VideoPlayer.Position = TimeSpan.Zero;
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            IsLoading = false;
            HasError = true;
            ErrorMessage = $"视频加载失败: {e.ErrorException?.Message ?? "未知错误"}";
            _timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_isUserDragging && VideoPlayer.Position.TotalSeconds > 0)
            {
                CurrentSeconds = VideoPlayer.Position.TotalSeconds;
                OnPropertyChanged(nameof(CurrentTimeText));
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (HasError) return;

            if (IsPlaying)
            {
                VideoPlayer.Pause();
                IsPlaying = false;
                _timer.Stop();
            }
            else
            {
                VideoPlayer.Play();
                IsPlaying = true;
                _timer.Start();
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Stop();
            IsPlaying = false;
            _timer.Stop();
            CurrentSeconds = 0;
            VideoPlayer.Position = TimeSpan.Zero;
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUserDragging && !HasError)
            {
                VideoPlayer.Position = TimeSpan.FromSeconds(e.NewValue);
                OnPropertyChanged(nameof(CurrentTimeText));
            }
        }

        private void ProgressSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isUserDragging = true;
        }

        private void ProgressSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isUserDragging = false;
            if (!HasError)
            {
                VideoPlayer.Position = TimeSpan.FromSeconds(ProgressSlider.Value);
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
            _timer?.Stop();
            VideoPlayer?.Stop();
            VideoPlayer?.Close();
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