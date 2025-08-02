using System;

namespace Examina.Models;

/// <summary>
/// 用户角色枚举
/// </summary>
public enum UserRole
{
    /// <summary>
    /// 学生
    /// </summary>
    Student = 1,

    /// <summary>
    /// 教师
    /// </summary>
    Teacher = 2,

    /// <summary>
    /// 管理员
    /// </summary>
    Administrator = 3
}

/// <summary>
/// 登录类型枚举
/// </summary>
public enum LoginType
{
    /// <summary>
    /// 用户名密码登录
    /// </summary>
    Credentials = 1,

    /// <summary>
    /// 手机短信验证码登录
    /// </summary>
    SmsCode = 2,

    /// <summary>
    /// 微信扫码登录
    /// </summary>
    WeChat = 3
}

/// <summary>
/// 登录模式枚举（UI用）
/// </summary>
public enum LoginMode
{
    /// <summary>
    /// 用户名密码登录
    /// </summary>
    Credentials,

    /// <summary>
    /// 手机短信验证码登录
    /// </summary>
    SmsCode,

    /// <summary>
    /// 微信扫码登录
    /// </summary>
    WeChat
}

/// <summary>
/// 用户信息模型
/// </summary>
public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsFirstLogin { get; set; }
    public bool AllowMultipleDevices { get; set; }
    public int MaxDeviceCount { get; set; }
}

/// <summary>
/// 设备信息模型
/// </summary>
public class DeviceInfo
{
    public int Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? OperatingSystem { get; set; }
    public string? BrowserInfo { get; set; }
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsTrusted { get; set; }
}

/// <summary>
/// 设备绑定请求模型
/// </summary>
public class DeviceBindRequest
{
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? OperatingSystem { get; set; }
    public string? BrowserInfo { get; set; }
}
