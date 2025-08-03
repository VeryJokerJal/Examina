using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Services;
using Microsoft.AspNetCore.Authorization;

namespace ExaminaWebApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ApplicationDbContext context, IJwtService jwtService, ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "用户名和密码不能为空" });
            }

            // 查找用户（支持用户名、邮箱、手机号登录）
            var user = await _context.Users
                .FirstOrDefaultAsync(u => 
                    (u.Username == request.Username || 
                     u.Email == request.Username || 
                     u.PhoneNumber == request.Username) && 
                    u.IsActive);

            if (user == null)
            {
                return Unauthorized(new { message = "用户不存在或已被禁用" });
            }

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "密码错误" });
            }

            // 更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 生成JWT令牌
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(10080); // 7天

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = new UserInfo
                {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Role = user.Role,
                    IsFirstLogin = user.IsFirstLogin,
                    AllowMultipleDevices = user.AllowMultipleDevices,
                    MaxDeviceCount = user.MaxDeviceCount
                }
            };

            _logger.LogInformation("用户 {Username} 登录成功", user.Username);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录过程中发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    [HttpPost("wechat-login")]
    public async Task<ActionResult<LoginResponse>> WeChatLogin([FromBody] WeChatLoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.QrCode))
            {
                return BadRequest(new { message = "二维码信息不能为空" });
            }

            // 这里应该调用微信API验证二维码并获取用户信息
            // 暂时使用模拟逻辑
            await Task.Delay(1000); // 模拟网络请求

            // 模拟微信用户信息
            var wechatOpenId = $"wx_{DateTime.Now.Ticks}";
            
            // 查找或创建微信用户
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.WeChatOpenId == wechatOpenId);

            if (user == null)
            {
                // 创建新的微信用户
                user = new User
                {
                    Username = $"微信用户_{DateTime.Now.Ticks % 10000}",
                    Email = $"wechat_{DateTime.Now.Ticks}@examina.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                    WeChatOpenId = wechatOpenId,
                    IsFirstLogin = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // 更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 生成JWT令牌
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(10080); // 7天

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                User = new UserInfo
                {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Role = user.Role,
                    IsFirstLogin = user.IsFirstLogin,
                    AllowMultipleDevices = user.AllowMultipleDevices,
                    MaxDeviceCount = user.MaxDeviceCount
                }
            };

            _logger.LogInformation("微信用户 {Username} 登录成功", user.Username);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "微信登录过程中发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    [HttpGet("validate")]
    [Authorize]
    public async Task<ActionResult> ValidateToken()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "无效的令牌" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "用户不存在或已被禁用" });
            }

            return Ok(new { message = "令牌有效", userId = user.Id, username = user.Username });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证令牌时发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        // JWT是无状态的，客户端删除令牌即可实现登出
        // 这里可以记录登出日志或执行其他清理操作
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null)
        {
            _logger.LogInformation("用户 {UserId} 登出", userIdClaim.Value);
        }

        return Ok(new { message = "登出成功" });
    }

    [HttpGet("qrcode")]
    public ActionResult GetQrCode()
    {
        try
        {
            // 生成二维码数据
            var qrData = $"examina_login_{DateTime.Now.Ticks}";
            var qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={qrData}";
            
            return Ok(new { qrCode = qrData, qrCodeUrl = qrCodeUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成二维码时发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }
}
