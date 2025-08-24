using System.ComponentModel;
using System.Reflection;
using Examina.Models.BenchSuite;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// BenchSuite目录管理服务实现
/// </summary>
public class BenchSuiteDirectoryService : IBenchSuiteDirectoryService
{
    private readonly ILogger<BenchSuiteDirectoryService> _logger;
    private readonly string _basePath;
    private readonly Dictionary<BenchSuiteFileType, string> _directoryMapping;

    public BenchSuiteDirectoryService(ILogger<BenchSuiteDirectoryService> logger)
    {
        _logger = logger;
        _basePath = @"C:\河北对口计算机\";
        _directoryMapping = new Dictionary<BenchSuiteFileType, string>
        {
            { BenchSuiteFileType.CSharp, "CSharp" },
            { BenchSuiteFileType.PowerPoint, "PPT" },
            { BenchSuiteFileType.Word, "WORD" },
            { BenchSuiteFileType.Excel, "EXCEL" },
            { BenchSuiteFileType.Windows, "WINDOWS" }
        };
    }

    /// <summary>
    /// 获取基础目录路径
    /// </summary>
    public string GetBasePath()
    {
        return _basePath;
    }

    /// <summary>
    /// 获取指定文件类型的目录路径
    /// </summary>
    public string GetDirectoryPath(BenchSuiteFileType fileType)
    {
        if (_directoryMapping.TryGetValue(fileType, out string? subdirectory))
        {
            return System.IO.Path.Combine(_basePath, subdirectory);
        }
        throw new ArgumentException($"不支持的文件类型: {fileType}", nameof(fileType));
    }

    /// <summary>
    /// 获取考试文件的完整路径
    /// </summary>
    public string GetExamFilePath(BenchSuiteFileType fileType, int examId, int studentId, string fileName)
    {
        string directoryPath = GetDirectoryPath(fileType);
        string examDirectory = System.IO.Path.Combine(directoryPath, $"Exam_{examId}", $"Student_{studentId}");
        return System.IO.Path.Combine(examDirectory, fileName);
    }

