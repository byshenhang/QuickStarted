using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace QuickStarted.Services
{
    /// <summary>
    /// 视频缩略图生成服务实现
    /// </summary>
    public class ThumbnailService : IThumbnailService
    {
        private readonly ILogService _logService;
        private string _ffmpegPath;

        public ThumbnailService(ILogService logService)
        {
            _logService = logService;
            try
            {
                _ffmpegPath = FindFFmpegPath();
            }
            catch (FileNotFoundException ex)
            {
                _logService.LogWarning($"初始化时未找到FFmpeg: {ex.Message}");
                _ffmpegPath = string.Empty; // 设置为空，后续会重新尝试查找
            }
        }

        /// <summary>
        /// 检查FFmpeg是否可用
        /// </summary>
        /// <param name="ffmpegPath">FFmpeg路径或命令名</param>
        /// <returns>是否可用</returns>
        private bool IsFFmpegAvailable(string ffmpegPath)
        {
            if (string.IsNullOrEmpty(ffmpegPath))
                return false;

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = "-version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit();
                
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 查找FFmpeg可执行文件路径
        /// </summary>
        /// <returns>FFmpeg路径</returns>
        private string FindFFmpegPath()
        {
            // 首先尝试使用ffmpeg命令（如果在PATH中）
            if (IsFFmpegAvailable("ffmpeg"))
            {
                _logService.LogInfo("找到系统PATH中的FFmpeg");
                return "ffmpeg";
            }

            // 尝试使用where命令查找完整路径
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = "ffmpeg",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    var lines = output.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var ffmpegPath = line.Trim();
                        if (File.Exists(ffmpegPath) && ffmpegPath.EndsWith("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            _logService.LogInfo($"找到系统FFmpeg: {ffmpegPath}");
                            return ffmpegPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"使用where命令查找FFmpeg失败: {ex.Message}");
            }

            // 检查应用程序目录下的FFmpeg
            var localPaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tool", "ffmpeg.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe")
            };

            foreach (var path in localPaths)
            {
                if (File.Exists(path))
                {
                    _logService.LogInfo($"找到本地FFmpeg: {path}");
                    return path;
                }
            }

            _logService.LogWarning("未找到FFmpeg，缩略图生成功能将不可用");
            throw new FileNotFoundException("未找到FFmpeg可执行文件。请确保已安装FFmpeg并添加到系统PATH中，或将ffmpeg.exe放置在应用程序目录下。");
        }

        /// <summary>
        /// 为视频生成缩略图
        /// </summary>
        public async Task<bool> GenerateThumbnailAsync(string videoPath, string outputPath, double timePosition = 1.0)
        {
            // 检查FFmpeg是否可用
            if (string.IsNullOrEmpty(_ffmpegPath) || !IsFFmpegAvailable(_ffmpegPath))
            {
                try
                {
                    _logService.LogInfo("重新查找FFmpeg路径...");
                    _ffmpegPath = FindFFmpegPath();
                }
                catch (FileNotFoundException ex)
                {
                    _logService.LogError($"FFmpeg不可用，无法生成缩略图: {ex.Message}");
                    return false;
                }
            }

            if (!File.Exists(videoPath))
            {
                _logService.LogWarning($"视频文件不存在: {videoPath}");
                return false;
            }

            try
            {
                // 确保输出目录存在
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 构建FFmpeg命令
                var arguments = $"-i \"{videoPath}\" -ss {timePosition:F1} -vframes 1 -q:v 2 -y \"{outputPath}\"";
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _ffmpegPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _logService.LogDebug($"执行FFmpeg命令: {_ffmpegPath} {arguments}");
                
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && File.Exists(outputPath))
                {
                    _logService.LogInfo($"成功生成缩略图: {outputPath}");
                    return true;
                }
                else
                {
                    _logService.LogError($"生成缩略图失败，退出码: {process.ExitCode}, 错误: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"生成缩略图异常: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 检查并生成缺失的缩略图
        /// </summary>
        public async Task<string?> EnsureThumbnailExistsAsync(string videoPath)
        {
            if (!File.Exists(videoPath))
            {
                return null;
            }

            var videoDir = Path.GetDirectoryName(videoPath);
            var videoName = Path.GetFileNameWithoutExtension(videoPath);
            
            // 检查是否已存在缩略图
            var thumbnailExtensions = new[] { ".jpg", ".png", ".jpeg" };
            foreach (var ext in thumbnailExtensions)
            {
                var existingThumbnail = Path.Combine(videoDir!, videoName + ext);
                if (File.Exists(existingThumbnail))
                {
                    return existingThumbnail;
                }
            }

            // 如果不存在，生成新的缩略图
            var thumbnailPath = Path.Combine(videoDir!, videoName + ".jpg");
            var success = await GenerateThumbnailAsync(videoPath, thumbnailPath);
            
            return success ? thumbnailPath : null;
        }
    }
}