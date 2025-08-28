using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Examina.Services;

/// <summary>
/// 目录清理服务实现
/// </summary>
public class DirectoryCleanupService : IDirectoryCleanupService
{
    private readonly ILogger<DirectoryCleanupService> _logger;
    private const string DefaultExamDirectory = @"C:\河北对口计算机";

    public DirectoryCleanupService(ILogger<DirectoryCleanupService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 清理指定目录下的所有文件和文件夹
    /// </summary>
    public async Task<DirectoryCleanupResult> CleanupDirectoryAsync(string directoryPath)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        int deletedFiles = 0;
        int deletedDirectories = 0;
        int skippedFiles = 0;
        int skippedDirectories = 0;
        List<string> detailedErrors = new();

        try
        {
            _logger.LogInformation("开始清理目录: {DirectoryPath}", directoryPath);

            if (!Directory.Exists(directoryPath))
            {
                _logger.LogInformation("目录不存在，无需清理: {DirectoryPath}", directoryPath);
                return DirectoryCleanupResult.Success(0, 0, stopwatch.ElapsedMilliseconds);
            }

            // 清理文件
            await CleanupFilesAsync(directoryPath, ref deletedFiles, ref skippedFiles, detailedErrors);

            // 清理子目录
            await CleanupSubDirectoriesAsync(directoryPath, ref deletedDirectories, ref skippedDirectories, detailedErrors);

            stopwatch.Stop();

            DirectoryCleanupResult result = new()
            {
                IsSuccess = true,
                DeletedFileCount = deletedFiles,
                DeletedDirectoryCount = deletedDirectories,
                SkippedFileCount = skippedFiles,
                SkippedDirectoryCount = skippedDirectories,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                DetailedErrors = detailedErrors
            };

            _logger.LogInformation("目录清理完成: {DirectoryPath}, 删除文件: {DeletedFiles}, 删除目录: {DeletedDirectories}, " +
                                 "跳过文件: {SkippedFiles}, 跳过目录: {SkippedDirectories}, 耗时: {ElapsedMs}ms",
                directoryPath, deletedFiles, deletedDirectories, skippedFiles, skippedDirectories, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            string errorMessage = $"清理目录时发生错误: {ex.Message}";
            _logger.LogError(ex, "清理目录失败: {DirectoryPath}", directoryPath);
            
            return DirectoryCleanupResult.Failure(errorMessage, detailedErrors);
        }
    }

    /// <summary>
    /// 清理考试/训练目录（默认为'C:\河北对口计算机'）
    /// </summary>
    public async Task<DirectoryCleanupResult> CleanupExamDirectoryAsync()
    {
        return await CleanupDirectoryAsync(DefaultExamDirectory);
    }

    /// <summary>
    /// 检查目录是否存在且可访问
    /// </summary>
    public bool IsDirectoryAccessible(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                return false;
            }

            // 尝试获取目录信息来测试访问权限
            DirectoryInfo dirInfo = new(directoryPath);
            _ = dirInfo.GetFiles();
            _ = dirInfo.GetDirectories();
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("目录不可访问: {DirectoryPath}, 错误: {Error}", directoryPath, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 清理目录中的所有文件
    /// </summary>
    private async Task CleanupFilesAsync(string directoryPath, ref int deletedFiles, ref int skippedFiles, List<string> detailedErrors)
    {
        try
        {
            string[] files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            
            foreach (string file in files)
            {
                try
                {
                    // 移除只读属性
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    deletedFiles++;
                    
                    _logger.LogDebug("已删除文件: {FilePath}", file);
                }
                catch (Exception ex)
                {
                    skippedFiles++;
                    string error = $"无法删除文件 {file}: {ex.Message}";
                    detailedErrors.Add(error);
                    _logger.LogWarning("跳过文件删除: {FilePath}, 原因: {Reason}", file, ex.Message);
                }

                // 每处理100个文件后让出控制权
                if ((deletedFiles + skippedFiles) % 100 == 0)
                {
                    await Task.Yield();
                }
            }
        }
        catch (Exception ex)
        {
            string error = $"枚举文件时发生错误: {ex.Message}";
            detailedErrors.Add(error);
            _logger.LogError(ex, "枚举目录文件失败: {DirectoryPath}", directoryPath);
        }
    }

    /// <summary>
    /// 清理目录中的所有子目录
    /// </summary>
    private async Task CleanupSubDirectoriesAsync(string directoryPath, ref int deletedDirectories, ref int skippedDirectories, List<string> detailedErrors)
    {
        try
        {
            string[] directories = Directory.GetDirectories(directoryPath, "*", SearchOption.TopDirectoryOnly);
            
            foreach (string directory in directories)
            {
                try
                {
                    // 递归删除子目录
                    Directory.Delete(directory, true);
                    deletedDirectories++;
                    
                    _logger.LogDebug("已删除目录: {DirectoryPath}", directory);
                }
                catch (Exception ex)
                {
                    skippedDirectories++;
                    string error = $"无法删除目录 {directory}: {ex.Message}";
                    detailedErrors.Add(error);
                    _logger.LogWarning("跳过目录删除: {DirectoryPath}, 原因: {Reason}", directory, ex.Message);
                }

                // 每处理10个目录后让出控制权
                if ((deletedDirectories + skippedDirectories) % 10 == 0)
                {
                    await Task.Yield();
                }
            }
        }
        catch (Exception ex)
        {
            string error = $"枚举子目录时发生错误: {ex.Message}";
            detailedErrors.Add(error);
            _logger.LogError(ex, "枚举子目录失败: {DirectoryPath}", directoryPath);
        }
    }
}
