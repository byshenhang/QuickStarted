using QuickStarted.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickStarted.Services
{
    /// <summary>
    /// 数据服务接口
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// 加载程序映射配置
        /// </summary>
        /// <returns>程序映射配置</returns>
        Task<ProgramMapping?> LoadProgramMappingAsync();

        /// <summary>
        /// 根据当前活动窗口的进程名称获取对应的程序信息
        /// </summary>
        /// <param name="processName">进程名称</param>
        /// <returns>匹配的程序名称</returns>
        string? GetMatchingProgram(string processName);

        /// <summary>
        /// 加载指定程序的笔记信息
        /// </summary>
        /// <param name="programName">程序名称</param>
        /// <returns>程序笔记信息</returns>
        Task<ProgramNotes?> LoadProgramNotesAsync(string programName);

        /// <summary>
        /// 获取所有可用的程序列表
        /// </summary>
        /// <returns>程序名称列表</returns>
        Task<List<string>> GetAvailableProgramsAsync();

        /// <summary>
        /// 加载指定程序的视频信息
        /// </summary>
        /// <param name="programName">程序名称</param>
        /// <returns>视频信息列表</returns>
        Task<List<VideoInfo>> LoadProgramVideosAsync(string programName);

        /// <summary>
        /// 获取所有可用的视频程序列表
        /// </summary>
        /// <returns>有视频的程序名称列表</returns>
        Task<List<string>> GetAvailableVideoProgramsAsync();
    }
}