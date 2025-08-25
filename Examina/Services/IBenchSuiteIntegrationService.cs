using Examina.Models.BenchSuite;

namespace Examina.Services;

/// <summary>
/// BenchSuite评分系统集成服务接口
/// </summary>
public interface IBenchSuiteIntegrationService
{
    /// <summary>
    /// 对考试文件进行评分
    /// </summary>
    /// <param name="request">评分请求</param>
    /// <returns>评分结果</returns>
    Task<BenchSuiteScoringResult> ScoreExamAsync(BenchSuiteScoringRequest request);

    /// <summary>
    /// 检查BenchSuite服务是否可用
    /// </summary>
    /// <returns>服务是否可用</returns>
    Task<bool> IsServiceAvailableAsync();

    /// <summary>
    /// 获取支持的文件类型
    /// </summary>
    /// <returns>支持的文件类型列表</returns>
    IEnumerable<BenchSuiteFileType> GetSupportedFileTypes();

    /// <summary>
    /// 验证文件目录结构（旧版本，保持兼容性）
    /// </summary>
    /// <param name="basePath">基础路径</param>
    /// <returns>验证结果</returns>
    Task<BenchSuiteDirectoryValidationResult> ValidateDirectoryStructureAsync(string basePath);

    /// <summary>
    /// 验证考试目录结构
    /// </summary>
    /// <param name="examType">考试类型</param>
    /// <param name="examId">考试ID</param>
    /// <returns>验证结果</returns>
    Task<BenchSuiteDirectoryValidationResult> ValidateExamDirectoryStructureAsync(ExamType examType, int examId);
}
