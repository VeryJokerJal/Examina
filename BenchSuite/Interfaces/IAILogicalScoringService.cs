namespace BenchSuite.Interfaces;

/// <summary>
/// AI逻辑性判分服务接口
/// </summary>
public interface IAILogicalScoringService
{
    /// <summary>
    /// 使用AI进行逻辑性判分，返回结构化的JSON格式结果
    /// </summary>
    /// <param name="sourceCode">学生提交的源代码</param>
    /// <param name="problemDescription">题目描述</param>
    /// <param name="expectedOutput">期望输出（可选）</param>
    /// <param name="testCases">测试用例（可选）</param>
    /// <returns>AI逻辑性判分结果</returns>
    Task<AILogicalScoringResult> ScoreLogicalReasoningAsync(
        string sourceCode,
        string problemDescription,
        string? expectedOutput = null,
        List<string>? testCases = null);

    /// <summary>
    /// 使用AI分析代码的逻辑错误
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="problemDescription">题目描述</param>
    /// <returns>逻辑错误分析结果</returns>
    Task<AILogicalAnalysisResult> AnalyzeLogicalErrorsAsync(
        string sourceCode,
        string problemDescription);

    /// <summary>
    /// 验证AI服务连接状态
    /// </summary>
    /// <returns>服务是否可用</returns>
    Task<bool> IsServiceAvailableAsync();

    /// <summary>
    /// 获取AI服务配置信息
    /// </summary>
    /// <returns>配置信息</returns>
    AIServiceConfiguration GetConfiguration();
}

/// <summary>
/// AI逻辑性判分结果
/// </summary>
public class AILogicalScoringResult
{
    /// <summary>
    /// 判分是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 推理步骤列表
    /// </summary>
    public List<ReasoningStep> Steps { get; set; } = [];

    /// <summary>
    /// 最终答案
    /// </summary>
    public string FinalAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 逻辑性评分（0-100）
    /// </summary>
    public double LogicalScore { get; set; }

    /// <summary>
    /// 详细评价
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// AI响应的原始JSON
    /// </summary>
    public string RawJsonResponse { get; set; } = string.Empty;

    /// <summary>
    /// 处理耗时（毫秒）
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}

/// <summary>
/// 推理步骤
/// </summary>
public class ReasoningStep
{
    /// <summary>
    /// 步骤说明
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// 步骤输出
    /// </summary>
    public string Output { get; set; } = string.Empty;
}

/// <summary>
/// AI逻辑错误分析结果
/// </summary>
public class AILogicalAnalysisResult
{
    /// <summary>
    /// 分析是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 检测到的逻辑错误列表
    /// </summary>
    public List<AIDetectedLogicalError> LogicalErrors { get; set; } = [];

    /// <summary>
    /// 代码质量评估
    /// </summary>
    public CodeQualityAssessment QualityAssessment { get; set; } = new();

    /// <summary>
    /// 改进建议
    /// </summary>
    public List<string> ImprovementSuggestions { get; set; } = [];
}

/// <summary>
/// AI检测到的逻辑错误
/// </summary>
public class AIDetectedLogicalError
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
    /// 严重程度
    /// </summary>
    public LogicalErrorSeverity Severity { get; set; }

    /// <summary>
    /// 错误位置（行号）
    /// </summary>
    public int? LineNumber { get; set; }

    /// <summary>
    /// 修复建议
    /// </summary>
    public string? FixSuggestion { get; set; }
}

/// <summary>
/// 代码质量评估
/// </summary>
public class CodeQualityAssessment
{
    /// <summary>
    /// 逻辑清晰度评分（0-100）
    /// </summary>
    public double LogicalClarity { get; set; }

    /// <summary>
    /// 算法效率评分（0-100）
    /// </summary>
    public double AlgorithmEfficiency { get; set; }

    /// <summary>
    /// 代码结构评分（0-100）
    /// </summary>
    public double CodeStructure { get; set; }

    /// <summary>
    /// 错误处理评分（0-100）
    /// </summary>
    public double ErrorHandling { get; set; }

    /// <summary>
    /// 总体评分（0-100）
    /// </summary>
    public double OverallScore { get; set; }
}

/// <summary>
/// 逻辑错误严重程度
/// </summary>
public enum LogicalErrorSeverity
{
    /// <summary>
    /// 轻微
    /// </summary>
    Minor,

    /// <summary>
    /// 中等
    /// </summary>
    Moderate,

    /// <summary>
    /// 严重
    /// </summary>
    Severe,

    /// <summary>
    /// 致命
    /// </summary>
    Critical
}

/// <summary>
/// AI服务配置
/// </summary>
public class AIServiceConfiguration
{
    /// <summary>
    /// API密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API端点地址
    /// </summary>
    public string ApiEndpoint { get; set; } = "https://api.gptnb.ai/v1/chat/completions";

    /// <summary>
    /// 使用的模型名称
    /// </summary>
    public string ModelName { get; set; } = "gpt-5-2025-08-07";

    /// <summary>
    /// 最大令牌数
    /// </summary>
    public int MaxTokens { get; set; } = 2000;

    /// <summary>
    /// 温度参数
    /// </summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 是否启用结构化输出
    /// </summary>
    public bool EnableStructuredOutput { get; set; } = true;

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    /// <returns>验证结果</returns>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(ApiKey) &&
               !string.IsNullOrEmpty(ApiEndpoint) &&
               !string.IsNullOrEmpty(ModelName) &&
               MaxTokens > 0 &&
               TimeoutSeconds > 0 &&
               Temperature >= 0;
    }

    /// <summary>
    /// 验证自定义端点的有效性
    /// </summary>
    /// <returns>端点是否有效</returns>
    public bool IsValidEndpoint()
    {
        return Uri.TryCreate(ApiEndpoint, UriKind.Absolute, out Uri? uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
