using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Services;
using System.Security.Claims;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 管理员/教师Cookie认证控制器
/// </summary>
[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly IDeviceService _deviceService;
    private readonly ILogger<AdminAuthController> _logger;

    public AdminAuthController(
        ApplicationDbContext context,
        ISessionService sessionService,
        IDeviceService deviceService,
        ILogger<AdminAuthController> logger)
    {
        _context = context;
        _sessionService = sessionService;
        _deviceService = deviceService;
        _logger = logger;
    }

    /// <summary>
    /// 管理员/教师登录
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] AdminLoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "用户名和密码不能为空" });
            }

            // 查找用户（支持用户名、邮箱登录）
            var user = await _context.Users
                .FirstOrDefaultAsync(u => 
                    (u.Username == request.Username || u.Email == request.Username) 
                    && u.IsActive 
                    && (u.Role == UserRole.Administrator || u.Role == UserRole.Teacher));

            if (user == null)
            {
                return Unauthorized(new { message = "用户不存在或权限不足" });
            }

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "密码错误" });
            }

            // 创建Claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("IsFirstLogin", user.IsFirstLogin.ToString())
            };

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // 设置Cookie认证属性
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = request.RememberMe,
                ExpiresUtc = request.RememberMe 
                    ? DateTimeOffset.UtcNow.AddDays(7) 
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            // 登录用户
            await HttpContext.SignInAsync(
                "Cookie",
                claimsPrincipal,
                authProperties);

            // 创建会话记录
            var sessionToken = HttpContext.Session.Id;
            var expiresAt = authProperties.ExpiresUtc?.DateTime ?? DateTime.UtcNow.AddHours(8);
            
            await _sessionService.CreateSessionAsync(
                user.Id,
                sessionToken,
                SessionType.Cookie,
                null, // Cookie认证不绑定设备
                null, // Cookie认证不使用刷新令牌
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                await GetLocationFromIp(GetClientIpAddress()),
                expiresAt);

            // 更新用户最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var response = new AdminLoginResponse
            {
                User = new UserInfo
                {
                    Id = user.Id.ToString(),
                    Username = user.Username,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    AvatarUrl = user.AvatarUrl,
                    Role = user.Role,
                    IsFirstLogin = user.IsFirstLogin,
                    AllowMultipleDevices = user.AllowMultipleDevices,
                    MaxDeviceCount = user.MaxDeviceCount
                },
                ExpiresAt = expiresAt
            };

            _logger.LogInformation("管理员/教师 {Username} 登录成功，角色: {Role}", user.Username, user.Role);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "管理员/教师登录失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 验证登录状态
    /// </summary>
    [HttpGet("validate")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult> ValidateLogin()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "未登录或登录已过期" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive || 
                (user.Role != UserRole.Administrator && user.Role != UserRole.Teacher))
            {
                return Unauthorized(new { message = "用户不存在、已被禁用或权限不足" });
            }

            return Ok(new 
            { 
                message = "登录有效", 
                userId = user.Id, 
                username = user.Username,
                role = user.Role.ToString(),
                realName = user.RealName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证登录状态时发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("profile")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult<UserInfo>> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "未登录或登录已过期" });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "用户不存在或已被禁用" });
            }

            var userInfo = new UserInfo
            {
                Id = user.Id.ToString(),
                Username = user.Username,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                IsFirstLogin = user.IsFirstLogin,
                AllowMultipleDevices = user.AllowMultipleDevices,
                MaxDeviceCount = user.MaxDeviceCount
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户信息失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 登出
    /// </summary>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            
            // 结束会话记录
            var sessionToken = HttpContext.Session.Id;
            await _sessionService.EndSessionAsync(sessionToken);

            // 登出用户
            await HttpContext.SignOutAsync("Cookie");

            if (userIdClaim != null)
            {
                _logger.LogInformation("管理员/教师 {UserId} 登出", userIdClaim.Value);
            }

            return Ok(new { message = "登出成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 管理员解绑用户设备
    /// </summary>
    [HttpDelete("devices/{deviceId}")]
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Roles = "Administrator")]
    public async Task<ActionResult> UnbindDevice(int deviceId)
    {
        try
        {
            var success = await _deviceService.AdminUnbindDeviceAsync(deviceId);
            if (success)
            {
                _logger.LogInformation("管理员解绑设备成功，设备ID: {DeviceId}", deviceId);
                return Ok(new { message = "设备解绑成功" });
            }
            else
            {
                return NotFound(new { message = "设备不存在" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "管理员解绑设备失败，设备ID: {DeviceId}", deviceId);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string GetClientIpAddress()
    {
        var ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        return ipAddress ?? "Unknown";
    }

    /// <summary>
    /// 根据IP地址获取地理位置（简化实现）
    /// </summary>
    private async Task<string> GetLocationFromIp(string ipAddress)
    {
        // 这里可以集成第三方IP地理位置服务
        // 简化实现，返回默认位置
        return await Task.FromResult("中国");
    }

    #endregion
}

/// <summary>
/// 管理员登录请求模型
/// </summary>
public class AdminLoginRequest
{
    /// <summary>
    /// 用户名（支持用户名、邮箱）
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否记住登录状态
    /// </summary>
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// 管理员登录响应模型
/// </summary>
public class AdminLoginResponse
{
    /// <summary>
    /// 用户信息
    /// </summary>
    public UserInfo User { get; set; } = new();
    
    /// <summary>
    /// 会话过期时间
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
