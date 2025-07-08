# QuickStarted - 智能快捷键助手

## 项目简介

QuickStarted 是一个基于 WPF 和 .NET 8.0 的智能桌面助手应用程序，通过长按反引号键（`）显示全屏半透明遮罩，并根据当前活动程序智能展示相应的快捷键提示和笔记信息。项目采用现代化的 MVVM 架构模式，使用依赖注入进行模块化管理。

## 功能特性

- **全局键盘钩子**: 监听并拦截反引号键（`）事件
- **长按检测**: 长按反引号键 500ms 后显示智能遮罩
- **智能程序识别**: 自动识别当前活动程序（3ds Max、Maya、Blender、Photoshop、VSCode等）
- **快捷键展示**: 分页显示当前程序的快捷键信息
- **Markdown 笔记**: 支持显示程序相关的 Markdown 格式笔记和教程
- **系统托盘集成**: 提供托盘图标和右键菜单管理
- **多屏幕支持**: 自动在鼠标所在屏幕显示遮罩
- **动画效果**: 窗口显示/隐藏时的淡入淡出动画
- **鼠标滚轮支持**: 支持鼠标滚轮切换快捷键页面
- **快捷键支持**: 按 ESC 键或松开反引号键隐藏遮罩

## 项目架构

### 目录结构
```
QuickStarted/
├── Services/                    # 服务层
│   ├── IWindowHookService.cs   # 键盘钩子服务接口
│   ├── WindowHookService.cs    # 键盘钩子服务实现
│   ├── IScreenService.cs       # 屏幕服务接口
│   ├── ScreenService.cs        # 屏幕服务实现
│   ├── IDataService.cs         # 数据服务接口
│   ├── DataService.cs          # 数据服务实现
│   ├── ITrayService.cs         # 托盘服务接口
│   ├── TrayService.cs          # 托盘服务实现
│   ├── ILogService.cs          # 日志服务接口
│   ├── LogService.cs           # 日志服务实现
│   └── MouseHookService.cs     # 鼠标钩子服务
├── ViewModels/                 # 视图模型层
│   └── MainViewModel.cs        # 主窗口视图模型
├── Models/                     # 数据模型
│   ├── ShortcutKey.cs          # 快捷键模型
│   ├── ProgramMapping.cs       # 程序映射模型
│   └── NoteInfo.cs             # 笔记信息模型
├── Controls/                   # 自定义控件
│   ├── MarkdownViewer.cs       # Markdown查看器控件
│   └── HtmlProcessors/         # HTML处理器组件
├── Converters/                 # 值转换器
├── Data/                       # 数据文件
│   ├── Remap.json              # 程序映射配置
│   └── ProjectData/            # 各程序数据目录
│       ├── 3dMax/              # 3ds Max 相关数据
│       ├── Blender/            # Blender 相关数据
│       └── VSCode/             # VSCode 相关数据
├── Themes/                     # 主题样式
├── MainWindow.xaml             # 主窗口视图
├── MainWindow.xaml.cs          # 主窗口代码隐藏
├── App.xaml                    # 应用程序资源
├── App.xaml.cs                 # 应用程序入口
└── QuickStarted.csproj         # 项目文件
```

### 架构设计

#### 1. 服务层 (Services)
- **IWindowHookService / WindowHookService**: 负责全局键盘钩子的管理，监听反引号键事件
- **IScreenService / ScreenService**: 负责多屏幕检测和工作区域计算
- **IDataService / DataService**: 负责程序数据管理、配置加载和程序识别
- **ITrayService / TrayService**: 负责系统托盘图标和菜单管理
- **ILogService / LogService**: 负责应用程序日志记录
- **MouseHookService**: 负责全局鼠标事件监听（滚轮切换页面）

#### 2. 视图模型层 (ViewModels)
- **MainViewModel**: 主窗口的业务逻辑，使用 CommunityToolkit.Mvvm 实现 MVVM 模式

#### 3. 视图层 (Views)
- **MainWindow**: 全屏遮罩窗口，支持动画效果

#### 4. 依赖注入
- 使用 Microsoft.Extensions.Hosting 和 Microsoft.Extensions.DependencyInjection
- 在 App.xaml.cs 中配置服务容器

## 技术栈

- **.NET 8.0-windows**: 目标框架
- **WPF + Windows Forms**: 混合UI框架
- **CommunityToolkit.Mvvm**: MVVM 框架支持
- **Microsoft.Extensions.Hosting**: 依赖注入容器
- **Microsoft.Extensions.DependencyInjection**: 依赖注入服务
- **Markdig.Signed**: Markdown 解析和渲染库
- **Win32 API**: 低级键盘钩子和屏幕检测

