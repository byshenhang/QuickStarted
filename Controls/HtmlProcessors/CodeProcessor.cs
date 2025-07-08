using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace QuickStarted.Controls.HtmlProcessors
{
    /// <summary>
    /// 代码处理器，处理 PRE、CODE 标签
    /// </summary>
    public class CodeProcessor : BaseHtmlProcessor
    {
        /// <summary>
        /// 检查是否可以处理指定的 HTML 行
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <returns>是否可以处理</returns>
        public override bool CanProcess(string htmlLine)
        {
            var trimmed = htmlLine.TrimStart();
            return trimmed.StartsWith("<pre>") || 
                   trimmed.StartsWith("<pre ") ||
                   trimmed.StartsWith("<code>") ||
                   trimmed.StartsWith("<code ") ||
                   trimmed.Contains("<pre><code>"); // 处理 Markdig 生成的嵌套代码块
        }
        
        /// <summary>
        /// 处理 HTML 行并添加到文档中
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        public override void Process(string htmlLine, FlowDocument document, HtmlProcessingContext context)
        {
            var trimmed = htmlLine.TrimStart();
            
            if (trimmed.Contains("<pre><code>"))
            {
                ProcessNestedCodeBlock(htmlLine, document, context);
            }
            else if (trimmed.StartsWith("<pre"))
            {
                ProcessPreBlock(htmlLine, document, context);
            }
            else if (trimmed.StartsWith("<code"))
            {
                ProcessCodeBlock(htmlLine, document, context);
            }
        }
        
        /// <summary>
        /// 处理 PRE 代码块
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        private void ProcessPreBlock(string htmlLine, FlowDocument document, HtmlProcessingContext context)
        {
            try
            {
                // 提取 pre 标签内容
                var match = System.Text.RegularExpressions.Regex.Match(htmlLine, @"<pre[^>]*>(.*?)</pre>", 
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                if (match.Success)
                {
                    var content = match.Groups[1].Value;
                    content = System.Net.WebUtility.HtmlDecode(content);
                    
                    // 移除内部的 code 标签但保留内容
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"</?code[^>]*>", "");
                    
                    // 保持原始格式，不要去除换行符
                    content = content.Trim();
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        var paragraph = CreateCodeBlockParagraph();
                        
                        // 处理多行代码，保持换行格式
                        var lines = content.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (i > 0)
                            {
                                paragraph.Inlines.Add(new LineBreak());
                            }
                            
                            var run = new Run(lines[i])
                            {
                                FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace"),
                                FontSize = 13,
                                Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212))
                            };
                            paragraph.Inlines.Add(run);
                        }
                        
                        document.Blocks.Add(paragraph);
                    }
                }
            }
            catch
            {
                var paragraph = CreateStyledParagraph();
                paragraph.Inlines.Add(new Run(htmlLine));
                document.Blocks.Add(paragraph);
            }
        }
        
        /// <summary>
        /// 处理 CODE 代码块
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        private void ProcessCodeBlock(string htmlLine, FlowDocument document, HtmlProcessingContext context)
        {
            try
            {
                // 提取 code 标签内容
                var match = System.Text.RegularExpressions.Regex.Match(htmlLine, @"<code[^>]*>(.*?)</code>", 
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                if (match.Success)
                {
                    var content = match.Groups[1].Value;
                    content = System.Net.WebUtility.HtmlDecode(content);
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        // 检查是否是内联代码（单行且较短）
                        if (!content.Contains('\n') && content.Length < 100)
                        {
                            // 内联代码：作为 Run 处理，通常会被其他处理器的 ProcessInlineElements 处理
                            var paragraph = CreateStyledParagraph();
                            var run = new Run(content)
                            {
                                FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace"),
                                FontSize = 13,
                                Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
                                Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212)),
                            };
                            paragraph.Inlines.Add(run);
                            document.Blocks.Add(paragraph);
                        }
                        else
                        {
                            // 代码块：作为独立段落处理
                            var paragraph = CreateCodeBlockParagraph();
                            var run = new Run(content)
                            {
                                FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace"),
                                FontSize = 13,
                                Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212))
                            };
                            paragraph.Inlines.Add(run);
                            document.Blocks.Add(paragraph);
                        }
                    }
                }
            }
            catch
            {
                var paragraph = CreateStyledParagraph();
                paragraph.Inlines.Add(new Run(htmlLine));
                document.Blocks.Add(paragraph);
            }
        }
        
        /// <summary>
        /// 处理 Markdig 生成的嵌套代码块 (pre > code)
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        private void ProcessNestedCodeBlock(string htmlLine, FlowDocument document, HtmlProcessingContext context)
        {
            try
            {
                // 提取 pre > code 嵌套结构的内容
                var match = System.Text.RegularExpressions.Regex.Match(htmlLine, @"<pre><code[^>]*>(.*?)</code></pre>", 
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                if (match.Success)
                {
                    var content = match.Groups[1].Value;
                    content = System.Net.WebUtility.HtmlDecode(content);
                    
                    // 保持原始格式，不要去除换行符
                    content = content.Trim();
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        var paragraph = CreateCodeBlockParagraph();
                        
                        // 处理多行代码，保持换行格式
                        var lines = content.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (i > 0)
                            {
                                paragraph.Inlines.Add(new LineBreak());
                            }
                            
                            var run = new Run(lines[i])
                            {
                                FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace"),
                                FontSize = 13,
                                Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212))
                            };
                            paragraph.Inlines.Add(run);
                        }
                        
                        document.Blocks.Add(paragraph);
                    }
                }
            }
            catch
            {
                var paragraph = CreateStyledParagraph();
                paragraph.Inlines.Add(new Run(htmlLine));
                document.Blocks.Add(paragraph);
            }
        }
        
        /// <summary>
        /// 创建代码块段落
        /// </summary>
        /// <returns>代码块段落</returns>
        private Paragraph CreateCodeBlockParagraph()
        {
            return new Paragraph
            {
                Margin = new Thickness(0, 8, 0, 8),
                Padding = new Thickness(12),
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace"),
                FontSize = 13,
                LineHeight = 18
            };
        }
    }
}