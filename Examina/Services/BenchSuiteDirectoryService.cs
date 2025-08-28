using BenchSuite.Models;
using Examina.Models;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// BenchSuite目录管理服务实现
/// </summary>
public class BenchSuiteDirectoryService : IBenchSuiteDirectoryService
{
    private readonly ILogger<BenchSuiteDirectoryService> _logger;
    private readonly string _basePath;
    private readonly Dictionary<ModuleType, string> _directoryMapping;

    public BenchSuiteDirectoryService(ILogger<BenchSuiteDirectoryService> logger)
    {
        _logger = logger;
        _basePath = @"C:\河北对口计算机\";
        _directoryMapping = new Dictionary<ModuleType, string>
        {
            { ModuleType.CSharp, "CSharp" },
            { ModuleType.PowerPoint, "PPT" },
            { ModuleType.Word, "WORD" },
            { ModuleType.Excel, "EXCEL" },
            { ModuleType.Windows, "WINDOWS" }
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
    /// 获取指定模块类型的目录路径（旧版本，保持兼容性）
    /// </summary>
    public string GetDirectoryPath(ModuleType moduleType)
    {
        return _directoryMapping.TryGetValue(moduleType, out string? subdirectory)
            ? System.IO.Path.Combine(_basePath, subdirectory)
            : throw new ArgumentException($"不支持的模块类型: {moduleType}", nameof(moduleType));
    }

    /// <summary>
    /// 获取指定考试类型和ID的模块类型目录路径
    /// </summary>
    public string GetExamDirectoryPath(ExamType examType, int examId, ModuleType moduleType)
    {
        if (!_directoryMapping.TryGetValue(moduleType, out string? subdirectory))
        {
            throw new ArgumentException($"不支持的模块类型: {moduleType}", nameof(moduleType));
        }

        string examTypeFolder = GetExamTypeFolder(examType);
        return System.IO.Path.Combine(_basePath, examTypeFolder, examId.ToString(), subdirectory);
    }

    /// <summary>
    /// 获取考试文件的完整路径（旧版本，保持兼容性）
    /// </summary>
    public string GetExamFilePath(ModuleType moduleType, int examId, int studentId, string fileName)
    {
        string directoryPath = GetDirectoryPath(moduleType);
        string examDirectory = System.IO.Path.Combine(directoryPath, $"Exam_{examId}", $"Student_{studentId}");
        return System.IO.Path.Combine(examDirectory, fileName);
    }

    /// <summary>
    /// 获取考试文件的完整路径（新版本）
    /// </summary>
    public string GetExamFilePath(ExamType examType, int examId, ModuleType moduleType, int studentId, string fileName)
    {
        string directoryPath = GetExamDirectoryPath(examType, examId, moduleType);
        string studentDirectory = System.IO.Path.Combine(directoryPath, $"Student_{studentId}");
        return System.IO.Path.Combine(studentDirectory, fileName);
    }

    /// <summary>
    /// 确保基础目录结构存在（仅创建基础目录，不创建科目文件夹）
    /// </summary>
    public async Task<bool> EnsureDirectoryStructureAsync()
    {
        try
        {
            _logger.LogInformation("确保BenchSuite基础目录结构存在，基础路径: {BasePath}", _basePath);

            // 仅创建基础目录
            if (!System.IO.Directory.Exists(_basePath))
            {
                _ = System.IO.Directory.CreateDirectory(_basePath);
                _logger.LogInformation("创建基础目录: {BasePath}", _basePath);
            }

            _logger.LogInformation("基础目录结构确保完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确保基础目录结构时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 确保指定考试的目录结构存在
    /// </summary>
    public async Task<bool> EnsureExamDirectoryStructureAsync(ExamType examType, int examId)
    {
        try
        {
            _logger.LogInformation("确保考试目录结构存在，考试类型: {ExamType}, 考试ID: {ExamId}", examType, examId);

            // 首先确保基础目录存在
            bool baseResult = await EnsureDirectoryStructureAsync();
            if (!baseResult)
            {
                return false;
            }

            // 创建考试类型目录
            string examTypeFolder = GetExamTypeFolder(examType);
            string examTypePath = System.IO.Path.Combine(_basePath, examTypeFolder);
            if (!System.IO.Directory.Exists(examTypePath))
            {
                _ = System.IO.Directory.CreateDirectory(examTypePath);
                _logger.LogInformation("创建考试类型目录: {ExamTypePath}", examTypePath);
            }

            // 创建考试ID目录
            string examIdPath = System.IO.Path.Combine(examTypePath, examId.ToString());
            if (!System.IO.Directory.Exists(examIdPath))
            {
                _ = System.IO.Directory.CreateDirectory(examIdPath);
                _logger.LogInformation("创建考试ID目录: {ExamIdPath}", examIdPath);
            }

            // 创建各科目目录
            int createdCount = 0;
            foreach (KeyValuePair<ModuleType, string> mapping in _directoryMapping)
            {
                string subjectPath = System.IO.Path.Combine(examIdPath, mapping.Value);
                if (!System.IO.Directory.Exists(subjectPath))
                {
                    _ = System.IO.Directory.CreateDirectory(subjectPath);
                    createdCount++;
                    _logger.LogInformation("创建科目目录: {SubjectPath}", subjectPath);
                }
            }

            _logger.LogInformation("考试目录结构确保完成，创建了 {CreatedCount} 个新目录", createdCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确保考试目录结构时发生异常");
            return false;
        }
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

            foreach (ModuleType moduleType in Enum.GetValues<ModuleType>())
            {
                string directoryPath = GetDirectoryPath(moduleType);
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    continue;
                }

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
    /// 获取目录使用情况统计（简化版本）
    /// </summary>
    public async Task<(int TotalFileCount, long TotalSizeBytes)> GetDirectoryUsageAsync()
    {
        try
        {
            _logger.LogInformation("获取目录使用情况统计");

            int totalFileCount = 0;
            long totalSizeBytes = 0;

            foreach (ModuleType moduleType in Enum.GetValues<ModuleType>())
            {
                string directoryPath = GetDirectoryPath(moduleType);
                if (System.IO.Directory.Exists(directoryPath))
                {
                    System.IO.DirectoryInfo directoryInfo = new(directoryPath);
                    System.IO.FileInfo[] files = directoryInfo.GetFiles("*", System.IO.SearchOption.AllDirectories);

                    totalFileCount += files.Length;
                    totalSizeBytes += files.Sum(f => f.Length);
                }
            }

            _logger.LogInformation("目录使用情况统计完成，总文件数: {TotalFileCount}, 总大小: {TotalSizeBytes} 字节",
                totalFileCount, totalSizeBytes);

            return (totalFileCount, totalSizeBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取目录使用情况时发生异常");
            return (0, 0);
        }
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
                _ = System.IO.Directory.CreateDirectory(backupPath);
            }

            int copiedFileCount = 0;

            foreach (ModuleType moduleType in Enum.GetValues<ModuleType>())
            {
                string sourceDirectory = System.IO.Path.Combine(GetDirectoryPath(moduleType), $"Exam_{examId}", $"Student_{studentId}");
                if (!System.IO.Directory.Exists(sourceDirectory))
                {
                    continue;
                }

                string targetDirectory = System.IO.Path.Combine(backupPath, GetModuleTypeDescription(moduleType));
                if (!System.IO.Directory.Exists(targetDirectory))
                {
                    _ = System.IO.Directory.CreateDirectory(targetDirectory);
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
                        _ = System.IO.Directory.CreateDirectory(targetDir);
                    }

                    _ = file.CopyTo(targetPath, true);
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
    /// 获取模块类型描述
    /// </summary>
    private static string GetModuleTypeDescription(ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Word => "Word文档",
            ModuleType.Excel => "Excel表格",
            ModuleType.PowerPoint => "PowerPoint演示文稿",
            ModuleType.CSharp => "C#编程",
            ModuleType.Windows => "Windows操作",
            _ => moduleType.ToString()
        };
    }

    /// <summary>
    /// 获取考试类型对应的文件夹名称
    /// </summary>
    private static string GetExamTypeFolder(ExamType examType)
    {
        return examType switch
        {
            ExamType.MockExam => "MockExams",
            ExamType.FormalExam => "OnlineExams",
            ExamType.ComprehensiveTraining => "ComprehensiveTraining",
            ExamType.SpecializedTraining => "SpecializedTraining",
            ExamType.Practice => "Practice",
            ExamType.SpecialPractice => "SpecialPractice",
            _ => "Unknown"
        };
    }

    #endregion
}
