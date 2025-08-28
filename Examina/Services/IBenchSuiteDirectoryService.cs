using Examina.Models;
using BenchSuite.Models;

namespace Examina.Services;

/// <summary>
/// BenchSuite目录管理服务接口
/// </summary>
public interface IBenchSuiteDirectoryService
{
    /// <summary>
    /// 获取基础目录路径
    /// </summary>
    /// <returns>基础目录路径</returns>
    string GetBasePath();

    /// <summary>
    /// 获取指定模块类型的目录路径
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <returns>目录路径</returns>
    string GetDirectoryPath(ModuleType moduleType);

    /// <summary>
    /// 获取指定考试类型和ID的模块类型目录路径
    /// </summary>
    /// <param name="examType">考试类型</param>
    /// <param name="examId">考试ID</param>
    /// <param name="moduleType">模块类型</param>
    /// <returns>目录路径</returns>
    string GetExamDirectoryPath(ExamType examType, int examId, ModuleType moduleType);

    /// <summary>
    /// 获取考试文件的完整路径（旧版本，保持兼容性）
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="fileName">文件名</param>
    /// <returns>完整文件路径</returns>
    string GetExamFilePath(ModuleType moduleType, int examId, int studentId, string fileName);

    /// <summary>
    /// 获取考试文件的完整路径（新版本）
    /// </summary>
    /// <param name="examType">考试类型</param>
    /// <param name="examId">考试ID</param>
    /// <param name="moduleType">模块类型</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="fileName">文件名</param>
    /// <returns>完整文件路径</returns>
    string GetExamFilePath(ExamType examType, int examId, ModuleType moduleType, int studentId, string fileName);

    /// <summary>
    /// 确保基础目录结构存在（仅创建基础目录，不创建科目文件夹）
    /// </summary>
    /// <returns>操作是否成功</returns>
    Task<bool> EnsureDirectoryStructureAsync();

    /// <summary>
    /// 确保指定考试的目录结构存在
    /// </summary>
    /// <param name="examType">考试类型</param>
    /// <param name="examId">考试ID</param>
    /// <returns>操作是否成功</returns>
    Task<bool> EnsureExamDirectoryStructureAsync(ExamType examType, int examId);

    /// <summary>
    /// 清理过期的考试文件
    /// </summary>
    /// <param name="retentionDays">保留天数</param>
    /// <returns>清理的文件数量</returns>
    Task<int> CleanupExpiredFilesAsync(int retentionDays = 30);

    /// <summary>
    /// 获取目录使用情况统计
    /// </summary>
    /// <returns>文件数量和总大小</returns>
    Task<(int TotalFileCount, long TotalSizeBytes)> GetDirectoryUsageAsync();

    /// <summary>
    /// 备份考试文件
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="backupPath">备份路径</param>
    /// <returns>备份是否成功</returns>
    Task<bool> BackupExamFilesAsync(int examId, int studentId, string backupPath);
}
