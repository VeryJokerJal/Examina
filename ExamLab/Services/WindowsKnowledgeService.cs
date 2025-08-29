using System;
using System.Collections.Generic;
using System.Linq;
using ExamLab.Models;

namespace ExamLab.Services;

/// <summary>
/// Windows知识点配置服务
/// </summary>
public class WindowsKnowledgeService
{
    public static WindowsKnowledgeService Instance { get; } = new();

    private readonly Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> _knowledgeConfigs;

    private WindowsKnowledgeService()
    {
        _knowledgeConfigs = InitializeKnowledgeConfigs();
    }

    /// <summary>
    /// 获取所有知识点配置
    /// </summary>
    public IEnumerable<WindowsKnowledgeConfig> GetAllKnowledgeConfigs()
    {
        return _knowledgeConfigs.Values;
    }

    /// <summary>
    /// 根据类型获取知识点配置
    /// </summary>
    public WindowsKnowledgeConfig? GetKnowledgeConfig(WindowsKnowledgeType type)
    {
        return _knowledgeConfigs.TryGetValue(type, out WindowsKnowledgeConfig? config) ? config : null;
    }

    /// <summary>
    /// 根据知识点配置创建操作点
    /// </summary>
    public OperationPoint CreateOperationPoint(WindowsKnowledgeType type)
    {
        WindowsKnowledgeConfig? config = GetKnowledgeConfig(type);
        if (config == null)
        {
            throw new ArgumentException($"未找到知识点类型 {type} 的配置");
        }

        OperationPoint operationPoint = new()
        {
            Name = config.Name,
            Description = config.Description,
            ModuleType = ModuleType.Windows,
            WindowsKnowledgeType = type
        };

        // 根据参数模板创建配置参数
        foreach (ConfigurationParameterTemplate template in config.ParameterTemplates)
        {
            ConfigurationParameter parameter = new()
            {
                Name = template.Name,
                DisplayName = template.DisplayName,
                Description = template.Description,
                Type = template.Type,
                IsRequired = template.IsRequired,
                Order = template.Order,
                MinValue = template.MinValue,
                MaxValue = template.MaxValue,
                EnumOptions = template.EnumOptions,
                Value = template.DefaultValue ?? string.Empty
            };

            operationPoint.Parameters.Add(parameter);
        }

        return operationPoint;
    }

    /// <summary>
    /// 更新现有操作点的参数类型（用于升级现有数据）
    /// </summary>
    /// <param name="operationPoint">要更新的操作点</param>
    public void UpdateOperationPointParameterTypes(OperationPoint operationPoint)
    {
        if (operationPoint.WindowsKnowledgeType == null) return;

        WindowsKnowledgeConfig? config = GetKnowledgeConfig(operationPoint.WindowsKnowledgeType.Value);
        if (config == null) return;

        // 更新每个参数的类型
        foreach (ConfigurationParameter parameter in operationPoint.Parameters)
        {
            ConfigurationParameterTemplate? template = config.ParameterTemplates
                .FirstOrDefault(t => t.Name == parameter.Name);

            if (template != null && parameter.Type != template.Type)
            {
                parameter.Type = template.Type;
            }
        }
    }

    private Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> InitializeKnowledgeConfigs()
    {
        Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> configs = [];

        // 第一类：文件操作
        InitializeFileOperations(configs);

        // 第二类：文件夹操作
        InitializeFolderOperations(configs);

        // 第三类：文件属性操作
        InitializeFileAttributeOperations(configs);

        // 第四类：文件内容操作
        InitializeFileContentOperations(configs);

        // 第五类：系统操作
        InitializeSystemOperations(configs);

        // 第六类：注册表操作
        InitializeRegistryOperations(configs);

        // 第七类：服务操作
        InitializeServiceOperations(configs);

        // 第八类：进程操作
        InitializeProcessOperations(configs);

        // 第九类：网络操作
        InitializeNetworkOperations(configs);

        // 第十类：压缩操作
        InitializeCompressionOperations(configs);

        return configs;
    }

