using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Admin;
using ExaminaWebApplication.Services.Admin;

namespace ExaminaWebApplication.Services;

/// <summary>
/// 设备管理服务实现
/// </summary>
public class DeviceService : IDeviceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DeviceService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISystemConfigurationService _systemConfigurationService;

    public DeviceService(
        ApplicationDbContext context,
        ILogger<DeviceService> logger,
        IConfiguration configuration,
        ISystemConfigurationService systemConfigurationService)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _systemConfigurationService = systemConfigurationService;
    }

    public async Task<UserDevice> BindDeviceAsync(int userId, DeviceBindRequest deviceRequest, string? ipAddress = null, string? location = null)
    {
        try
        {
            // 检查用户是否存在
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("用户不存在", nameof(userId));
            }

            // 检查是否可以绑定新设备
            DeviceBindResult bindResult = await CanBindNewDeviceAsync(userId);
            if (!bindResult.CanBind)
            {
                if (bindResult.RequiresKickout)
                {
                    // 需要踢出最早的设备
                    bool kickoutSuccess = await KickoutOldestDeviceAsync(userId);
                    if (!kickoutSuccess)
                    {
                        throw new InvalidOperationException("无法踢出最早的设备，设备绑定失败");
                    }
                    _logger.LogInformation("为用户 {UserId} 踢出最早设备以绑定新设备", userId);
                }
                else
                {
                    throw new InvalidOperationException(bindResult.Message ?? "已达到最大设备绑定数量");
                }
            }

            // 检查当前用户是否已经绑定了这个设备
            var existingUserDevice = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceFingerprint == deviceRequest.DeviceFingerprint);

            if (existingUserDevice != null)
            {
                // 用户已经绑定了这个设备，更新信息并返回
                existingUserDevice.LastUsedAt = DateTime.UtcNow;
                existingUserDevice.IpAddress = ipAddress;
                existingUserDevice.Location = location;
                await _context.SaveChangesAsync();
                return existingUserDevice;
            }

            // 检查设备指纹是否被其他用户使用
            var deviceUsedByOtherUser = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceFingerprint == deviceRequest.DeviceFingerprint && d.UserId != userId);

            if (deviceUsedByOtherUser != null)
            {
                // 设备指纹冲突处理：为当前用户生成新的设备指纹
                string originalFingerprint = deviceRequest.DeviceFingerprint;
                string newFingerprint = await GenerateUniqueDeviceFingerprintAsync(originalFingerprint, userId);

                _logger.LogWarning("设备指纹冲突，原指纹: {OriginalFingerprint}，新指纹: {NewFingerprint}，用户ID: {UserId}",
                    originalFingerprint, newFingerprint, userId);

                deviceRequest.DeviceFingerprint = newFingerprint;
            }

            // 创建新设备
            var device = new UserDevice
            {
                UserId = userId,
                DeviceFingerprint = deviceRequest.DeviceFingerprint,
                DeviceName = string.IsNullOrEmpty(deviceRequest.DeviceName) 
                    ? $"{deviceRequest.DeviceType}_{DateTime.Now:yyyyMMdd}" 
                    : deviceRequest.DeviceName,
                DeviceType = deviceRequest.DeviceType,
                OperatingSystem = deviceRequest.OperatingSystem,
                BrowserInfo = deviceRequest.BrowserInfo,
                IpAddress = ipAddress,
                Location = location,
                CreatedAt = DateTime.Now,
                LastUsedAt = DateTime.Now,
                IsActive = true,
                ExpiresAt = DateTime.Now.AddDays(
                    int.Parse(_configuration["DeviceSecurity:DeviceTokenExpirationDays"] ?? "30")),
                IsTrusted = false
            };

            _context.UserDevices.Add(device);
            await _context.SaveChangesAsync();

            _logger.LogInformation("为用户 {UserId} 绑定新设备 {DeviceId}", userId, device.Id);
            return device;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "绑定设备失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<UserDevice?> ValidateDeviceAsync(int userId, string deviceFingerprint)
    {
        try
        {
            var device = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId 
                    && d.DeviceFingerprint == deviceFingerprint 
                    && d.IsActive 
                    && (d.ExpiresAt == null || d.ExpiresAt > DateTime.UtcNow));

            return device;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证设备失败，用户ID: {UserId}, 设备指纹: {DeviceFingerprint}", userId, deviceFingerprint);
            return null;
        }
    }

    public async Task<List<DeviceInfo>> GetUserDevicesAsync(int userId)
    {
        try
        {
            var devices = await _context.UserDevices
                .Include(d => d.User)
                .Where(d => d.UserId == userId && d.IsActive)
                .OrderByDescending(d => d.LastUsedAt)
                .Select(d => new DeviceInfo
                {
                    Id = d.Id,
                    DeviceName = d.DeviceName,
                    DeviceType = d.DeviceType,
                    OperatingSystem = d.OperatingSystem,
                    BrowserInfo = d.BrowserInfo,
                    IpAddress = d.IpAddress,
                    Location = d.Location,
                    CreatedAt = d.CreatedAt,
                    LastUsedAt = d.LastUsedAt,
                    IsActive = d.IsActive,
                    IsTrusted = d.IsTrusted,
                    UserId = d.UserId,
                    User = d.User != null ? new DeviceUserInfo
                    {
                        Id = d.User.Id,
                        Username = d.User.Username,
                        RealName = d.User.RealName,
                        Email = d.User.Email,
                        PhoneNumber = d.User.PhoneNumber,
                        Role = d.User.Role,
                        IsActive = d.User.IsActive
                    } : null
                })
                .ToListAsync();

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户设备列表失败，用户ID: {UserId}", userId);
            return [];
        }
    }

    public async Task<List<DeviceInfo>> GetAllDevicesAsync(bool includeInactive = false, string? searchKeyword = null, UserRole? userRole = null)
    {
        try
        {
            var query = _context.UserDevices
                .Include(d => d.User)
                .AsQueryable();

            // 筛选活跃状态
            if (!includeInactive)
            {
                query = query.Where(d => d.IsActive);
            }

            // 用户角色筛选
            if (userRole.HasValue)
            {
                query = query.Where(d => d.User.Role == userRole.Value);
            }

            // 搜索关键词筛选
            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query = query.Where(d =>
                    d.DeviceName.Contains(searchKeyword) ||
                    d.DeviceType.Contains(searchKeyword) ||
                    (d.OperatingSystem != null && d.OperatingSystem.Contains(searchKeyword)) ||
                    (d.IpAddress != null && d.IpAddress.Contains(searchKeyword)) ||
                    d.User.Username.Contains(searchKeyword) ||
                    (d.User.RealName != null && d.User.RealName.Contains(searchKeyword)));
            }

            var devices = await query
                .OrderByDescending(d => d.LastUsedAt)
                .Select(d => new DeviceInfo
                {
                    Id = d.Id,
                    DeviceName = d.DeviceName,
                    DeviceType = d.DeviceType,
                    OperatingSystem = d.OperatingSystem,
                    BrowserInfo = d.BrowserInfo,
                    IpAddress = d.IpAddress,
                    Location = d.Location,
                    CreatedAt = d.CreatedAt,
                    LastUsedAt = d.LastUsedAt,
                    IsActive = d.IsActive,
                    IsTrusted = d.IsTrusted,
                    UserId = d.UserId,
                    User = new DeviceUserInfo
                    {
                        Id = d.User.Id,
                        Username = d.User.Username,
                        RealName = d.User.RealName,
                        Email = d.User.Email,
                        PhoneNumber = d.User.PhoneNumber,
                        Role = d.User.Role,
                        IsActive = d.User.IsActive
                    }
                })
                .ToListAsync();

            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有设备列表失败");
            return [];
        }
    }

    public async Task<bool> UnbindDeviceAsync(int userId, int deviceId)
    {
        try
        {
            var device = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);

            if (device == null)
            {
                return false;
            }

            device.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("用户 {UserId} 解绑设备 {DeviceId}", userId, deviceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解绑设备失败，用户ID: {UserId}, 设备ID: {DeviceId}", userId, deviceId);
            return false;
        }
    }

    public async Task<bool> AdminUnbindDeviceAsync(int deviceId)
    {
        try
        {
            var device = await _context.UserDevices.FindAsync(deviceId);
            if (device == null)
            {
                return false;
            }

            device.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("管理员解绑设备 {DeviceId}", deviceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "管理员解绑设备失败，设备ID: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<bool> UpdateDeviceLastUsedAsync(int deviceId, string? ipAddress = null, string? location = null)
    {
        try
        {
            var device = await _context.UserDevices.FindAsync(deviceId);
            if (device == null || !device.IsActive)
            {
                return false;
            }

            device.LastUsedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(ipAddress))
            {
                device.IpAddress = ipAddress;
            }
            if (!string.IsNullOrEmpty(location))
            {
                device.Location = location;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备最后使用时间失败，设备ID: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<DeviceBindResult> CanBindNewDeviceAsync(int userId)
    {
        try
        {
            User? user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new DeviceBindResult { CanBind = false, Message = "用户不存在" };
            }

            // 获取系统配置
            DeviceLimitConfigurationModel deviceConfig = await _systemConfigurationService.GetDeviceLimitConfigurationAsync();

            // 如果未启用设备限制，则允许绑定
            if (!deviceConfig.EnableDeviceLimit)
            {
                return new DeviceBindResult { CanBind = true };
            }

            // 管理员不受设备数量限制
            if (user.Role == UserRole.Administrator)
            {
                return new DeviceBindResult { CanBind = true };
            }

            // 获取当前活跃设备数量
            int currentDeviceCount = await _context.UserDevices
                .CountAsync(d => d.UserId == userId && d.IsActive);

            // 确定最大设备数量限制
            int maxDeviceCount = deviceConfig.MaxDeviceCount;

            // 如果当前设备数量小于限制，可以绑定
            if (currentDeviceCount < maxDeviceCount)
            {
                return new DeviceBindResult { CanBind = true };
            }

            // 达到设备数量限制，根据踢出策略决定
            if (deviceConfig.KickoutPolicy == DeviceKickoutPolicy.KickoutOldest)
            {
                return new DeviceBindResult
                {
                    CanBind = true,
                    RequiresKickout = true,
                    Message = "将踢出最早登录的设备"
                };
            }
            else
            {
                return new DeviceBindResult
                {
                    CanBind = false,
                    Message = $"已达到最大设备数量限制（{maxDeviceCount}台），无法绑定新设备"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查设备绑定权限失败，用户ID: {UserId}", userId);
            return new DeviceBindResult { CanBind = false, Message = "检查设备绑定权限时发生错误" };
        }
    }

    /// <summary>
    /// 踢出用户最早登录的设备
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否成功踢出</returns>
    public async Task<bool> KickoutOldestDeviceAsync(int userId)
    {
        try
        {
            UserDevice? oldestDevice = await _context.UserDevices
                .Where(d => d.UserId == userId && d.IsActive)
                .OrderBy(d => d.LastUsedAt ?? d.CreatedAt)
                .FirstOrDefaultAsync();

            if (oldestDevice == null)
            {
                _logger.LogWarning("用户 {UserId} 没有可踢出的设备", userId);
                return false;
            }

            // 标记设备为非活跃状态
            oldestDevice.IsActive = false;
            oldestDevice.LastUsedAt = DateTime.UtcNow;

            // 删除该设备的所有活跃会话
            List<UserSession> deviceSessions = await _context.UserSessions
                .Where(s => s.DeviceId == oldestDevice.Id && s.IsActive)
                .ToListAsync();

            foreach (UserSession session in deviceSessions)
            {
                session.IsActive = false;
                session.LastActivityAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("成功踢出用户 {UserId} 的最早设备 {DeviceId}，同时注销了 {SessionCount} 个会话",
                userId, oldestDevice.Id, deviceSessions.Count);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "踢出最早设备失败，用户ID: {UserId}", userId);
            return false;
        }
    }

    public string GenerateDeviceFingerprint(string userAgent, string ipAddress, string? additionalInfo = null)
    {
        try
        {
            var input = $"{userAgent}|{ipAddress}|{additionalInfo ?? ""}|{DateTime.UtcNow:yyyyMMdd}";
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成设备指纹失败");
            return Guid.NewGuid().ToString();
        }
    }

    public async Task<bool> UnbindDeviceAsync(int deviceId)
    {
        try
        {
            var device = await _context.UserDevices.FindAsync(deviceId);
            if (device == null)
            {
                return false;
            }

            device.IsActive = false;
            device.LastUsedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("设备解绑成功，设备ID: {DeviceId}", deviceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解绑设备失败，设备ID: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<bool> SetDeviceTrustAsync(int deviceId, bool isTrusted)
    {
        try
        {
            var device = await _context.UserDevices.FindAsync(deviceId);
            if (device == null)
            {
                return false;
            }

            device.IsTrusted = isTrusted;
            device.LastUsedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("设备信任状态设置成功，设备ID: {DeviceId}, 信任状态: {IsTrusted}", deviceId, isTrusted);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置设备信任状态失败，设备ID: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<bool> ExtendDeviceExpiryAsync(int deviceId, int days)
    {
        try
        {
            var device = await _context.UserDevices.FindAsync(deviceId);
            if (device == null)
            {
                return false;
            }

            // 如果设备已过期，从当前时间开始延长；否则从原有过期时间延长
            var baseDate = device.ExpiresAt > DateTime.Now ? device.ExpiresAt : DateTime.Now;
            device.ExpiresAt = baseDate?.AddDays(days) ?? DateTime.Now.AddDays(days);
            device.LastUsedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("设备有效期延长成功，设备ID: {DeviceId}, 延长天数: {Days}, 新过期时间: {ExpiresAt}",
                deviceId, days, device.ExpiresAt);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "延长设备有效期失败，设备ID: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<bool> SetDeviceTrustedAsync(int deviceId, bool isTrusted)
    {
        try
        {
            var device = await _context.UserDevices.FindAsync(deviceId);
            if (device == null)
            {
                return false;
            }

            device.IsTrusted = isTrusted;
            await _context.SaveChangesAsync();

            _logger.LogInformation("设置设备 {DeviceId} 信任状态为 {IsTrusted}", deviceId, isTrusted);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置设备信任状态失败，设备ID: {DeviceId}", deviceId);
            return false;
        }
    }

    public async Task<int> CleanupExpiredDevicesAsync()
    {
        try
        {
            var expiredDevices = await _context.UserDevices
                .Where(d => d.ExpiresAt != null && d.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            foreach (var device in expiredDevices)
            {
                device.IsActive = false;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("清理了 {Count} 个过期设备", expiredDevices.Count);
            return expiredDevices.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期设备失败");
            return 0;
        }
    }

    /// <summary>
    /// 生成唯一的设备指纹
    /// </summary>
    /// <param name="originalFingerprint">原始设备指纹</param>
    /// <param name="userId">用户ID</param>
    /// <returns>唯一的设备指纹</returns>
    private async Task<string> GenerateUniqueDeviceFingerprintAsync(string originalFingerprint, int userId)
    {
        // 尝试不同的后缀来生成唯一指纹
        for (int i = 1; i <= 100; i++)
        {
            string newFingerprint = $"{originalFingerprint}-U{userId}-{i:D2}";

            // 检查新指纹是否已存在
            bool exists = await _context.UserDevices
                .AnyAsync(d => d.DeviceFingerprint == newFingerprint);

            if (!exists)
            {
                return newFingerprint;
            }
        }

        // 如果前100个都被占用，使用时间戳
        string timestampFingerprint = $"{originalFingerprint}-U{userId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        return timestampFingerprint;
    }
}
