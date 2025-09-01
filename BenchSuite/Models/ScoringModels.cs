using BenchSuite.Interfaces;

namespace BenchSuite.Models;

/// <summary>
/// 打分结果模型
/// </summary>
public class ScoringResult
{
    /// <summary>
    /// 关联的题目ID
    /// </summary>
    public string? QuestionId { get; set; }

    /// <summary>
    /// 关联的题目标题（便于识别）
    /// </summary>
    public string? QuestionTitle { get; set; }

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
    /// 知识点检测结果列表（只包含与当前题目相关的知识点）
    /// </summary>
    public List<KnowledgePointResult> KnowledgePointResults { get; set; } = [];

    /// <summary>
    /// 检测是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public string Details { get; set; } = string.Empty;

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
    /// 关联的题目ID
    /// </summary>
    public string? QuestionId { get; set; }

    /// <summary>
    /// 关联的操作点ID（更明确的关联）
    /// </summary>
    public string? OperationPointId { get; set; }

    /// <summary>
    /// 知识点ID
    /// </summary>
    public string KnowledgePointId { get; set; } = string.Empty;

    /// <summary>
    /// 知识点名称
    /// </summary>
    public string KnowledgePointName { get; set; } = string.Empty;

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
    public double TotalScore { get; set; }

    /// <summary>
    /// 该知识点的获得分数
    /// </summary>
    public double AchievedScore { get; set; }

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
    public Dictionary<string, string> Parameters { get; set; } = [];
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
    public double ErrorTolerance { get; set; } = (double)0.1m;

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
/// C#评分模式枚举 - 与ExamLab的CSharpQuestionType保持一致
/// </summary>
public enum CSharpScoringMode
{
    /// <summary>
    /// 代码补全模式 - 基于NotImplementedException的填空
    /// </summary>
    CodeCompletion,

    /// <summary>
    /// 调试纠错模式 - 找出并修复代码中的错误
    /// </summary>
    Debugging,

    /// <summary>
    /// 编写实现模式 - 完整实现指定功能并通过测试
    /// </summary>
    Implementation
}

/// <summary>
/// C#代码评分结果
/// </summary>
public class CSharpScoringResult
{
    /// <summary>
    /// 评分模式
    /// </summary>
    public CSharpScoringMode Mode { get; set; }

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
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 填空结果（代码补全模式）
    /// </summary>
    public List<FillBlankResult> FillBlankResults { get; set; } = [];

    /// <summary>
    /// 编译结果（所有模式都可能需要）
    /// </summary>
    public CompilationResult? CompilationResult { get; set; }

    /// <summary>
    /// 单元测试结果（实现模式）
    /// </summary>
    public UnitTestResult? UnitTestResult { get; set; }

    /// <summary>
    /// 调试结果（调试纠错模式）
    /// </summary>
    public DebuggingResult? DebuggingResult { get; set; }

    /// <summary>
    /// AI逻辑性判分结果（实现模式）
    /// </summary>
    public AILogicalScoringResult? AILogicalResult { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 编译耗时（毫秒）
    /// </summary>
    public long CompilationTimeMs { get; set; }

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// 耗时（毫秒）
    /// </summary>
    public long ElapsedMilliseconds => (long)(EndTime - StartTime).TotalMilliseconds;
}

/// <summary>
/// 填空描述符
/// </summary>
public class BlankDescriptor
{
    /// <summary>
    /// 方法名称
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// 语句索引
    /// </summary>
    public int StatementIndex { get; set; } = -1;

    /// <summary>
    /// 抛出语句节点
    /// </summary>
    public object? ThrowNode { get; set; }

    /// <summary>
    /// 前一个语句
    /// </summary>
    public object? PrevStatement { get; set; }

    /// <summary>
    /// 后一个语句
    /// </summary>
    public object? NextStatement { get; set; }

    /// <summary>
    /// 位置摘要
    /// </summary>
    public string LocationSummary => $"{MethodName} - stmt#{StatementIndex}";
}

/// <summary>
/// 填空结果
/// </summary>
public class FillBlankResult
{
    /// <summary>
    /// 填空索引
    /// </summary>
    public int BlankIndex { get; set; }

    /// <summary>
    /// 填空描述符
    /// </summary>
    public BlankDescriptor Descriptor { get; set; } = new();

    /// <summary>
    /// 是否匹配
    /// </summary>
    public bool Matched { get; set; }

