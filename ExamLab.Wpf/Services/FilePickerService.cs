using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ExamLab.Services;

/// <summary>
/// 文件选择器服务 - WPF版本，使用Microsoft.Win32.OpenFileDialog和SaveFileDialog
/// </summary>
public static class FilePickerService
{
    /// <summary>
    /// 文件信息包装类，用于兼容原有的StorageFile接口
    /// </summary>
    public class FileInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 选择单个文件进行打开
    /// </summary>
    /// <param name="fileTypes">支持的文件类型扩展名列表</param>
    /// <param name="suggestedStartLocation">建议的起始位置（WPF版本中忽略）</param>
    /// <returns>选择的文件，如果用户取消则返回null</returns>
    public static async Task<FileInfo?> PickSingleFileAsync(
        IList<string> fileTypes,
        object? suggestedStartLocation = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dialog = new OpenFileDialog();

                // 设置文件过滤器
                if (fileTypes.Any())
                {
                    var filter = string.Join("|", fileTypes.Select(ft =>
                        $"{ft.ToUpper()} 文件|*{ft}"));
                    dialog.Filter = filter;
                }

                if (dialog.ShowDialog() == true)
                {
                    var fileInfo = new System.IO.FileInfo(dialog.FileName);
                    return new FileInfo
                    {
                        Path = dialog.FileName,
                        Name = fileInfo.Name,
                        DisplayName = System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name),
                        FileType = fileInfo.Extension
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                NotificationService.ShowErrorAsync("文件选择失败", $"无法打开文件选择器：{ex.Message}").Wait();
                return null;
            }
        });
    }

    /// <summary>
    /// 选择JSON文件进行打开
    /// </summary>
    /// <returns>选择的JSON文件</returns>
    public static async Task<FileInfo?> PickJsonFileForOpenAsync()
    {
        var fileTypes = new List<string> { ".json" };
        return await PickSingleFileAsync(fileTypes);
    }

    /// <summary>
    /// 选择JSON文件保存位置
    /// </summary>
    /// <param name="suggestedFileName">建议的文件名</param>
    /// <returns>选择的保存文件</returns>
    public static async Task<FileInfo?> PickJsonFileForSaveAsync(string suggestedFileName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = suggestedFileName,
                    Filter = "JSON文件|*.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var fileInfo = new System.IO.FileInfo(dialog.FileName);
                    return new FileInfo
                    {
                        Path = dialog.FileName,
                        Name = fileInfo.Name,
                        DisplayName = System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name),
                        FileType = fileInfo.Extension
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                NotificationService.ShowErrorAsync("文件保存失败", $"无法打开文件保存器：{ex.Message}").Wait();
                return null;
            }
        });
    }

    /// <summary>
    /// 选择XML文件进行打开
    /// </summary>
    /// <returns>选择的XML文件</returns>
    public static async Task<FileInfo?> PickXmlFileForOpenAsync()
    {
        var fileTypes = new List<string> { ".xml" };
        return await PickSingleFileAsync(fileTypes);
    }

    /// <summary>
    /// 选择XML文件保存位置
    /// </summary>
    /// <param name="suggestedFileName">建议的文件名</param>
    /// <returns>选择的保存文件</returns>
    public static async Task<FileInfo?> PickXmlFileForSaveAsync(string suggestedFileName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    FileName = suggestedFileName,
                    Filter = "XML文件|*.xml"
                };

                if (dialog.ShowDialog() == true)
                {
                    var fileInfo = new System.IO.FileInfo(dialog.FileName);
                    return new FileInfo
                    {
                        Path = dialog.FileName,
                        Name = fileInfo.Name,
                        DisplayName = System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name),
                        FileType = fileInfo.Extension
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                NotificationService.ShowErrorAsync("文件保存失败", $"无法打开文件保存器：{ex.Message}").Wait();
                return null;
            }
        });
    }

    /// <summary>
    /// 获取文件大小的字符串表示
    /// </summary>
    /// <param name="file">文件信息</param>
    /// <returns>文件大小字符串</returns>
    public static async Task<string> GetFileSizeStringAsync(FileInfo file)
    {
        return await Task.Run(() =>
        {
            try
            {
                var fileInfo = new System.IO.FileInfo(file.Path);
                if (!fileInfo.Exists)
                    return "未知大小";

                long bytes = fileInfo.Length;
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double size = bytes;

                while (size >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    size /= 1024;
                }

                return $"{size:0.##} {sizes[order]}";
            }
            catch
            {
                return "未知大小";
            }
        });
    }

    /// <summary>
    /// 读取文件内容
    /// </summary>
    /// <param name="file">文件信息</param>
    /// <returns>文件内容</returns>
    public static async Task<string> ReadTextAsync(FileInfo file)
    {
        return await File.ReadAllTextAsync(file.Path);
    }

    /// <summary>
    /// 写入文件内容
    /// </summary>
    /// <param name="file">文件信息</param>
    /// <param name="content">要写入的内容</param>
    public static async Task WriteTextAsync(FileInfo file, string content)
    {
        await File.WriteAllTextAsync(file.Path, content);
    }
}