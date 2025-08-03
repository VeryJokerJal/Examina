using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Models;

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
/// 用户实体模型
/// </summary>
public class User
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 微信OpenId，用于微信登录
    /// </summary>
    [StringLength(100)]
    public string? WeChatOpenId { get; set; }



    /// <summary>
    /// 用户角色
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Student;

    /// <summary>
    /// 真实姓名
    /// </summary>
    [StringLength(50)]
    public string? RealName { get; set; }

    /// <summary>
    /// 学号（学生）或工号（教师/管理员）
    /// </summary>
    [StringLength(50)]
    public string? StudentId { get; set; }

    /// <summary>
    /// 是否首次登录
    /// </summary>
    public bool IsFirstLogin { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 是否允许多设备登录（主要针对学生）
    /// </summary>
    public bool AllowMultipleDevices { get; set; } = false;

    /// <summary>
    /// 最大设备数量
    /// </summary>
    public int MaxDeviceCount { get; set; } = 1;

    /// <summary>
    /// 用户设备列表
    /// </summary>
    public virtual ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();

    /// <summary>
    /// 用户会话列表
    /// </summary>
    public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
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
/// 登录请求模型
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名（支持用户名、邮箱、手机号）
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 登录类型
    /// </summary>
    public LoginType LoginType { get; set; } = LoginType.Credentials;

    /// <summary>
    /// 短信验证码（短信登录时使用）
    /// </summary>
    public string? SmsCode { get; set; }

    /// <summary>
    /// 微信二维码信息（微信登录时使用）
    /// </summary>
    public string? QrCode { get; set; }

    /// <summary>
    /// 设备指纹信息
    /// </summary>
    public DeviceBindRequest? DeviceInfo { get; set; }
}

/// <summary>
/// 登录响应模型
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfo User { get; set; } = new();

    /// <summary>
    /// 是否需要设备绑定
    /// </summary>
    public bool RequireDeviceBinding { get; set; }
}

/// <summary>
/// 用户信息模型
/// </summary>
public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsFirstLogin { get; set; }
    public bool AllowMultipleDevices { get; set; }
    public int MaxDeviceCount { get; set; }
}

/// <summary>
/// 短信验证码请求模型
/// </summary>
public class SmsCodeRequest
{
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// 短信验证码登录请求模型
/// </summary>
public class SmsLoginRequest
{
    /// <summary>
    /// 手机号
    /// </summary>
    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// 短信验证码
    /// </summary>
    [Required]
    public string SmsCode { get; set; } = string.Empty;

    /// <summary>
    /// 设备指纹信息
    /// </summary>
    public DeviceBindRequest? DeviceInfo { get; set; }
}

/// <summary>
/// 用户信息完善请求模型
/// </summary>
public class CompleteUserInfoRequest
{
    /// <summary>
    /// 用户名（可修改）
    /// </summary>
    [StringLength(50)]
    public string? Username { get; set; }

    /// <summary>
    /// 密码（可选设置）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 微信OpenId（用于绑定微信）
    /// </summary>
    [StringLength(100)]
    public string? WeChatOpenId { get; set; }
}

/// <summary>
/// 微信登录请求模型
/// </summary>
public class WeChatLoginRequest
{
    [Required]
    public string QrCode { get; set; } = string.Empty;

    public DeviceBindRequest? DeviceInfo { get; set; }
}

/// <summary>
/// 更新用户资料请求模型
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    [StringLength(50)]
    public string? Username { get; set; }
}

/// <summary>
/// 修改密码请求模型
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// 当前密码
    /// </summary>
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}
