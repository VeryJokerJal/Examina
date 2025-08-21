using Examina.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Examina.Configuration;

/// <summary>
/// BenchSuite集成设置类
/// </summary>
public static class BenchSuiteIntegrationSetup
{
    /// <summary>
    /// 配置BenchSuite集成服务
    /// </summary>
    /// <returns>配置完成的服务提供者</returns>
    public static async Task<IServiceProvider> ConfigureBenchSuiteIntegrationAsync()
    {
        // 创建服务集合
        ServiceCollection services = new();
        
        // 配置日志
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // 配置Examina应用程序服务（包括BenchSuite服务）
        services.ConfigureExaminaServices();
        
        // 构建并初始化服务提供者
        IServiceProvider serviceProvider = await services.BuildAndInitializeAsync();
        
        // 初始化应用程序服务管理器
        AppServiceManager.Initialize(serviceProvider);
        
        return serviceProvider;
    }
    
    /// <summary>
    /// 验证BenchSuite集成配置
    /// </summary>
    /// <returns>验证结果</returns>
    public static async Task<BenchSuiteIntegrationValidationResult> ValidateIntegrationAsync()
    {
        BenchSuiteIntegrationValidationResult result = new();
        
        try
        {
            // 检查服务管理器是否已初始化
            result.ServiceManagerInitialized = AppServiceManager.IsServiceAvailable<EnhancedExamToolbarService>();
            
            // 检查BenchSuite集成服务
            EnhancedExamToolbarService? enhancedService = AppServiceManager.GetService<EnhancedExamToolbarService>();
            result.EnhancedExamToolbarServiceAvailable = enhancedService != null;
            
            // 检查BenchSuite目录服务
            IBenchSuiteDirectoryService? directoryService = AppServiceManager.GetService<IBenchSuiteDirectoryService>();
            result.DirectoryServiceAvailable = directoryService != null;
            
            // 检查BenchSuite集成服务
            IBenchSuiteIntegrationService? integrationService = AppServiceManager.GetService<IBenchSuiteIntegrationService>();
            result.IntegrationServiceAvailable = integrationService != null;
            
            // 如果所有服务都可用，进行功能测试
            if (result.AllServicesAvailable)
            {
                if (integrationService != null)
                {
                    result.BenchSuiteServiceReachable = await integrationService.IsServiceAvailableAsync();
                }
                
                if (directoryService != null)
                {
                    var directoryValidation = await directoryService.EnsureDirectoryStructureAsync();
                    result.DirectoryStructureValid = directoryValidation.IsValid;
                    result.DirectoryValidationDetails = directoryValidation.Details;
                }
            }
            
            result.OverallValid = result.AllServicesAvailable && result.BenchSuiteServiceReachable && result.DirectoryStructureValid;
        }
        catch (Exception ex)
        {
            result.ValidationError = ex.Message;
            result.OverallValid = false;
        }
        
        return result;
    }
}

/// <summary>
/// BenchSuite集成验证结果
/// </summary>
public class BenchSuiteIntegrationValidationResult
{
    /// <summary>
    /// 服务管理器是否已初始化
    /// </summary>
    public bool ServiceManagerInitialized { get; set; }
    
    /// <summary>
    /// 增强考试工具栏服务是否可用
    /// </summary>
    public bool EnhancedExamToolbarServiceAvailable { get; set; }
    
    /// <summary>
    /// 目录服务是否可用
    /// </summary>
    public bool DirectoryServiceAvailable { get; set; }
    
    /// <summary>
    /// 集成服务是否可用
    /// </summary>
    public bool IntegrationServiceAvailable { get; set; }
    
    /// <summary>
    /// BenchSuite服务是否可达
    /// </summary>
    public bool BenchSuiteServiceReachable { get; set; }
    
    /// <summary>
    /// 目录结构是否有效
    /// </summary>
    public bool DirectoryStructureValid { get; set; }
    
    /// <summary>
    /// 目录验证详情
    /// </summary>
    public string DirectoryValidationDetails { get; set; } = string.Empty;
    
    /// <summary>
    /// 验证错误信息
    /// </summary>
    public string? ValidationError { get; set; }
    
    /// <summary>
    /// 所有服务是否都可用
    /// </summary>
    public bool AllServicesAvailable => ServiceManagerInitialized && 
                                       EnhancedExamToolbarServiceAvailable && 
                                       DirectoryServiceAvailable && 
                                       IntegrationServiceAvailable;
    
    /// <summary>
    /// 整体验证是否通过
    /// </summary>
    public bool OverallValid { get; set; }
    
    /// <summary>
    /// 获取验证摘要
    /// </summary>
    /// <returns>验证摘要</returns>
    public string GetValidationSummary()
    {
        if (OverallValid)
        {
            return "✅ BenchSuite集成验证通过，所有服务正常运行";
        }
        
        List<string> issues = new();
        
        if (!ServiceManagerInitialized)
            issues.Add("❌ 服务管理器未初始化");
        
        if (!EnhancedExamToolbarServiceAvailable)
            issues.Add("❌ 增强考试工具栏服务不可用");
        
        if (!DirectoryServiceAvailable)
            issues.Add("❌ 目录服务不可用");
        
        if (!IntegrationServiceAvailable)
            issues.Add("❌ 集成服务不可用");
        
        if (!BenchSuiteServiceReachable)
            issues.Add("❌ BenchSuite服务不可达");
        
        if (!DirectoryStructureValid)
            issues.Add("❌ 目录结构无效");
        
        if (!string.IsNullOrEmpty(ValidationError))
            issues.Add($"❌ 验证错误: {ValidationError}");
        
        return string.Join("\n", issues);
    }
}
