using System;
using System.Collections.Generic;

namespace QuickStarted.Controls.HtmlProcessors
{
    /// <summary>
    /// HTML 处理上下文，用于在处理器之间共享状态和配置
    /// </summary>
    public class HtmlProcessingContext
    {
        /// <summary>
        /// 基础 URI，用于解析相对路径
        /// </summary>
        public Uri? BaseUri { get; set; }
        
        /// <summary>
        /// 当前列表类型（有序或无序）
        /// </summary>
        public ListType CurrentListType { get; set; } = ListType.None;
        
        /// <summary>
        /// 当前列表嵌套级别
        /// </summary>
        public int ListNestingLevel { get; set; } = 0;
        
        /// <summary>
        /// 当前表格状态
        /// </summary>
        public bool IsInTable { get; set; } = false;
        
        /// <summary>
        /// 当前代码块状态
        /// </summary>
        public bool IsInCodeBlock { get; set; } = false;
        
        /// <summary>
        /// 处理器配置
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// 动态属性存储，用于处理器之间共享临时状态
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// 获取配置值
        /// </summary>
        /// <typeparam name="T">配置值类型</typeparam>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        public T GetConfiguration<T>(string key, T defaultValue = default(T))
        {
            if (Configuration.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// 设置配置值
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="value">配置值</param>
        public void SetConfiguration(string key, object value)
        {
            Configuration[key] = value;
        }
        
        /// <summary>
        /// 获取属性值
        /// </summary>
        /// <typeparam name="T">属性值类型</typeparam>
        /// <param name="key">属性键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>属性值</returns>
        public T GetProperty<T>(string key, T defaultValue = default(T))
        {
            if (Properties.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// 设置属性值
        /// </summary>
        /// <param name="key">属性键</param>
        /// <param name="value">属性值</param>
        public void SetProperty(string key, object value)
        {
            Properties[key] = value;
        }
        
        /// <summary>
        /// 移除属性
        /// </summary>
        /// <param name="key">属性键</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveProperty(string key)
        {
            return Properties.Remove(key);
        }
        
        /// <summary>
        /// 检查属性是否存在
        /// </summary>
        /// <param name="key">属性键</param>
        /// <returns>是否存在</returns>
        public bool HasProperty(string key)
        {
            return Properties.ContainsKey(key);
        }
    }
    
    /// <summary>
    /// 列表类型枚举
    /// </summary>
    public enum ListType
    {
        /// <summary>
        /// 无列表
        /// </summary>
        None,
        
        /// <summary>
        /// 有序列表
        /// </summary>
        Ordered,
        
        /// <summary>
        /// 无序列表
        /// </summary>
        Unordered
    }
}