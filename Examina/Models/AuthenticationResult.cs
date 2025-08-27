namespace Examina.Models;

/// <summary>
/// 认证结果模型
/// </summary>
public class AuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
    public UserInfo? User { get; set; }
    public bool RequireDeviceBinding { get; set; }
    public LoginResponse? LoginResponse { get; set; }

    /// <summary>
    /// 错误类型
    /// </summary>
    public AuthenticationErrorType ErrorType { get; set; } = AuthenticationErrorType.Unknown;

    /// <summary>
    /// 设备限制信息（当ErrorType为DeviceLimitExceeded时使用）
    /// </summary>
    public DeviceLimitInfo? DeviceLimitInfo { get; set; }

    /// <summary>
    /// 是否可以重试
    /// </summary>
    public bool CanRetry { get; set; } = true;

    /// <summary>
    /// 建议的操作
    /// </summary>
    public string? SuggestedAction { get; set; }
}

/// <summary>
/// 认证错误类型
/// </summary>
public enum AuthenticationErrorType
{
    /// <summary>
    /// 未知错误
    /// </summary>
    Unknown,

    /// <summary>
    /// 网络错误
    /// </summary>
    NetworkError,

    /// <summary>
    /// 凭据无效（用户名密码错误）
    /// </summary>
    InvalidCredentials,

    /// <summary>
    /// 短信验证码无效
    /// </summary>
    InvalidSmsCode,

    /// <summary>
    /// 设备数量超出限制
    /// </summary>
    DeviceLimitExceeded,

    /// <summary>
    /// 账户被锁定
    /// </summary>
    AccountLocked,

    /// <summary>
    /// 服务器错误
    /// </summary>
    ServerError,

    /// <summary>
    /// 令牌过期
    /// </summary>
    TokenExpired,

    /// <summary>
    /// 设备未授权
    /// </summary>
    DeviceUnauthorized
}

/// <summary>
/// 设备限制信息
/// </summary>
public class DeviceLimitInfo
{
    /// <summary>
    /// 当前绑定的设备数量
    /// </summary>
    public int CurrentDeviceCount { get; set; }

    /// <summary>
    /// 允许的最大设备数量
    /// </summary>
    public int MaxDeviceCount { get; set; }

    /// <summary>
    /// 设备管理页面URL
    /// </summary>
    public string? DeviceManagementUrl { get; set; }
}

/// <summary>
/// 登录请求模型
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public LoginType LoginType { get; set; } = LoginType.Credentials;
    public string? SmsCode { get; set; }
    public string? QrCode { get; set; }
    public DeviceBindRequest? DeviceInfo { get; set; }
}

/// <summary>
/// 短信验证码登录请求模型
/// </summary>
public class SmsLoginRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string SmsCode { get; set; } = string.Empty;
    public DeviceBindRequest? DeviceInfo { get; set; }
}

/// <summary>
/// 登录响应模型
/// </summary>
public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserInfo User { get; set; } = new();
    public bool RequireDeviceBinding { get; set; }
}

/// <summary>
/// 刷新令牌请求模型
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? DeviceFingerprint { get; set; }
}

/// <summary>
/// 刷新令牌响应模型
/// </summary>
public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// 用户信息完善请求模型
/// </summary>
public class CompleteUserInfoRequest
{
    /// <summary>
    /// 用户名（可修改）
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 密码（可选设置）
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 手机号码（必填）
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 微信OpenId（用于绑定微信）
    /// </summary>
    public string? WeChatOpenId { get; set; }
}

/// <summary>
/// 持久化登录数据模型
/// </summary>
public class PersistentLoginData
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
    /// 保存时间
    /// </summary>
    public DateTime SavedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 是否需要设备绑定
    /// </summary>
    public bool RequireDeviceBinding { get; set; }
}

/// <summary>
/// 更新用户资料请求模型（后端专用）
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }
}