    /// <summary>
    /// 期望文本
    /// </summary>
    public string ExpectedText { get; set; } = string.Empty;

    /// <summary>
    /// 学生文本
    /// </summary>
    public string StudentText { get; set; } = string.Empty;

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 分数
    /// </summary>
    public double Score { get; set; }
}

/// <summary>
/// 编译结果
/// </summary>
public class CompilationResult
{
    /// <summary>
    /// 是否编译成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 编译错误列表
    /// </summary>
    public List<CompilationError> Errors { get; set; } = [];

    /// <summary>
    /// 编译警告列表
    /// </summary>
    public List<CompilationWarning> Warnings { get; set; } = [];

    /// <summary>
    /// 编译后的程序集字节
    /// </summary>
    public byte[]? AssemblyBytes { get; set; }

    /// <summary>
    /// 编译耗时（毫秒）
    /// </summary>
    public long CompilationTimeMs { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// 编译错误
/// </summary>
public class CompilationError
{
    /// <summary>
    /// 错误代码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 行号
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// 列号
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 严重程度
    /// </summary>
    public string Severity { get; set; } = string.Empty;
}

/// <summary>
/// 编译警告
/// </summary>
public class CompilationWarning
{
    /// <summary>
    /// 警告代码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 警告消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 行号
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// 列号
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}

/// <summary>
/// 单元测试结果
/// </summary>
public class UnitTestResult
{
    /// <summary>
    /// 是否所有测试通过
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 总测试数
    /// </summary>
    public int TotalTests { get; set; }

    /// <summary>
    /// 通过的测试数
    /// </summary>
    public int PassedTests { get; set; }

    /// <summary>
    /// 失败的测试数
    /// </summary>
    public int FailedTests { get; set; }

    /// <summary>
    /// 跳过的测试数
    /// </summary>
    public int SkippedTests { get; set; }

    /// <summary>
    /// 测试用例结果列表
    /// </summary>
    public List<TestCaseResult> TestCaseResults { get; set; } = [];

    /// <summary>
    /// 测试运行耗时（毫秒）
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 测试用例结果
/// </summary>
public class TestCaseResult
{
    /// <summary>
    /// 测试用例名称
    /// </summary>
    public string TestName { get; set; } = string.Empty;

    /// <summary>
    /// 是否通过
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 堆栈跟踪
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// 输出信息
    /// </summary>
    public string Output { get; set; } = string.Empty;
}

/// <summary>
/// 调试结果
/// </summary>
public class DebuggingResult
{
    /// <summary>
    /// 是否成功修复所有错误
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 原始代码中发现的错误数量
    /// </summary>
    public int TotalErrors { get; set; }

    /// <summary>
    /// 已修复的错误数量
    /// </summary>
    public int FixedErrors { get; set; }

    /// <summary>
    /// 剩余的错误数量
    /// </summary>
    public int RemainingErrors { get; set; }

    /// <summary>
    /// 错误检测结果列表
    /// </summary>
    public List<ErrorDetectionResult> ErrorDetections { get; set; } = [];

    /// <summary>
    /// 修复验证结果
    /// </summary>
    public List<FixVerificationResult> FixVerifications { get; set; } = [];

    /// <summary>
    /// 调试耗时（毫秒）
    /// </summary>
    public long DebuggingTimeMs { get; set; }

    /// <summary>
    /// 详细信息
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 错误检测结果
/// </summary>
public class ErrorDetectionResult
{
    /// <summary>
    /// 错误类型
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// 错误描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 错误位置（行号）
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// 错误位置（列号）
    /// </summary>
    public int ColumnNumber { get; set; }

    /// <summary>
    /// 错误严重程度
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// 是否已修复
    /// </summary>
    public bool IsFixed { get; set; }

    /// <summary>
    /// 修复建议
    /// </summary>
    public string? FixSuggestion { get; set; }
}

/// <summary>
/// 修复验证结果
/// </summary>
public class FixVerificationResult
{
    /// <summary>
    /// 修复的错误类型
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// 修复是否正确
    /// </summary>
    public bool IsCorrectFix { get; set; }

    /// <summary>
    /// 修复前的代码
    /// </summary>
    public string BeforeCode { get; set; } = string.Empty;

    /// <summary>
    /// 修复后的代码
    /// </summary>
    public string AfterCode { get; set; } = string.Empty;

    /// <summary>
    /// 验证消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 得分
    /// </summary>
    public double Score { get; set; }
}
