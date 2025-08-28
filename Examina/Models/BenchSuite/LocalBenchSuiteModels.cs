using Examina.Models;
using BenchSuite.Models;

namespace Examina.Models.BenchSuite;

/// <summary>
/// BenchSuite文件类型枚举（本地定义）
/// </summary>
public enum BenchSuiteFileType
{
    /// <summary>
    /// Word文档
    /// </summary>
    Word,

    /// <summary>
    /// Excel表格
    /// </summary>
    Excel,

    /// <summary>
    /// PowerPoint演示文稿
    /// </summary>
    PowerPoint,

    /// <summary>
    /// C#代码文件
    /// </summary>
    CSharp,

    /// <summary>
    /// Windows系统操作
    /// </summary>
    Windows,

    /// <summary>
    /// 其他文件类型
    /// </summary>
    Other
}

/// <summary>
/// BenchSuite评分请求（本地定义）
/// </summary>
public class BenchSuiteScoringRequest
{
    /// <summary>
    /// 考试ID
    /// </summary>
    public int ExamId { get; set; }

    /// <summary>
    /// 考试类型
    /// </summary>
    public ExamType ExamType { get; set; }

    /// <summary>
    /// 学生用户ID
    /// </summary>
    public int StudentUserId { get; set; }

    /// <summary>
    /// 基础路径
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径字典（按文件类型分组）
    /// </summary>
    public Dictionary<BenchSuiteFileType, List<string>> FilePaths { get; set; } = new();
}

/// <summary>
/// BenchSuite评分结果（本地定义）
/// </summary>
public class BenchSuiteScoringResult
{
    /// <summary>
    /// 评分是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    public double TotalScore { get; set; }

    /// <summary>
    /// 获得分数
    /// </summary>
    public double AchievedScore { get; set; }

    /// <summary>
    /// 得分率
    /// </summary>
    public double ScoreRate => TotalScore > 0 ? AchievedScore / TotalScore : 0;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 知识点检测结果列表
    /// </summary>
    public List<KnowledgePointResult> KnowledgePointResults { get; set; } = new();

    /// <summary>
    /// 详细信息
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 评分结果按模块类型分组（用于兼容现有代码）
    /// </summary>
    public Dictionary<ModuleType, ScoringResult> ModuleResults { get; set; } = new();
}

/// <summary>
/// BenchSuite评分结果扩展方法
/// </summary>
public static class BenchSuiteScoringResultExtensions
{
    /// <summary>
    /// 将BenchSuiteScoringResult转换为按模块类型分组的ScoringResult字典
    /// </summary>
    /// <param name="benchSuiteResult">BenchSuite评分结果</param>
    /// <returns>按模块类型分组的评分结果字典</returns>
    public static Dictionary<ModuleType, ScoringResult> ToModuleResults(this BenchSuiteScoringResult benchSuiteResult)
    {
        if (benchSuiteResult.ModuleResults.Count > 0)
        {
            return benchSuiteResult.ModuleResults;
        }

        // 如果没有预设的模块结果，根据知识点结果创建
        Dictionary<ModuleType, ScoringResult> moduleResults = new();

        // 按知识点类型分组
        var groupedResults = benchSuiteResult.KnowledgePointResults
            .GroupBy(kp => GetModuleTypeFromKnowledgePoint(kp))
            .ToList();

        foreach (var group in groupedResults)
        {
            ModuleType moduleType = group.Key;
            List<KnowledgePointResult> knowledgePoints = [.. group];

            ScoringResult scoringResult = new()
            {
                IsSuccess = benchSuiteResult.IsSuccess,
                ErrorMessage = benchSuiteResult.ErrorMessage,
                StartTime = benchSuiteResult.StartTime,
                EndTime = benchSuiteResult.EndTime,
                TotalScore = knowledgePoints.Sum(kp => kp.TotalScore),
                AchievedScore = knowledgePoints.Sum(kp => kp.AchievedScore),
                KnowledgePointResults = knowledgePoints
            };

            moduleResults[moduleType] = scoringResult;
        }

        // 如果没有知识点结果，创建一个默认的综合结果
        if (moduleResults.Count == 0)
        {
            moduleResults[ModuleType.Windows] = new ScoringResult
            {
                IsSuccess = benchSuiteResult.IsSuccess,
                ErrorMessage = benchSuiteResult.ErrorMessage,
                StartTime = benchSuiteResult.StartTime,
                EndTime = benchSuiteResult.EndTime,
                TotalScore = benchSuiteResult.TotalScore,
                AchievedScore = benchSuiteResult.AchievedScore,
                KnowledgePointResults = benchSuiteResult.KnowledgePointResults
            };
        }

        return moduleResults;
    }

    /// <summary>
    /// 根据知识点结果推断模块类型
    /// </summary>
    /// <param name="knowledgePoint">知识点结果</param>
    /// <returns>模块类型</returns>
    private static ModuleType GetModuleTypeFromKnowledgePoint(KnowledgePointResult knowledgePoint)
    {
        string type = knowledgePoint.KnowledgePointType?.ToLowerInvariant() ?? "";
        string name = knowledgePoint.KnowledgePointName?.ToLowerInvariant() ?? "";

        // 根据知识点类型和名称推断模块类型
        if (type.Contains("word") || name.Contains("word") || type.Contains("文档"))
            return ModuleType.Word;
        
        if (type.Contains("excel") || name.Contains("excel") || type.Contains("表格"))
            return ModuleType.Excel;
        
        if (type.Contains("powerpoint") || type.Contains("ppt") || name.Contains("powerpoint") || type.Contains("演示"))
            return ModuleType.PowerPoint;
        
        if (type.Contains("csharp") || type.Contains("c#") || name.Contains("csharp") || type.Contains("代码"))
            return ModuleType.CSharp;
        
        if (type.Contains("windows") || name.Contains("windows") || type.Contains("系统"))
            return ModuleType.Windows;

        // 默认返回Windows模块
        return ModuleType.Windows;
    }
}
