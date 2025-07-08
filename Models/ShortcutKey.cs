using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace QuickStarted.Models
{
    /// <summary>
    /// 快捷键信息
    /// </summary>
    public class ShortcutKey
    {
        /// <summary>
        /// 快捷键组合
        /// </summary>
        [JsonPropertyName("shortcutkey")]
        public string ShortcutKeyValue { get; set; } = string.Empty;

        /// <summary>
        /// 功能名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 功能描述
        /// </summary>
        [JsonPropertyName("desc")]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 快捷键页面
    /// </summary>
    public class ShortcutKeyPage
    {
        /// <summary>
        /// 页面索引
        /// </summary>
        [JsonPropertyName("index")]
        public int Index { get; set; }

        /// <summary>
        /// 页面名称
        /// </summary>
        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 快捷键数据
        /// </summary>
        [JsonPropertyName("Data")]
        public List<ShortcutKey> Data { get; set; } = new();
    }

    /// <summary>
    /// 快捷键配置文件
    /// </summary>
    public class ShortcutKeyConfig
    {
        /// <summary>
        /// 编辑时间
        /// </summary>
        [JsonPropertyName("EditTime")]
        public string EditTime { get; set; } = string.Empty;

        /// <summary>
        /// 快捷键页面列表
        /// </summary>
        [JsonPropertyName("Pages")]
        public List<ShortcutKeyPage> Pages { get; set; } = new();

        /// <summary>
        /// 获取快捷键数据（兼容旧版本）
        /// </summary>
        [JsonIgnore]
        public List<ShortcutKey> Data => Pages.FirstOrDefault()?.Data ?? new List<ShortcutKey>();
    }
}