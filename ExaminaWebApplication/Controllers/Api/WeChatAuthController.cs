using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ExaminaWebApplication.Services;

namespace ExaminaWebApplication.Controllers.Api
{
    [ApiController]
    [Route("api/auth/wechat")]
    public class WeChatAuthController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<WeChatAuthController> _logger;

        public WeChatAuthController(IMemoryCache cache, ILogger<WeChatAuthController> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// 微信OAuth回调：?code=xxx&state=examina_{qrCodeKey}
        /// 将对应二维码状态更新为“已确认”，并写入授权码，以便桌面端轮询后完成登录
        /// </summary>
        [AllowAnonymous]
        [HttpGet("callback")]
        public IActionResult Callback([FromQuery] string code, [FromQuery] string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                {
                    return BadRequest("缺少必要参数");
                }

                const string statePrefix = "examina_";
                if (!state.StartsWith(statePrefix, StringComparison.Ordinal))
                {
                    return BadRequest("无效的state参数");
                }

                string qrCodeKey = state.Substring(statePrefix.Length);
                if (string.IsNullOrWhiteSpace(qrCodeKey))
                {
                    return BadRequest("无效的二维码标识");
                }

                // 与 WeChatService 中一致的缓存键前缀
                string cacheKey = "wechat_qrcode_" + qrCodeKey;

                // 更新缓存中的二维码状态：2=已确认，并附带授权码
                WeChatScanStatus status = new WeChatScanStatus
                {
                    Status = 2,
                    Message = "已确认，等待客户端完成登录",
                    Code = code,
                    OpenId = null
                };

                // 设置一个较短的有效期，足够桌面端轮询并完成登录
                _cache.Set(cacheKey, status, TimeSpan.FromMinutes(3));

                _logger.LogInformation("微信回调成功，二维码已确认。QrKey={QrKey}", qrCodeKey);

                const string html = "<!DOCTYPE html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>微信登录成功</title><style>body{font-family:system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;background:#f7f7f7;color:#222}.card{background:#fff;border-radius:12px;box-shadow:0 6px 24px rgba(0,0,0,0.08);padding:24px 28px;text-align:center;max-width:520px}.card h1{font-size:22px;margin:0 0 12px}.card p{margin:6px 0 0;color:#555}.ok{font-size:48px;margin-bottom:8px}</style></head><body><div class=\"card\"><div class=\"ok\">✅</div><h1>微信授权已完成</h1><p>请回到考试客户端，稍候将自动完成登录。</p></div><script>setTimeout(function(){ if(window.close) window.close(); }, 1500);</script></body></html>";

                return Content(html, "text/html; charset=utf-8");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理微信授权回调失败");
                return StatusCode(500, new { message = "服务器内部错误" });
            }
        }
    }
}

