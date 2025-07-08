using System.Collections.Generic;
using System.Windows.Documents;

namespace QuickStarted.Controls.HtmlProcessors
{
    /// <summary>
    /// HTML 处理器管理器，负责管理和调度所有 HTML 处理器
    /// </summary>
    public class HtmlProcessorManager
    {
        private readonly List<IHtmlProcessor> _processors;
        
        /// <summary>
        /// 初始化 HTML 处理器管理器
        /// </summary>
        public HtmlProcessorManager()
        {
            _processors = new List<IHtmlProcessor>
            {
                new HeadingProcessor(),
                new CodeProcessor(),
                new ImageProcessor(),
                new TableProcessor(),
                new ListProcessor(),
                new ParagraphProcessor() // 段落处理器放在最后，作为默认处理器
            };
        }
        
        /// <summary>
        /// 处理 HTML 内容并转换为 FlowDocument
        /// </summary>
        /// <param name="html">HTML 内容</param>
        /// <param name="context">处理上下文</param>
        /// <returns>FlowDocument</returns>
        public FlowDocument ProcessHtml(string html, HtmlProcessingContext context)
        {
            var document = new FlowDocument();
            
            if (string.IsNullOrWhiteSpace(html))
            {
                return document;
            }
            
            // 预处理 HTML
            var processedHtml = PreprocessHtml(html);
            var lines = processedHtml.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // 跳过空行和无效行
                if (string.IsNullOrWhiteSpace(trimmedLine) || 
                    trimmedLine.StartsWith("</ul>") || 
                    trimmedLine.StartsWith("</ol>"))
                {
                    continue;
                }
                
                // 跳过表格结构标签，但保留表格内容标签
                if (trimmedLine.StartsWith("<tbody>") ||
                    trimmedLine.StartsWith("</tbody>") ||
                    trimmedLine.StartsWith("<thead>") ||
                    trimmedLine.StartsWith("</thead>"))
                {
                    continue;
                }
                
                // 查找能够处理当前行的处理器
                var processor = FindProcessor(trimmedLine);
                
                if (processor != null)
                {
                    processor.Process(trimmedLine, document, context);
                }
                else
                {
                    // 没有找到合适的处理器，使用默认处理方式
                    ProcessUnknownLine(trimmedLine, document);
                }
            }
            
            return document;
        }
        
        /// <summary>
        /// 查找能够处理指定 HTML 行的处理器
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <returns>处理器，如果没有找到则返回 null</returns>
        private IHtmlProcessor FindProcessor(string htmlLine)
        {
            foreach (var processor in _processors)
            {
                if (processor.CanProcess(htmlLine))
                {
                    return processor;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 处理未知的 HTML 行
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        private void ProcessUnknownLine(string htmlLine, FlowDocument document)
        {
            // 移除所有 HTML 标签，只保留文本内容
            var content = System.Text.RegularExpressions.Regex.Replace(htmlLine, @"<[^>]+>", "");
            content = System.Net.WebUtility.HtmlDecode(content.Trim());
            
            if (!string.IsNullOrWhiteSpace(content))
            {
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run(content));
                document.Blocks.Add(paragraph);
            }
        }
        
        /// <summary>
        /// 预处理 HTML，合并多行标签和清理格式
        /// </summary>
        /// <param name="html">原始 HTML</param>
        /// <returns>预处理后的 HTML</returns>
        private string PreprocessHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }
            
            var result = html;
            
            // 特殊处理嵌套的 pre > code 结构
            result = System.Text.RegularExpressions.Regex.Replace(result, 
                @"<pre><code[^>]*>\s*([\s\S]*?)\s*</code></pre>", 
                "<pre><code>$1</code></pre>", 
                System.Text.RegularExpressions.RegexOptions.Multiline | 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            // 合并跨行的标签，但保持嵌套代码块的完整性
            var patterns = new[]
            {
                (@"<p[^>]*>\s*([\s\S]*?)\s*</p>", "<p>$1</p>"),
                (@"<li[^>]*>\s*([\s\S]*?)\s*</li>", "<li>$1</li>"),
                (@"<td[^>]*>\s*([\s\S]*?)\s*</td>", "<td>$1</td>"),
                (@"<th[^>]*>\s*([\s\S]*?)\s*</th>", "<th>$1</th>")
                // 移除 pre 和 code 的单独处理，保持嵌套结构完整
            };
            
            foreach (var (pattern, replacement) in patterns)
            {
                result = System.Text.RegularExpressions.Regex.Replace(result, pattern, replacement, 
                    System.Text.RegularExpressions.RegexOptions.Multiline | 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            
            // 清理多余的空白字符
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\r\n|\r", "\n");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\n\s*\n", "\n");
            
            return result;
        }
        
        /// <summary>
        /// 添加自定义处理器
        /// </summary>
        /// <param name="processor">处理器</param>
        public void AddProcessor(IHtmlProcessor processor)
        {
            if (processor != null && !_processors.Contains(processor))
            {
                // 插入到段落处理器之前，保持段落处理器作为默认处理器
                var insertIndex = _processors.Count > 0 ? _processors.Count - 1 : 0;
                _processors.Insert(insertIndex, processor);
            }
        }
        
        /// <summary>
        /// 移除处理器
        /// </summary>
        /// <param name="processor">处理器</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveProcessor(IHtmlProcessor processor)
        {
            return processor != null && _processors.Remove(processor);
        }
        
        /// <summary>
        /// 获取所有处理器
        /// </summary>
        /// <returns>处理器列表</returns>
        public IReadOnlyList<IHtmlProcessor> GetProcessors()
        {
            return _processors.AsReadOnly();
        }
    }
}