using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Services.Admin;

/// <summary>
/// 系统配置管理服务实现
/// </summary>
public class SystemConfigurationService : ISystemConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SystemConfigurationService> _logger;

    public SystemConfigurationService(ApplicationDbContext context, ILogger<SystemConfigurationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有系统配置
    /// </summary>
    public async Task<List<SystemConfigurationDto>> GetAllConfigurationsAsync()
    {
        try
        {
            List<SystemConfiguration> configurations = await _context.SystemConfigurations
                .Include(c => c.Creator)
                .Include(c => c.Updater)
                .OrderBy(c => c.Category)
                .ThenBy(c => c.ConfigKey)
                .ToListAsync();

            return configurations.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有系统配置失败");
            return [];
        }
    }

    /// <summary>
    /// 根据分类获取系统配置
    /// </summary>
    public async Task<List<SystemConfigurationDto>> GetConfigurationsByCategoryAsync(string category)
    {
        try
        {
            List<SystemConfiguration> configurations = await _context.SystemConfigurations
                .Include(c => c.Creator)
                .Include(c => c.Updater)
                .Where(c => c.Category == category && c.IsEnabled)
                .OrderBy(c => c.ConfigKey)
                .ToListAsync();

            return configurations.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据分类获取系统配置失败，分类: {Category}", category);
            return [];
        }
    }

    /// <summary>
    /// 根据键名获取配置值
    /// </summary>
    public async Task<string?> GetConfigurationValueAsync(string configKey)
    {
        try
        {
            SystemConfiguration? configuration = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigKey == configKey && c.IsEnabled);

            return configuration?.ConfigValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取配置值失败，键名: {ConfigKey}", configKey);
            return null;
        }
    }

    /// <summary>
    /// 根据键名获取配置值，如果不存在则返回默认值
    /// </summary>
    public async Task<string> GetConfigurationValueAsync(string configKey, string defaultValue)
    {
        string? value = await GetConfigurationValueAsync(configKey);
        return value ?? defaultValue;
    }

    /// <summary>
    /// 获取整型配置值
    /// </summary>
    public async Task<int> GetIntConfigurationValueAsync(string configKey, int defaultValue)
    {
        string? value = await GetConfigurationValueAsync(configKey);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (int.TryParse(value, out int result))
        {
            return result;
        }

        _logger.LogWarning("配置值无法转换为整型，键名: {ConfigKey}, 值: {Value}, 使用默认值: {DefaultValue}",
            configKey, value, defaultValue);
        return defaultValue;
    }

    /// <summary>
    /// 获取布尔型配置值
    /// </summary>
    public async Task<bool> GetBoolConfigurationValueAsync(string configKey, bool defaultValue)
    {
        string? value = await GetConfigurationValueAsync(configKey);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        // 支持常见的布尔值表示
        string lowerValue = value.ToLowerInvariant();
        if (lowerValue is "1" or "yes" or "on" or "enabled")
        {
            return true;
        }
        if (lowerValue is "0" or "no" or "off" or "disabled")
        {
            return false;
        }

        _logger.LogWarning("配置值无法转换为布尔型，键名: {ConfigKey}, 值: {Value}, 使用默认值: {DefaultValue}",
            configKey, value, defaultValue);
        return defaultValue;
    }

    /// <summary>
    /// 更新配置值
    /// </summary>
    public async Task<bool> UpdateConfigurationAsync(string configKey, string configValue, int userId, string? description = null)
    {
        try
        {
            SystemConfiguration? configuration = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigKey == configKey);

            if (configuration == null)
            {
                // 如果配置不存在，创建新配置
                return await CreateConfigurationAsync(configKey, configValue, description, "General", userId);
            }

            // 更新现有配置
            configuration.ConfigValue = configValue;
            configuration.UpdatedAt = DateTime.UtcNow;
            configuration.UpdatedBy = userId;

            if (!string.IsNullOrEmpty(description))
            {
                configuration.Description = description;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("配置更新成功，键名: {ConfigKey}, 值: {ConfigValue}, 操作用户: {UserId}",
                configKey, configValue, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新配置失败，键名: {ConfigKey}, 值: {ConfigValue}, 操作用户: {UserId}",
                configKey, configValue, userId);
            return false;
        }
    }

    /// <summary>
    /// 批量更新配置
    /// </summary>
    public async Task<bool> UpdateConfigurationsAsync(List<UpdateSystemConfigurationRequest> configurations, int userId)
    {
        try
        {
            foreach (UpdateSystemConfigurationRequest request in configurations)
            {
                bool success = await UpdateConfigurationAsync(request.ConfigKey, request.ConfigValue, userId, request.Description);
                if (!success)
                {
                    _logger.LogWarning("批量更新配置时失败，键名: {ConfigKey}", request.ConfigKey);
                    return false;
                }
            }

            _logger.LogInformation("批量更新配置成功，更新数量: {Count}, 操作用户: {UserId}",
                configurations.Count, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新配置失败，操作用户: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 创建新配置
    /// </summary>
    public async Task<bool> CreateConfigurationAsync(string configKey, string configValue, string? description, string category, int userId)
    {
        try
        {
            // 检查配置是否已存在
            bool exists = await ConfigurationExistsAsync(configKey);
            if (exists)
            {
                _logger.LogWarning("配置已存在，无法创建，键名: {ConfigKey}", configKey);
                return false;
            }

            SystemConfiguration configuration = new()
            {
                ConfigKey = configKey,
                ConfigValue = configValue,
                Description = description,
                Category = category,
                IsEnabled = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.SystemConfigurations.Add(configuration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("配置创建成功，键名: {ConfigKey}, 值: {ConfigValue}, 操作用户: {UserId}",
                configKey, configValue, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建配置失败，键名: {ConfigKey}, 值: {ConfigValue}, 操作用户: {UserId}",
                configKey, configValue, userId);
            return false;
        }
    }

    /// <summary>
    /// 删除配置
    /// </summary>
    public async Task<bool> DeleteConfigurationAsync(string configKey)
    {
        try
        {
            SystemConfiguration? configuration = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigKey == configKey);

            if (configuration == null)
            {
                _logger.LogWarning("配置不存在，无法删除，键名: {ConfigKey}", configKey);
                return false;
            }

            _context.SystemConfigurations.Remove(configuration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("配置删除成功，键名: {ConfigKey}", configKey);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除配置失败，键名: {ConfigKey}", configKey);
            return false;
        }
    }

    /// <summary>
    /// 检查配置是否存在
    /// </summary>
    public async Task<bool> ConfigurationExistsAsync(string configKey)
    {
        try
        {
            return await _context.SystemConfigurations
                .AnyAsync(c => c.ConfigKey == configKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查配置是否存在失败，键名: {ConfigKey}", configKey);
            return false;
        }
    }

    /// <summary>
    /// 获取设备限制配置
    /// </summary>
    public async Task<DeviceLimitConfigurationModel> GetDeviceLimitConfigurationAsync()
    {
        try
        {
            int maxDeviceCount = await GetIntConfigurationValueAsync(SystemConfigurationKeys.MaxDeviceCountLimit, 3);
            bool enableDeviceLimit = await GetBoolConfigurationValueAsync(SystemConfigurationKeys.EnableDeviceCountLimit, true);
            string kickoutPolicyValue = await GetConfigurationValueAsync(SystemConfigurationKeys.DeviceKickoutPolicy, "1");
            int sessionExpirationDays = await GetIntConfigurationValueAsync(SystemConfigurationKeys.DeviceSessionExpirationDays, 30);

            DeviceKickoutPolicy kickoutPolicy = DeviceKickoutPolicy.KickoutOldest;
            if (Enum.TryParse<DeviceKickoutPolicy>(kickoutPolicyValue, out DeviceKickoutPolicy parsedPolicy))
            {
                kickoutPolicy = parsedPolicy;
            }

            return new DeviceLimitConfigurationModel
            {
                MaxDeviceCount = maxDeviceCount,
                EnableDeviceLimit = enableDeviceLimit,
                KickoutPolicy = kickoutPolicy,
                SessionExpirationDays = sessionExpirationDays
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备限制配置失败");
            return new DeviceLimitConfigurationModel(); // 返回默认配置
        }
    }

    /// <summary>
    /// 更新设备限制配置
    /// </summary>
    public async Task<bool> UpdateDeviceLimitConfigurationAsync(DeviceLimitConfigurationModel configuration, int userId)
    {
        try
        {
            List<UpdateSystemConfigurationRequest> updates = new()
            {
                new UpdateSystemConfigurationRequest
                {
                    ConfigKey = SystemConfigurationKeys.MaxDeviceCountLimit,
                    ConfigValue = configuration.MaxDeviceCount.ToString(),
                    Description = "用户最大设备数量限制"
                },
                new UpdateSystemConfigurationRequest
                {
                    ConfigKey = SystemConfigurationKeys.EnableDeviceCountLimit,
                    ConfigValue = configuration.EnableDeviceLimit.ToString(),
                    Description = "是否启用设备数量限制"
                },
                new UpdateSystemConfigurationRequest
                {
                    ConfigKey = SystemConfigurationKeys.DeviceKickoutPolicy,
                    ConfigValue = ((int)configuration.KickoutPolicy).ToString(),
                    Description = "设备踢出策略（1: 踢出最早登录的设备, 2: 拒绝新登录）"
                },
                new UpdateSystemConfigurationRequest
                {
                    ConfigKey = SystemConfigurationKeys.DeviceSessionExpirationDays,
                    ConfigValue = configuration.SessionExpirationDays.ToString(),
                    Description = "设备会话过期时间（天）"
                }
            };

            bool success = await UpdateConfigurationsAsync(updates, userId);

            if (success)
            {
                _logger.LogInformation("设备限制配置更新成功，操作用户: {UserId}", userId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备限制配置失败，操作用户: {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// 初始化默认配置
    /// </summary>
    public async Task<bool> InitializeDefaultConfigurationsAsync()
    {
        try
        {
            // 检查是否已经初始化过
            bool hasConfigurations = await _context.SystemConfigurations.AnyAsync();
            if (hasConfigurations)
            {
                _logger.LogInformation("系统配置已存在，跳过初始化");
                return true;
            }

            List<SystemConfiguration> defaultConfigurations = new()
            {
                new SystemConfiguration
                {
                    ConfigKey = SystemConfigurationKeys.MaxDeviceCountLimit,
                    ConfigValue = "3",
                    Description = "用户最大设备数量限制",
                    Category = "DeviceManagement",
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                },
                new SystemConfiguration
                {
                    ConfigKey = SystemConfigurationKeys.EnableDeviceCountLimit,
                    ConfigValue = "true",
                    Description = "是否启用设备数量限制",
                    Category = "DeviceManagement",
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                },
                new SystemConfiguration
                {
                    ConfigKey = SystemConfigurationKeys.DeviceKickoutPolicy,
                    ConfigValue = "1",
                    Description = "设备踢出策略（1: 踢出最早登录的设备, 2: 拒绝新登录）",
                    Category = "DeviceManagement",
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                },
                new SystemConfiguration
                {
                    ConfigKey = SystemConfigurationKeys.DeviceSessionExpirationDays,
                    ConfigValue = "30",
                    Description = "设备会话过期时间（天）",
                    Category = "DeviceManagement",
                    IsEnabled = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.SystemConfigurations.AddRange(defaultConfigurations);
            await _context.SaveChangesAsync();

            _logger.LogInformation("默认系统配置初始化成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化默认配置失败");
            return false;
        }
    }

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    private static SystemConfigurationDto MapToDto(SystemConfiguration configuration)
    {
        return new SystemConfigurationDto
        {
            Id = configuration.Id,
            ConfigKey = configuration.ConfigKey,
            ConfigValue = configuration.ConfigValue,
            Description = configuration.Description,
            Category = configuration.Category,
            IsEnabled = configuration.IsEnabled,
            CreatedAt = configuration.CreatedAt,
            UpdatedAt = configuration.UpdatedAt,
            CreatorName = configuration.Creator?.Username,
            UpdaterName = configuration.Updater?.Username
        };
    }
}
