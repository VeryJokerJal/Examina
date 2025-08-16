using ExaminaWebApplication.Models.Windows;

namespace ExaminaWebApplication.Data.Windows;

/// <summary>
/// Windows文件系统操作点数据定义类
/// </summary>
public static class WindowsOperationData
{
    /// <summary>
    /// 获取所有Windows操作点定义
    /// </summary>
    /// <returns></returns>
    public static List<WindowsOperationPoint> GetWindowsOperationPoints()
    {
        return new List<WindowsOperationPoint>
        {
            // 操作点1：创建文件/文件夹
            new() {
                Id = 1,
                OperationNumber = 1,
                Name = "创建文件/文件夹",
                Description = "在指定位置创建新的文件或文件夹",
                OperationType = WindowsOperationType.Create,
                OperationMode = WindowsOperationMode.Universal
            },
            
            // 操作点2：复制文件/文件夹
            new() {
                Id = 2,
                OperationNumber = 2,
                Name = "复制文件/文件夹",
                Description = "将文件或文件夹复制到指定位置",
                OperationType = WindowsOperationType.Copy,
                OperationMode = WindowsOperationMode.Universal
            },
            
            // 操作点3：移动文件/文件夹
            new() {
                Id = 3,
                OperationNumber = 3,
                Name = "移动文件/文件夹",
                Description = "将文件或文件夹移动到指定位置",
                OperationType = WindowsOperationType.Move,
                OperationMode = WindowsOperationMode.Universal
            },
            
            // 操作点4：删除文件/文件夹
            new() {
                Id = 4,
                OperationNumber = 4,
                Name = "删除文件/文件夹",
                Description = "删除指定的文件或文件夹",
                OperationType = WindowsOperationType.Delete,
                OperationMode = WindowsOperationMode.Universal
            },
            
            // 操作点5：重命名文件/文件夹
            new() {
                Id = 5,
                OperationNumber = 5,
                Name = "重命名文件/文件夹",
                Description = "修改文件或文件夹的名称",
                OperationType = WindowsOperationType.Rename,
                OperationMode = WindowsOperationMode.Universal
            },
            
            // 操作点6：创建快捷方式
            new() {
                Id = 6,
                OperationNumber = 6,
                Name = "创建快捷方式",
                Description = "为文件或文件夹创建快捷方式",
                OperationType = WindowsOperationType.CreateShortcut,
                OperationMode = WindowsOperationMode.Universal
            },
            
            // 操作点7：修改属性
            new() {
                Id = 7,
                OperationNumber = 7,
                Name = "修改文件/文件夹属性",
                Description = "修改文件或文件夹的属性（如只读、隐藏等）",
                OperationType = WindowsOperationType.ModifyProperties,
                OperationMode = WindowsOperationMode.Universal
            },
            
            // 操作点8：复制并重命名
            new() {
                Id = 8,
                OperationNumber = 8,
                Name = "复制并重命名",
                Description = "复制文件或文件夹并同时重命名",
                OperationType = WindowsOperationType.CopyAndRename,
                OperationMode = WindowsOperationMode.Universal
            }
        };
    }

