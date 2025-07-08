using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuickStarted.Models
{
    /// <summary>
    /// 程序映射配置
    /// </summary>
    public class ProgramMapping
    {
        /// <summary>
        /// 编辑时间
        /// </summary>
        [JsonPropertyName("EditTime")]
        public string EditTime { get; set; } = string.Empty;

        /// <summary>
        /// 映射数据
        /// </summary>
        [JsonPropertyName("MapData")]
        public List<Dictionary<string, List<string>>> MapData { get; set; } = new();
    }
}