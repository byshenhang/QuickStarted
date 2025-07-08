using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace QuickStarted.Controls.HtmlProcessors
{
    /// <summary>
    /// 标题处理器，处理 H1-H6 标签
    /// </summary>
    public class HeadingProcessor : BaseHtmlProcessor
    {
        /// <summary>
        /// 检查是否可以处理指定的 HTML 行
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <returns>是否可以处理</returns>
        public override bool CanProcess(string htmlLine)
        {
            return htmlLine.TrimStart().StartsWith("<h") && 
                   System.Text.RegularExpressions.Regex.IsMatch(htmlLine, @"<h[1-6](?:\s[^>]*)?>.*?</h[1-6]>");
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
                // 支持带属性的标题标签，如 <h1 id="xxx">title</h1>
                var match = System.Text.RegularExpressions.Regex.Match(htmlLine, @"<h(\d)(?:\s[^>]*)?>\s*(.*?)\s*</h\d>");
                if (match.Success)
                {
                    var level = int.Parse(match.Groups[1].Value);
                    var text = System.Net.WebUtility.HtmlDecode(match.Groups[2].Value.Trim());
                    
                    var paragraph = CreateStyledParagraph();
                    
                    // 处理标题文本中的内联元素
                    ProcessInlineElements(text, paragraph);
                    
                    // 为段落中的所有 Run 应用标题样式
                     foreach (var inline in paragraph.Inlines)
                     {
                         if (inline is Run run)
                         {
                             run.FontWeight = FontWeights.Bold;
                             run.FontSize = GetFontSizeForLevel(level);
                             run.Foreground = GetForegroundForLevel(level);
                         }
                     }
                    
                    paragraph.Margin = new Thickness(0, GetTopMarginForLevel(level), 0, GetBottomMarginForLevel(level));
                    document.Blocks.Add(paragraph);
                }
            }
            catch
            {
                // 处理失败，添加原文
                var paragraph = CreateStyledParagraph();
                paragraph.Inlines.Add(new Run(htmlLine));
                document.Blocks.Add(paragraph);
            }
        }
        
        /// <summary>
        /// 获取标题级别对应的字体大小
        /// </summary>
        /// <param name="level">标题级别</param>
        /// <returns>字体大小</returns>
        private double GetFontSizeForLevel(int level)
        {
            return level switch
            {
                1 => 24,
                2 => 20,
                3 => 16,
                4 => 14,
                5 => 12,
                6 => 10,
                _ => 12
            };
        }
        
        /// <summary>
        /// 获取标题级别对应的前景色
        /// </summary>
        /// <param name="level">标题级别</param>
        /// <returns>前景色</returns>
        private Brush GetForegroundForLevel(int level)
        {
            return level switch
            {
                1 => new SolidColorBrush(Color.FromRgb(255, 255, 255)), // 白色
                2 => new SolidColorBrush(Color.FromRgb(240, 240, 240)), // 浅灰色
                3 => new SolidColorBrush(Color.FromRgb(220, 220, 220)), // 更浅的灰色
                _ => new SolidColorBrush(Color.FromRgb(200, 200, 200))   // 默认灰色
            };
        }
        
        /// <summary>
        /// 获取标题级别对应的上边距
        /// </summary>
        /// <param name="level">标题级别</param>
        /// <returns>上边距</returns>
        private double GetTopMarginForLevel(int level)
        {
            return level <= 2 ? 15 : 10;
        }
        
        /// <summary>
        /// 获取标题级别对应的下边距
        /// </summary>
        /// <param name="level">标题级别</param>
        /// <returns>下边距</returns>
        private double GetBottomMarginForLevel(int level)
        {
            return level <= 2 ? 10 : 5;
        }
    }
}