using System.Threading.Tasks;

namespace QuickStarted.Services
{
    /// <summary>
    /// 视频缩略图生成服务接口
    /// </summary>
    public interface IThumbnailService
    {
        /// <summary>
        /// 为视频生成缩略图
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <param name="outputPath">输出缩略图路径</param>
        /// <param name="timePosition">截取时间位置（秒）</param>
        /// <returns>是否生成成功</returns>
        Task<bool> GenerateThumbnailAsync(string videoPath, string outputPath, double timePosition = 1.0);
        
        /// <summary>
        /// 检查并生成缺失的缩略图
        /// </summary>
        /// <param name="videoPath">视频文件路径</param>
        /// <returns>缩略图路径（如果生成成功）</returns>
        Task<string?> EnsureThumbnailExistsAsync(string videoPath);
    }
}