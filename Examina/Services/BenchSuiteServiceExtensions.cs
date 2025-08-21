using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBenchSuiteServices(this IServiceCollection services)
    {
        // 注册BenchSuite集成服务
        _ = services.AddSingleton<IBenchSuiteIntegrationService, BenchSuiteIntegrationService>();

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
                Models.BenchSuite.BenchSuiteDirectoryValidationResult result = await directoryService.EnsureDirectoryStructureAsync();
                if (result.IsValid)
                {
                    logger?.LogInformation("BenchSuite目录结构初始化成功");
                }
                else
                {
                    logger?.LogWarning("BenchSuite目录结构初始化失败: {ErrorMessage}", result.ErrorMessage);
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
