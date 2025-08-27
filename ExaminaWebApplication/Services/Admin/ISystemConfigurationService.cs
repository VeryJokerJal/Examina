using ExaminaWebApplication.Models.Admin;

namespace ExaminaWebApplication.Services.Admin;

/// <summary>
/// 系统配置管理服务接口
/// </summary>
public interface ISystemConfigurationService
{
    /// <summary>
    /// 获取所有系统配置
    /// </summary>
    /// <returns>系统配置列表</returns>
    Task<List<SystemConfigurationDto>> GetAllConfigurationsAsync();

    /// <summary>
    /// 根据分类获取系统配置
    /// </summary>
    /// <param name="category">配置分类</param>
    /// <returns>系统配置列表</returns>
    Task<List<SystemConfigurationDto>> GetConfigurationsByCategoryAsync(string category);

    /// <summary>
    /// 根据键名获取配置值
    /// </summary>
    /// <param name="configKey">配置键名</param>
    /// <returns>配置值，如果不存在则返回null</returns>
    Task<string?> GetConfigurationValueAsync(string configKey);

    /// <summary>
    /// 根据键名获取配置值，如果不存在则返回默认值
    /// </summary>
    /// <param name="configKey">配置键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值或默认值</returns>
    Task<string> GetConfigurationValueAsync(string configKey, string defaultValue);

    /// <summary>
    /// 获取整型配置值
    /// </summary>
    /// <param name="configKey">配置键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值或默认值</returns>
    Task<int> GetIntConfigurationValueAsync(string configKey, int defaultValue);

    /// <summary>
    /// 获取布尔型配置值
    /// </summary>
    /// <param name="configKey">配置键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>配置值或默认值</returns>
    Task<bool> GetBoolConfigurationValueAsync(string configKey, bool defaultValue);

    /// <summary>
    /// 更新配置值
    /// </summary>
    /// <param name="configKey">配置键名</param>
    /// <param name="configValue">配置值</param>
    /// <param name="userId">操作用户ID</param>
    /// <param name="description">配置描述（可选）</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateConfigurationAsync(string configKey, string configValue, int userId, string? description = null);

    /// <summary>
    /// 批量更新配置
    /// </summary>
    /// <param name="configurations">配置更新请求列表</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateConfigurationsAsync(List<UpdateSystemConfigurationRequest> configurations, int userId);

    /// <summary>
    /// 创建新配置
    /// </summary>
    /// <param name="configKey">配置键名</param>
    /// <param name="configValue">配置值</param>
    /// <param name="description">配置描述</param>
    /// <param name="category">配置分类</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>是否创建成功</returns>
    Task<bool> CreateConfigurationAsync(string configKey, string configValue, string? description, string category, int userId);

    /// <summary>
    /// 删除配置
    /// </summary>
    /// <param name="configKey">配置键名</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteConfigurationAsync(string configKey);

    /// <summary>
    /// 获取设备限制配置
    /// </summary>
    /// <returns>设备限制配置模型</returns>
    Task<DeviceLimitConfigurationModel> GetDeviceLimitConfigurationAsync();

    /// <summary>
    /// 更新设备限制配置
    /// </summary>
    /// <param name="configuration">设备限制配置模型</param>
    /// <param name="userId">操作用户ID</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateDeviceLimitConfigurationAsync(DeviceLimitConfigurationModel configuration, int userId);

    /// <summary>
    /// 初始化默认配置
    /// </summary>
    /// <returns>是否初始化成功</returns>
    Task<bool> InitializeDefaultConfigurationsAsync();

    /// <summary>
    /// 检查配置是否存在
    /// </summary>
    /// <param name="configKey">配置键名</param>
    /// <returns>是否存在</returns>
    Task<bool> ConfigurationExistsAsync(string configKey);
}
