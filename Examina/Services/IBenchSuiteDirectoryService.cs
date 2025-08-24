using Examina.Models;
using Examina.Models.BenchSuite;

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
    /// 获取指定文件类型的目录路径
    /// </summary>
    /// <param name="fileType">文件类型</param>
    /// <returns>目录路径</returns>
    string GetDirectoryPath(BenchSuiteFileType fileType);

    /// <summary>
    /// 获取指定考试类型和ID的文件类型目录路径
    /// </summary>
    /// <param name="examType">考试类型</param>
    /// <param name="examId">考试ID</param>
    /// <param name="fileType">文件类型</param>
    /// <returns>目录路径</returns>
    string GetExamDirectoryPath(ExamType examType, int examId, BenchSuiteFileType fileType);

    /// <summary>
    /// 获取考试文件的完整路径（旧版本，保持兼容性）
    /// </summary>
    /// <param name="fileType">文件类型</param>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="fileName">文件名</param>
    /// <returns>完整文件路径</returns>
    string GetExamFilePath(BenchSuiteFileType fileType, int examId, int studentId, string fileName);

    /// <summary>
    /// 获取考试文件的完整路径（新版本）
    /// </summary>
    /// <param name="examType">考试类型</param>
    /// <param name="examId">考试ID</param>
    /// <param name="fileType">文件类型</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="fileName">文件名</param>
    /// <returns>完整文件路径</returns>
    string GetExamFilePath(ExamType examType, int examId, BenchSuiteFileType fileType, int studentId, string fileName);

    /// <summary>
    /// 确保目录结构存在
    /// </summary>
    /// <returns>操作结果</returns>
    Task<BenchSuiteDirectoryValidationResult> EnsureDirectoryStructureAsync();

    /// <summary>
    /// 清理过期的考试文件
    /// </summary>
    /// <param name="retentionDays">保留天数</param>
    /// <returns>清理的文件数量</returns>
    Task<int> CleanupExpiredFilesAsync(int retentionDays = 30);

    /// <summary>
    /// 获取目录使用情况统计
    /// </summary>
    /// <returns>目录使用情况</returns>
    Task<BenchSuiteDirectoryUsageInfo> GetDirectoryUsageAsync();

    /// <summary>
    /// 备份考试文件
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="studentId">学生ID</param>
    /// <param name="backupPath">备份路径</param>
    /// <returns>备份是否成功</returns>
    Task<bool> BackupExamFilesAsync(int examId, int studentId, string backupPath);
}
