using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ExamLab.Models;

/// <summary>
/// Windows知识点类型枚举
/// </summary>
public enum WindowsKnowledgeType
{
    // 第一类：文件操作
    CreateFile = 1,              // 知识点1：创建文件
    DeleteFile = 2,              // 知识点2：删除文件
    CopyFile = 3,                // 知识点3：复制文件
    MoveFile = 4,                // 知识点4：移动文件
    RenameFile = 5,              // 知识点5：重命名文件
    
    // 第二类：文件夹操作
    CreateFolder = 6,            // 知识点6：创建文件夹
    DeleteFolder = 7,            // 知识点7：删除文件夹
    CopyFolder = 8,              // 知识点8：复制文件夹
    MoveFolder = 9,              // 知识点9：移动文件夹
    RenameFolder = 10,           // 知识点10：重命名文件夹
    
    // 第三类：文件属性操作
    SetFileAttributes = 11,      // 知识点11：设置文件属性
    SetFilePermissions = 12,     // 知识点12：设置文件权限
    
    // 第四类：文件内容操作
    WriteTextToFile = 13,        // 知识点13：写入文本到文件
    AppendTextToFile = 14,       // 知识点14：追加文本到文件
    
    // 第五类：系统操作
    CreateShortcut = 15,         // 知识点15：创建快捷方式
    SetEnvironmentVariable = 16, // 知识点16：设置环境变量
    
    // 第六类：注册表操作
    CreateRegistryKey = 17,      // 知识点17：创建注册表项
    SetRegistryValue = 18,       // 知识点18：设置注册表值
    DeleteRegistryKey = 19,      // 知识点19：删除注册表项
    
    // 第七类：服务操作
    StartService = 20,           // 知识点20：启动服务
    StopService = 21,            // 知识点21：停止服务
    
    // 第八类：进程操作
    StartProcess = 22,           // 知识点22：启动进程
    KillProcess = 23,            // 知识点23：终止进程
    
    // 第九类：网络操作
    PingHost = 24,               // 知识点24：Ping主机
    DownloadFile = 25,           // 知识点25：下载文件
    
    // 第十类：压缩操作
    CreateZipArchive = 26,       // 知识点26：创建ZIP压缩包
    ExtractZipArchive = 27,      // 知识点27：解压ZIP压缩包
}

/// <summary>
/// Windows知识点配置模型
/// </summary>
public class WindowsKnowledgeConfig : ReactiveObject
{
    /// <summary>
    /// 知识点类型
    /// </summary>
    [Reactive] public WindowsKnowledgeType KnowledgeType { get; set; }

    /// <summary>
    /// 知识点名称
    /// </summary>
    [Reactive] public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 知识点描述
    /// </summary>
    [Reactive] public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 知识点分类
    /// </summary>
    [Reactive] public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 配置参数模板
    /// </summary>
    public ObservableCollection<ConfigurationParameterTemplate> ParameterTemplates { get; set; } = [];
}