    /// <summary>
    /// 获取Windows操作点的参数配置
    /// </summary>
    /// <returns></returns>
    public static List<WindowsOperationParameter> GetWindowsOperationParameters()
    {
        List<WindowsOperationParameter> parameters = new();

        // 操作点1：创建文件/文件夹的参数
        parameters.AddRange(new[]
        {
            new WindowsOperationParameter
            {
                Id = 1,
                OperationPointId = 1,
                ParameterOrder = 1,
                ParameterName = "创建路径",
                ParameterDescription = "要创建文件或文件夹的完整路径",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Users\\Desktop\\新建文件夹"
            },
            new WindowsOperationParameter
            {
                Id = 2,
                OperationPointId = 1,
                ParameterOrder = 2,
                ParameterName = "创建类型",
                ParameterDescription = "指定创建文件还是文件夹",
                DataType = WindowsParameterDataType.Enum,
                IsRequired = true,
                EnumTypeId = 1, // 创建类型枚举
                ExampleValue = "Folder"
            }
        });

        // 操作点2：复制文件/文件夹的参数
        parameters.AddRange(new[]
        {
            new WindowsOperationParameter
            {
                Id = 3,
                OperationPointId = 2,
                ParameterOrder = 1,
                ParameterName = "源路径",
                ParameterDescription = "要复制的文件或文件夹的路径",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Users\\Desktop\\原文件.txt"
            },
            new WindowsOperationParameter
            {
                Id = 4,
                OperationPointId = 2,
                ParameterOrder = 2,
                ParameterName = "目标路径",
                ParameterDescription = "复制到的目标位置",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Users\\Documents\\复制的文件.txt"
            }
        });

        // 操作点3：移动文件/文件夹的参数
        parameters.AddRange(new[]
        {
            new WindowsOperationParameter
            {
                Id = 5,
                OperationPointId = 3,
                ParameterOrder = 1,
                ParameterName = "源路径",
                ParameterDescription = "要移动的文件或文件夹的路径",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Users\\Desktop\\要移动的文件.txt"
            },
            new WindowsOperationParameter
            {
                Id = 6,
                OperationPointId = 3,
                ParameterOrder = 2,
                ParameterName = "目标路径",
                ParameterDescription = "移动到的目标位置",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Users\\Documents\\移动的文件.txt"
            }
        });

        // 操作点4：删除文件/文件夹的参数
        parameters.AddRange(new[]
        {
            new WindowsOperationParameter
            {
                Id = 7,
                OperationPointId = 4,
                ParameterOrder = 1,
                ParameterName = "删除路径",
                ParameterDescription = "要删除的文件或文件夹的路径",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Users\\Desktop\\要删除的文件.txt"
            },
            new WindowsOperationParameter
            {
                Id = 8,
                OperationPointId = 4,
                ParameterOrder = 2,
                ParameterName = "删除方式",
                ParameterDescription = "指定删除方式（移到回收站或永久删除）",
                DataType = WindowsParameterDataType.Enum,
                IsRequired = true,
                EnumTypeId = 2, // 删除方式枚举
                DefaultValue = "MoveToRecycleBin"
            }
        });

        // 操作点5：重命名文件/文件夹的参数
        parameters.AddRange(new[]
        {
            new WindowsOperationParameter
            {
                Id = 9,
                OperationPointId = 5,
                ParameterOrder = 1,
                ParameterName = "原路径",
                ParameterDescription = "要重命名的文件或文件夹的当前路径",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Users\\Desktop\\旧名称.txt"
            },
            new WindowsOperationParameter
            {
                Id = 10,
                OperationPointId = 5,
                ParameterOrder = 2,
                ParameterName = "新名称",
                ParameterDescription = "新的文件或文件夹名称",
                DataType = WindowsParameterDataType.String,
                IsRequired = true,
                ExampleValue = "新名称.txt"
            }
        });

        // 操作点6：创建快捷方式的参数
        parameters.AddRange(new[]
        {
            new WindowsOperationParameter
            {
                Id = 11,
                OperationPointId = 6,
                ParameterOrder = 1,
                ParameterName = "目标路径",
                ParameterDescription = "要创建快捷方式的目标文件或文件夹路径",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Program Files\\应用程序\\app.exe"
            },
            new WindowsOperationParameter
            {
                Id = 12,
                OperationPointId = 6,
                ParameterOrder = 2,
                ParameterName = "快捷方式路径",
                ParameterDescription = "快捷方式的保存位置和名称",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Users\\Desktop\\应用程序.lnk"
            }
        });

        // 操作点7：修改属性的参数
        parameters.AddRange(new[]
        {
            new WindowsOperationParameter
            {
                Id = 13,
                OperationPointId = 7,
                ParameterOrder = 1,
                ParameterName = "目标路径",
                ParameterDescription = "要修改属性的文件或文件夹路径",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Users\\Desktop\\文件.txt"
            },
            new WindowsOperationParameter
            {
                Id = 14,
                OperationPointId = 7,
                ParameterOrder = 2,
                ParameterName = "属性设置",
                ParameterDescription = "要设置的文件属性",
                DataType = WindowsParameterDataType.Enum,
                IsRequired = true,
                EnumTypeId = 3, // 文件属性枚举
                AllowMultipleValues = true,
                ExampleValue = "ReadOnly"
            }
        });

        // 操作点8：复制并重命名的参数
        parameters.AddRange(new[]
        {
            new WindowsOperationParameter
            {
                Id = 15,
                OperationPointId = 8,
                ParameterOrder = 1,
                ParameterName = "源路径",
                ParameterDescription = "要复制的文件或文件夹的路径",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Users\\Desktop\\原文件.txt"
            },
            new WindowsOperationParameter
            {
                Id = 16,
                OperationPointId = 8,
                ParameterOrder = 2,
                ParameterName = "目标路径",
                ParameterDescription = "复制后的新文件路径（包含新名称）",
                DataType = WindowsParameterDataType.Path,
                IsRequired = true,
                ExampleValue = "C:\\Users\\Documents\\新名称.txt"
            }
        });

        return parameters;
    }
}
