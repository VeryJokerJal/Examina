using BenchSuite.Models;

namespace BenchSuite.Interfaces;

/// <summary>
/// 打分服务接口
/// </summary>
public interface IScoringService
{
    /// <summary>
    /// 对指定文件进行打分
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="examModel">试卷模型</param>
    /// <param name="configuration">打分配置</param>
    /// <returns>打分结果</returns>
    Task<ScoringResult> ScoreFileAsync(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null);

    /// <summary>
    /// 对指定文件进行打分（同步版本）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="examModel">试卷模型</param>
    /// <param name="configuration">打分配置</param>
    /// <returns>打分结果</returns>
    ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null);

    /// <summary>
    /// 验证文件是否可以被处理
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否可以处理</returns>
    bool CanProcessFile(string filePath);

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    /// <returns>支持的文件扩展名列表</returns>
    IEnumerable<string> GetSupportedExtensions();
}

/// <summary>
/// PPT打分服务接口
/// </summary>
public interface IPowerPointScoringService : IScoringService
{
    /// <summary>
    /// 检测PPT中的特定知识点
    /// </summary>
    /// <param name="filePath">PPT文件路径</param>
    /// <param name="knowledgePointType">知识点类型</param>
    /// <param name="parameters">检测参数</param>
    /// <returns>知识点检测结果</returns>
    Task<KnowledgePointResult> DetectKnowledgePointAsync(string filePath, string knowledgePointType, Dictionary<string, string> parameters);

    /// <summary>
    /// 批量检测PPT中的知识点
    /// </summary>
    /// <param name="filePath">PPT文件路径</param>
    /// <param name="knowledgePoints">要检测的知识点列表</param>
    /// <returns>知识点检测结果列表</returns>
    Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(string filePath, List<OperationPointModel> knowledgePoints);
}

/// <summary>
/// Windows打分服务接口
/// </summary>
public interface IWindowsScoringService : IScoringService
{
    /// <summary>
    /// 检测Windows文件系统中的特定操作
    /// </summary>
    /// <param name="basePath">检测的基础路径</param>
    /// <param name="operationType">操作类型</param>
    /// <param name="parameters">检测参数</param>
    /// <returns>操作检测结果</returns>
    Task<KnowledgePointResult> DetectWindowsOperationAsync(string basePath, string operationType, Dictionary<string, string> parameters);

    /// <summary>
    /// 批量检测Windows文件系统操作
    /// </summary>
    /// <param name="basePath">检测的基础路径</param>
    /// <param name="operationPoints">要检测的操作点列表</param>
    /// <returns>操作检测结果列表</returns>
    Task<List<KnowledgePointResult>> DetectWindowsOperationsAsync(string basePath, List<OperationPointModel> operationPoints);

    /// <summary>
    /// 验证文件或文件夹是否存在
    /// </summary>
    /// <param name="path">文件或文件夹路径</param>
    /// <returns>是否存在</returns>
    bool PathExists(string path);

    /// <summary>
    /// 获取文件或文件夹的属性信息
    /// </summary>
    /// <param name="path">文件或文件夹路径</param>
    /// <returns>属性信息字典</returns>
    Dictionary<string, object> GetPathAttributes(string path);
}
