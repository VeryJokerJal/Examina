using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using TencentCloud.Sms.V20210111;
using TencentCloud.Sms.V20210111.Models;

namespace ExaminaWebApplication.Services;

/// <summary>
/// 腾讯云短信服务实现
/// 提供验证码发送、验证、频率限制等功能
/// </summary>
public class SmsService : ISmsService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<SmsService> _logger;
    private readonly IConfiguration _configuration;
    private readonly SmsClient? _smsClient;

    // 缓存键前缀
    private const string CODE_CACHE_PREFIX = "sms_code_";
    private const string SEND_TIME_CACHE_PREFIX = "sms_send_time_";
    private const string DAILY_COUNT_CACHE_PREFIX = "sms_daily_count_";
    private const string ATTEMPT_COUNT_CACHE_PREFIX = "sms_attempt_count_";

    // 配置常量
    private readonly string _sdkAppId;
    private readonly string _templateId;
    private readonly string _signName;
    private readonly string _region;
    private readonly bool _enabled;
    private readonly int _codeValidMinutes;
    private readonly int _sendIntervalSeconds;
    private readonly int _maxDailyCount;
    private readonly int _maxAttemptCount;
    private readonly int _retryCount;
    private readonly int _retryDelayMs;

    // 手机号验证正则
    private static readonly Regex PhoneRegex = new(@"^1[3-9]\d{9}$", RegexOptions.Compiled);

    public SmsService(IMemoryCache cache, ILogger<SmsService> logger, IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;
        _configuration = configuration;

        // 读取SMS服务启用状态
        _enabled = _configuration.GetValue("TencentSms:Enabled", true);

        // 读取基础配置
        _sdkAppId = _configuration["TencentSms:SDKAppID"] ?? "";
        _templateId = _configuration["TencentSms:TemplateID"] ?? "";
        _signName = _configuration["TencentSms:SignName"] ?? "";
        _region = _configuration["TencentSms:Region"] ?? "ap-beijing";

        // 读取限制配置
        _codeValidMinutes = _configuration.GetValue("TencentSms:CodeValidMinutes", 5);
        _sendIntervalSeconds = _configuration.GetValue("TencentSms:SendIntervalSeconds", 60);
        _maxDailyCount = _configuration.GetValue("TencentSms:MaxDailyCount", 10);
        _maxAttemptCount = _configuration.GetValue("TencentSms:MaxAttemptCount", 5);
        _retryCount = _configuration.GetValue("TencentSms:RetryCount", 3);
        _retryDelayMs = _configuration.GetValue("TencentSms:RetryDelayMs", 1000);

        // 如果SMS服务未启用，记录日志并跳过客户端初始化
        if (!_enabled)
        {
            _logger.LogInformation("SMS服务已禁用，将使用模拟模式");
            _smsClient = null;
            return;
        }

        // 验证必需配置
        if (string.IsNullOrEmpty(_sdkAppId) || string.IsNullOrEmpty(_templateId) ||
            string.IsNullOrEmpty(_signName))
        {
            _logger.LogWarning("SMS服务配置不完整，将使用模拟模式");
            _smsClient = null;
            return;
        }

        string? secretId = _configuration["TencentSms:SecretId"];
        string? secretKey = _configuration["TencentSms:SecretKey"];

        if (string.IsNullOrEmpty(secretId) || string.IsNullOrEmpty(secretKey))
        {
            _logger.LogWarning("SMS服务密钥配置不完整，将使用模拟模式");
            _smsClient = null;
            return;
        }

        try
        {
            // 初始化腾讯云SMS客户端
            Credential credential = new()
            {
                SecretId = secretId,
                SecretKey = secretKey
            };

            ClientProfile clientProfile = new();
            HttpProfile httpProfile = new()
            {
                Endpoint = "sms.tencentcloudapi.com",
                Timeout = 30 // 30秒超时
            };
            clientProfile.HttpProfile = httpProfile;

            _smsClient = new SmsClient(credential, _region, clientProfile);
            _logger.LogInformation("SMS服务初始化成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS服务初始化失败，将使用模拟模式");
            _smsClient = null;
        }
    }

    public async Task<bool> SendVerificationCodeAsync(string phoneNumber, string code)
    {
        try
        {
            // 输入验证
            if (!ValidatePhoneNumber(phoneNumber))
            {
                _logger.LogWarning("无效的手机号格式: {PhoneNumber}", phoneNumber);
                return false;
            }

            if (!ValidateVerificationCode(code))
            {
                _logger.LogWarning("无效的验证码格式: {Code}", code);
                return false;
            }

            // 检查发送权限
            (bool CanSend, string Reason) = await CheckSendPermissionAsync(phoneNumber);
            if (!CanSend)
            {
                _logger.LogWarning("手机号 {PhoneNumber} 发送验证码被限制: {Reason}", phoneNumber, Reason);
                return false;
            }

            // 如果SMS服务未启用，使用模拟模式
            if (!_enabled || _smsClient == null)
            {
                return await SendMockVerificationCodeAsync(phoneNumber, code);
            }

            // 发送真实短信
            bool success = await SendRealSmsWithRetryAsync(phoneNumber, code);

            if (success)
            {
                // 缓存验证码和发送记录
                await CacheVerificationCodeAsync(phoneNumber, code);
                await UpdateSendRecordsAsync(phoneNumber);

                _logger.LogInformation("验证码发送成功，手机号: {PhoneNumber}", phoneNumber);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送验证码异常，手机号: {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public string GenerateVerificationCode(int length = 6)
    {
        // 参数验证
        if (length is < 4 or > 8)
        {
            throw new ArgumentException("验证码长度必须在4-8位之间", nameof(length));
        }

        // 使用加密安全的随机数生成器
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[length];
        rng.GetBytes(bytes);

        StringBuilder code = new(length);
        for (int i = 0; i < length; i++)
        {
            _ = code.Append(bytes[i] % 10);
        }

        return code.ToString();
    }

    public Task<bool> VerifyCodeAsync(string phoneNumber, string code)
    {
        try
        {
            // 输入验证
            if (!ValidatePhoneNumber(phoneNumber))
            {
                _logger.LogWarning("验证码验证失败，无效的手机号: {PhoneNumber}", phoneNumber);
                return Task.FromResult(false);
            }

            if (!ValidateVerificationCode(code))
            {
                _logger.LogWarning("验证码验证失败，无效的验证码格式: {Code}", code);
                return Task.FromResult(false);
            }

            string cacheKey = CODE_CACHE_PREFIX + phoneNumber;

            if (_cache.TryGetValue(cacheKey, out VerificationCodeData? codeData) && codeData != null)
            {
                // 检查验证码是否过期
                if (DateTime.UtcNow > codeData.ExpiresAt)
                {
                    _cache.Remove(cacheKey);
                    _logger.LogWarning("验证码已过期，手机号: {PhoneNumber}", phoneNumber);
                    return Task.FromResult(false);
                }

                // 验证码匹配
                if (codeData.Code == code)
                {
                    // 验证成功，删除缓存的验证码
                    _cache.Remove(cacheKey);

                    // 清除验证尝试次数
                    string attemptCountKey = ATTEMPT_COUNT_CACHE_PREFIX + phoneNumber;
                    _cache.Remove(attemptCountKey);

                    _logger.LogInformation("验证码验证成功，手机号: {PhoneNumber}", phoneNumber);
                    return Task.FromResult(true);
                }
                else
                {
                    // 增加验证尝试次数
                    IncrementAttemptCount(phoneNumber);
                }
            }

            _logger.LogWarning("验证码验证失败，手机号: {PhoneNumber}", phoneNumber);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证验证码异常，手机号: {PhoneNumber}", phoneNumber);
            return Task.FromResult(false);
        }
    }

    public Task<bool> CanSendCodeAsync(string phoneNumber)
    {
        try
        {
            string sendTimeKey = SEND_TIME_CACHE_PREFIX + phoneNumber;

            if (_cache.TryGetValue(sendTimeKey, out DateTime lastSendTime))
            {
                TimeSpan timeSinceLastSend = DateTime.UtcNow - lastSendTime;
                return Task.FromResult(timeSinceLastSend.TotalSeconds >= _sendIntervalSeconds);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查发送权限异常，手机号: {PhoneNumber}", phoneNumber);
            return Task.FromResult(false);
        }
    }

    public Task<int> GetCodeRemainingTimeAsync(string phoneNumber)
    {
        try
        {
            // 输入验证
            if (!ValidatePhoneNumber(phoneNumber))
            {
                return Task.FromResult(-1);
            }

            string cacheKey = CODE_CACHE_PREFIX + phoneNumber;

            if (_cache.TryGetValue(cacheKey, out VerificationCodeData? codeData) && codeData != null)
            {
                DateTime now = DateTime.UtcNow;

                // 检查是否已过期
                if (now > codeData.ExpiresAt)
                {
                    _cache.Remove(cacheKey);
                    return Task.FromResult(-1);
                }

                // 计算剩余时间（秒）
                int remainingTime = (int)(codeData.ExpiresAt - now).TotalSeconds;
                return Task.FromResult(Math.Max(0, remainingTime));
            }

            return Task.FromResult(-1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验证码剩余时间异常，手机号: {PhoneNumber}", phoneNumber);
            return Task.FromResult(-1);
        }
    }

    public Task<int> CleanupExpiredCodesAsync()
    {
        // MemoryCache会自动清理过期项，这里返回0
        // 如果使用数据库存储验证码，则需要实现清理逻辑
        return Task.FromResult(0);
    }

    #region 私有辅助方法

    /// <summary>
    /// 验证手机号格式
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns>是否有效</returns>
    private static bool ValidatePhoneNumber(string phoneNumber)
    {
        return !string.IsNullOrWhiteSpace(phoneNumber) && PhoneRegex.IsMatch(phoneNumber);
    }

    /// <summary>
    /// 验证验证码格式
    /// </summary>
    /// <param name="code">验证码</param>
    /// <returns>是否有效</returns>
    private static bool ValidateVerificationCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        // 验证码应该是4-8位数字
        return code.Length >= 4 && code.Length <= 8 && code.All(char.IsDigit);
    }

    /// <summary>
    /// 检查发送权限
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns>检查结果</returns>
    private async Task<(bool CanSend, string Reason)> CheckSendPermissionAsync(string phoneNumber)
    {
        try
        {
            // 检查发送间隔
            if (!await CanSendCodeAsync(phoneNumber))
            {
                return (false, "发送过于频繁");
            }

            // 检查每日发送次数
            string dailyCountKey = DAILY_COUNT_CACHE_PREFIX + phoneNumber + "_" + DateTime.UtcNow.ToString("yyyyMMdd");
            if (_cache.TryGetValue(dailyCountKey, out int dailyCount) && dailyCount >= _maxDailyCount)
            {
                return (false, "每日发送次数超限");
            }

            // 检查验证尝试次数
            string attemptCountKey = ATTEMPT_COUNT_CACHE_PREFIX + phoneNumber;
            if (_cache.TryGetValue(attemptCountKey, out int attemptCount) && attemptCount >= _maxAttemptCount)
            {
                return (false, "验证尝试次数超限");
            }

            return (true, "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查发送权限异常，手机号: {PhoneNumber}", phoneNumber);
            return (false, "系统异常");
        }
    }

    /// <summary>
    /// 发送模拟验证码（开发环境或SMS服务未启用时使用）
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="code">验证码</param>
    /// <returns>是否成功</returns>
    private async Task<bool> SendMockVerificationCodeAsync(string phoneNumber, string code)
    {
        try
        {
            // 模拟发送延迟
            await Task.Delay(500);

            // 缓存验证码
            await CacheVerificationCodeAsync(phoneNumber, code);
            await UpdateSendRecordsAsync(phoneNumber);

            _logger.LogInformation("模拟发送验证码成功，手机号: {PhoneNumber}, 验证码: {Code}", phoneNumber, code);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "模拟发送验证码异常，手机号: {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    /// <summary>
    /// 带重试机制发送真实短信
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="code">验证码</param>
    /// <returns>是否成功</returns>
    private async Task<bool> SendRealSmsWithRetryAsync(string phoneNumber, string code)
    {
        for (int attempt = 1; attempt <= _retryCount; attempt++)
        {
            try
            {
                bool success = await SendRealSmsAsync(phoneNumber, code);
                if (success)
                {
                    return true;
                }

                if (attempt < _retryCount)
                {
                    _logger.LogWarning("短信发送失败，第 {Attempt} 次重试，手机号: {PhoneNumber}", attempt, phoneNumber);
                    await Task.Delay(_retryDelayMs * attempt); // 递增延迟
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "短信发送异常，第 {Attempt} 次尝试，手机号: {PhoneNumber}", attempt, phoneNumber);

                if (attempt < _retryCount)
                {
                    await Task.Delay(_retryDelayMs * attempt);
                }
            }
        }

        _logger.LogError("短信发送失败，已重试 {RetryCount} 次，手机号: {PhoneNumber}", _retryCount, phoneNumber);
        return false;
    }

    /// <summary>
    /// 发送真实短信
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="code">验证码</param>
    /// <returns>是否成功</returns>
    private async Task<bool> SendRealSmsAsync(string phoneNumber, string code)
    {
        if (_smsClient == null)
        {
            throw new InvalidOperationException("SMS客户端未初始化");
        }

        // 构造请求
        SendSmsRequest request = new()
        {
            PhoneNumberSet = [$"+86{phoneNumber}"],
            SmsSdkAppId = _sdkAppId,
            TemplateId = _templateId,
            SignName = _signName,
            TemplateParamSet = [code]
        };

        // 发送短信
        SendSmsResponse response = await _smsClient.SendSms(request);

        if (response.SendStatusSet != null && response.SendStatusSet.Length > 0)
        {
            SendStatus status = response.SendStatusSet[0];
            if (status.Code == "Ok")
            {
                _logger.LogInformation("腾讯云短信发送成功，手机号: {PhoneNumber}, SerialNo: {SerialNo}",
                    phoneNumber, status.SerialNo);
                return true;
            }
            else
            {
                _logger.LogError("腾讯云短信发送失败，手机号: {PhoneNumber}, 错误码: {Code}, 错误信息: {Message}",
                    phoneNumber, status.Code, status.Message);
                return false;
            }
        }

        _logger.LogError("腾讯云短信发送失败，手机号: {PhoneNumber}, 响应为空", phoneNumber);
        return false;
    }

    /// <summary>
    /// 缓存验证码
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="code">验证码</param>
    /// <returns></returns>
    private Task CacheVerificationCodeAsync(string phoneNumber, string code)
    {
        try
        {
            string cacheKey = CODE_CACHE_PREFIX + phoneNumber;
            VerificationCodeData codeData = new()
            {
                Code = code,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_codeValidMinutes)
            };

            _ = _cache.Set(cacheKey, codeData, TimeSpan.FromMinutes(_codeValidMinutes));
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存验证码异常，手机号: {PhoneNumber}", phoneNumber);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 更新发送记录
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    /// <returns></returns>
    private Task UpdateSendRecordsAsync(string phoneNumber)
    {
        try
        {
            DateTime now = DateTime.UtcNow;

            // 更新发送时间
            string sendTimeKey = SEND_TIME_CACHE_PREFIX + phoneNumber;
            _ = _cache.Set(sendTimeKey, now, TimeSpan.FromSeconds(_sendIntervalSeconds));

            // 更新每日发送次数
            string dailyCountKey = DAILY_COUNT_CACHE_PREFIX + phoneNumber + "_" + now.ToString("yyyyMMdd");
            int currentCount = _cache.TryGetValue(dailyCountKey, out int count) ? count : 0;
            _ = _cache.Set(dailyCountKey, currentCount + 1, TimeSpan.FromDays(1));

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新发送记录异常，手机号: {PhoneNumber}", phoneNumber);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 增加验证尝试次数
    /// </summary>
    /// <param name="phoneNumber">手机号</param>
    private void IncrementAttemptCount(string phoneNumber)
    {
        try
        {
            string attemptCountKey = ATTEMPT_COUNT_CACHE_PREFIX + phoneNumber;
            int currentCount = _cache.TryGetValue(attemptCountKey, out int count) ? count : 0;
            _ = _cache.Set(attemptCountKey, currentCount + 1, TimeSpan.FromHours(1)); // 1小时后重置
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "增加验证尝试次数异常，手机号: {PhoneNumber}", phoneNumber);
        }
    }

    #endregion

    #region 内部数据结构

    /// <summary>
    /// 验证码数据
    /// </summary>
    private class VerificationCodeData
    {
        public string Code { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    #endregion
}
