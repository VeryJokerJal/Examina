using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;

namespace ExaminaWebApplication.Services;

/// <summary>
/// 设备管理服务实现
/// </summary>
public class DeviceService : IDeviceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DeviceService> _logger;
    private readonly IConfiguration _configuration;

    public DeviceService(ApplicationDbContext context, ILogger<DeviceService> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
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
            if (!await CanBindNewDeviceAsync(userId))
            {
                throw new InvalidOperationException("已达到最大设备绑定数量");
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
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddDays(
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
                    IsTrusted = d.IsTrusted
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

    public async Task<bool> CanBindNewDeviceAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // 管理员和教师通常允许多设备
            if (user.Role == UserRole.Administrator || user.Role == UserRole.Teacher)
            {
                var currentDeviceCount = await _context.UserDevices
                    .CountAsync(d => d.UserId == userId && d.IsActive);
                return currentDeviceCount < user.MaxDeviceCount;
            }

            // 学生根据配置决定
            if (user.AllowMultipleDevices)
            {
                var currentDeviceCount = await _context.UserDevices
                    .CountAsync(d => d.UserId == userId && d.IsActive);
                return currentDeviceCount < user.MaxDeviceCount;
            }

            // 学生默认只允许一个设备
            var hasActiveDevice = await _context.UserDevices
                .AnyAsync(d => d.UserId == userId && d.IsActive);
            return !hasActiveDevice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查设备绑定权限失败，用户ID: {UserId}", userId);
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
