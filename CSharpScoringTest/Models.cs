namespace CSharpScoringTest;

/// <summary>
/// C#代码编译结果
/// </summary>
public class CompilationResult
{
    /// <summary>
    /// 编译是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 编译错误信息
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 编译警告信息
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 生成的可执行文件路径
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// 编译耗时（毫秒）
    /// </summary>
    public long CompilationTimeMs { get; set; }
}

/// <summary>
/// C#代码执行结果
/// </summary>
public class ExecutionResult
{
    /// <summary>
    /// 执行是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 程序输出
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// 错误输出
    /// </summary>
    public string ErrorOutput { get; set; } = string.Empty;

    /// <summary>
    /// 退出代码
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// 是否超时
    /// </summary>
    public bool IsTimeout { get; set; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public string? ExceptionMessage { get; set; }
}

/// <summary>
/// AI代码质量评分结果
/// </summary>
public class AIScoringResult
{
    /// <summary>
    /// 总分（0-30分）
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 逻辑性得分（0-10分）
    /// </summary>
    public decimal LogicScore { get; set; }

    /// <summary>
    /// 冗余检测得分（0-10分）
    /// </summary>
    public decimal RedundancyScore { get; set; }

    /// <summary>
    /// 结构得分（0-5分）
    /// </summary>
    public decimal StructureScore { get; set; }

    /// <summary>
    /// 效率得分（0-5分）
    /// </summary>
    public decimal EfficiencyScore { get; set; }

    /// <summary>
    /// 发现的问题列表
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// 改进建议列表
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// 详细反馈
    /// </summary>
    public string DetailedFeedback { get; set; } = string.Empty;

    /// <summary>
    /// AI评分是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// AI评分错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// C#代码评分结果
/// </summary>
public class CSharpScoringResult
{
    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    public decimal TotalScore { get; set; }

    /// <summary>
    /// 获得分数
    /// </summary>
    public decimal AchievedScore { get; set; }

    /// <summary>
    /// 检测是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 编译结果
    /// </summary>
    public CompilationResult CompilationResult { get; set; } = new();

    /// <summary>
    /// 执行结果
    /// </summary>
    public ExecutionResult ExecutionResult { get; set; } = new();

    /// <summary>
    /// 输出是否匹配期望
    /// </summary>
    public bool OutputMatches { get; set; }

    /// <summary>
    /// AI评分结果
    /// </summary>
    public AIScoringResult AIScoringResult { get; set; } = new();

    /// <summary>
    /// 最终得分
    /// </summary>
    public decimal FinalScore { get; set; }

    /// <summary>
    /// 评分阶段（编译/执行/AI评分）
    /// </summary>
    public string ScoringStage { get; set; } = string.Empty;
}

/// <summary>
/// C#代码评分配置
/// </summary>
public class CSharpScoringConfiguration
{
    /// <summary>
    /// 代码执行超时时间（秒）
    /// </summary>
    public int ExecutionTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// OpenAI API密钥
    /// </summary>
    public string? OpenAIApiKey { get; set; }

    /// <summary>
    /// OpenAI模型名称
    /// </summary>
    public string OpenAIModel { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// AI评分的最大分数
    /// </summary>
    public decimal MaxAIScore { get; set; } = 30.0m;

    /// <summary>
    /// 是否启用AI评分
    /// </summary>
    public bool EnableAIScoring { get; set; } = true;

    /// <summary>
    /// 输出比较是否忽略大小写
    /// </summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// 输出比较是否忽略空白字符
    /// </summary>
    public bool IgnoreWhitespace { get; set; } = true;
}

/// <summary>
/// AI评分响应模型
/// </summary>
internal class AIScoringResponse
{
    public decimal Score { get; set; }
    public decimal LogicScore { get; set; }
    public decimal RedundancyScore { get; set; }
    public decimal StructureScore { get; set; }
    public decimal EfficiencyScore { get; set; }
    public List<string>? Issues { get; set; }
    public List<string>? Suggestions { get; set; }
    public string? DetailedFeedback { get; set; }
}
