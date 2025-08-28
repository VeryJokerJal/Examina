using BenchSuite.Interfaces;

namespace Examina.Configuration;

/// <summary>
/// Examina项目中的AI服务配置管理
/// </summary>
public static class ExaminaAIConfiguration
{
    /// <summary>
    /// 创建默认的AI服务配置
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <param name="customEndpoint">自定义API端点（可选）</param>
    /// <returns>AI服务配置</returns>
    public static AIServiceConfiguration CreateDefaultConfiguration(string apiKey, string? customEndpoint = null)
    {
        return new AIServiceConfiguration
        {
            ApiKey = apiKey,
            ApiEndpoint = customEndpoint ?? "https://api.gptnb.ai/v1/chat/completions",
            ModelName = "gpt-5-2025-08-07",
            MaxTokens = 2000,
            Temperature = 0.1,
            TimeoutSeconds = 30,
            EnableStructuredOutput = true
        };
    }

    /// <summary>
    /// 创建用于综合实训的AI配置
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <param name="customEndpoint">自定义API端点（可选）</param>
    /// <returns>综合实训AI配置</returns>
    public static AIServiceConfiguration CreateComprehensiveTrainingConfiguration(string apiKey, string? customEndpoint = null)
    {
        return new AIServiceConfiguration
        {
            ApiKey = apiKey,
            ApiEndpoint = customEndpoint ?? "https://api.gptnb.ai/v1/chat/completions",
            ModelName = "gpt-5-2025-08-07",
            MaxTokens = 3000, // 更多令牌用于详细分析
            Temperature = 0.05, // 更低温度确保一致性
            TimeoutSeconds = 45,
            EnableStructuredOutput = true
        };
    }

    /// <summary>
    /// 创建用于专项训练的AI配置
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <param name="customEndpoint">自定义API端点（可选）</param>
    /// <returns>专项训练AI配置</returns>
    public static AIServiceConfiguration CreateSpecializedTrainingConfiguration(string apiKey, string? customEndpoint = null)
    {
        return new AIServiceConfiguration
        {
            ApiKey = apiKey,
            ApiEndpoint = customEndpoint ?? "https://api.gptnb.ai/v1/chat/completions",
            ModelName = "gpt-5-2025-08-07",
            MaxTokens = 2500,
            Temperature = 0.08,
            TimeoutSeconds = 40,
            EnableStructuredOutput = true
        };
    }

    /// <summary>
    /// 从环境变量或配置文件获取API密钥
    /// </summary>
    /// <returns>API密钥，如果未找到则返回null</returns>
    public static string? GetApiKeyFromEnvironment()
    {
        // 优先从环境变量获取
        string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (!string.IsNullOrEmpty(apiKey))
        {
            return apiKey;
        }

        // 从配置文件读取API密钥
        apiKey = GetApiKeyFromConfiguration();
        return !string.IsNullOrEmpty(apiKey) ? apiKey : null;
    }

    /// <summary>
    /// 从配置文件获取API密钥
    /// </summary>
    /// <returns>API密钥，如果未找到则返回null</returns>
    public static string? GetApiKeyFromConfiguration()
    {
        // 返回配置的API密钥
        return "sk-iN0MTBnLBP6ImiJ89530E332022142279b32A44729136484";
    }

    /// <summary>
    /// 验证AI配置是否有效
    /// </summary>
    /// <param name="configuration">AI配置</param>
    /// <returns>验证结果</returns>
    public static AIConfigurationValidationResult ValidateConfiguration(AIServiceConfiguration configuration)
    {
        AIConfigurationValidationResult result = new();

        if (!configuration.IsValid())
        {
            result.IsValid = false;
            result.ErrorMessage = "AI服务配置无效：请检查API密钥、端点地址、模型名称等基本配置";
            return result;
        }

        if (!configuration.IsValidEndpoint())
        {
            result.IsValid = false;
            result.ErrorMessage = "API端点地址格式无效";
            return result;
        }

        result.IsValid = true;
        result.Message = "AI服务配置验证通过";
        return result;
    }
}

/// <summary>
/// AI配置验证结果
/// </summary>
public class AIConfigurationValidationResult
{
    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 成功信息
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// AI服务类型枚举
/// </summary>
public enum AIServiceType
{
    /// <summary>
    /// 默认服务
    /// </summary>
    Default,

    /// <summary>
    /// 综合实训
    /// </summary>
    ComprehensiveTraining,

    /// <summary>
    /// 专项训练
    /// </summary>
    SpecializedTraining
}
