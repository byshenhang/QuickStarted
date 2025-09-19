using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickStarted.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuickStarted.ViewModels
{
    /// <summary>
    /// 笔记页 ViewModel
    /// </summary>
    public partial class NotesViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<NoteCategory> _noteCategories = new();

        [ObservableProperty]
        private NoteCategory? _selectedCategory;

        [ObservableProperty]
        private NoteFile? _selectedNoteFile;

        [ObservableProperty]
        private string _markdownContent = string.Empty;

        /// <summary>
        /// 重置并载入分类与文件；默认选中第一类与其第一个文件
        /// </summary>
        public void SetCategories(System.Collections.Generic.IEnumerable<NoteCategory> categories)
        {
            NoteCategories.Clear();
            foreach (var c in categories)
                NoteCategories.Add(c);

            if (NoteCategories.Any())
            {
                SelectedCategory = NoteCategories.First();
                if (SelectedCategory.NoteFiles.Any())
                {
                    SelectedNoteFile = SelectedCategory.NoteFiles.First();
                    ForceRefreshMarkdown(SelectedNoteFile.Content);
                }
                else
                {
                    SelectedNoteFile = null;
                    MarkdownContent = string.Empty;
                }
            }
            else
            {
                SelectedCategory = null;
                SelectedNoteFile = null;
                MarkdownContent = string.Empty;
            }
        }

        [RelayCommand]
        private void SelectCategory(NoteCategory category)
        {
            SelectedCategory = category;
            if (category.NoteFiles.Any())
            {
                SelectedNoteFile = category.NoteFiles.First();
                ForceRefreshMarkdown(SelectedNoteFile.Content);
            }
            else
            {
                SelectedNoteFile = null;
                MarkdownContent = string.Empty;
            }
        }

        [RelayCommand]
        private void SelectNoteFile(NoteFile noteFile)
        {
            SelectedNoteFile = noteFile;
            ForceRefreshMarkdown(noteFile.Content);
        }

        /// <summary>
        /// 为确保某些第三方控件在内容相同情况下也能重绘，这里先清空再赋值。
        /// </summary>
        private void ForceRefreshMarkdown(string newContent)
        {
            if (MarkdownContent == newContent)
            {
                // 触发一次明显变化
                MarkdownContent = string.Empty;
            }
            MarkdownContent = newContent ?? string.Empty;
        }
    }
}