namespace Examina.Services;

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// API基础URL
    /// </summary>
    string ApiBaseUrl { get; }
    
    /// <summary>
    /// 学生认证API端点
    /// </summary>
    string StudentAuthEndpoint { get; }
    
    /// <summary>
    /// 管理员认证API端点
    /// </summary>
    string AdminAuthEndpoint { get; }
    
    /// <summary>
    /// 应用程序名称
    /// </summary>
    string ApplicationName { get; }
    
    /// <summary>
    /// 应用程序版本
    /// </summary>
    string ApplicationVersion { get; }
    
    /// <summary>
    /// 是否为调试模式
    /// </summary>
    bool IsDebugMode { get; }
    
    /// <summary>
    /// 令牌刷新阈值（分钟）
    /// </summary>
    int TokenRefreshThresholdMinutes { get; }
    
    /// <summary>
    /// 自动登录是否启用
    /// </summary>
    bool AutoLoginEnabled { get; }
}
