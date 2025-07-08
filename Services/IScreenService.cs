using System.Windows;

namespace QuickStarted.Services
{
    /// <summary>
    /// 屏幕信息
    /// </summary>
    public class ScreenInfo
    {
        /// <summary>
        /// 工作区域
        /// </summary>
        public Rect WorkingArea { get; set; }

        /// <summary>
        /// 屏幕边界
        /// </summary>
        public Rect Bounds { get; set; }
    }

    /// <summary>
    /// 屏幕服务接口
    /// </summary>
    public interface IScreenService
    {
        /// <summary>
        /// 获取鼠标所在屏幕的工作区域
        /// </summary>
        /// <returns>工作区域矩形</returns>
        Rect GetMouseScreenWorkArea();

        /// <summary>
        /// 获取当前屏幕信息
        /// </summary>
        /// <returns>屏幕信息</returns>
        ScreenInfo GetCurrentScreenInfo();
    }
}