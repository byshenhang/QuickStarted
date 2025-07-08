using System.Collections.Generic;

namespace QuickStarted.Models
{
    /// <summary>
    /// 笔记文件信息
    /// </summary>
    public class NoteFile
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件内容
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// 笔记分类信息
    /// </summary>
    public class NoteCategory
    {
        /// <summary>
        /// 分类名称
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// 分类路径
        /// </summary>
        public string CategoryPath { get; set; } = string.Empty;

        /// <summary>
        /// 笔记文件列表
        /// </summary>
        public List<NoteFile> NoteFiles { get; set; } = new();
    }

    /// <summary>
    /// 程序笔记信息
    /// </summary>
    public class ProgramNotes
    {
        /// <summary>
        /// 程序名称
        /// </summary>
        public string ProgramName { get; set; } = string.Empty;

        /// <summary>
        /// 程序路径
        /// </summary>
        public string ProgramPath { get; set; } = string.Empty;

        /// <summary>
        /// 快捷键配置
        /// </summary>
        public ShortcutKeyConfig? ShortcutKeys { get; set; }

        /// <summary>
        /// 笔记分类列表
        /// </summary>
        public List<NoteCategory> NoteCategories { get; set; } = new();
    }
}