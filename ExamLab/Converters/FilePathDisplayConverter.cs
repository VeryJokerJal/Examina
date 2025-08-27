using System;
using Microsoft.UI.Xaml.Data;

namespace ExamLab.Converters;

/// <summary>
/// 文件路径显示转换器 - 将绝对路径转换为相对路径显示
/// </summary>
public class FilePathDisplayConverter : IValueConverter
{
    /// <summary>
    /// 默认基础路径前缀列表（按优先级排序）
    /// </summary>
    private static readonly string[] DefaultBasePaths = 
    {
        @"C:\Users\Jal",
        @"C:\河北对口计算机",
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    };

    /// <summary>
    /// 将绝对路径转换为相对路径显示
    /// </summary>
    /// <param name="value">文件路径值</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="parameter">转换参数（可选的自定义基础路径）</param>
    /// <param name="language">语言</param>
    /// <returns>转换后的相对路径显示</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string filePath || string.IsNullOrWhiteSpace(filePath))
        {
            return string.Empty;
        }

        try
        {
            // 如果参数中指定了自定义基础路径，优先使用
            if (parameter is string customBasePath && !string.IsNullOrWhiteSpace(customBasePath))
            {
                string relativePath = ConvertToRelativePath(filePath, customBasePath);
                if (!string.IsNullOrEmpty(relativePath))
                {
                    return relativePath;
                }
            }

            // 尝试使用默认基础路径列表
            foreach (string basePath in DefaultBasePaths)
            {
                if (!string.IsNullOrWhiteSpace(basePath))
                {
                    string relativePath = ConvertToRelativePath(filePath, basePath);
                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        return relativePath;
                    }
                }
            }

            // 如果没有匹配的基础路径，返回原始路径
            return filePath;
        }
        catch (Exception)
        {
            // 转换失败时返回原始路径
            return filePath;
        }
    }

    /// <summary>
    /// 反向转换（不支持）
    /// </summary>
    /// <param name="value">值</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="parameter">参数</param>
    /// <param name="language">语言</param>
    /// <returns>抛出异常</returns>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException("FilePathDisplayConverter不支持反向转换");
    }

    /// <summary>
    /// 将绝对路径转换为相对路径
    /// </summary>
    /// <param name="fullPath">完整路径</param>
    /// <param name="basePath">基础路径</param>
    /// <returns>相对路径，如果不匹配则返回空字符串</returns>
    private static string ConvertToRelativePath(string fullPath, string basePath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || string.IsNullOrWhiteSpace(basePath))
        {
            return string.Empty;
        }

        try
        {
            // 标准化路径分隔符
            string normalizedFullPath = fullPath.Replace('/', '\\');
            string normalizedBasePath = basePath.Replace('/', '\\');

            // 确保基础路径以反斜杠结尾
            if (!normalizedBasePath.EndsWith("\\"))
            {
                normalizedBasePath += "\\";
            }

            // 检查完整路径是否以基础路径开头（不区分大小写）
            if (normalizedFullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
            {
                // 提取相对路径部分
                string relativePart = normalizedFullPath.Substring(normalizedBasePath.Length);
                
                // 如果相对路径不为空，在前面添加反斜杠
                if (!string.IsNullOrEmpty(relativePart))
                {
                    return "\\" + relativePart;
                }
            }

            return string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