    /// <summary>
    /// 确保目录结构存在
    /// </summary>
    public async Task<BenchSuiteDirectoryValidationResult> EnsureDirectoryStructureAsync()
    {
        BenchSuiteDirectoryValidationResult result = new();

        try
        {
            _logger.LogInformation("确保BenchSuite目录结构存在，基础路径: {BasePath}", _basePath);

            // 创建基础目录
            if (!System.IO.Directory.Exists(_basePath))
            {
                System.IO.Directory.CreateDirectory(_basePath);
                _logger.LogInformation("创建基础目录: {BasePath}", _basePath);
            }

            // 创建各子目录
            List<string> createdDirectories = new();
            foreach (KeyValuePair<BenchSuiteFileType, string> mapping in _directoryMapping)
            {
                string directoryPath = System.IO.Path.Combine(_basePath, mapping.Value);
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    System.IO.Directory.CreateDirectory(directoryPath);
                    createdDirectories.Add(directoryPath);
                    _logger.LogInformation("创建子目录: {DirectoryPath}", directoryPath);
                }
            }

            result.IsValid = true;
            result.Details = createdDirectories.Count > 0 
                ? $"成功创建 {createdDirectories.Count} 个目录" 
                : "目录结构已存在";

            _logger.LogInformation("目录结构确保完成: {Details}", result.Details);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确保目录结构时发生异常");
            result.IsValid = false;
            result.ErrorMessage = $"确保目录结构时发生异常: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 清理过期的考试文件
    /// </summary>
    public async Task<int> CleanupExpiredFilesAsync(int retentionDays = 30)
    {
        int deletedFileCount = 0;

        try
        {
            _logger.LogInformation("开始清理过期考试文件，保留天数: {RetentionDays}", retentionDays);

            DateTime cutoffDate = DateTime.Now.AddDays(-retentionDays);

            foreach (BenchSuiteFileType fileType in Enum.GetValues<BenchSuiteFileType>())
            {
                string directoryPath = GetDirectoryPath(fileType);
                if (!System.IO.Directory.Exists(directoryPath))
                    continue;

                System.IO.DirectoryInfo directoryInfo = new(directoryPath);
                System.IO.FileInfo[] files = directoryInfo.GetFiles("*", System.IO.SearchOption.AllDirectories);

                foreach (System.IO.FileInfo file in files)
                {
                    if (file.LastWriteTime < cutoffDate)
                    {
                        try
                        {
                            file.Delete();
                            deletedFileCount++;
                            _logger.LogDebug("删除过期文件: {FilePath}", file.FullName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "删除文件失败: {FilePath}", file.FullName);
                        }
                    }
                }
            }

            _logger.LogInformation("清理过期文件完成，删除文件数量: {DeletedFileCount}", deletedFileCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期文件时发生异常");
        }

        return deletedFileCount;
    }

    /// <summary>
    /// 获取目录使用情况统计
    /// </summary>
    public async Task<BenchSuiteDirectoryUsageInfo> GetDirectoryUsageAsync()
    {
        BenchSuiteDirectoryUsageInfo usageInfo = new()
        {
            BasePath = _basePath,
            LastUpdated = DateTime.Now
        };

        try
        {
            _logger.LogInformation("获取目录使用情况统计");

            foreach (BenchSuiteFileType fileType in Enum.GetValues<BenchSuiteFileType>())
            {
                string directoryPath = GetDirectoryPath(fileType);
                DirectoryTypeUsage typeUsage = new()
                {
                    FileType = fileType,
                    DirectoryPath = directoryPath
                };

                if (System.IO.Directory.Exists(directoryPath))
                {
                    System.IO.DirectoryInfo directoryInfo = new(directoryPath);
                    System.IO.FileInfo[] files = directoryInfo.GetFiles("*", System.IO.SearchOption.AllDirectories);

                    typeUsage.FileCount = files.Length;
                    typeUsage.SizeBytes = files.Sum(f => f.Length);
                    typeUsage.LatestFileTime = files.Length > 0 ? files.Max(f => f.LastWriteTime) : null;
                    typeUsage.OldestFileTime = files.Length > 0 ? files.Min(f => f.LastWriteTime) : null;
                }

                usageInfo.TypeUsages[fileType] = typeUsage;
                usageInfo.TotalFileCount += typeUsage.FileCount;
                usageInfo.TotalSizeBytes += typeUsage.SizeBytes;
            }

            _logger.LogInformation("目录使用情况统计完成，总文件数: {TotalFileCount}, 总大小: {TotalSizeBytes} 字节",
                usageInfo.TotalFileCount, usageInfo.TotalSizeBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取目录使用情况时发生异常");
        }

        return usageInfo;
    }

    /// <summary>
    /// 备份考试文件
    /// </summary>
    public async Task<bool> BackupExamFilesAsync(int examId, int studentId, string backupPath)
    {
        try
        {
            _logger.LogInformation("开始备份考试文件，考试ID: {ExamId}, 学生ID: {StudentId}, 备份路径: {BackupPath}",
                examId, studentId, backupPath);

            // 确保备份目录存在
            if (!System.IO.Directory.Exists(backupPath))
            {
                System.IO.Directory.CreateDirectory(backupPath);
            }

            int copiedFileCount = 0;

            foreach (BenchSuiteFileType fileType in Enum.GetValues<BenchSuiteFileType>())
            {
                string sourceDirectory = System.IO.Path.Combine(GetDirectoryPath(fileType), $"Exam_{examId}", $"Student_{studentId}");
                if (!System.IO.Directory.Exists(sourceDirectory))
                    continue;

                string targetDirectory = System.IO.Path.Combine(backupPath, GetFileTypeDescription(fileType));
                if (!System.IO.Directory.Exists(targetDirectory))
                {
                    System.IO.Directory.CreateDirectory(targetDirectory);
                }

                System.IO.DirectoryInfo sourceInfo = new(sourceDirectory);
                System.IO.FileInfo[] files = sourceInfo.GetFiles("*", System.IO.SearchOption.AllDirectories);

                foreach (System.IO.FileInfo file in files)
                {
                    string relativePath = System.IO.Path.GetRelativePath(sourceDirectory, file.FullName);
                    string targetPath = System.IO.Path.Combine(targetDirectory, relativePath);

                    string? targetDir = System.IO.Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(targetDir) && !System.IO.Directory.Exists(targetDir))
                    {
                        System.IO.Directory.CreateDirectory(targetDir);
                    }

                    file.CopyTo(targetPath, true);
                    copiedFileCount++;
                }
            }

            _logger.LogInformation("考试文件备份完成，复制文件数量: {CopiedFileCount}", copiedFileCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "备份考试文件时发生异常");
            return false;
        }
    }

    #region 私有方法

    /// <summary>
    /// 获取文件类型描述
    /// </summary>
    private static string GetFileTypeDescription(BenchSuiteFileType fileType)
    {
        FieldInfo? field = fileType.GetType().GetField(fileType.ToString());
        DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? fileType.ToString();
    }

    #endregion
}
