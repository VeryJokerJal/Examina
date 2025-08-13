using System.Collections.Generic;

namespace BenchSuite.Models;

/// <summary>
/// 打分结果模型
/// </summary>
public class ScoringResult
{
    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 获得分数
    /// </summary>
    public decimal AchievedScore { get; set; }

    /// <summary>
    /// 得分率
    /// </summary>
    public decimal ScoreRate => TotalScore > 0 ? AchievedScore / TotalScore : 0;

    /// <summary>
    /// 知识点检测结果列表
    /// </summary>
    public List<KnowledgePointResult> KnowledgePointResults { get; set; } = new();

    /// <summary>
    /// 每道题目的评分结果（最终返回以题目为单位）
    /// </summary>
    public List<QuestionScoreResult> QuestionResults { get; set; } = new();


    /// <summary>
    /// 检测是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 检测开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 检测结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 检测耗时（毫秒）
    /// </summary>
    public long ElapsedMilliseconds => (long)(EndTime - StartTime).TotalMilliseconds;
}

/// <summary>
/// 知识点检测结果
/// </summary>
public class KnowledgePointResult
{
    /// <summary>
    /// 知识点ID
    /// </summary>
    public string KnowledgePointId { get; set; } = string.Empty;

    /// <summary>
    /// 知识点名称
    /// </summary>
    public string KnowledgePointName { get; set; } = string.Empty;

    /// <summary>
    /// 关联的题目ID
    /// </summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>
    /// 知识点类型
    /// </summary>
    public string KnowledgePointType { get; set; } = string.Empty;

    /// <summary>
    /// 知识点分类
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 该知识点的总分
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 该知识点的获得分数
    /// </summary>
    public decimal AchievedScore { get; set; }

    /// <summary>
    /// 是否答案正确
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// 检测详情
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 期望值
    /// </summary>
    public string? ExpectedValue { get; set; }

    /// <summary>
    /// 实际值
    /// </summary>
    public string? ActualValue { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 配置参数
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// 打分配置
/// </summary>
public class ScoringConfiguration
{
    /// <summary>
    /// 是否启用部分分数
    /// </summary>
    public bool EnablePartialScoring { get; set; } = true;

    /// <summary>
    /// 错误容忍度（0-1之间）
    /// </summary>
    public decimal ErrorTolerance { get; set; } = 0.1m;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 是否详细记录检测过程
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}

/// <summary>
/// 题目评分结果
/// </summary>
public class QuestionScoreResult
{
    /// <summary>
    /// 题目ID
    /// </summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>
    /// 题目标题
    /// </summary>
    public string QuestionTitle { get; set; } = string.Empty;

    /// <summary>
    /// 题目总分
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 题目获得分数（全对得分，否则0）
    /// </summary>
    public decimal AchievedScore { get; set; }

    /// <summary>
    /// 题内是否全部操作点正确
    /// </summary>
    public bool IsCorrect { get; set; }
}
