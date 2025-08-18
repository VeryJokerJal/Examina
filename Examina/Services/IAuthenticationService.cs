using Examina.Models;

namespace Examina.Services;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// 用户信息更新事件
    /// </summary>
    event EventHandler<UserInfo?>? UserInfoUpdated;
    /// <summary>
    /// 用户名密码登录
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns>认证结果</returns>
    Task<AuthenticationResult> LoginWithCredentialsAsync(string username, string password);

    /// <summary>
    /// 短信验证码登录
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="smsCode">短信验证码</param>
    /// <returns>认证结果</returns>
    Task<AuthenticationResult> LoginWithSmsAsync(string phoneNumber, string smsCode);

    /// <summary>
    /// 微信扫码登录
    /// </summary>
    /// <param name="qrCode">二维码标识</param>
    /// <returns>认证结果</returns>
    Task<AuthenticationResult> LoginWithWeChatAsync(string qrCode);

    /// <summary>
    /// 发送短信验证码
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendSmsCodeAsync(string phoneNumber);

    /// <summary>
    /// 获取微信登录二维码
    /// </summary>
    /// <returns>二维码信息</returns>
    Task<WeChatQrCodeInfo?> GetWeChatQrCodeAsync();

    /// <summary>
    /// 检查微信二维码状态
    /// </summary>
    /// <param name="qrCodeKey">二维码标识</param>
    /// <returns>扫描状态</returns>
    Task<WeChatScanStatus?> CheckWeChatStatusAsync(string qrCodeKey);

    /// <summary>
    /// 验证访问令牌
    /// </summary>
    /// <param name="token">访问令牌</param>
    /// <returns>是否有效</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <returns>新的认证结果</returns>
    Task<AuthenticationResult> RefreshTokenAsync();

    /// <summary>
    /// 登出
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// 获取用户设备列表
    /// </summary>
    /// <returns>设备列表</returns>
    Task<List<DeviceInfo>> GetUserDevicesAsync();

    /// <summary>
    /// 完善用户信息
    /// </summary>
    /// <param name="request">用户信息完善请求</param>
    /// <returns>更新后的用户信息</returns>
    Task<UserInfo?> CompleteUserInfoAsync(CompleteUserInfoRequest request);

    /// <summary>
    /// 检查用户是否需要完善信息
    /// </summary>
    /// <returns>是否需要完善信息</returns>
    bool RequiresUserInfoCompletion();

    /// <summary>
    /// 更新用户资料
    /// </summary>
    /// <param name="request">更新用户资料请求</param>
    /// <returns>是否更新成功</returns>
    Task<bool> UpdateUserProfileAsync(UpdateUserProfileRequest request);

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="request">修改密码请求</param>
    /// <returns>是否修改成功</returns>
    Task<bool> ChangePasswordAsync(ChangePasswordRequest request);

    /// <summary>
    /// 获取当前访问令牌
    /// </summary>
    /// <returns>访问令牌</returns>
    Task<string?> GetAccessTokenAsync();

    /// <summary>
    /// 刷新用户信息
    /// </summary>
    /// <returns>是否刷新成功</returns>
    Task<bool> RefreshUserInfoAsync();

    /// <summary>
    /// 保存登录信息到本地存储
    /// </summary>
    /// <param name="loginResponse">登录响应</param>
    /// <returns>是否保存成功</returns>
    Task<bool> SaveLoginDataAsync(LoginResponse loginResponse);

    /// <summary>
    /// 从本地存储加载登录信息
    /// </summary>
    /// <returns>持久化登录数据，如果不存在或无效则返回null</returns>
    Task<PersistentLoginData?> LoadLoginDataAsync();

    /// <summary>
    /// 清除本地存储的登录信息
    /// </summary>
    /// <returns>是否清除成功</returns>
    Task<bool> ClearLoginDataAsync();

    /// <summary>
    /// 自动验证本地存储的登录信息
    /// </summary>
    /// <returns>验证结果</returns>
    Task<AuthenticationResult> AutoAuthenticateAsync();

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>刷新结果</returns>
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// 是否已认证
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 当前用户信息
    /// </summary>
    UserInfo? CurrentUser { get; }

    /// <summary>
    /// 当前访问令牌
    /// </summary>
    string? CurrentAccessToken { get; }

    /// <summary>
    /// 当前刷新令牌
    /// </summary>
    string? CurrentRefreshToken { get; }

    /// <summary>
    /// 令牌过期时间
    /// </summary>
    DateTime? TokenExpiresAt { get; }

    /// <summary>
    /// 是否需要刷新令牌
    /// </summary>
    bool NeedsTokenRefresh { get; }
}

/// <summary>
/// 微信二维码信息
/// </summary>
public class WeChatQrCodeInfo
{
    public string QrCodeKey { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// 微信扫描状态
/// </summary>
public class WeChatScanStatus
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? OpenId { get; set; }
}
