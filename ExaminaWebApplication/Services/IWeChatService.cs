namespace ExaminaWebApplication.Services;

/// <summary>
/// 微信登录服务接口
/// </summary>
public interface IWeChatService
{
    /// <summary>
    /// 生成微信登录二维码
    /// </summary>
    /// <returns>二维码信息</returns>
    Task<WeChatQrCodeInfo> GenerateLoginQrCodeAsync();
    
    /// <summary>
    /// 检查二维码扫描状态
    /// </summary>
    /// <param name="qrCodeKey">二维码标识</param>
    /// <returns>扫描状态信息</returns>
    Task<WeChatScanStatus> CheckQrCodeStatusAsync(string qrCodeKey);
    
    /// <summary>
    /// 通过授权码获取用户信息
    /// </summary>
    /// <param name="code">授权码</param>
    /// <returns>微信用户信息</returns>
    Task<WeChatUserInfo?> GetUserInfoByCodeAsync(string code);
    
    /// <summary>
    /// 通过OpenId获取用户信息
    /// </summary>
    /// <param name="openId">微信OpenId</param>
    /// <returns>微信用户信息</returns>
    Task<WeChatUserInfo?> GetUserInfoByOpenIdAsync(string openId);
    
    /// <summary>
    /// 验证微信签名
    /// </summary>
    /// <param name="signature">签名</param>
    /// <param name="timestamp">时间戳</param>
    /// <param name="nonce">随机数</param>
    /// <returns>是否验证成功</returns>
    bool VerifySignature(string signature, string timestamp, string nonce);
    
    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <param name="refreshToken">刷新令牌</param>
    /// <returns>新的访问令牌信息</returns>
    Task<WeChatAccessTokenInfo?> RefreshAccessTokenAsync(string refreshToken);
}

/// <summary>
/// 微信二维码信息
/// </summary>
public class WeChatQrCodeInfo
{
    /// <summary>
    /// 二维码标识
    /// </summary>
    public string QrCodeKey { get; set; } = string.Empty;
    
    /// <summary>
    /// 二维码URL
    /// </summary>
    public string QrCodeUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// 微信扫描状态
/// </summary>
public class WeChatScanStatus
{
    /// <summary>
    /// 状态码：0-未扫描，1-已扫描未确认，2-已确认，3-已过期
    /// </summary>
    public int Status { get; set; }
    
    /// <summary>
    /// 状态描述
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 授权码（状态为2时有效）
    /// </summary>
    public string? Code { get; set; }
    
    /// <summary>
    /// 用户OpenId（状态为2时有效）
    /// </summary>
    public string? OpenId { get; set; }
}

/// <summary>
/// 微信用户信息
/// </summary>
public class WeChatUserInfo
{
    /// <summary>
    /// 微信OpenId
    /// </summary>
    public string OpenId { get; set; } = string.Empty;
    
    /// <summary>
    /// 微信UnionId
    /// </summary>
    public string? UnionId { get; set; }
    
    /// <summary>
    /// 昵称
    /// </summary>
    public string Nickname { get; set; } = string.Empty;
    
    /// <summary>
    /// 头像URL
    /// </summary>
    public string? AvatarUrl { get; set; }
    
    /// <summary>
    /// 性别：1-男，2-女，0-未知
    /// </summary>
    public int Gender { get; set; }
    
    /// <summary>
    /// 国家
    /// </summary>
    public string? Country { get; set; }
    
    /// <summary>
    /// 省份
    /// </summary>
    public string? Province { get; set; }
    
    /// <summary>
    /// 城市
    /// </summary>
    public string? City { get; set; }
}

/// <summary>
/// 微信访问令牌信息
/// </summary>
public class WeChatAccessTokenInfo
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
    /// 过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }
    
    /// <summary>
    /// 作用域
    /// </summary>
    public string Scope { get; set; } = string.Empty;
    
    /// <summary>
    /// 微信OpenId
    /// </summary>
    public string OpenId { get; set; } = string.Empty;
}
