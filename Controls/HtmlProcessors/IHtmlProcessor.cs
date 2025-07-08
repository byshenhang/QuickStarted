using System.Windows.Documents;

namespace QuickStarted.Controls.HtmlProcessors
{
    /// <summary>
    /// HTML 处理器接口
    /// </summary>
    public interface IHtmlProcessor
    {
        /// <summary>
        /// 检查是否可以处理指定的 HTML 行
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <returns>是否可以处理</returns>
        bool CanProcess(string htmlLine);
        
        /// <summary>
        /// 处理 HTML 行并添加到文档中
        /// </summary>
        /// <param name="htmlLine">HTML 行</param>
        /// <param name="document">流文档</param>
        /// <param name="context">处理上下文</param>
        void Process(string htmlLine, FlowDocument document, HtmlProcessingContext context);
    }
}