## 核心技术实现

### 1. 全局键盘钩子
```csharp
// 使用 Win32 API 设置低级键盘钩子监听反引号键
SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
// 监听反引号键（VK_OEM_3）的按下和释放事件
if (vkCode == VK_OEM_3) {
    // 处理反引号键事件并拦截
    return (IntPtr)1;
}
```

### 2. 多屏幕支持
```csharp
// 获取鼠标位置并找到对应显示器
GetCursorPos(out POINT cursorPos);
IntPtr hMonitor = MonitorFromPoint(cursorPos, MONITOR_DEFAULTTONEAREST);
GetMonitorInfo(hMonitor, ref monitorInfo);
```

### 3. MVVM 数据绑定
```csharp
// 使用 CommunityToolkit.Mvvm 的 ObservableProperty
[ObservableProperty]
private bool _isWindowVisible = false;
```

### 4. 依赖注入配置
```csharp
// 在 App.xaml.cs 中配置完整的服务容器
services.AddSingleton<ILogService, LogService>();
services.AddSingleton<IWindowHookService, WindowHookService>();
services.AddSingleton<IScreenService, ScreenService>();
services.AddSingleton<IDataService, DataService>();
services.AddSingleton<ITrayService, TrayService>();
services.AddSingleton<MainViewModel>();
services.AddSingleton<MainWindow>();
```

### 5. 程序识别和数据管理
```csharp
// 根据当前活动窗口进程名称匹配程序
var matchedProgram = _dataService.GetMatchingProgram(processName);
// 加载对应程序的快捷键和笔记数据
var programNotes = await _dataService.LoadProgramNotesAsync(matchedProgram);
```

### 6. Markdown 渲染
```csharp
// 使用 Markdig 解析 Markdown 内容
var pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .Build();
var html = Markdown.ToHtml(markdown, pipeline);
```

## 支持的程序

当前版本支持以下程序的快捷键展示：

- **3ds Max**: 3dsmax.exe, max.exe, maxstart
- **Maya**: maya.exe, mayabatch
- **Blender**: blender.exe
- **Photoshop**: Photoshop.exe
- **VSCode**: code.exe, Code.exe, vscode

可通过修改 `Data/Remap.json` 文件添加更多程序支持。

## 使用方法

1. **启动应用**: 运行 QuickStarted.exe
2. **显示助手**: 长按反引号键（`）500ms
3. **切换页面**: 使用鼠标滚轮或页面按钮切换快捷键页面
4. **查看笔记**: 点击笔记分类查看 Markdown 格式的教程和笔记
5. **隐藏助手**: 松开反引号键或按 ESC 键
6. **托盘管理**: 右键点击系统托盘图标进行应用管理

## 开发说明

### 构建项目
```bash
dotnet build
```

### 运行项目
```bash
dotnet run
```

### 发布项目
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## 注意事项

1. **管理员权限**: 应用可能需要管理员权限才能正常设置全局键盘钩子
2. **防病毒软件**: 某些防病毒软件可能会将键盘钩子识别为恶意行为
3. **性能影响**: 全局钩子会对系统性能产生轻微影响
4. **兼容性**: 仅支持 Windows 平台

## 数据文件结构

### 程序映射配置 (Data/Remap.json)
```json
{
  "EditTime": "2024-01-15 10:30:00.000000",
  "MapData": [
    {
      "3dMax": ["3dsmax", "3dsmax.exe", "max.exe"]
    },
    {
      "VSCode": ["code", "code.exe", "Code.exe"]
    }
  ]
}
```

### 快捷键配置 (Data/ProjectData/{Program}/ShortcutKeys.json)
```json
{
  "EditTime": "2024-01-15 10:30:00.000000",
  "Pages": [
    {
      "index": 1,
      "Name": "基础操作",
      "Data": [
        {
          "shortcutkey": "Ctrl+S",
          "name": "保存",
          "desc": "保存当前文件"
        }
      ]
    }
  ]
}
```

## 扩展功能建议

- [x] 添加配置文件支持
- [x] 添加系统托盘图标
- [x] 添加日志记录功能
- [ ] 支持自定义快捷键触发
- [ ] 支持多种遮罩样式和主题
- [ ] 支持开机自启动
- [ ] 添加快捷键搜索功能
- [ ] 支持自定义程序数据导入导出
- [ ] 添加使用统计和热门快捷键推荐

## 许可证

本项目采用 MIT 许可证。