namespace ExaminaWebApplication.Services;

/// <summary>
/// 短信服务接口
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// 发送验证码短信
    /// </summary>
    /// <param name="phoneNumber">手机号码</param>
    /// <param name="code">验证码</param>
    /// <returns>是否发送成功</returns>
    Task<bool> SendVerificationCodeAsync(string phoneNumber, string code);
    
    /// <summary>
    /// 生成验证码
    /// </summary>
    /// <param name="length">验证码长度，默认6位</param>
    /// <returns>验证码</returns>
    string GenerateVerificationCode(int length = 6);
    
    /// <summary>
    /// 验证验证码
    /// </summary>
    /// <param name="phoneNumber">手机号码</param>
    /// <param name="code">验证码</param>
    /// <returns>是否验证成功</returns>
    Task<bool> VerifyCodeAsync(string phoneNumber, string code);
    
    /// <summary>
    /// 检查是否可以发送验证码（防止频繁发送）
    /// </summary>
    /// <param name="phoneNumber">手机号码</param>
    /// <returns>是否可以发送</returns>
    Task<bool> CanSendCodeAsync(string phoneNumber);
    
    /// <summary>
    /// 获取验证码剩余有效时间（秒）
    /// </summary>
    /// <param name="phoneNumber">手机号码</param>
    /// <returns>剩余有效时间，-1表示无有效验证码</returns>
    Task<int> GetCodeRemainingTimeAsync(string phoneNumber);
    
    /// <summary>
    /// 清理过期验证码
    /// </summary>
    /// <returns>清理的验证码数量</returns>
    Task<int> CleanupExpiredCodesAsync();
}
