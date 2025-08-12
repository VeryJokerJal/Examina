using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace ExamLab.Services;

/// <summary>
/// 文件选择器服务 - 封装Windows.Storage.Pickers的文件操作功能
/// </summary>
public static class FilePickerService
{
    /// <summary>
    /// 选择单个文件进行打开
    /// </summary>
    /// <param name="fileTypes">支持的文件类型扩展名列表</param>
    /// <param name="suggestedStartLocation">建议的起始位置</param>
    /// <returns>选择的文件，如果用户取消则返回null</returns>
    public static async Task<StorageFile?> PickSingleFileAsync(
        IList<string> fileTypes,
        PickerLocationId suggestedStartLocation = PickerLocationId.DocumentsLibrary)
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = suggestedStartLocation,
                ViewMode = PickerViewMode.List
            };

            // 添加支持的文件类型
            foreach (string fileType in fileTypes)
            {
                picker.FileTypeFilter.Add(fileType);
            }

            // 获取当前窗口句柄
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            return await picker.PickSingleFileAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("文件选择失败", $"无法打开文件选择器：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 选择多个文件进行打开
    /// </summary>
    /// <param name="fileTypes">支持的文件类型扩展名列表</param>
    /// <param name="suggestedStartLocation">建议的起始位置</param>
    /// <returns>选择的文件列表</returns>
    public static async Task<IReadOnlyList<StorageFile>?> PickMultipleFilesAsync(
        IList<string> fileTypes,
        PickerLocationId suggestedStartLocation = PickerLocationId.DocumentsLibrary)
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = suggestedStartLocation,
                ViewMode = PickerViewMode.List
            };

            // 添加支持的文件类型
            foreach (string fileType in fileTypes)
            {
                picker.FileTypeFilter.Add(fileType);
            }

            // 获取当前窗口句柄
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            return await picker.PickMultipleFilesAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("文件选择失败", $"无法打开文件选择器：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 选择文件保存位置
    /// </summary>
    /// <param name="suggestedFileName">建议的文件名</param>
    /// <param name="fileTypeChoices">文件类型选择字典</param>
    /// <param name="suggestedStartLocation">建议的起始位置</param>
    /// <returns>选择的保存文件，如果用户取消则返回null</returns>
    public static async Task<StorageFile?> PickSaveFileAsync(
        string suggestedFileName,
        IDictionary<string, IList<string>> fileTypeChoices,
        PickerLocationId suggestedStartLocation = PickerLocationId.DocumentsLibrary)
    {
        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = suggestedStartLocation,
                SuggestedFileName = suggestedFileName
            };

            // 添加文件类型选择
            foreach (var choice in fileTypeChoices)
            {
                picker.FileTypeChoices.Add(choice.Key, choice.Value);
            }

            // 获取当前窗口句柄
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            return await picker.PickSaveFileAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("文件保存失败", $"无法打开文件保存器：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 选择JSON文件进行打开
    /// </summary>
    /// <returns>选择的JSON文件</returns>
    public static async Task<StorageFile?> PickJsonFileForOpenAsync()
    {
        var fileTypes = new List<string> { ".json" };
        return await PickSingleFileAsync(fileTypes);
    }

    /// <summary>
    /// 选择JSON文件保存位置
    /// </summary>
    /// <param name="suggestedFileName">建议的文件名</param>
    /// <returns>选择的保存文件</returns>
    public static async Task<StorageFile?> PickJsonFileForSaveAsync(string suggestedFileName)
    {
        var fileTypeChoices = new Dictionary<string, IList<string>>
        {
            ["JSON文件"] = new List<string> { ".json" }
        };

        return await PickSaveFileAsync(suggestedFileName, fileTypeChoices);
    }

    /// <summary>
    /// 选择ExamLab项目文件保存位置
    /// </summary>
    /// <param name="suggestedFileName">建议的文件名</param>
    /// <returns>选择的保存文件</returns>
    public static async Task<StorageFile?> PickProjectFileForSaveAsync(string suggestedFileName)
    {
        Dictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>
        {
            ["ExamLab项目文件"] = new List<string> { ".xml" }
        };

        return await PickSaveFileAsync(suggestedFileName, fileTypeChoices);
    }

    /// <summary>
    /// 选择试卷文件进行导入
    /// </summary>
    /// <returns>选择的试卷文件</returns>
    public static async Task<StorageFile?> PickExamFileForImportAsync()
    {
        var fileTypes = new List<string> { ".json", ".xml" };
        return await PickSingleFileAsync(fileTypes);
    }

    /// <summary>
    /// 选择ExamLab项目文件进行导入
    /// </summary>
    /// <returns>选择的项目文件</returns>
    public static async Task<StorageFile?> PickProjectFileForImportAsync()
    {
        List<string> fileTypes = new List<string> { ".xml" };
        return await PickSingleFileAsync(fileTypes);
    }

    /// <summary>
    /// 选择试卷文件保存位置
    /// </summary>
    /// <param name="examName">试卷名称</param>
    /// <param name="exportFormat">导出格式</param>
    /// <returns>选择的保存文件</returns>
    public static async Task<StorageFile?> PickExamFileForExportAsync(string examName, ExportFormat exportFormat = ExportFormat.Json)
    {
        string suggestedFileName = SanitizeFileName($"{examName}_{DateTime.Now:yyyyMMdd_HHmmss}");
        
        var fileTypeChoices = exportFormat switch
        {
            ExportFormat.Json => new Dictionary<string, IList<string>>
            {
                ["JSON文件"] = new List<string> { ".json" }
            },
            ExportFormat.Xml => new Dictionary<string, IList<string>>
            {
                ["XML文件"] = new List<string> { ".xml" }
            },
            ExportFormat.Both => new Dictionary<string, IList<string>>
            {
                ["JSON文件"] = new List<string> { ".json" },
                ["XML文件"] = new List<string> { ".xml" }
            },
            _ => new Dictionary<string, IList<string>>
            {
                ["JSON文件"] = new List<string> { ".json" }
            }
        };

        return await PickSaveFileAsync(suggestedFileName, fileTypeChoices);
    }

    /// <summary>
    /// 选择文件夹
    /// </summary>
    /// <param name="suggestedStartLocation">建议的起始位置</param>
    /// <returns>选择的文件夹</returns>
    public static async Task<StorageFolder?> PickFolderAsync(
        PickerLocationId suggestedStartLocation = PickerLocationId.DocumentsLibrary)
    {
        try
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = suggestedStartLocation,
                ViewMode = PickerViewMode.List
            };

            // 添加文件类型过滤器（必须至少有一个）
            picker.FileTypeFilter.Add("*");

            // 获取当前窗口句柄
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            return await picker.PickSingleFolderAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync("文件夹选择失败", $"无法打开文件夹选择器：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 批量导出试卷到指定文件夹
    /// </summary>
    /// <param name="examNames">试卷名称列表</param>
    /// <param name="exportFormat">导出格式</param>
    /// <returns>选择的文件夹和生成的文件名列表</returns>
    public static async Task<(StorageFolder? folder, List<string> fileNames)> PickFolderForBatchExportAsync(
        IList<string> examNames, 
        ExportFormat exportFormat = ExportFormat.Json)
    {
        var folder = await PickFolderAsync();
        var fileNames = new List<string>();

        if (folder != null)
        {
            string extension = exportFormat switch
            {
                ExportFormat.Json => ".json",
                ExportFormat.Xml => ".xml",
                _ => ".json"
            };

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            foreach (string examName in examNames)
            {
                string fileName = SanitizeFileName($"{examName}_{timestamp}{extension}");
                fileNames.Add(fileName);
            }
        }

        return (folder, fileNames);
    }

    /// <summary>
    /// 清理文件名中的非法字符
    /// </summary>
    /// <param name="fileName">原始文件名</param>
    /// <returns>清理后的文件名</returns>
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "untitled";
        }

        // 移除或替换非法字符
        char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
        string sanitized = fileName;

        foreach (char invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar, '_');
        }

        // 限制文件名长度
        if (sanitized.Length > 100)
        {
            sanitized = sanitized.Substring(0, 100);
        }

        return sanitized;
    }

    /// <summary>
    /// 验证文件扩展名
    /// </summary>
    /// <param name="file">要验证的文件</param>
    /// <param name="expectedExtensions">期望的扩展名列表</param>
    /// <returns>是否为有效的文件类型</returns>
    public static bool IsValidFileType(StorageFile file, IList<string> expectedExtensions)
    {
        if (file == null)
        {
            return false;
        }

        string fileExtension = System.IO.Path.GetExtension(file.Name).ToLowerInvariant();
        
        foreach (string expectedExtension in expectedExtensions)
        {
            if (string.Equals(fileExtension, expectedExtension, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取文件大小的友好显示格式
    /// </summary>
    /// <param name="file">文件</param>
    /// <returns>格式化的文件大小字符串</returns>
    public static async Task<string> GetFileSizeStringAsync(StorageFile file)
    {
        try
        {
            var properties = await file.GetBasicPropertiesAsync();
            ulong sizeInBytes = properties.Size;

            return sizeInBytes switch
            {
                < 1024 => $"{sizeInBytes} B",
                < 1024 * 1024 => $"{sizeInBytes / 1024.0:F1} KB",
                < 1024 * 1024 * 1024 => $"{sizeInBytes / (1024.0 * 1024.0):F1} MB",
                _ => $"{sizeInBytes / (1024.0 * 1024.0 * 1024.0):F1} GB"
            };
        }
        catch
        {
            return "未知大小";
        }
    }

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="folder">文件夹</param>
    /// <param name="fileName">文件名</param>
    /// <returns>文件是否存在</returns>
    public static async Task<bool> FileExistsAsync(StorageFolder folder, string fileName)
    {
        try
        {
            await folder.GetFileAsync(fileName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 生成唯一的文件名（如果文件已存在）
    /// </summary>
    /// <param name="folder">目标文件夹</param>
    /// <param name="originalFileName">原始文件名</param>
    /// <returns>唯一的文件名</returns>
    public static async Task<string> GenerateUniqueFileNameAsync(StorageFolder folder, string originalFileName)
    {
        if (!await FileExistsAsync(folder, originalFileName))
        {
            return originalFileName;
        }

        string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(originalFileName);
        string extension = System.IO.Path.GetExtension(originalFileName);

        int counter = 1;
        string newFileName;

        do
        {
            newFileName = $"{nameWithoutExtension} ({counter}){extension}";
            counter++;
        }
        while (await FileExistsAsync(folder, newFileName));

        return newFileName;
    }
}

/// <summary>
/// 导出格式枚举
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// JSON格式
    /// </summary>
    Json,

    /// <summary>
    /// XML格式
    /// </summary>
    Xml,

    /// <summary>
    /// 同时支持JSON和XML
    /// </summary>
    Both
}
