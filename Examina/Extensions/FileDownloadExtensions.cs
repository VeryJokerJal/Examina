using System.IO;
using Avalonia.Controls;
using Examina.Models.FileDownload;
using Examina.Views.FileDownload;

namespace Examina.Extensions;

/// <summary>
/// 文件下载扩展方法
/// </summary>
public static class FileDownloadExtensions
{
    /// <summary>
    /// 为模拟考试准备文件
    /// </summary>
    /// <param name="parent">父窗口</param>
    /// <param name="examId">考试ID</param>
    /// <param name="examName">考试名称</param>
    /// <returns>文件准备是否成功</returns>
    public static async Task<bool> PrepareFilesForMockExamAsync(this Window parent, int examId, string examName)
    {
        return await FileDownloadPreparationWindow.ShowDownloadPreparationAsync(
            parent,
            $"模拟考试: {examName}",
            FileDownloadTaskType.MockExam,
            examId);
    }

    /// <summary>
    /// 为正式考试准备文件
    /// </summary>
    /// <param name="parent">父窗口</param>
    /// <param name="examId">考试ID</param>
    /// <param name="examName">考试名称</param>
    /// <returns>文件准备是否成功</returns>
    public static async Task<bool> PrepareFilesForOnlineExamAsync(this Window parent, int examId, string examName)
    {
        return await FileDownloadPreparationWindow.ShowDownloadPreparationAsync(
            parent,
            $"上机统考: {examName}",
            FileDownloadTaskType.OnlineExam,
            examId);
    }

    /// <summary>
    /// 为综合实训准备文件
    /// </summary>
    /// <param name="parent">父窗口</param>
    /// <param name="trainingId">训练ID</param>
    /// <param name="trainingName">训练名称</param>
    /// <returns>文件准备是否成功</returns>
    public static async Task<bool> PrepareFilesForComprehensiveTrainingAsync(this Window parent, int trainingId, string trainingName)
    {
        return await FileDownloadPreparationWindow.ShowDownloadPreparationAsync(
            parent,
            $"综合实训: {trainingName}",
            FileDownloadTaskType.ComprehensiveTraining,
            trainingId);
    }

    /// <summary>
    /// 为专项训练准备文件
    /// </summary>
    /// <param name="parent">父窗口</param>
    /// <param name="trainingId">训练ID</param>
    /// <param name="trainingName">训练名称</param>
    /// <returns>文件准备是否成功</returns>
    public static async Task<bool> PrepareFilesForSpecializedTrainingAsync(this Window parent, int trainingId, string trainingName)
    {
        return await FileDownloadPreparationWindow.ShowDownloadPreparationAsync(
            parent,
            $"专项训练: {trainingName}",
            FileDownloadTaskType.SpecializedTraining,
            trainingId);
    }

    /// <summary>
    /// 通用文件准备方法
    /// </summary>
    /// <param name="parent">父窗口</param>
    /// <param name="taskName">任务名称</param>
    /// <param name="taskType">任务类型</param>
    /// <param name="relatedId">关联ID</param>
    /// <returns>文件准备是否成功</returns>
    public static async Task<bool> PrepareFilesAsync(this Window parent, string taskName, FileDownloadTaskType taskType, int relatedId)
    {
        return await FileDownloadPreparationWindow.ShowDownloadPreparationAsync(parent, taskName, taskType, relatedId);
    }
}