    private void InitializeFileOperations(Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> configs)
    {
        // 知识点1：创建文件
        configs[WindowsKnowledgeType.CreateFile] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.CreateFile,
            Name = "创建文件",
            Description = "在指定路径创建新文件",
            Category = "文件操作",
            ParameterTemplates =
            [
                new() { Name = "FilePath", DisplayName = "文件路径", Description = "要创建的文件完整路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "FileContent", DisplayName = "文件内容", Description = "文件的初始内容（可选）", Type = ParameterType.Text, IsRequired = false, Order = 2 }
            ]
        };

        // 知识点2：删除文件
        configs[WindowsKnowledgeType.DeleteFile] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.DeleteFile,
            Name = "删除文件",
            Description = "删除指定路径的文件",
            Category = "文件操作",
            ParameterTemplates =
            [
                new() { Name = "FilePath", DisplayName = "文件路径", Description = "要删除的文件完整路径", Type = ParameterType.File, IsRequired = true, Order = 1 }
            ]
        };

        // 知识点3：复制文件
        configs[WindowsKnowledgeType.CopyFile] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.CopyFile,
            Name = "复制文件",
            Description = "将文件从源路径复制到目标路径",
            Category = "文件操作",
            ParameterTemplates =
            [
                new() { Name = "SourcePath", DisplayName = "源文件路径", Description = "要复制的源文件路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "DestinationPath", DisplayName = "目标文件路径", Description = "复制到的目标文件路径", Type = ParameterType.File, IsRequired = true, Order = 2 },
                new() { Name = "Overwrite", DisplayName = "是否覆盖", Description = "如果目标文件存在是否覆盖", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "是,否" }
            ]
        };

        // 知识点4：移动文件
        configs[WindowsKnowledgeType.MoveFile] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.MoveFile,
            Name = "移动文件",
            Description = "将文件从源路径移动到目标路径",
            Category = "文件操作",
            ParameterTemplates =
            [
                new() { Name = "SourcePath", DisplayName = "源文件路径", Description = "要移动的源文件路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "DestinationPath", DisplayName = "目标文件路径", Description = "移动到的目标文件路径", Type = ParameterType.File, IsRequired = true, Order = 2 }
            ]
        };

