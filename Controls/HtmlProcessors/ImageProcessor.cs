using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace QuickStarted.Controls.HtmlProcessors
{
    /// <summary>
    /// 图片处理器，处理 IMG 标签
    /// </summary>
    public class ImageProcessor : BaseHtmlProcessor
    {
        /// <summary>
        /// 检查是否可以处理指定的 HTML 行
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <returns>是否可以处理</returns>
        public override bool CanProcess(string htmlLine)
        {
            return htmlLine.Contains("<img ");
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
                // 提取图片的 src 和 alt 属性
                var srcMatch = System.Text.RegularExpressions.Regex.Match(htmlLine, @"src=[""']([^""']+)[""']");
                var altMatch = System.Text.RegularExpressions.Regex.Match(htmlLine, @"alt=[""']([^""']*)[""']");
                
                if (srcMatch.Success)
                {
                    var src = srcMatch.Groups[1].Value;
                    var alt = altMatch.Success ? altMatch.Groups[1].Value : "";
                    
                    ProcessImage(src, alt, document, context);
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
        /// 处理图片
        /// </summary>
        /// <param name="src">图片源路径</param>
        /// <param name="alt">替代文本</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        private void ProcessImage(string src, string alt, FlowDocument document, HtmlProcessingContext context)
        {
            try
            {
                var imagePath = ResolveImagePath(src, context.BaseUri);
                
                if (File.Exists(imagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    
                    var image = new Image
                    {
                        Source = bitmap,
                        Stretch = System.Windows.Media.Stretch.Uniform,
                        MaxWidth = 600,
                        MaxHeight = 400,
                        Margin = new Thickness(0, 8, 0, 8)
                    };
                    
                    // 如果有替代文本，设置工具提示
                    if (!string.IsNullOrWhiteSpace(alt))
                    {
                        image.ToolTip = alt;
                    }
                    
                    var container = new BlockUIContainer(image)
                    {
                        Margin = new Thickness(0, 8, 0, 8)
                    };
                    
                    document.Blocks.Add(container);
                    
                    // 如果有替代文本，在图片下方显示
                    if (!string.IsNullOrWhiteSpace(alt))
                    {
                        var captionParagraph = CreateStyledParagraph(new Thickness(0, 0, 0, 8));
                        captionParagraph.TextAlignment = TextAlignment.Center;
                        captionParagraph.FontStyle = FontStyles.Italic;
                        captionParagraph.FontSize = 12;
                        captionParagraph.Inlines.Add(new Run(alt));
                        document.Blocks.Add(captionParagraph);
                    }
                }
                else
                {
                    // 图片文件不存在，显示详细的调试信息
                    var paragraph = CreateStyledParagraph();
                    var baseUriInfo = context.BaseUri != null ? context.BaseUri.ToString() : "null";
                    var displayText = $"[图片未找到]\n原始路径: {src}\n解析路径: {imagePath}\n基础URI: {baseUriInfo}";
                    
                    if (!string.IsNullOrWhiteSpace(alt))
                    {
                        displayText = $"[图片: {alt}]\n" + displayText;
                    }
                    
                    paragraph.Inlines.Add(new Run(displayText)
                    {
                        FontStyle = FontStyles.Italic,
                        Foreground = System.Windows.Media.Brushes.Orange
                    });
                    document.Blocks.Add(paragraph);
                }
            }
            catch (Exception ex)
            {
                // 处理图片时出错，显示详细错误信息
                var paragraph = CreateStyledParagraph();
                var displayText = $"[图片加载失败]\n原始路径: {src}\n错误: {ex.Message}";
                
                if (!string.IsNullOrWhiteSpace(alt))
                {
                    displayText = $"[图片: {alt}]\n" + displayText;
                }
                
                paragraph.Inlines.Add(new Run(displayText)
                {
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Red
                });
                document.Blocks.Add(paragraph);
            }
        }
        
        /// <summary>
        /// 解析图片路径
        /// </summary>
        /// <param name="src">原始路径</param>
        /// <param name="baseUri">基础URI</param>
        /// <returns>解析后的绝对路径</returns>
        private string ResolveImagePath(string src, Uri baseUri)
        {
            // 如果是绝对路径，直接返回
            if (Path.IsPathRooted(src) || Uri.IsWellFormedUriString(src, UriKind.Absolute))
            {
                return src;
            }
            
            // 如果有基础URI，相对于基础URI解析
            if (baseUri != null)
            {
                try
                {
                    var resolvedUri = new Uri(baseUri, src);
                    var localPath = resolvedUri.LocalPath;
                    
                    // 确保路径存在
                    if (File.Exists(localPath))
                    {
                        return localPath;
                    }
                }
                catch
                {
                    // 解析失败，尝试其他方法
                }
            }
            
            // 尝试相对于当前工作目录解析
            try
            {
                var fullPath = Path.GetFullPath(src);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            catch
            {
                // 忽略异常
            }
            
            return src;
        }
    }
}