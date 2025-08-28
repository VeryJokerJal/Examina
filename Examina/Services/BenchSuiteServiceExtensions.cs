using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BenchSuite.Interfaces;
using BenchSuite.Services;
using Examina.Configuration;

namespace Examina.Services;

/// <summary>
/// BenchSuite服务扩展方法
/// </summary>
public static class BenchSuiteServiceExtensions
{
    /// <summary>
    /// 注册BenchSuite相关服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="enableAI">是否启用AI功能</param>
    /// <param name="aiServiceType">AI服务类型</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBenchSuiteServices(this IServiceCollection services, bool enableAI = false, AIServiceType aiServiceType = AIServiceType.Default)
    {
        // 注册AI逻辑性判分服务（如果启用）
        if (enableAI)
        {
            services.AddSingleton<IAILogicalScoringService>(provider =>
            {
                string? apiKey = ExaminaAIConfiguration.GetApiKeyFromEnvironment();
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("未找到AI API密钥，请设置环境变量OPENAI_API_KEY或检查配置文件");
                }

                AIServiceConfiguration config = aiServiceType switch
                {
                    AIServiceType.ComprehensiveTraining => ExaminaAIConfiguration.CreateComprehensiveTrainingConfiguration(apiKey),
                    AIServiceType.SpecializedTraining => ExaminaAIConfiguration.CreateSpecializedTrainingConfiguration(apiKey),
                    _ => ExaminaAIConfiguration.CreateDefaultConfiguration(apiKey)
                };

                return new AILogicalScoringService(config);
            });
        }

        // 注册BenchSuite集成服务
        services.AddSingleton<IBenchSuiteIntegrationService>(provider =>
        {
            ILogger<BenchSuiteIntegrationService> logger = provider.GetRequiredService<ILogger<BenchSuiteIntegrationService>>();
            IAILogicalScoringService? aiService = enableAI ? provider.GetService<IAILogicalScoringService>() : null;
            return new BenchSuiteIntegrationService(logger, aiService);
        });

        // 注册BenchSuite目录服务
        _ = services.AddSingleton<IBenchSuiteDirectoryService, BenchSuiteDirectoryService>();

        // 注册增强的考试工具栏服务
        _ = services.AddTransient<EnhancedExamToolbarService>();

        return services;
    }

    /// <summary>
    /// 初始化BenchSuite服务
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    /// <returns>初始化任务</returns>
    public static async Task InitializeBenchSuiteServicesAsync(this IServiceProvider serviceProvider)
    {
        try
        {
            ILoggerFactory? loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            ILogger? logger = loggerFactory?.CreateLogger("BenchSuiteServiceExtensions");
            logger?.LogInformation("开始初始化BenchSuite服务");

            // 获取目录服务并确保目录结构存在
            IBenchSuiteDirectoryService? directoryService = serviceProvider.GetService<IBenchSuiteDirectoryService>();
            if (directoryService != null)
            {
                bool result = await directoryService.EnsureDirectoryStructureAsync();
                if (result)
                {
                    logger?.LogInformation("BenchSuite目录结构初始化成功");
                }
                else
                {
                    logger?.LogWarning("BenchSuite目录结构初始化失败");
                }
            }

            // 检查BenchSuite集成服务是否可用
            IBenchSuiteIntegrationService? integrationService = serviceProvider.GetService<IBenchSuiteIntegrationService>();
            if (integrationService != null)
            {
                bool serviceAvailable = await integrationService.IsServiceAvailableAsync();
                if (serviceAvailable)
                {
                    logger?.LogInformation("BenchSuite集成服务可用");
                }
                else
                {
                    logger?.LogWarning("BenchSuite集成服务不可用");
                }
            }

            logger?.LogInformation("BenchSuite服务初始化完成");
        }
        catch (Exception ex)
        {
            ILoggerFactory? loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            ILogger? logger = loggerFactory?.CreateLogger("BenchSuiteServiceExtensions");
            logger?.LogError(ex, "BenchSuite服务初始化失败");
        }
    }
}
