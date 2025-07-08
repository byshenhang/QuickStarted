using System.Windows;
using System.Windows.Documents;

namespace QuickStarted.Controls.HtmlProcessors
{
    /// <summary>
    /// 段落处理器，处理 P 标签
    /// </summary>
    public class ParagraphProcessor : BaseHtmlProcessor
    {
        /// <summary>
        /// 检查是否可以处理指定的 HTML 行
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <returns>是否可以处理</returns>
        public override bool CanProcess(string htmlLine)
        {
            return htmlLine.TrimStart().StartsWith("<p>") || 
                   htmlLine.TrimStart().StartsWith("<p ");
        }
        
        /// <summary>
        /// 处理 HTML 行并添加到文档中
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        public override void Process(string htmlLine, FlowDocument document, HtmlProcessingContext context)
        {
            try
            {
                // 提取段落内容，支持多行段落
                var match = System.Text.RegularExpressions.Regex.Match(htmlLine, @"<p[^>]*>(.*?)</p>", 
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                if (match.Success)
                {
                    var content = match.Groups[1].Value;
                    content = System.Net.WebUtility.HtmlDecode(content.Trim());
                    
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var paragraph = CreateStyledParagraph(new Thickness(0, 0, 0, 8));
                        ProcessInlineElements(content, paragraph);
                        
                        if (paragraph.Inlines.Count > 0)
                        {
                            document.Blocks.Add(paragraph);
                        }
                    }
                }
                else
                {
                    // 如果不是完整的段落标签，按原方式处理
                    var content = System.Text.RegularExpressions.Regex.Replace(htmlLine, @"</?p[^>]*>", "");
                    content = System.Net.WebUtility.HtmlDecode(content.Trim());
                    
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var paragraph = CreateStyledParagraph(new Thickness(0, 0, 0, 8));
                        ProcessInlineElements(content, paragraph);
                        
                        if (paragraph.Inlines.Count > 0)
                        {
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
    }
}