/// <summary>
/// 文件下载帮助类
/// </summary>
public static class FileDownloadHelper
{
    /// <summary>
    /// 检查是否需要下载文件
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <param name="relatedId">关联ID</param>
    /// <returns>是否需要下载文件</returns>
    public static async Task<bool> HasFilesToDownloadAsync(FileDownloadTaskType taskType, int relatedId)
    {
        try
        {
            App? app = Avalonia.Application.Current as App;
            Services.IFileDownloadService? fileDownloadService = app?.GetService<Services.IFileDownloadService>();

            if (fileDownloadService == null)
            {
                return false;
            }

            List<FileDownloadInfo> files = taskType is FileDownloadTaskType.OnlineExam
                ? await fileDownloadService.GetExamFilesAsync(relatedId, taskType)
                : await fileDownloadService.GetTrainingFilesAsync(relatedId, taskType);
            return files.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取下载目录路径
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <param name="relatedId">关联ID</param>
    /// <returns>下载目录路径</returns>
    public static string GetDownloadDirectoryPath(FileDownloadTaskType taskType, int relatedId)
    {
        try
        {
            App? app = Avalonia.Application.Current as App;
            Services.IFileDownloadService? fileDownloadService = app?.GetService<Services.IFileDownloadService>();

            if (fileDownloadService == null)
            {
                // 返回默认路径
                string baseDownloadPath = @"C:\河北对口计算机";

                // 专项训练直接使用基础路径，不创建训练ID子目录
                if (taskType == FileDownloadTaskType.SpecializedTraining)
                {
                    return baseDownloadPath;
                }

                string taskTypeFolder = taskType switch
                {
                    FileDownloadTaskType.MockExam => "模拟考试",
                    FileDownloadTaskType.OnlineExam => "上机统考",
                    FileDownloadTaskType.ComprehensiveTraining => "综合实训",
                    _ => "其他"
                };
                return Path.Combine(baseDownloadPath, taskTypeFolder, relatedId.ToString());
            }

            // 使用服务的方法获取路径
            return fileDownloadService.GetDownloadDirectory(taskType, relatedId);
        }
        catch
        {
            // 异常情况下返回默认路径
            string baseDownloadPath = @"C:\河北对口计算机";

            // 专项训练直接使用基础路径，不创建训练ID子目录
            if (taskType == FileDownloadTaskType.SpecializedTraining)
            {
                return baseDownloadPath;
            }

            return Path.Combine(baseDownloadPath, taskType.ToString(), relatedId.ToString());
        }
    }
    public static string GetDownloadDirectory(FileDownloadTaskType taskType, int relatedId)
    {
        try
        {
            App? app = Avalonia.Application.Current as App;
            Services.IFileDownloadService? fileDownloadService = app?.GetService<Services.IFileDownloadService>();

            return fileDownloadService?.GetDownloadDirectory(taskType, relatedId) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 清理下载的临时文件
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <param name="relatedId">关联ID</param>
    public static async Task CleanupDownloadedFilesAsync(FileDownloadTaskType taskType, int relatedId)
    {
        try
        {
            string downloadDir = GetDownloadDirectory(taskType, relatedId);
            if (!string.IsNullOrEmpty(downloadDir) && Directory.Exists(downloadDir))
            {
                await Task.Run(() => Directory.Delete(downloadDir, true));
            }
        }
        catch
        {
            // 忽略清理错误
        }
    }

    /// <summary>
    /// 获取任务类型的显示名称
    /// </summary>
    /// <param name="taskType">任务类型</param>
    /// <returns>显示名称</returns>
    public static string GetTaskTypeDisplayName(FileDownloadTaskType taskType)
    {
        return taskType switch
        {
            FileDownloadTaskType.MockExam => "模拟考试",
            FileDownloadTaskType.OnlineExam => "上机统考",
            FileDownloadTaskType.ComprehensiveTraining => "综合实训",
            FileDownloadTaskType.SpecializedTraining => "专项训练",
            _ => "未知"
        };
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    /// <param name="bytes">字节数</param>
    /// <returns>格式化的文件大小</returns>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// 格式化时间间隔
    /// </summary>
    /// <param name="timeSpan">时间间隔</param>
    /// <returns>格式化的时间字符串</returns>
    public static string FormatTimeSpan(TimeSpan timeSpan)
    {
        return timeSpan.TotalHours >= 1
            ? $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
            : $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }
}
