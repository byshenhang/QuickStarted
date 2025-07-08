using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace QuickStarted.Controls.HtmlProcessors
{
    /// <summary>
    /// 列表处理器，处理 UL、OL、LI 标签
    /// </summary>
    public class ListProcessor : BaseHtmlProcessor
    {
        /// <summary>
        /// 检查是否可以处理指定的 HTML 行
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <returns>是否可以处理</returns>
        public override bool CanProcess(string htmlLine)
        {
            var trimmed = htmlLine.TrimStart();
            return trimmed.StartsWith("<ul>") || 
                   trimmed.StartsWith("<ol>") || 
                   trimmed.StartsWith("<li>") ||
                   trimmed.StartsWith("<li ");
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
            
            if (trimmed.StartsWith("<ul>"))
            {
                ProcessUnorderedListStart(context);
            }
            else if (trimmed.StartsWith("<ol>"))
            {
                ProcessOrderedListStart(context);
            }
            else if (trimmed.StartsWith("<li"))
            {
                ProcessListItem(htmlLine, document, context);
            }
        }
        
        /// <summary>
        /// 处理无序列表开始
        /// </summary>
        /// <param name="context">处理上下文</param>
        private void ProcessUnorderedListStart(HtmlProcessingContext context)
        {
            context.CurrentListType = ListType.Unordered;
            context.ListNestingLevel++;
        }
        
        /// <summary>
        /// 处理有序列表开始
        /// </summary>
        /// <param name="context">处理上下文</param>
        private void ProcessOrderedListStart(HtmlProcessingContext context)
        {
            context.CurrentListType = ListType.Ordered;
            context.ListNestingLevel++;
            // 重置有序列表计数器
            context.SetConfiguration($"OrderedListCounter_{context.ListNestingLevel}", 0);
        }
        
        /// <summary>
        /// 处理列表项
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        private void ProcessListItem(string htmlLine, FlowDocument document, HtmlProcessingContext context)
        {
            try
            {
                // 提取列表项内容，支持多行列表项
                var match = System.Text.RegularExpressions.Regex.Match(htmlLine, @"<li[^>]*>(.*?)</li>", 
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                if (match.Success)
                {
                    var content = match.Groups[1].Value;
                    content = System.Net.WebUtility.HtmlDecode(content.Trim());
                    
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var paragraph = CreateListItemParagraph(context);
                        AddListMarker(paragraph, context);
                        ProcessInlineElements(content, paragraph);
                        document.Blocks.Add(paragraph);
                    }
                }
                else
                {
                    // 如果不是完整的列表项标签，按原方式处理
                    var content = System.Text.RegularExpressions.Regex.Replace(htmlLine, @"</?li[^>]*>", "");
                    content = System.Net.WebUtility.HtmlDecode(content.Trim());
                    
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var paragraph = CreateListItemParagraph(context);
                        AddListMarker(paragraph, context);
                        ProcessInlineElements(content, paragraph);
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
        /// 创建列表项段落
        /// </summary>
        /// <param name="context">处理上下文</param>
        /// <returns>段落</returns>
        private Paragraph CreateListItemParagraph(HtmlProcessingContext context)
        {
            var leftMargin = 20 * context.ListNestingLevel;
            return CreateStyledParagraph(new Thickness(leftMargin, 0, 0, 4));
        }
        
        /// <summary>
        /// 添加列表标记（项目符号或数字）
        /// </summary>
        /// <param name="paragraph">段落</param>
        /// <param name="context">处理上下文</param>
        private void AddListMarker(Paragraph paragraph, HtmlProcessingContext context)
        {
            Run markerRun;
            
            if (context.CurrentListType == ListType.Ordered)
            {
                // 有序列表：使用数字
                var counterKey = $"OrderedListCounter_{context.ListNestingLevel}";
                var counter = context.GetConfiguration(counterKey, 0) + 1;
                context.SetConfiguration(counterKey, counter);
                
                markerRun = new Run($"{counter}. ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                };
            }
            else
            {
                // 无序列表：使用项目符号
                var bullet = GetBulletForLevel(context.ListNestingLevel);
                markerRun = new Run($"{bullet} ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                };
            }
            
            paragraph.Inlines.Add(markerRun);
        }
        
        /// <summary>
        /// 获取指定嵌套级别的项目符号
        /// </summary>
        /// <param name="level">嵌套级别</param>
        /// <returns>项目符号</returns>
        private string GetBulletForLevel(int level)
        {
            return (level % 3) switch
            {
                1 => "•",  // 实心圆点
                2 => "◦",  // 空心圆点
                0 => "▪",  // 实心方块
                _ => "•"   // 默认
            };
        }
    }
}