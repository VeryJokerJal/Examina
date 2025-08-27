using ExaminaWebApplication.Models;

namespace ExaminaWebApplication.Services;

/// <summary>
/// 设备管理服务接口
/// </summary>
public interface IDeviceService
{
    /// <summary>
    /// 绑定设备到用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="deviceRequest">设备绑定请求</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="location">地理位置</param>
    /// <returns>绑定的设备信息</returns>
    Task<UserDevice> BindDeviceAsync(int userId, DeviceBindRequest deviceRequest, string? ipAddress = null, string? location = null);
    
    /// <summary>
    /// 验证设备是否已绑定到用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="deviceFingerprint">设备指纹</param>
    /// <returns>设备信息，如果未绑定则返回null</returns>
    Task<UserDevice?> ValidateDeviceAsync(int userId, string deviceFingerprint);
    
    /// <summary>
    /// 获取用户的所有设备
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>设备列表</returns>
    Task<List<DeviceInfo>> GetUserDevicesAsync(int userId);
    
    /// <summary>
    /// 解绑设备
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="deviceId">设备ID</param>
    /// <returns>是否成功解绑</returns>
    Task<bool> UnbindDeviceAsync(int userId, int deviceId);
    
    /// <summary>
    /// 管理员解绑用户设备
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>是否成功解绑</returns>
    Task<bool> AdminUnbindDeviceAsync(int deviceId);
    
    /// <summary>
    /// 更新设备最后使用时间
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="location">地理位置</param>
    /// <returns>是否成功更新</returns>
    Task<bool> UpdateDeviceLastUsedAsync(int deviceId, string? ipAddress = null, string? location = null);
    
    /// <summary>
    /// 检查用户是否可以绑定新设备
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否可以绑定新设备</returns>
    Task<bool> CanBindNewDeviceAsync(int userId);
    
    /// <summary>
    /// 生成设备指纹
    /// </summary>
    /// <param name="userAgent">用户代理</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="additionalInfo">附加信息</param>
    /// <returns>设备指纹</returns>
    string GenerateDeviceFingerprint(string userAgent, string ipAddress, string? additionalInfo = null);
    
    /// <summary>
    /// 设置设备为受信任设备
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isTrusted">是否受信任</param>
    /// <returns>是否成功设置</returns>
    Task<bool> SetDeviceTrustedAsync(int deviceId, bool isTrusted);

    /// <summary>
    /// 清理过期设备
    /// </summary>
    /// <returns>清理的设备数量</returns>
    Task<int> CleanupExpiredDevicesAsync();

    /// <summary>
    /// 解绑设备（管理员操作）
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <returns>是否成功解绑</returns>
    Task<bool> UnbindDeviceAsync(int deviceId);

    /// <summary>
    /// 设置设备信任状态
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="isTrusted">是否信任</param>
    /// <returns>是否成功设置</returns>
    Task<bool> SetDeviceTrustAsync(int deviceId, bool isTrusted);

    /// <summary>
    /// 延长设备有效期
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="days">延长天数</param>
    /// <returns>是否成功延长</returns>
    Task<bool> ExtendDeviceExpiryAsync(int deviceId, int days);
}
