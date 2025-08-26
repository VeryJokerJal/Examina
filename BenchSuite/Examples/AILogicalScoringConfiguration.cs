using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services;

namespace BenchSuite.Examples;

/// <summary>
/// AI逻辑性判分配置示例
/// </summary>
public static class AILogicalScoringConfiguration
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
            ModelName = "gpt-5-2025-08-07", // 使用最新模型
            MaxTokens = 2000,
            Temperature = 0.1m, // 低温度确保一致性
            TimeoutSeconds = 30,
            EnableStructuredOutput = true // 启用结构化输出
        };
    }

    /// <summary>
    /// 创建高精度的AI服务配置
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <param name="customEndpoint">自定义API端点（可选）</param>
    /// <returns>高精度AI服务配置</returns>
    public static AIServiceConfiguration CreateHighPrecisionConfiguration(string apiKey, string? customEndpoint = null)
    {
        return new AIServiceConfiguration
        {
            ApiKey = apiKey,
            ApiEndpoint = customEndpoint ?? "https://api.gptnb.ai/v1/chat/completions",
            ModelName = "gpt-5-2025-08-07", // 使用最新模型
            MaxTokens = 3000,
            Temperature = 0.05m, // 更低的温度
            TimeoutSeconds = 60, // 更长的超时时间
            EnableStructuredOutput = true
        };
    }

    /// <summary>
    /// 创建快速响应的AI服务配置
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <param name="customEndpoint">自定义API端点（可选）</param>
    /// <returns>快速响应AI服务配置</returns>
    public static AIServiceConfiguration CreateFastResponseConfiguration(string apiKey, string? customEndpoint = null)
    {
        return new AIServiceConfiguration
        {
            ApiKey = apiKey,
            ApiEndpoint = customEndpoint ?? "https://api.gptnb.ai/v1/chat/completions",
            ModelName = "gpt-5-2025-08-07", // 使用最新模型
            MaxTokens = 1500,
            Temperature = 0.2m,
            TimeoutSeconds = 15,
            EnableStructuredOutput = true
        };
    }

    /// <summary>
    /// 创建带AI功能的C#评分服务
    /// </summary>
    /// <param name="apiKey">OpenAI API密钥</param>
    /// <param name="configurationType">配置类型</param>
    /// <returns>C#评分服务</returns>
    public static CSharpScoringService CreateCSharpScoringServiceWithAI(
        string apiKey, 
        AIConfigurationType configurationType = AIConfigurationType.Default)
    {
        AIServiceConfiguration config = configurationType switch
        {
            AIConfigurationType.HighPrecision => CreateHighPrecisionConfiguration(apiKey),
            AIConfigurationType.FastResponse => CreateFastResponseConfiguration(apiKey),
            _ => CreateDefaultConfiguration(apiKey)
        };

        IAILogicalScoringService aiService = new AILogicalScoringService(config);
        return new CSharpScoringService(aiService);
    }

    /// <summary>
    /// 创建不带AI功能的C#评分服务（传统模式）
    /// </summary>
    /// <returns>传统C#评分服务</returns>
    public static CSharpScoringService CreateTraditionalCSharpScoringService()
    {
        return new CSharpScoringService(); // 不传入AI服务
    }

    /// <summary>
    /// 验证AI服务配置
    /// </summary>
    /// <param name="configuration">AI服务配置</param>
    /// <returns>验证结果</returns>
    public static async Task<AIConfigurationValidationResult> ValidateConfigurationAsync(AIServiceConfiguration configuration)
    {
        AIConfigurationValidationResult result = new();

        try
        {
            // 基本配置验证
            if (string.IsNullOrEmpty(configuration.ApiKey))
            {
                result.IsValid = false;
                result.ErrorMessage = "API密钥不能为空";
                return result;
            }

            if (configuration.MaxTokens <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "最大令牌数必须大于0";
                return result;
            }

            if (configuration.TimeoutSeconds <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "超时时间必须大于0";
                return result;
            }

            // 服务连接验证
            IAILogicalScoringService aiService = new AILogicalScoringService(configuration);
            bool isAvailable = await aiService.IsServiceAvailableAsync();

            if (!isAvailable)
            {
                result.IsValid = false;
                result.ErrorMessage = "无法连接到AI服务，请检查API密钥和网络连接";
                return result;
            }

            result.IsValid = true;
            result.Message = "配置验证成功，AI服务可用";
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"配置验证失败: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 获取推荐的配置设置
    /// </summary>
    /// <param name="scenario">使用场景</param>
    /// <returns>推荐配置</returns>
    public static AIConfigurationRecommendation GetRecommendedConfiguration(UsageScenario scenario)
    {
        return scenario switch
        {
            UsageScenario.Production => new AIConfigurationRecommendation
            {
                ConfigurationType = AIConfigurationType.Default,
                ModelName = "gpt-4o-mini",
                MaxTokens = 2000,
                Temperature = 0.1m,
                TimeoutSeconds = 30,
                Reason = "生产环境推荐使用稳定可靠的配置，平衡成本和性能"
            },
            UsageScenario.Development => new AIConfigurationRecommendation
            {
                ConfigurationType = AIConfigurationType.FastResponse,
                ModelName = "gpt-3.5-turbo",
                MaxTokens = 1500,
                Temperature = 0.2m,
                TimeoutSeconds = 15,
                Reason = "开发环境推荐使用快速响应配置，提高开发效率"
            },
            UsageScenario.HighAccuracy => new AIConfigurationRecommendation
            {
                ConfigurationType = AIConfigurationType.HighPrecision,
                ModelName = "gpt-4",
                MaxTokens = 3000,
                Temperature = 0.05m,
                TimeoutSeconds = 60,
                Reason = "高精度场景推荐使用最强模型，确保判分准确性"
            },
            _ => new AIConfigurationRecommendation
            {
                ConfigurationType = AIConfigurationType.Default,
                ModelName = "gpt-4o-mini",
                MaxTokens = 2000,
                Temperature = 0.1m,
                TimeoutSeconds = 30,
                Reason = "默认推荐配置"
            }
        };
    }
}

/// <summary>
/// AI配置类型
/// </summary>
public enum AIConfigurationType
{
    /// <summary>
    /// 默认配置
    /// </summary>
    Default,

    /// <summary>
    /// 高精度配置
    /// </summary>
    HighPrecision,

    /// <summary>
    /// 快速响应配置
    /// </summary>
    FastResponse
}

/// <summary>
/// 使用场景
/// </summary>
public enum UsageScenario
{
    /// <summary>
    /// 生产环境
    /// </summary>
    Production,

    /// <summary>
    /// 开发环境
    /// </summary>
    Development,

    /// <summary>
    /// 高精度要求
    /// </summary>
    HighAccuracy
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
/// AI配置推荐
/// </summary>
public class AIConfigurationRecommendation
{
    /// <summary>
    /// 配置类型
    /// </summary>
    public AIConfigurationType ConfigurationType { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// 最大令牌数
    /// </summary>
    public int MaxTokens { get; set; }

    /// <summary>
    /// 温度参数
    /// </summary>
    public decimal Temperature { get; set; }

    /// <summary>
    /// 超时时间
    /// </summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// 推荐理由
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
