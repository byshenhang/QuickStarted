using QuickStarted.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickStarted.Services
{
    /// <summary>
    /// 数据服务实现
    /// </summary>
    public class DataService : IDataService
    {
        private readonly string _dataPath;
        private readonly ILogService _logService;
        private readonly IThumbnailService _thumbnailService;
        private ProgramMapping? _cachedMapping;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        /// <param name="thumbnailService">缩略图服务</param>
        public DataService(ILogService logService, IThumbnailService thumbnailService)
        {
            _logService = logService;
            _thumbnailService = thumbnailService;
            _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _logService.LogInfo($"DataService初始化，数据路径: {_dataPath}");
        }

        /// <summary>
        /// 加载程序映射配置
        /// </summary>
        /// <returns>程序映射配置</returns>
        public async Task<ProgramMapping?> LoadProgramMappingAsync()
        {
            if (_cachedMapping != null)
            {
                _logService.LogDebug("使用缓存的程序映射配置");
                return _cachedMapping;
            }

            try
            {
                var remapPath = Path.Combine(_dataPath, "Remap.json");
                _logService.LogInfo($"尝试加载程序映射配置文件: {remapPath}");
                
                if (!File.Exists(remapPath))
                {
                    _logService.LogError($"程序映射配置文件不存在: {remapPath}");
                    return null;
                }

                var jsonContent = await File.ReadAllTextAsync(remapPath);
                _logService.LogDebug($"读取到配置文件内容，长度: {jsonContent.Length} 字符");
                
                _cachedMapping = JsonSerializer.Deserialize<ProgramMapping>(jsonContent);
                
                if (_cachedMapping?.MapData != null)
                {
                    _logService.LogInfo($"成功加载程序映射配置，包含 {_cachedMapping.MapData.Count} 个映射组");
                    foreach (var mapping in _cachedMapping.MapData)
                    {
                        foreach (var program in mapping)
                        {
                            _logService.LogDebug($"程序映射: {program.Key} -> [{string.Join(", ", program.Value)}]");
                        }
                    }
                }
                else
                {
                    _logService.LogWarning("程序映射配置为空或格式不正确");
                }
                
                return _cachedMapping;
            }
            catch (Exception ex)
            {
                _logService.LogError($"加载程序映射配置失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 根据当前活动窗口的进程名称获取对应的程序信息
        /// </summary>
        /// <param name="processName">进程名称</param>
        /// <returns>匹配的程序名称</returns>
        public string? GetMatchingProgram(string processName)
        {
            _logService.LogInfo($"开始匹配程序，进程名称: '{processName}'");
            
            if (_cachedMapping?.MapData == null)
            {
                _logService.LogError("程序映射配置为空，无法进行匹配");
                return null;
            }
            
            if (string.IsNullOrEmpty(processName))
            {
                _logService.LogWarning("进程名称为空，无法进行匹配");
                return null;
            }

            foreach (var mapping in _cachedMapping.MapData)
            {
                foreach (var program in mapping)
                {
                    var programName = program.Key;
                    var executableNames = program.Value;
                    
                    _logService.LogDebug($"检查程序 '{programName}' 的可执行文件列表: [{string.Join(", ", executableNames)}]");

                    // 检查进程名称是否匹配配置中的可执行文件名
                    foreach (var exe in executableNames)
                    {
                        var exeWithoutExtension = exe.Replace(".exe", "");
                        
                        bool exactMatch = string.Equals(processName, exe, StringComparison.OrdinalIgnoreCase);
                        bool exactMatchWithoutExt = string.Equals(processName, exeWithoutExtension, StringComparison.OrdinalIgnoreCase);
                        bool containsMatch = processName.Contains(exeWithoutExtension, StringComparison.OrdinalIgnoreCase);
                        
                        _logService.LogDebug($"匹配检查 - 进程: '{processName}' vs 配置: '{exe}' | 完全匹配: {exactMatch} | 无扩展名匹配: {exactMatchWithoutExt} | 包含匹配: {containsMatch}");
                        
                        if (exactMatch || exactMatchWithoutExt || containsMatch)
                        {
                            _logService.LogInfo($"成功匹配到程序: '{programName}' (通过可执行文件: '{exe}')");
                            return programName;
                        }
                    }
                }
            }

            _logService.LogWarning($"未找到匹配的程序，进程名称: '{processName}'");
            return null;
        }
        
        /// <summary>
        /// 加载指定程序的笔记信息
        /// </summary>
        /// <param name="programName">程序名称</param>
        /// <returns>程序笔记信息</returns>
        public async Task<ProgramNotes?> LoadProgramNotesAsync(string programName)
        {
            _logService.LogInfo($"开始加载程序笔记数据: '{programName}'");
            
            try
            {
                var programPath = Path.Combine(_dataPath, "ProjectData", programName);
                _logService.LogInfo($"程序数据路径: {programPath}");
                
                if (!Directory.Exists(programPath))
                {
                    _logService.LogError($"程序数据目录不存在: {programPath}");
                    return null;
                }

                var programNotes = new ProgramNotes
                {
                    ProgramName = programName,
                    ProgramPath = programPath
                };

                // 加载快捷键配置
                var shortcutKeysPath = Path.Combine(programPath, "ShortcutKeys.json");
                _logService.LogInfo($"尝试加载快捷键配置: {shortcutKeysPath}");
                
                if (File.Exists(shortcutKeysPath))
                {
                    var shortcutContent = await File.ReadAllTextAsync(shortcutKeysPath);
                    _logService.LogDebug($"读取快捷键配置内容，长度: {shortcutContent.Length} 字符");
                    
                    programNotes.ShortcutKeys = JsonSerializer.Deserialize<ShortcutKeyConfig>(shortcutContent);
                    
                    if (programNotes.ShortcutKeys?.Data != null)
                    {
                        _logService.LogInfo($"成功加载 {programNotes.ShortcutKeys.Data.Count} 个快捷键");
                        foreach (var shortcut in programNotes.ShortcutKeys.Data)
                        {
                            _logService.LogDebug($"快捷键: {shortcut.ShortcutKeyValue} - {shortcut.Description}");
                        }
                    }
                    else
                    {
                        _logService.LogWarning("快捷键配置为空或格式不正确");
                    }
                }
                else
                { 
                    _logService.LogWarning($"快捷键配置文件不存在: {shortcutKeysPath}");
                }

                // 加载笔记分类
                var directories = Directory.GetDirectories(programPath);
                _logService.LogInfo($"找到 {directories.Length} 个笔记分类目录");
                
                foreach (var dir in directories)
                {
                    var categoryName = Path.GetFileName(dir);
                    _logService.LogDebug($"处理笔记分类: {categoryName}");
                    
                    var category = new NoteCategory
                    {
                        CategoryName = categoryName,
                        CategoryPath = dir
                    };

                    // 加载该分类下的所有Markdown文件
                    var markdownFiles = Directory.GetFiles(dir, "*.md", SearchOption.TopDirectoryOnly);
                    _logService.LogDebug($"分类 '{categoryName}' 中找到 {markdownFiles.Length} 个Markdown文件");
                    
                    foreach (var file in markdownFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        _logService.LogDebug($"加载笔记文件: {fileName}");
                        
                        var noteFile = new NoteFile
                        {
                            FileName = fileName,
                            FilePath = file,
                            Content = await File.ReadAllTextAsync(file)
                        };
                        category.NoteFiles.Add(noteFile);
                        
                        _logService.LogDebug($"笔记文件 '{fileName}' 内容长度: {noteFile.Content.Length} 字符");
                    }

                    if (category.NoteFiles.Any())
                    {
                        programNotes.NoteCategories.Add(category);
                        _logService.LogInfo($"添加笔记分类 '{categoryName}'，包含 {category.NoteFiles.Count} 个文件");
                    }
                    else
                    {
                        _logService.LogWarning($"笔记分类 '{categoryName}' 中没有找到有效的Markdown文件");
                    }
                }

                _logService.LogInfo($"程序 '{programName}' 数据加载完成，包含 {programNotes.NoteCategories.Count} 个笔记分类");
                return programNotes;
            }
            catch (Exception ex)
            {
                _logService.LogError($"加载程序笔记失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 加载指定程序的视频信息
        /// </summary>
        /// <param name="programName">程序名称</param>
        /// <returns>视频信息列表</returns>
        public async Task<List<VideoInfo>> LoadProgramVideosAsync(string programName)
        {
            _logService.LogInfo($"开始加载程序视频数据: '{programName}'");
            
            var videos = new List<VideoInfo>();
            
            try
            {
                // 直接使用传入的程序名称，因为它已经是通过GetMatchingProgram方法匹配后的结果
                if (string.IsNullOrEmpty(programName))
                {
                    _logService.LogWarning("程序名称为空，无法加载视频数据");
                    return videos;
                }

                var videoDataPath = Path.Combine(_dataPath, "VideoData", programName);
                _logService.LogInfo($"视频数据路径: {videoDataPath}");
                
                if (!Directory.Exists(videoDataPath))
                {
                    _logService.LogWarning($"视频数据目录不存在: {videoDataPath}");
                    return videos;
                }

                // 获取所有MP4文件
                var videoFiles = Directory.GetFiles(videoDataPath, "*.mp4", SearchOption.TopDirectoryOnly);
                _logService.LogInfo($"找到 {videoFiles.Length} 个视频文件");
                
                foreach (var videoFile in videoFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(videoFile);
                        var fileName = Path.GetFileNameWithoutExtension(videoFile);
                        
                        var video = new VideoInfo
                        {
                            Name = fileName,
                            FilePath = videoFile,
                            FileSize = fileInfo.Length,
                            CreatedTime = fileInfo.CreationTime,
                            ModifiedTime = fileInfo.LastWriteTime
                        };

                        // 查找对应的预览图（支持jpg、png、jpeg）
                        var thumbnailExtensions = new[] { ".jpg", ".png", ".jpeg" };
                        foreach (var ext in thumbnailExtensions)
                        {
                            var thumbnailPath = Path.Combine(videoDataPath, fileName + ext);
                            if (File.Exists(thumbnailPath))
                            {
                                video.ThumbnailPath = thumbnailPath;
                                break;
                            }
                        }

                        // 如果没有找到预览图，尝试生成一个
                        if (string.IsNullOrEmpty(video.ThumbnailPath))
                        {
                            try
                            {
                                var generatedThumbnail = await _thumbnailService.EnsureThumbnailExistsAsync(videoFile);
                                if (!string.IsNullOrEmpty(generatedThumbnail))
                                {
                                    video.ThumbnailPath = generatedThumbnail;
                                    _logService.LogInfo($"为视频 '{fileName}' 生成了预览图: {generatedThumbnail}");
                                }
                            }
                            catch (Exception thumbnailEx)
                            {
                                _logService.LogWarning($"为视频 '{fileName}' 生成预览图失败: {thumbnailEx.Message}");
                            }
                        }

                        // 尝试读取描述文件（如果存在）
                        var descriptionPath = Path.Combine(videoDataPath, fileName + ".txt");
                        if (File.Exists(descriptionPath))
                        {
                            video.Description = await File.ReadAllTextAsync(descriptionPath);
                        }

                        videos.Add(video);
                        _logService.LogDebug($"加载视频: {fileName}, 大小: {video.FormattedFileSize}");
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"加载视频文件失败 '{videoFile}': {ex.Message}", ex);
                    }
                }

                // 按修改时间降序排序（最新的在前面）
                videos = videos.OrderByDescending(v => v.ModifiedTime).ToList();
                
                _logService.LogInfo($"程序 '{programName}' 视频数据加载完成，共 {videos.Count} 个视频");
                return videos;
            }
            catch (Exception ex)
            {
                _logService.LogError($"加载程序视频失败: {ex.Message}", ex);
                return videos;
            }
        }

        /// <summary>
        /// 获取所有可用的视频程序列表
        /// </summary>
        /// <returns>有视频的程序名称列表</returns>
        public async Task<List<string>> GetAvailableVideoProgramsAsync()
        {
            try
            {
                var videoDataPath = Path.Combine(_dataPath, "VideoData");
                if (!Directory.Exists(videoDataPath))
                {
                    _logService.LogWarning($"VideoData目录不存在: {videoDataPath}");
                    return new List<string>();
                }

                var directories = Directory.GetDirectories(videoDataPath)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .Where(programName => 
                    {
                        // 检查目录中是否有MP4文件
                        var programPath = Path.Combine(videoDataPath, programName);
                        return Directory.GetFiles(programPath, "*.mp4", SearchOption.TopDirectoryOnly).Length > 0;
                    })
                    .ToList();

                _logService.LogInfo($"找到 {directories.Count} 个有视频的程序");
                return directories;
            }
            catch (Exception ex)
            {
                _logService.LogError($"获取可用视频程序列表失败: {ex.Message}", ex);
                return new List<string>();
            }
        }

        /// <summary>
        /// 获取所有可用的程序列表
        /// </summary>
        /// <returns>程序名称列表</returns>
        public async Task<List<string>> GetAvailableProgramsAsync()
        {
            try
            {
                var projectDataPath = Path.Combine(_dataPath, "ProjectData");
                if (!Directory.Exists(projectDataPath))
                    return new List<string>();

                var directories = Directory.GetDirectories(projectDataPath)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Cast<string>()
                    .ToList();

                return directories;
            }
            catch (Exception ex)
            {
                _logService.LogError($"获取可用程序列表失败: {ex.Message}", ex);
                return new List<string>();
            }
        }

    }
}