        // 知识点5：重命名文件
        configs[WindowsKnowledgeType.RenameFile] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.RenameFile,
            Name = "重命名文件",
            Description = "重命名指定的文件",
            Category = "文件操作",
            ParameterTemplates =
            [
                new() { Name = "FilePath", DisplayName = "文件路径", Description = "要重命名的文件路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "NewName", DisplayName = "新文件名", Description = "文件的新名称", Type = ParameterType.Text, IsRequired = true, Order = 2 }
            ]
        };
    }

    private void InitializeFolderOperations(Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> configs)
    {
        // 知识点6：创建文件夹
        configs[WindowsKnowledgeType.CreateFolder] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.CreateFolder,
            Name = "创建文件夹",
            Description = "在指定路径创建新文件夹",
            Category = "文件夹操作",
            ParameterTemplates =
            [
                new() { Name = "FolderPath", DisplayName = "文件夹路径", Description = "要创建的文件夹完整路径", Type = ParameterType.Folder, IsRequired = true, Order = 1 }
            ]
        };

        // 知识点7：删除文件夹
        configs[WindowsKnowledgeType.DeleteFolder] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.DeleteFolder,
            Name = "删除文件夹",
            Description = "删除指定路径的文件夹",
            Category = "文件夹操作",
            ParameterTemplates =
            [
                new() { Name = "FolderPath", DisplayName = "文件夹路径", Description = "要删除的文件夹完整路径", Type = ParameterType.Folder, IsRequired = true, Order = 1 },
                new() { Name = "Recursive", DisplayName = "递归删除", Description = "是否删除子文件夹和文件", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "是,否" }
            ]
        };

        // 知识点8：复制文件夹
        configs[WindowsKnowledgeType.CopyFolder] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.CopyFolder,
            Name = "复制文件夹",
            Description = "将文件夹从源路径复制到目标路径",
            Category = "文件夹操作",
            ParameterTemplates =
            [
                new() { Name = "SourcePath", DisplayName = "源文件夹路径", Description = "要复制的源文件夹路径", Type = ParameterType.Folder, IsRequired = true, Order = 1 },
                new() { Name = "DestinationPath", DisplayName = "目标文件夹路径", Description = "复制到的目标文件夹路径", Type = ParameterType.Folder, IsRequired = true, Order = 2 }
            ]
        };

        // 知识点9：移动文件夹
        configs[WindowsKnowledgeType.MoveFolder] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.MoveFolder,
            Name = "移动文件夹",
            Description = "将文件夹从源路径移动到目标路径",
            Category = "文件夹操作",
            ParameterTemplates =
            [
                new() { Name = "SourcePath", DisplayName = "源文件夹路径", Description = "要移动的源文件夹路径", Type = ParameterType.Folder, IsRequired = true, Order = 1 },
                new() { Name = "DestinationPath", DisplayName = "目标文件夹路径", Description = "移动到的目标文件夹路径", Type = ParameterType.Folder, IsRequired = true, Order = 2 }
            ]
        };

        // 知识点10：重命名文件夹
        configs[WindowsKnowledgeType.RenameFolder] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.RenameFolder,
            Name = "重命名文件夹",
            Description = "重命名指定的文件夹",
            Category = "文件夹操作",
            ParameterTemplates =
            [
                new() { Name = "FolderPath", DisplayName = "文件夹路径", Description = "要重命名的文件夹路径", Type = ParameterType.Folder, IsRequired = true, Order = 1 },
                new() { Name = "NewName", DisplayName = "新文件夹名", Description = "文件夹的新名称", Type = ParameterType.Text, IsRequired = true, Order = 2 }
            ]
        };
    }

    private void InitializeFileAttributeOperations(Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> configs)
    {
        // 知识点11：设置文件属性
        configs[WindowsKnowledgeType.SetFileAttributes] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.SetFileAttributes,
            Name = "设置文件属性",
            Description = "设置文件的属性（只读、隐藏等）",
            Category = "文件属性操作",
            ParameterTemplates =
            [
                new() { Name = "FilePath", DisplayName = "文件路径", Description = "要设置属性的文件路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "Attributes", DisplayName = "文件属性", Description = "要设置的文件属性", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "只读,隐藏,系统,存档,正常" }
            ]
        };

        // 知识点12：设置文件权限
        configs[WindowsKnowledgeType.SetFilePermissions] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.SetFilePermissions,
            Name = "设置文件权限",
            Description = "设置文件的访问权限",
            Category = "文件属性操作",
            ParameterTemplates =
            [
                new() { Name = "FilePath", DisplayName = "文件路径", Description = "要设置权限的文件路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "UserName", DisplayName = "用户名", Description = "要设置权限的用户名", Type = ParameterType.Text, IsRequired = true, Order = 2 },
                new() { Name = "Permission", DisplayName = "权限类型", Description = "要设置的权限类型", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "完全控制,修改,读取和执行,读取,写入" }
            ]
        };
    }

    private void InitializeFileContentOperations(Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> configs)
    {
        // 知识点13：写入文本到文件
        configs[WindowsKnowledgeType.WriteTextToFile] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.WriteTextToFile,
            Name = "写入文本到文件",
            Description = "将文本内容写入到文件中",
            Category = "文件内容操作",
            ParameterTemplates =
            [
                new() { Name = "FilePath", DisplayName = "文件路径", Description = "要写入的文件路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "Content", DisplayName = "文本内容", Description = "要写入的文本内容", Type = ParameterType.Text, IsRequired = true, Order = 2 },
                new() { Name = "Encoding", DisplayName = "编码格式", Description = "文本编码格式", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "UTF-8,UTF-16,ASCII,GBK" }
            ]
        };

        // 知识点14：追加文本到文件
        configs[WindowsKnowledgeType.AppendTextToFile] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.AppendTextToFile,
            Name = "追加文本到文件",
            Description = "将文本内容追加到文件末尾",
            Category = "文件内容操作",
            ParameterTemplates =
            [
                new() { Name = "FilePath", DisplayName = "文件路径", Description = "要追加内容的文件路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "Content", DisplayName = "文本内容", Description = "要追加的文本内容", Type = ParameterType.Text, IsRequired = true, Order = 2 }
            ]
        };
    }

    private void InitializeSystemOperations(Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> configs)
    {
        // 知识点15：创建快捷方式
        configs[WindowsKnowledgeType.CreateShortcut] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.CreateShortcut,
            Name = "创建快捷方式",
            Description = "为指定程序或文件创建快捷方式",
            Category = "系统操作",
            ParameterTemplates =
            [
                new() { Name = "TargetPath", DisplayName = "目标路径", Description = "快捷方式指向的目标路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "ShortcutPath", DisplayName = "快捷方式路径", Description = "快捷方式文件的保存路径", Type = ParameterType.File, IsRequired = true, Order = 2 },
                new() { Name = "Description", DisplayName = "描述", Description = "快捷方式的描述信息", Type = ParameterType.Text, IsRequired = false, Order = 3 }
            ]
        };

        // 知识点16：设置环境变量
        configs[WindowsKnowledgeType.SetEnvironmentVariable] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.SetEnvironmentVariable,
            Name = "设置环境变量",
            Description = "设置系统或用户环境变量",
            Category = "系统操作",
            ParameterTemplates =
            [
                new() { Name = "VariableName", DisplayName = "变量名", Description = "环境变量的名称", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "VariableValue", DisplayName = "变量值", Description = "环境变量的值", Type = ParameterType.Text, IsRequired = true, Order = 2 },
                new() { Name = "Target", DisplayName = "作用范围", Description = "环境变量的作用范围", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "用户,系统,进程" }
            ]
        };
    }

    private void InitializeRegistryOperations(Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> configs)
    {
        // 知识点17：创建注册表项
        configs[WindowsKnowledgeType.CreateRegistryKey] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.CreateRegistryKey,
            Name = "创建注册表项",
            Description = "在注册表中创建新的项",
            Category = "注册表操作",
            ParameterTemplates =
            [
                new() { Name = "KeyPath", DisplayName = "注册表项路径", Description = "要创建的注册表项路径", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "RootKey", DisplayName = "根键", Description = "注册表根键", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "HKEY_CURRENT_USER,HKEY_LOCAL_MACHINE,HKEY_CLASSES_ROOT,HKEY_USERS,HKEY_CURRENT_CONFIG" }
            ]
        };

        // 知识点18：设置注册表值
        configs[WindowsKnowledgeType.SetRegistryValue] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.SetRegistryValue,
            Name = "设置注册表值",
            Description = "在注册表项中设置值",
            Category = "注册表操作",
            ParameterTemplates =
            [
                new() { Name = "KeyPath", DisplayName = "注册表项路径", Description = "注册表项路径", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "ValueName", DisplayName = "值名称", Description = "注册表值的名称", Type = ParameterType.Text, IsRequired = true, Order = 2 },
                new() { Name = "ValueData", DisplayName = "值数据", Description = "注册表值的数据", Type = ParameterType.Text, IsRequired = true, Order = 3 },
                new() { Name = "ValueType", DisplayName = "值类型", Description = "注册表值的类型", Type = ParameterType.Enum, IsRequired = true, Order = 4,
                    EnumOptions = "字符串,DWORD,二进制,多字符串,可扩展字符串" }
            ]
        };

        // 知识点19：删除注册表项
        configs[WindowsKnowledgeType.DeleteRegistryKey] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.DeleteRegistryKey,
            Name = "删除注册表项",
            Description = "删除指定的注册表项",
            Category = "注册表操作",
            ParameterTemplates =
            [
                new() { Name = "KeyPath", DisplayName = "注册表项路径", Description = "要删除的注册表项路径", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "RootKey", DisplayName = "根键", Description = "注册表根键", Type = ParameterType.Enum, IsRequired = true, Order = 2,
                    EnumOptions = "HKEY_CURRENT_USER,HKEY_LOCAL_MACHINE,HKEY_CLASSES_ROOT,HKEY_USERS,HKEY_CURRENT_CONFIG" }
            ]
        };
    }

    private void InitializeServiceOperations(Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> configs)
    {
        // 知识点20：启动服务
        configs[WindowsKnowledgeType.StartService] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.StartService,
            Name = "启动服务",
            Description = "启动指定的Windows服务",
            Category = "服务操作",
            ParameterTemplates =
            [
                new() { Name = "ServiceName", DisplayName = "服务名称", Description = "要启动的服务名称", Type = ParameterType.Text, IsRequired = true, Order = 1 }
            ]
        };

        // 知识点21：停止服务
        configs[WindowsKnowledgeType.StopService] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.StopService,
            Name = "停止服务",
            Description = "停止指定的Windows服务",
            Category = "服务操作",
            ParameterTemplates =
            [
                new() { Name = "ServiceName", DisplayName = "服务名称", Description = "要停止的服务名称", Type = ParameterType.Text, IsRequired = true, Order = 1 }
            ]
        };
    }

    private void InitializeProcessOperations(Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> configs)
    {
        // 知识点22：启动进程
        configs[WindowsKnowledgeType.StartProcess] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.StartProcess,
            Name = "启动进程",
            Description = "启动指定的程序或进程",
            Category = "进程操作",
            ParameterTemplates =
            [
                new() { Name = "ProcessPath", DisplayName = "程序路径", Description = "要启动的程序路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "Arguments", DisplayName = "启动参数", Description = "程序启动参数（可选）", Type = ParameterType.Text, IsRequired = false, Order = 2 },
                new() { Name = "WorkingDirectory", DisplayName = "工作目录", Description = "程序工作目录（可选）", Type = ParameterType.Folder, IsRequired = false, Order = 3 }
            ]
        };

        // 知识点23：终止进程
        configs[WindowsKnowledgeType.KillProcess] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.KillProcess,
            Name = "终止进程",
            Description = "终止指定的进程",
            Category = "进程操作",
            ParameterTemplates =
            [
                new() { Name = "ProcessName", DisplayName = "进程名称", Description = "要终止的进程名称", Type = ParameterType.Text, IsRequired = true, Order = 1 }
            ]
        };
    }

    private void InitializeNetworkOperations(Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> configs)
    {
        // 知识点24：Ping主机
        configs[WindowsKnowledgeType.PingHost] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.PingHost,
            Name = "Ping主机",
            Description = "测试与指定主机的网络连通性",
            Category = "网络操作",
            ParameterTemplates =
            [
                new() { Name = "HostName", DisplayName = "主机名或IP", Description = "要Ping的主机名或IP地址", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "Timeout", DisplayName = "超时时间", Description = "Ping超时时间（毫秒）", Type = ParameterType.Number, IsRequired = false, Order = 2, MinValue = 1000, MaxValue = 30000, DefaultValue = "5000" }
            ]
        };

        // 知识点25：下载文件
        configs[WindowsKnowledgeType.DownloadFile] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.DownloadFile,
            Name = "下载文件",
            Description = "从指定URL下载文件到本地",
            Category = "网络操作",
            ParameterTemplates =
            [
                new() { Name = "Url", DisplayName = "下载URL", Description = "要下载的文件URL", Type = ParameterType.Text, IsRequired = true, Order = 1 },
                new() { Name = "LocalPath", DisplayName = "本地保存路径", Description = "文件保存到本地的路径", Type = ParameterType.File, IsRequired = true, Order = 2 }
            ]
        };
    }

    private void InitializeCompressionOperations(Dictionary<WindowsKnowledgeType, WindowsKnowledgeConfig> configs)
    {
        // 知识点26：创建ZIP压缩包
        configs[WindowsKnowledgeType.CreateZipArchive] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.CreateZipArchive,
            Name = "创建ZIP压缩包",
            Description = "将文件或文件夹压缩为ZIP格式",
            Category = "压缩操作",
            ParameterTemplates =
            [
                new() { Name = "SourcePath", DisplayName = "源路径", Description = "要压缩的文件或文件夹路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "ZipPath", DisplayName = "压缩包路径", Description = "生成的ZIP文件路径", Type = ParameterType.File, IsRequired = true, Order = 2 },
                new() { Name = "CompressionLevel", DisplayName = "压缩级别", Description = "压缩级别", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "无压缩,最快,最优,默认" }
            ]
        };

        // 知识点27：解压ZIP压缩包
        configs[WindowsKnowledgeType.ExtractZipArchive] = new WindowsKnowledgeConfig
        {
            KnowledgeType = WindowsKnowledgeType.ExtractZipArchive,
            Name = "解压ZIP压缩包",
            Description = "解压ZIP文件到指定目录",
            Category = "压缩操作",
            ParameterTemplates =
            [
                new() { Name = "ZipPath", DisplayName = "压缩包路径", Description = "要解压的ZIP文件路径", Type = ParameterType.File, IsRequired = true, Order = 1 },
                new() { Name = "ExtractPath", DisplayName = "解压目录", Description = "解压到的目标目录", Type = ParameterType.Folder, IsRequired = true, Order = 2 },
                new() { Name = "Overwrite", DisplayName = "是否覆盖", Description = "如果目标文件存在是否覆盖", Type = ParameterType.Enum, IsRequired = true, Order = 3,
                    EnumOptions = "是,否" }
            ]
        };
    }
}
