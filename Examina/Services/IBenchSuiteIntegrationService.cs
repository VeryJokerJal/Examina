using Examina.Models;
using BenchSuite.Models;

namespace Examina.Services;

/// <summary>
/// BenchSuite评分系统集成服务接口
/// </summary>
public interface IBenchSuiteIntegrationService
{
    /// <summary>
    /// 对考试文件进行评分
    /// </summary>
    /// <param name="examType">考试类型</param>
    /// <param name="examId">考试ID</param>
    /// <param name="studentUserId">学生用户ID</param>
    /// <param name="filePaths">文件路径字典（按模块类型分组）</param>
    /// <returns>评分结果字典（按模块类型分组）</returns>
    Task<Dictionary<ModuleType, ScoringResult>> ScoreExamAsync(ExamType examType, int examId, int studentUserId, Dictionary<ModuleType, List<string>> filePaths);

    /// <summary>
    /// 检查BenchSuite服务是否可用
    /// </summary>
    /// <returns>服务是否可用</returns>
    Task<bool> IsServiceAvailableAsync();

    /// <summary>
    /// 获取支持的模块类型
    /// </summary>
    /// <returns>支持的模块类型列表</returns>
    IEnumerable<ModuleType> GetSupportedModuleTypes();

    /// <summary>
    /// 验证文件目录结构
    /// </summary>
    /// <param name="basePath">基础路径</param>
    /// <returns>验证是否成功</returns>
    Task<bool> ValidateDirectoryStructureAsync(string basePath);

    /// <summary>
    /// 验证考试目录结构
    /// </summary>
    /// <param name="examType">考试类型</param>
    /// <param name="examId">考试ID</param>
    /// <returns>验证是否成功</returns>
    Task<bool> ValidateExamDirectoryStructureAsync(ExamType examType, int examId);
}
