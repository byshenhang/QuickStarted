using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace QuickStarted.Controls.HtmlProcessors
{
    /// <summary>
    /// 表格处理器，处理 TABLE、TR、TD、TH 标签
    /// </summary>
    public class TableProcessor : BaseHtmlProcessor
    {
        /// <summary>
        /// 检查是否可以处理指定的 HTML 行
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <returns>是否可以处理</returns>
        public override bool CanProcess(string htmlLine)
        {
            var trimmed = htmlLine.TrimStart();
            return trimmed.StartsWith("<table") ||
                   trimmed.StartsWith("<tr") ||
                   trimmed.StartsWith("<td") ||
                   trimmed.StartsWith("<th") ||
                   trimmed.StartsWith("</table>") ||
                   trimmed.StartsWith("</tr>") ||
                   trimmed.StartsWith("</td>") ||
                   trimmed.StartsWith("</th>");
        }

        /// <summary>
        /// 处理表格相关的 HTML 标签
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        public override void Process(string htmlLine, FlowDocument document, HtmlProcessingContext context)
        {
            var trimmed = htmlLine.TrimStart();
            
            if (trimmed.StartsWith("<table"))
            {
                ProcessTableStart(document, context);
            }
            else if (trimmed.StartsWith("</table>"))
            {
                ProcessTableEnd(document, context);
            }
            else if (trimmed.StartsWith("<tr"))
            {
                ProcessRowStart(document, context);
            }
            else if (trimmed.StartsWith("</tr>"))
            {
                ProcessRowEnd(document, context);
            }
            else if (trimmed.StartsWith("<th"))
            {
                ProcessHeaderCell(htmlLine, document, context);
            }
            else if (trimmed.StartsWith("<td"))
            {
                ProcessDataCell(htmlLine, document, context);
            }
        }

        /// <summary>
        /// 处理表格开始标签
        /// </summary>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        private void ProcessTableStart(FlowDocument document, HtmlProcessingContext context)
        {
            // 创建 WPF Table 元素
            var table = new Table()
            {
                CellSpacing = 0,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                Margin = new Thickness(0, 10, 0, 10)
            };
            
            // 添加默认列定义（3列，对应示例表格）
            for (int i = 0; i < 3; i++)
            {
                table.Columns.Add(new TableColumn()
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
            }
            
            // 设置表格状态
            context.SetProperty("IsInTable", true);
            context.SetProperty("CurrentTable", table);
            context.SetProperty("CurrentRowGroup", null);
            context.SetProperty("CurrentRow", null);
            context.SetProperty("IsHeaderRow", false);
            
            document.Blocks.Add(table);
        }

        /// <summary>
        /// 处理表格结束标签
        /// </summary>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        private void ProcessTableEnd(FlowDocument document, HtmlProcessingContext context)
        {
            context.SetProperty("IsInTable", false);
            context.SetProperty("CurrentTable", null);
            context.SetProperty("CurrentRowGroup", null);
            context.SetProperty("CurrentRow", null);
            context.SetProperty("IsHeaderRow", false);
        }

        /// <summary>
        /// 处理行开始标签
        /// </summary>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        private void ProcessRowStart(FlowDocument document, HtmlProcessingContext context)
        {
            var table = context.GetProperty<Table>("CurrentTable");
            if (table != null)
            {
                // 确保有 RowGroup
                var rowGroup = context.GetProperty<TableRowGroup>("CurrentRowGroup");
                if (rowGroup == null)
                {
                    rowGroup = new TableRowGroup();
                    table.RowGroups.Add(rowGroup);
                    context.SetProperty("CurrentRowGroup", rowGroup);
                }
                
                // 创建新行
                var row = new TableRow();
                rowGroup.Rows.Add(row);
                context.SetProperty("CurrentRow", row);
            }
        }

        /// <summary>
        /// 处理行结束标签
        /// </summary>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        private void ProcessRowEnd(FlowDocument document, HtmlProcessingContext context)
        {
            context.SetProperty("CurrentRow", null);
            context.SetProperty("IsHeaderRow", false);
        }

        /// <summary>
        /// 处理表头单元格
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        private void ProcessHeaderCell(string htmlLine, FlowDocument document, HtmlProcessingContext context)
        {
            var content = ExtractCellContent(htmlLine, "th");
            if (!string.IsNullOrEmpty(content))
            {
                var row = context.GetProperty<TableRow>("CurrentRow");
                if (row != null)
                {
                    var cell = new TableCell()
                    {
                        BorderThickness = new Thickness(1),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                        Background = new SolidColorBrush(Color.FromArgb(128, 50, 50, 50)),
                        Padding = new Thickness(8, 4, 8, 4)
                    };
                    
                    var paragraph = new Paragraph()
                    {
                        Margin = new Thickness(0)
                    };
                    
                    var run = new Run(content)
                    {
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                        FontSize = 14
                    };
                    
                    paragraph.Inlines.Add(run);
                    cell.Blocks.Add(paragraph);
                    row.Cells.Add(cell);
                    
                    context.SetProperty("IsHeaderRow", true);
                }
            }
        }

        /// <summary>
        /// 处理数据单元格
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        private void ProcessDataCell(string htmlLine, FlowDocument document, HtmlProcessingContext context)
        {
            var content = ExtractCellContent(htmlLine, "td");
            if (!string.IsNullOrEmpty(content))
            {
                var row = context.GetProperty<TableRow>("CurrentRow");
                if (row != null)
                {
                    var cell = new TableCell()
                    {
                        BorderThickness = new Thickness(1),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68)),
                        Background = new SolidColorBrush(Color.FromArgb(128, 40, 40, 40)),
                        Padding = new Thickness(8, 4, 8, 4)
                    };
                    
                    var paragraph = new Paragraph()
                    {
                        Margin = new Thickness(0)
                    };
                    
                    var run = new Run(content)
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212)),
                        FontSize = 13
                    };
                    
                    paragraph.Inlines.Add(run);
                    cell.Blocks.Add(paragraph);
                    row.Cells.Add(cell);
                }
            }
        }

        /// <summary>
        /// 提取单元格内容
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="tagName">标签名称（th 或 td）</param>
        /// <returns>单元格内容</returns>
        private string ExtractCellContent(string htmlLine, string tagName)
        {
            try
            {
                var pattern = $@"<{tagName}[^>]*>(.*?)</{tagName}>";
                var match = System.Text.RegularExpressions.Regex.Match(htmlLine, pattern, 
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                    
                if (match.Success)
                {
                    var content = match.Groups[1].Value;
                    content = System.Net.WebUtility.HtmlDecode(content.Trim());
                    return content;
                }
            }
            catch
            {
                // 如果正则表达式匹配失败，返回空字符串
            }
            
            return string.Empty;
        }
    }
}