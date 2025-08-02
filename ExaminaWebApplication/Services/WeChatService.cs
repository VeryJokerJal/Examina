using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExaminaWebApplication.Services;

/// <summary>
/// 微信登录服务实现
/// </summary>
public class WeChatService : IWeChatService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    
    // 缓存键前缀
    private const string QRCODE_CACHE_PREFIX = "wechat_qrcode_";
    
    // 微信API地址
    private const string WECHAT_OAUTH_URL = "https://open.weixin.qq.com/connect/qrconnect";
    private const string WECHAT_ACCESS_TOKEN_URL = "https://api.weixin.qq.com/sns/oauth2/access_token";
    private const string WECHAT_USER_INFO_URL = "https://api.weixin.qq.com/sns/userinfo";
    private const string WECHAT_REFRESH_TOKEN_URL = "https://api.weixin.qq.com/sns/oauth2/refresh_token";
    
    // 配置参数
    private readonly string _appId;
    private readonly string _appSecret;
    private readonly string _callbackDomain;

    public WeChatService(IMemoryCache cache, ILogger<WeChatService> logger, IConfiguration configuration, HttpClient httpClient)
    {
        _cache = cache;
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
        
        // 读取微信配置
        _appId = _configuration["WeChat:AppId"] ?? throw new ArgumentException("WeChat:AppId not configured");
        _appSecret = _configuration["WeChat:AppSecret"] ?? throw new ArgumentException("WeChat:AppSecret not configured");
        _callbackDomain = _configuration["WeChat:CallbackDomain"] ?? throw new ArgumentException("WeChat:CallbackDomain not configured");
    }

    public Task<WeChatQrCodeInfo> GenerateLoginQrCodeAsync()
    {
        try
        {
            // 生成唯一的二维码标识
            var qrCodeKey = Guid.NewGuid().ToString("N");
            var state = $"examina_{qrCodeKey}";
            var redirectUri = $"https://{_callbackDomain}/api/auth/wechat/callback";

            // 构造微信授权URL
            var qrCodeUrl = $"{WECHAT_OAUTH_URL}?" +
                           $"appid={_appId}&" +
                           $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                           $"response_type=code&" +
                           $"scope=snsapi_login&" +
                           $"state={state}#wechat_redirect";

            var qrCodeInfo = new WeChatQrCodeInfo
            {
                QrCodeKey = qrCodeKey,
                QrCodeUrl = qrCodeUrl,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10) // 二维码10分钟有效
            };

            // 缓存二维码信息
            var cacheKey = QRCODE_CACHE_PREFIX + qrCodeKey;
            _cache.Set(cacheKey, new WeChatScanStatus { Status = 0, Message = "等待扫描" }, TimeSpan.FromMinutes(10));

            _logger.LogInformation("生成微信登录二维码: {QrCodeKey}", qrCodeKey);
            return Task.FromResult(qrCodeInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成微信登录二维码失败");
            throw;
        }
    }

    public Task<WeChatScanStatus> CheckQrCodeStatusAsync(string qrCodeKey)
    {
        try
        {
            var cacheKey = QRCODE_CACHE_PREFIX + qrCodeKey;

            if (_cache.TryGetValue(cacheKey, out WeChatScanStatus? status))
            {
                return Task.FromResult(status ?? new WeChatScanStatus { Status = 3, Message = "二维码已过期" });
            }

            return Task.FromResult(new WeChatScanStatus { Status = 3, Message = "二维码已过期" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查二维码状态失败: {QrCodeKey}", qrCodeKey);
            return Task.FromResult(new WeChatScanStatus { Status = 3, Message = "检查状态失败" });
        }
    }

    public async Task<WeChatUserInfo?> GetUserInfoByCodeAsync(string code)
    {
        try
        {
            // 1. 通过code获取access_token
            var tokenUrl = $"{WECHAT_ACCESS_TOKEN_URL}?" +
                          $"appid={_appId}&" +
                          $"secret={_appSecret}&" +
                          $"code={code}&" +
                          $"grant_type=authorization_code";
            
            var tokenResponse = await _httpClient.GetStringAsync(tokenUrl);
            var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenResponse);
            
            if (tokenData.TryGetProperty("errcode", out _))
            {
                var errorMsg = tokenData.TryGetProperty("errmsg", out var errMsg) ? errMsg.GetString() : "获取access_token失败";
                _logger.LogError("获取微信access_token失败: {Error}", errorMsg);
                return null;
            }
            
            var accessToken = tokenData.GetProperty("access_token").GetString();
            var openId = tokenData.GetProperty("openid").GetString();
            
            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(openId))
            {
                _logger.LogError("微信返回的access_token或openid为空");
                return null;
            }
            
            // 2. 通过access_token获取用户信息
            var userInfoUrl = $"{WECHAT_USER_INFO_URL}?" +
                             $"access_token={accessToken}&" +
                             $"openid={openId}";
            
            var userInfoResponse = await _httpClient.GetStringAsync(userInfoUrl);
            var userInfoData = JsonSerializer.Deserialize<JsonElement>(userInfoResponse);
            
            if (userInfoData.TryGetProperty("errcode", out _))
            {
                var errorMsg = userInfoData.TryGetProperty("errmsg", out var errMsg) ? errMsg.GetString() : "获取用户信息失败";
                _logger.LogError("获取微信用户信息失败: {Error}", errorMsg);
                return null;
            }
            
            var userInfo = new WeChatUserInfo
            {
                OpenId = userInfoData.GetProperty("openid").GetString() ?? "",
                UnionId = userInfoData.TryGetProperty("unionid", out var unionId) ? unionId.GetString() : null,
                Nickname = userInfoData.GetProperty("nickname").GetString() ?? "",
                AvatarUrl = userInfoData.TryGetProperty("headimgurl", out var avatar) ? avatar.GetString() : null,
                Gender = userInfoData.TryGetProperty("sex", out var sex) ? sex.GetInt32() : 0,
                Country = userInfoData.TryGetProperty("country", out var country) ? country.GetString() : null,
                Province = userInfoData.TryGetProperty("province", out var province) ? province.GetString() : null,
                City = userInfoData.TryGetProperty("city", out var city) ? city.GetString() : null
            };
            
            _logger.LogInformation("获取微信用户信息成功: {OpenId}", userInfo.OpenId);
            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通过授权码获取微信用户信息失败: {Code}", code);
            return null;
        }
    }

    public Task<WeChatUserInfo?> GetUserInfoByOpenIdAsync(string openId)
    {
        try
        {
            // 这里需要有效的access_token，实际应用中需要存储和管理access_token
            // 简化实现，返回基本信息
            var userInfo = new WeChatUserInfo
            {
                OpenId = openId,
                Nickname = $"微信用户_{openId.Substring(0, 8)}"
            };
            return Task.FromResult<WeChatUserInfo?>(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "通过OpenId获取微信用户信息失败: {OpenId}", openId);
            return Task.FromResult<WeChatUserInfo?>(null);
        }
    }

    public bool VerifySignature(string signature, string timestamp, string nonce)
    {
        try
        {
            // 微信签名验证逻辑
            var token = _configuration["WeChat:Token"] ?? "";
            var tmpArr = new[] { token, timestamp, nonce };
            Array.Sort(tmpArr);
            var tmpStr = string.Join("", tmpArr);
            
            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(tmpStr));
            var hashString = Convert.ToHexString(hashBytes).ToLower();
            
            return hashString == signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证微信签名失败");
            return false;
        }
    }

    public async Task<WeChatAccessTokenInfo?> RefreshAccessTokenAsync(string refreshToken)
    {
        try
        {
            var refreshUrl = $"{WECHAT_REFRESH_TOKEN_URL}?" +
                            $"appid={_appId}&" +
                            $"grant_type=refresh_token&" +
                            $"refresh_token={refreshToken}";
            
            var response = await _httpClient.GetStringAsync(refreshUrl);
            var data = JsonSerializer.Deserialize<JsonElement>(response);
            
            if (data.TryGetProperty("errcode", out _))
            {
                var errorMsg = data.TryGetProperty("errmsg", out var errMsg) ? errMsg.GetString() : "刷新token失败";
                _logger.LogError("刷新微信access_token失败: {Error}", errorMsg);
                return null;
            }
            
            var tokenInfo = new WeChatAccessTokenInfo
            {
                AccessToken = data.GetProperty("access_token").GetString() ?? "",
                RefreshToken = data.GetProperty("refresh_token").GetString() ?? "",
                ExpiresIn = data.GetProperty("expires_in").GetInt32(),
                Scope = data.GetProperty("scope").GetString() ?? "",
                OpenId = data.GetProperty("openid").GetString() ?? ""
            };
            
            _logger.LogInformation("刷新微信access_token成功: {OpenId}", tokenInfo.OpenId);
            return tokenInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新微信access_token失败");
            return null;
        }
    }
}
