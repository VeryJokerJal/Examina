using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace ExamLab.Services;

/// <summary>
/// 应用程序设置服务 - 管理应用程序的配置和状态
/// </summary>
public static class AppSettingsService
{
    private const string LAST_PROJECT_PATH_KEY = "LastProjectPath";
    private const string AUTO_RECOVERY_ENABLED_KEY = "AutoRecoveryEnabled";

    /// <summary>
    /// 获取最后保存的项目文件路径
    /// </summary>
    /// <returns>项目文件路径，如果不存在则返回null</returns>
    public static string? GetLastProjectPath()
    {
        try
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(LAST_PROJECT_PATH_KEY))
            {
                return localSettings.Values[LAST_PROJECT_PATH_KEY] as string;
            }
        }
        catch (Exception)
        {
            // 忽略设置读取错误
        }
        return null;
    }

    /// <summary>
    /// 设置最后保存的项目文件路径
    /// </summary>
    /// <param name="projectPath">项目文件路径</param>
    public static void SetLastProjectPath(string? projectPath)
    {
        try
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                if (localSettings.Values.ContainsKey(LAST_PROJECT_PATH_KEY))
                {
                    localSettings.Values.Remove(LAST_PROJECT_PATH_KEY);
                }
            }
            else
            {
                localSettings.Values[LAST_PROJECT_PATH_KEY] = projectPath;
            }
        }
        catch (Exception)
        {
            // 忽略设置保存错误
        }
    }

    /// <summary>
    /// 获取是否启用自动恢复功能
    /// </summary>
    /// <returns>是否启用自动恢复，默认为true</returns>
    public static bool IsAutoRecoveryEnabled()
    {
        try
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey(AUTO_RECOVERY_ENABLED_KEY))
            {
                return (bool)(localSettings.Values[AUTO_RECOVERY_ENABLED_KEY] ?? true);
            }
        }
        catch (Exception)
        {
            // 忽略设置读取错误
        }
        return true; // 默认启用
    }

    /// <summary>
    /// 设置是否启用自动恢复功能
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public static void SetAutoRecoveryEnabled(bool enabled)
    {
        try
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[AUTO_RECOVERY_ENABLED_KEY] = enabled;
        }
        catch (Exception)
        {
            // 忽略设置保存错误
        }
    }

    /// <summary>
    /// 检查文件是否存在且可访问
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件是否存在且可访问</returns>
    public static async Task<bool> IsFileAccessibleAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                return false;
            }

            // 尝试获取StorageFile以验证访问权限
            StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
            return file != null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 清除所有应用程序设置
    /// </summary>
    public static void ClearAllSettings()
    {
        try
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values.Clear();
        }
        catch (Exception)
        {
            // 忽略清除错误
        }
    }
}
