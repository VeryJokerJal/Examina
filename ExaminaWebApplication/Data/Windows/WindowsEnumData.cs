using ExaminaWebApplication.Models.Windows;

namespace ExaminaWebApplication.Data.Windows;

/// <summary>
/// Windows枚举数据定义类 - 包含所有Windows操作中使用的枚举值
/// </summary>
public static class WindowsEnumData
{
    /// <summary>
    /// 获取所有枚举类型定义
    /// </summary>
    /// <returns></returns>
    public static List<WindowsEnumType> GetEnumTypes()
    {
        return new List<WindowsEnumType>
        {
            // 创建类型
            new() {
                Id = 1,
                TypeName = "CreateType",
                Description = "创建类型（文件或文件夹）",
                Category = "创建操作"
            },
            
            // 删除方式
            new() {
                Id = 2,
                TypeName = "DeleteMode",
                Description = "删除方式",
                Category = "删除操作"
            },
            
            // 文件属性
            new() {
                Id = 3,
                TypeName = "FileAttributes",
                Description = "文件属性设置",
                Category = "属性操作"
            }
        };
    }

    /// <summary>
    /// 获取所有枚举值定义
    /// </summary>
    /// <returns></returns>
    public static List<WindowsEnumValue> GetEnumValues()
    {
        List<WindowsEnumValue> enumValues = new();

        // 创建类型枚举值
        enumValues.AddRange(new[]
        {
            new WindowsEnumValue 
            { 
                Id = 1, 
                EnumTypeId = 1, 
                EnumKey = "File", 
                EnumValue = 1, 
                DisplayName = "文件", 
                Description = "创建新文件", 
                SortOrder = 1 
            },
            new WindowsEnumValue 
            { 
                Id = 2, 
                EnumTypeId = 1, 
                EnumKey = "Folder", 
                EnumValue = 2, 
                DisplayName = "文件夹", 
                Description = "创建新文件夹", 
                SortOrder = 2,
                IsDefault = true 
            }
        });

        // 删除方式枚举值
        enumValues.AddRange(new[]
        {
            new WindowsEnumValue 
            { 
                Id = 3, 
                EnumTypeId = 2, 
                EnumKey = "MoveToRecycleBin", 
                EnumValue = 1, 
                DisplayName = "移到回收站", 
                Description = "将文件移动到回收站，可以恢复", 
                SortOrder = 1,
                IsDefault = true 
            },
            new WindowsEnumValue 
            { 
                Id = 4, 
                EnumTypeId = 2, 
                EnumKey = "PermanentDelete", 
                EnumValue = 2, 
                DisplayName = "永久删除", 
                Description = "永久删除文件，无法恢复", 
                SortOrder = 2 
            }
        });

        // 文件属性枚举值
        enumValues.AddRange(new[]
        {
            new WindowsEnumValue 
            { 
                Id = 5, 
                EnumTypeId = 3, 
                EnumKey = "ReadOnly", 
                EnumValue = 1, 
                DisplayName = "只读", 
                Description = "设置文件为只读属性", 
                SortOrder = 1 
            },
            new WindowsEnumValue 
            { 
                Id = 6, 
                EnumTypeId = 3, 
                EnumKey = "Hidden", 
                EnumValue = 2, 
                DisplayName = "隐藏", 
                Description = "设置文件为隐藏属性", 
                SortOrder = 2 
            },
            new WindowsEnumValue 
            { 
                Id = 7, 
                EnumTypeId = 3, 
                EnumKey = "System", 
                EnumValue = 4, 
                DisplayName = "系统", 
                Description = "设置文件为系统属性", 
                SortOrder = 3 
            },
            new WindowsEnumValue 
            { 
                Id = 8, 
                EnumTypeId = 3, 
                EnumKey = "Archive", 
                EnumValue = 32, 
                DisplayName = "存档", 
                Description = "设置文件为存档属性", 
                SortOrder = 4 
            },
            new WindowsEnumValue 
            { 
                Id = 9, 
                EnumTypeId = 3, 
                EnumKey = "Normal", 
                EnumValue = 128, 
                DisplayName = "普通", 
                Description = "清除所有特殊属性，设为普通文件", 
                SortOrder = 5,
                IsDefault = true 
            }
        });

        return enumValues;
    }
}
