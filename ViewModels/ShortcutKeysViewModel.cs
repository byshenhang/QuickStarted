using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickStarted.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuickStarted.ViewModels
{
    /// <summary>
    /// 快捷键页 ViewModel
    /// </summary>
    public partial class ShortcutKeysViewModel : ObservableObject
    {
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
        /// 页面标签是否可见
        /// </summary>
        [ObservableProperty]
        private bool _isPageTabsVisible = true;

        /// <summary>
        /// 动画方向：true 向右滑入；false 向左滑入
        /// </summary>
        public bool IsSlideFromRight { get; private set; } = true;

        /// <summary>
        /// 页切换事件（参数表示动画方向：true=从右，false=从左）
        /// </summary>
        public event EventHandler<bool>? PageChanged;

        /// <summary>
        /// 使用一组页面初始化/重置数据，默认选中第一页
        /// </summary>
        public void SetPages(System.Collections.Generic.IEnumerable<ShortcutKeyPage> pages)
        {
            ShortcutKeyPages.Clear();
            foreach (var p in pages.OrderBy(p => p.Index))
                ShortcutKeyPages.Add(p);

            if (ShortcutKeyPages.Count > 0)
            {
                SetCurrentPageInternal(0, raiseEvent:false);
                IsPageTabsVisible = true;
            }
            else
            {
                CurrentPageIndex = 0;
                CurrentShortcutKeyPage = null;
                CurrentPageName = string.Empty;
                CurrentPageProgress = string.Empty;
                ShortcutKeys.Clear();
                IsPageTabsVisible = false;
            }
        }

        /// <summary>
        /// 上一页
        /// </summary>
        [RelayCommand]
        public void PreviousPage()
        {
            if (ShortcutKeyPages.Count <= 1) return;

            var newIndex = CurrentPageIndex - 1;
            if (newIndex < 0) newIndex = ShortcutKeyPages.Count - 1;

            IsSlideFromRight = false;
            SetCurrentPageInternal(newIndex, raiseEvent:true);
        }

        /// <summary>
        /// 下一页
        /// </summary>
        [RelayCommand]
        public void NextPage()
        {
            if (ShortcutKeyPages.Count <= 1) return;

            var newIndex = CurrentPageIndex + 1;
            if (newIndex >= ShortcutKeyPages.Count) newIndex = 0;

            IsSlideFromRight = true;
            SetCurrentPageInternal(newIndex, raiseEvent:true);
        }

        /// <summary>
        /// 跳转到指定页（索引从0开始）
        /// </summary>
        [RelayCommand]
        public void JumpToPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= ShortcutKeyPages.Count || pageIndex == CurrentPageIndex) return;

            IsSlideFromRight = pageIndex > CurrentPageIndex;
            SetCurrentPageInternal(pageIndex, raiseEvent:true);
        }

        /// <summary>
        /// 处理鼠标滚轮（&gt;0 上一页；&lt;0 下一页）
        /// </summary>
        public void HandleMouseWheel(int delta)
        {
            if (delta > 0) PreviousPage();
            else if (delta < 0) NextPage();
        }

        private void SetCurrentPageInternal(int pageIndex, bool raiseEvent)
        {
            CurrentPageIndex = pageIndex;
            CurrentShortcutKeyPage = ShortcutKeyPages[pageIndex];
            CurrentPageName = CurrentShortcutKeyPage.Name;
            CurrentPageProgress = $"第{pageIndex + 1}页 / 共{ShortcutKeyPages.Count}页";

            ShortcutKeys.Clear();
            foreach (var s in CurrentShortcutKeyPage.Data)
                ShortcutKeys.Add(s);

            if (raiseEvent)
                PageChanged?.Invoke(this, IsSlideFromRight);
        }
    }
}