using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace QuickStarted.Controls.HtmlProcessors
{
    /// <summary>
    /// HTML 处理器基类，提供通用的处理逻辑
    /// </summary>
    public abstract class BaseHtmlProcessor : IHtmlProcessor
    {
        /// <summary>
        /// 检查是否可以处理指定的 HTML 行
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <returns>是否可以处理</returns>
        public abstract bool CanProcess(string htmlLine);
        
        /// <summary>
        /// 处理 HTML 行并添加到文档中
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        public abstract void Process(string htmlLine, FlowDocument document, HtmlProcessingContext context);
        
        /// <summary>
        /// 处理内联元素（粗体、斜体、链接等）
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="paragraph">段落</param>
        protected virtual void ProcessInlineElements(string content, Paragraph paragraph)
        {
            try
            {
                // 处理换行符
                content = content.Replace("\n", " ").Replace("\r", "");
                
                // 处理链接
                content = System.Text.RegularExpressions.Regex.Replace(content, @"<a\s+href=[""']([^""']*)[""'][^>]*>(.*?)</a>", 
                    match => $"\u0001LINK_START\u0001{match.Groups[1].Value}\u0001LINK_TEXT\u0001{match.Groups[2].Value}\u0001LINK_END\u0001");
                
                // 处理粗体
                content = System.Text.RegularExpressions.Regex.Replace(content, @"<strong>(.*?)</strong>", 
                    match => $"\u0001BOLD_START\u0001{match.Groups[1].Value}\u0001BOLD_END\u0001");
                content = System.Text.RegularExpressions.Regex.Replace(content, @"<b>(.*?)</b>", 
                    match => $"\u0001BOLD_START\u0001{match.Groups[1].Value}\u0001BOLD_END\u0001");
                
                // 处理斜体
                content = System.Text.RegularExpressions.Regex.Replace(content, @"<em>(.*?)</em>", 
                    match => $"\u0001ITALIC_START\u0001{match.Groups[1].Value}\u0001ITALIC_END\u0001");
                content = System.Text.RegularExpressions.Regex.Replace(content, @"<i>(.*?)</i>", 
                    match => $"\u0001ITALIC_START\u0001{match.Groups[1].Value}\u0001ITALIC_END\u0001");
                
                // 处理内联代码
                content = System.Text.RegularExpressions.Regex.Replace(content, @"<code>(.*?)</code>", 
                    match => $"\u0001CODE_START\u0001{match.Groups[1].Value}\u0001CODE_END\u0001");
                
                // 移除其他 HTML 标签
                content = System.Text.RegularExpressions.Regex.Replace(content, @"<[^>]+>", "");
                content = System.Net.WebUtility.HtmlDecode(content);
                
                // 解析格式化标记并创建 Run
                var parts = content.Split(new[] { '\u0001' }, StringSplitOptions.None);
                bool isBold = false, isItalic = false, isCode = false;
                string linkUrl = null;
                
                foreach (var part in parts)
                {
                    if (part == "BOLD_START")
                        isBold = true;
                    else if (part == "BOLD_END")
                        isBold = false;
                    else if (part == "ITALIC_START")
                        isItalic = true;
                    else if (part == "ITALIC_END")
                        isItalic = false;
                    else if (part == "CODE_START")
                        isCode = true;
                    else if (part == "CODE_END")
                        isCode = false;
                    else if (part == "LINK_START")
                    {
                        // 下一个部分是URL
                    }
                    else if (part == "LINK_TEXT")
                    {
                        // 下一个部分是链接文本
                    }
                    else if (part == "LINK_END")
                    {
                        linkUrl = null;
                    }
                    else if (!string.IsNullOrEmpty(part))
                    {
                        // 检查是否是链接URL
                        var prevIndex = Array.IndexOf(parts, part) - 1;
                        if (prevIndex >= 0 && parts[prevIndex] == "LINK_START")
                        {
                            linkUrl = part;
                            continue;
                        }
                        
                        // 检查是否是链接文本
                        if (prevIndex >= 0 && parts[prevIndex] == "LINK_TEXT" && !string.IsNullOrEmpty(linkUrl))
                        {
                            try
                            {
                                var hyperlink = new Hyperlink(new Run(part))
                                {
                                    NavigateUri = new Uri(linkUrl, UriKind.RelativeOrAbsolute),
                                    Foreground = new SolidColorBrush(Color.FromRgb(0, 102, 204))
                                };
                                hyperlink.RequestNavigate += OnRequestNavigate;
                                paragraph.Inlines.Add(hyperlink);
                            }
                            catch
                            {
                                // 如果创建链接失败，添加普通文本
                                paragraph.Inlines.Add(new Run(part));
                            }
                            continue;
                        }
                        
                        var run = new Run(part);
                        
                        if (isBold)
                            run.FontWeight = FontWeights.Bold;
                        if (isItalic)
                            run.FontStyle = FontStyles.Italic;
                        if (isCode)
                        {
                            run.FontFamily = new FontFamily("Consolas, Courier New, monospace");
                            run.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                        }
                        
                        paragraph.Inlines.Add(run);
                    }
                }
            }
            catch
            {
                // 如果处理失败，添加原始内容
                paragraph.Inlines.Add(new Run(content));
            }
        }
        
        /// <summary>
        /// 处理超链接点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // 使用默认浏览器打开链接
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.ToString(),
                    UseShellExecute = true
                });
            }
            catch
            {
                // 忽略打开链接失败的错误
            }
            
            e.Handled = true;
        }
        
        /// <summary>
        /// 创建带有默认样式的段落
        /// </summary>
        /// <param name="margin">边距</param>
        /// <returns>段落</returns>
        protected virtual Paragraph CreateStyledParagraph(Thickness? margin = null)
        {
            var paragraph = new Paragraph();
            if (margin.HasValue)
            {
                paragraph.Margin = margin.Value;
            }
            return paragraph;
        }
    }
}