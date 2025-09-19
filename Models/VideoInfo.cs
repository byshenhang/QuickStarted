using System;

namespace QuickStarted.Models
{
    /// <summary>
    /// 视频信息模型
    /// </summary>
    public class VideoInfo
    {
        /// <summary>
        /// 视频文件名（不含扩展名）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 视频文件完整路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 预览图路径（如果存在）
        /// </summary>
        public string? ThumbnailPath { get; set; }

        /// <summary>
        /// 视频文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 视频创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 视频修改时间
        /// </summary>
        public DateTime ModifiedTime { get; set; }

        /// <summary>
        /// 视频时长（如果能获取到）
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// 视频描述（可选）
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 格式化的文件大小字符串
        /// </summary>
        public string FormattedFileSize
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F1} KB";
                else if (FileSize < 1024 * 1024 * 1024)
                    return $"{FileSize / (1024.0 * 1024.0):F1} MB";
                else
                    return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }

    }
}