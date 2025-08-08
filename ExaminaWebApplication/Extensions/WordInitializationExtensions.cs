using ExaminaWebApplication.Services.Word;

namespace ExaminaWebApplication.Extensions;

/// <summary>
/// Word功能初始化扩展方法
/// </summary>
public static class WordInitializationExtensions
{
    /// <summary>
    /// 初始化Word操作点数据
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns></returns>
    public static async Task<WebApplication> InitializeWordDataAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        ILogger<WebApplication> logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();
        
        try
        {
            logger.LogInformation("开始初始化Word操作点数据...");
            
            WordOperationService wordOperationService = scope.ServiceProvider.GetRequiredService<WordOperationService>();
            await wordOperationService.InitializeWordOperationDataAsync();
            
            logger.LogInformation("Word操作点数据初始化完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Word操作点数据初始化失败");
            // 不抛出异常，避免影响应用程序启动
        }
        
        return app;
    }

    /// <summary>
    /// 验证Word功能配置
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns></returns>
    public static async Task<WebApplication> ValidateWordConfigurationAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        ILogger<WebApplication> logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();
        
        try
        {
            logger.LogInformation("开始验证Word功能配置...");
            
            WordOperationService wordOperationService = scope.ServiceProvider.GetRequiredService<WordOperationService>();
            
            // 获取操作点统计信息
            object statistics = await wordOperationService.GetOperationPointStatisticsAsync();
            logger.LogInformation("Word操作点统计信息: {@Statistics}", statistics);
            
            // 获取枚举类型数量
            var enumTypes = await wordOperationService.GetAllEnumTypesAsync();
            logger.LogInformation("Word枚举类型数量: {Count}", enumTypes.Count);
            
            logger.LogInformation("Word功能配置验证完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Word功能配置验证失败");
        }
        
        return app;
    }

    /// <summary>
    /// 配置Word功能的开发环境初始化
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <returns></returns>
    public static async Task<WebApplication> ConfigureWordDevelopmentAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        using IServiceScope scope = app.Services.CreateScope();
        ILogger<WebApplication> logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();
        
        try
        {
            logger.LogInformation("开始配置Word功能开发环境...");
            
            // 在开发环境中自动初始化数据
            await app.InitializeWordDataAsync();
            
            // 验证配置
            await app.ValidateWordConfigurationAsync();
            
            logger.LogInformation("Word功能开发环境配置完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Word功能开发环境配置失败");
        }
        
        return app;
    }
}
