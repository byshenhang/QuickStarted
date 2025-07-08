using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using Markdig;
using QuickStarted.Controls.HtmlProcessors;
using Brushes = System.Windows.Media.Brushes;

namespace QuickStarted.Controls
{
    /// <summary>
    /// 自定义 Markdown 查看器控件
    /// </summary>
    public class MarkdownViewer : Control
    {
        private FlowDocumentScrollViewer? _viewer;
        private MarkdownPipeline? _pipeline;

        /// <summary>
        /// Markdown 内容依赖属性
        /// </summary>
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(
                nameof(Markdown),
                typeof(string),
                typeof(MarkdownViewer),
                new PropertyMetadata(string.Empty, OnMarkdownChanged));

        /// <summary>
        /// 基础 URI 依赖属性，用于解析相对路径
        /// </summary>
        public static readonly DependencyProperty BaseUriProperty =
            DependencyProperty.Register(
                nameof(BaseUri),
                typeof(Uri),
                typeof(MarkdownViewer),
                new PropertyMetadata(null, OnBaseUriChanged));

        /// <summary>
        /// Markdown 内容
        /// </summary>
        public string Markdown
        {
            get => (string)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }

        /// <summary>
        /// 基础 URI
        /// </summary>
        public Uri? BaseUri
        {
            get => (Uri?)GetValue(BaseUriProperty);
            set => SetValue(BaseUriProperty, value);
        }

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static MarkdownViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(MarkdownViewer),
                new FrameworkPropertyMetadata(typeof(MarkdownViewer)));
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public MarkdownViewer()
        {
            // 初始化 Markdig 管道，启用常用扩展
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseEmojiAndSmiley()
                .UsePipeTables()
                .UseGridTables()
                .UseFootnotes()
                .UseTaskLists()
                .UseMathematics()
                .UseGenericAttributes()
                .Build();
        }

        /// <summary>
        /// 应用模板时调用
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            _viewer = GetTemplateChild("PART_Viewer") as FlowDocumentScrollViewer;
            
            if (_viewer != null)
            {
                // 订阅超链接点击事件
                _viewer.AddHandler(Hyperlink.RequestNavigateEvent, new RequestNavigateEventHandler(OnRequestNavigate));
            }
            
            UpdateContent();
        }

        /// <summary>
        /// Markdown 内容变化时的回调
        /// </summary>
        /// <param name="d">依赖对象</param>
        /// <param name="e">事件参数</param>
        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownViewer viewer)
            {
                viewer.UpdateContent();
            }
        }

        /// <summary>
        /// 基础 URI 变化时的回调
        /// </summary>
        /// <param name="d">依赖对象</param>
        /// <param name="e">事件参数</param>
        private static void OnBaseUriChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownViewer viewer)
            {
                viewer.UpdateContent();
            }
        }

        /// <summary>
        /// 更新内容显示
        /// </summary>
        private void UpdateContent()
        {
            if (_viewer == null || _pipeline == null)
                return;

            // 如果 Markdown 内容为空，清空显示
            if (string.IsNullOrEmpty(Markdown))
            {
                ClearContent();
                return;
            }

            try
            {
                // 解析 Markdown 为 HTML
                var html = Markdig.Markdown.ToHtml(Markdown, _pipeline);
                
                // 使用新的处理器架构转换 HTML 为 FlowDocument
                var context = new HtmlProcessingContext { BaseUri = BaseUri };
                var manager = new HtmlProcessorManager();
                var document = manager.ProcessHtml(html, context);
                
                // 应用样式
                ApplyDocumentStyle(document);
                
                _viewer.Document = document;
            }
            catch (Exception ex)
            {
                // 如果解析失败，显示错误信息
                var errorDocument = new FlowDocument();
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run($"Markdown 解析错误: {ex.Message}") 
                { 
                    Foreground = Brushes.Red 
                });
                errorDocument.Blocks.Add(paragraph);
                _viewer.Document = errorDocument;
            }
        }

        /// <summary>
        /// 清空内容显示
        /// </summary>
        private void ClearContent()
        {
            if (_viewer != null)
            {
                _viewer.Document = new FlowDocument();
            }
        }

        /// <summary>
        /// 应用文档样式
        /// </summary>
        /// <param name="document">流文档</param>
        private void ApplyDocumentStyle(FlowDocument document)
        {
            // 设置文档基本样式
            document.FontFamily = new FontFamily("Segoe UI");
            document.FontSize = 12;
            document.Foreground = Brushes.White;
            document.Background = Brushes.Transparent;
            document.PagePadding = new Thickness(0);
            
            // 遍历所有块元素应用样式
            foreach (var block in document.Blocks)
            {
                ApplyBlockStyle(block);
            }
        }

        /// <summary>
        /// 应用块元素样式
        /// </summary>
        /// <param name="block">块元素</param>
        private void ApplyBlockStyle(Block block)
        {
            switch (block)
            {
                case Paragraph paragraph:
                    paragraph.Foreground = Brushes.White;
                    paragraph.Margin = new Thickness(0, 0, 0, 8);
                    break;
                    
                case Section section:
                    section.Foreground = Brushes.White;
                    foreach (var childBlock in section.Blocks)
                    {
                        ApplyBlockStyle(childBlock);
                    }
                    break;
                    
                case List list:
                    list.Foreground = Brushes.White;
                    list.Margin = new Thickness(0, 0, 0, 8);
                    break;
                    
                case Table table:
                    table.Foreground = Brushes.White;
                    table.BorderBrush = new SolidColorBrush(Color.FromRgb(68, 68, 68));
                    table.BorderThickness = new Thickness(1);
                    break;
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
                Process.Start(new ProcessStartInfo
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

    }
}