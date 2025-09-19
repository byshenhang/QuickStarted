using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using QuickStarted.ViewModels;

namespace QuickStarted.Views
{
    public partial class ShortcutKeysView : UserControl
    {
        public ShortcutKeysView()
        {
            InitializeComponent();
            Loaded += ShortcutKeysView_Loaded;
            Unloaded += ShortcutKeysView_Unloaded;
        }

        private ShortcutKeysViewModel? VM => DataContext as ShortcutKeysViewModel;

        private void ShortcutKeysView_Loaded(object sender, RoutedEventArgs e)
        {
            if (VM != null)
                VM.PageChanged += VM_PageChanged;
        }

        private void ShortcutKeysView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (VM != null)
                VM.PageChanged -= VM_PageChanged;
        }

        private void VM_PageChanged(object? sender, bool slideFromRight)
        {
            try
            {
                var storyboardKey = slideFromRight ? "SlideInFromRight" : "SlideInFromLeft";
                var storyboard = (Storyboard)ShortcutKeysContainer.Resources[storyboardKey];
                storyboard.Begin();
            }
            catch
            {
                // 忽略动画异常，确保不会影响正常显示
            }
        }
    }
}