using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuickStarted.Models;
using QuickStarted.ViewModels;
using Application = System.Windows.Application;

namespace QuickStarted.Views
{
    /// <summary>
    /// VideosView.xaml 的交互逻辑
    /// </summary>
    public partial class VideosView : UserControl
    {
        public VideosView()
        {
            InitializeComponent();
        }

        private void VideoCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is VideoInfo video)
            {
                var viewModel = DataContext as VideosViewModel;
                viewModel?.PlayVideoCommand?.Execute(video);
            }
        }

    }
}