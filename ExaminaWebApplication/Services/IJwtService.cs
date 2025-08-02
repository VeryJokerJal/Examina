using ExaminaWebApplication.Models;
using System.Security.Claims;

namespace ExaminaWebApplication.Services;

/// <summary>
/// JWT服务接口
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// 生成访问令牌
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <param name="deviceId">设备ID（可选）</param>
    /// <returns>访问令牌</returns>
    string GenerateAccessToken(User user, int? deviceId = null);

    /// <summary>
    /// 生成刷新令牌
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <param name="deviceId">设备ID（可选）</param>
    /// <returns>刷新令牌</returns>
    string GenerateRefreshToken(User user, int? deviceId = null);

    /// <summary>
    /// 验证访问令牌
    /// </summary>
    /// <param name="token">访问令牌</param>
    /// <returns>是否有效</returns>
    bool ValidateAccessToken(string token);

    /// <summary>
    /// 验证刷新令牌
    /// </summary>
    /// <param name="token">刷新令牌</param>
    /// <returns>是否有效</returns>
    bool ValidateRefreshToken(string token);

    /// <summary>
    /// 从令牌中获取用户ID
    /// </summary>
    /// <param name="token">令牌</param>
    /// <returns>用户ID，如果无效则返回null</returns>
    int? GetUserIdFromToken(string token);

    /// <summary>
    /// 从令牌中获取设备ID
    /// </summary>
    /// <param name="token">令牌</param>
    /// <returns>设备ID，如果无效则返回null</returns>
    int? GetDeviceIdFromToken(string token);

    /// <summary>
    /// 从令牌中获取用户角色
    /// </summary>
    /// <param name="token">令牌</param>
    /// <returns>用户角色</returns>
    UserRole? GetUserRoleFromToken(string token);

    /// <summary>
    /// 从令牌中获取所有声明
    /// </summary>
    /// <param name="token">令牌</param>
    /// <returns>声明集合</returns>
    ClaimsPrincipal? GetClaimsFromToken(string token);

    /// <summary>
    /// 获取令牌过期时间
    /// </summary>
    /// <param name="token">令牌</param>
    /// <returns>过期时间，如果无效则返回null</returns>
    DateTime? GetTokenExpirationTime(string token);

    /// <summary>
    /// 检查令牌是否即将过期（在指定分钟内过期）
    /// </summary>
    /// <param name="token">令牌</param>
    /// <param name="minutesBeforeExpiry">过期前的分钟数，默认30分钟</param>
    /// <returns>是否即将过期</returns>
    bool IsTokenNearExpiry(string token, int minutesBeforeExpiry = 30);

    /// <summary>
    /// 生成令牌（兼容旧版本）
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <returns>访问令牌</returns>
    [Obsolete("请使用 GenerateAccessToken 方法")]
    string GenerateToken(User user);

    /// <summary>
    /// 验证令牌（兼容旧版本）
    /// </summary>
    /// <param name="token">令牌</param>
    /// <returns>是否有效</returns>
    [Obsolete("请使用 ValidateAccessToken 方法")]
    bool ValidateToken(string token);
}
