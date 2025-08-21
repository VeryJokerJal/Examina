using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// 服务集合扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 配置Examina应用程序服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection ConfigureExaminaServices(this IServiceCollection services)
    {
        // 注册BenchSuite相关服务
        services.AddBenchSuiteServices();
        
        // 注册其他应用程序服务
        // services.AddSingleton<IOtherService, OtherService>();
        
        return services;
    }

    /// <summary>
    /// 创建服务提供者并初始化服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务提供者</returns>
    public static async Task<IServiceProvider> BuildAndInitializeAsync(this IServiceCollection services)
    {
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        
        // 初始化BenchSuite服务
        await serviceProvider.InitializeBenchSuiteServicesAsync();
        
        return serviceProvider;
    }
}

/// <summary>
/// 应用程序服务管理器
/// </summary>
public static class AppServiceManager
{
    private static IServiceProvider? _serviceProvider;
    
    /// <summary>
    /// 初始化服务提供者
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// 获取服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    public static T? GetService<T>() where T : class
    {
        return _serviceProvider?.GetService<T>();
    }
    
    /// <summary>
    /// 获取必需的服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    public static T GetRequiredService<T>() where T : class
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("服务提供者未初始化，请先调用Initialize方法");
        }
        
        return _serviceProvider.GetRequiredService<T>();
    }
    
    /// <summary>
    /// 检查服务是否可用
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务是否可用</returns>
    public static bool IsServiceAvailable<T>() where T : class
    {
        return _serviceProvider?.GetService<T>() != null;
    }
}
