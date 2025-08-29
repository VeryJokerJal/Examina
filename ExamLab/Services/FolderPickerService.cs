using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace ExamLab.Services;

/// <summary>
/// 文件夹选择器服务 - 封装Windows.Storage.Pickers的文件夹操作功能
/// </summary>
public static class FolderPickerService
{
    /// <summary>
    /// 选择单个文件夹
    /// </summary>
    /// <param name="suggestedStartLocation">建议的起始位置</param>
    /// <returns>选择的文件夹，如果用户取消则返回null</returns>
    public static async Task<StorageFolder?> PickSingleFolderAsync(
        PickerLocationId suggestedStartLocation = PickerLocationId.DocumentsLibrary)
    {
        try
        {
            FolderPicker picker = new()
            {
                SuggestedStartLocation = suggestedStartLocation,
                ViewMode = PickerViewMode.List
            };

            // 添加文件类型过滤器（文件夹选择器需要至少一个文件类型）
            picker.FileTypeFilter.Add("*");

            // 获取当前窗口句柄
            nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
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
    /// 选择桌面文件夹
    /// </summary>
    /// <returns>选择的文件夹</returns>
    public static async Task<StorageFolder?> PickDesktopFolderAsync()
    {
        return await PickSingleFolderAsync(PickerLocationId.Desktop);
    }

    /// <summary>
    /// 选择文档文件夹
    /// </summary>
    /// <returns>选择的文件夹</returns>
    public static async Task<StorageFolder?> PickDocumentsFolderAsync()
    {
        return await PickSingleFolderAsync(PickerLocationId.DocumentsLibrary);
    }

    /// <summary>
    /// 选择下载文件夹
    /// </summary>
    /// <returns>选择的文件夹</returns>
    public static async Task<StorageFolder?> PickDownloadsFolderAsync()
    {
        return await PickSingleFolderAsync(PickerLocationId.Downloads);
    }

    /// <summary>
    /// 选择图片文件夹
    /// </summary>
    /// <returns>选择的文件夹</returns>
    public static async Task<StorageFolder?> PickPicturesFolderAsync()
    {
        return await PickSingleFolderAsync(PickerLocationId.PicturesLibrary);
    }

    /// <summary>
    /// 选择音乐文件夹
    /// </summary>
    /// <returns>选择的文件夹</returns>
    public static async Task<StorageFolder?> PickMusicFolderAsync()
    {
        return await PickSingleFolderAsync(PickerLocationId.MusicLibrary);
    }

    /// <summary>
    /// 选择视频文件夹
    /// </summary>
    /// <returns>选择的文件夹</returns>
    public static async Task<StorageFolder?> PickVideosFolderAsync()
    {
        return await PickSingleFolderAsync(PickerLocationId.VideosLibrary);
    }

    /// <summary>
    /// 验证文件夹是否存在且可访问
    /// </summary>
    /// <param name="folderPath">文件夹路径</param>
    /// <returns>是否有效</returns>
    public static async Task<bool> IsFolderValidAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return false;
        }

        try
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            return folder != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取文件夹的显示名称
    /// </summary>
    /// <param name="folder">存储文件夹</param>
    /// <returns>显示名称</returns>
    public static string GetFolderDisplayName(StorageFolder folder)
    {
        if (folder == null)
        {
            return string.Empty;
        }

        return folder.DisplayName;
    }

    /// <summary>
    /// 获取文件夹的完整路径
    /// </summary>
    /// <param name="folder">存储文件夹</param>
    /// <returns>完整路径</returns>
    public static string GetFolderPath(StorageFolder folder)
    {
        if (folder == null)
        {
            return string.Empty;
        }

        return folder.Path;
    }

    /// <summary>
    /// 获取文件夹大小信息字符串
    /// </summary>
    /// <param name="folder">存储文件夹</param>
    /// <returns>大小信息字符串</returns>
    public static async Task<string> GetFolderSizeStringAsync(StorageFolder folder)
    {
        if (folder == null)
        {
            return "未知大小";
        }

        try
        {
            // 获取文件夹中的文件数量
            var files = await folder.GetFilesAsync();
            var folders = await folder.GetFoldersAsync();
            
            return $"{files.Count} 个文件，{folders.Count} 个文件夹";
        }
        catch
        {
            return "无法获取大小信息";
        }
    }
}
