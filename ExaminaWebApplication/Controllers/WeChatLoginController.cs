using ExaminaWebApplication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 微信登录页面控制器
/// </summary>
[AllowAnonymous]
public class WeChatLoginController : Controller
{
    private readonly IWeChatService _weChatService;
    private readonly ILogger<WeChatLoginController> _logger;
    private readonly IConfiguration _configuration;

    public WeChatLoginController(
        IWeChatService weChatService,
        ILogger<WeChatLoginController> logger,
        IConfiguration configuration)
    {
        _weChatService = weChatService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// 微信扫码登录页面
    /// </summary>
    [HttpGet]
    [Route("wechat-login")]
    public IActionResult Index()
    {
        try
        {
            _logger.LogInformation("显示微信扫码登录页面");
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "显示微信扫码登录页面失败");
            return View("Error");
        }
    }

    /// <summary>
    /// 微信登录配置测试页面
    /// </summary>
    [HttpGet]
    [Route("wechat-login/test")]
    public IActionResult Test()
    {
        try
        {
            _logger.LogInformation("显示微信登录配置测试页面");
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "显示微信登录配置测试页面失败");
            return View("Error");
        }
    }

    /// <summary>
    /// 获取微信登录配置（用于JS SDK）
    /// </summary>
    [HttpGet]
    [Route("api/auth/wechat/config")]
    public async Task<IActionResult> GetWeChatConfig()
    {
        try
        {
            _logger.LogInformation("开始获取微信登录配置");

            // 检查微信服务是否可用
            if (_weChatService == null)
            {
                _logger.LogError("微信服务未注册");
                return Json(new
                {
                    success = false,
                    message = "微信服务不可用"
                });
            }

            // 生成微信登录二维码信息
            WeChatQrCodeInfo qrCodeInfo = await _weChatService.GenerateLoginQrCodeAsync();
            _logger.LogInformation("生成二维码信息成功: {QrCodeKey}", qrCodeInfo.QrCodeKey);

            // 从配置中获取微信应用信息
            string appId = GetWeChatAppId();
            string redirectUri = GetWeChatRedirectUri();

            _logger.LogInformation("微信配置 - AppId: {AppId}, RedirectUri: {RedirectUri}",
                string.IsNullOrEmpty(appId) ? "未配置" : appId[..8] + "***", redirectUri);

            if (string.IsNullOrEmpty(appId))
            {
                _logger.LogError("微信AppId未配置");
                return Json(new
                {
                    success = false,
                    message = "微信AppId未配置"
                });
            }

            var config = new
            {
                success = true,
                data = new
                {
                    wxLoginConfig = new
                    {
                        appid = appId,
                        redirect_uri = redirectUri,
                        state = $"examina_{qrCodeInfo.QrCodeKey}",
                        scope = "snsapi_login"
                    },
                    qrCodeKey = qrCodeInfo.QrCodeKey,
                    expiresAt = qrCodeInfo.ExpiresAt
                }
            };

            _logger.LogInformation("生成微信登录配置成功: {QrCodeKey}", qrCodeInfo.QrCodeKey);
            return Json(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取微信登录配置失败");
            return Json(new
            {
                success = false,
                message = $"获取微信登录配置失败: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// 检查微信登录状态（用于前端轮询）
    /// </summary>
    [HttpGet]
    [Route("api/auth/wechat/check-status")]
    public async Task<IActionResult> CheckStatus([FromQuery] string state)
    {
        try
        {
            if (string.IsNullOrEmpty(state) || !state.StartsWith("examina_"))
            {
                return Json(new
                {
                    success = false,
                    message = "无效的状态参数"
                });
            }

            string qrCodeKey = state["examina_".Length..];
            WeChatScanStatus status = await _weChatService.CheckQrCodeStatusAsync(qrCodeKey);

            var result = new
            {
                success = true,
                data = new
                {
                    status = GetStatusString(status.Status),
                    message = status.Message,
                    code = status.Code,
                    hasCallback = !string.IsNullOrEmpty(status.Code)
                }
            };

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查微信登录状态失败: {State}", state);
            return Json(new
            {
                success = false,
                message = "检查登录状态失败"
            });
        }
    }

    /// <summary>
    /// 获取微信应用ID
    /// </summary>
    private string GetWeChatAppId()
    {
        // 从配置中获取微信应用ID
        return _configuration.GetValue<string>("WeChat:AppId") ?? "";
    }

    /// <summary>
    /// 获取微信回调地址
    /// </summary>
    private string GetWeChatRedirectUri()
    {
        // 从配置中获取回调域名
        string callbackDomain = _configuration.GetValue<string>("WeChat:CallbackDomain") ?? "localhost";
        return $"https://{callbackDomain}/api/auth/wechat/callback";
    }

    /// <summary>
    /// 将数字状态转换为字符串
    /// </summary>
    private static string GetStatusString(int status)
    {
        return status switch
        {
            0 => "waiting",
            1 => "scanned",
            2 => "confirmed",
            3 => "expired",
            _ => "error"
        };
    }


}
