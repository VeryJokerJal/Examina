using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Services;
using ExaminaWebApplication.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 系统登录控制器
/// </summary>
public class LoginController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly ILogger<LoginController> _logger;

    public LoginController(
        ApplicationDbContext context,
        ISessionService sessionService,
        ILogger<LoginController> logger)
    {
        _context = context;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// 登录页面
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route("Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        // 如果已经登录，重定向到相应页面
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToRoleBasedHome();
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new AdminLoginViewModel());
    }

    /// <summary>
    /// 处理登录请求
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [Route("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model, string? returnUrl = null)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/AdminLogin/Login.cshtml", model);
            }

            // 查找用户（支持手机号、邮箱登录）
            User? user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    ((u.PhoneNumber != null && u.PhoneNumber == model.Identifier) || u.Email == model.Identifier)
                    && u.IsActive
                    && (u.Role == UserRole.Administrator || u.Role == UserRole.Teacher));

            if (user == null)
            {
                ModelState.AddModelError("", "用户不存在或权限不足");
                return View(model);
            }

            // 验证密码
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "密码错误");
                return View("~/Views/AdminLogin/Login.cshtml", model);
            }

            // 创建Claims
            List<Claim> claims =
            [
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("IsFirstLogin", user.IsFirstLogin.ToString())
            ];

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
            }

            if (!string.IsNullOrEmpty(user.RealName))
            {
                claims.Add(new Claim("RealName", user.RealName));
            }

            ClaimsIdentity claimsIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal claimsPrincipal = new(claimsIdentity);

            // 设置Cookie认证属性
            AuthenticationProperties authProperties = new()
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            // 登录用户
            await HttpContext.SignInAsync(
                "Cookie",
                claimsPrincipal,
                authProperties);

            // 创建会话记录
            string sessionToken = HttpContext.Session.Id;
            DateTime expiresAt = authProperties.ExpiresUtc?.DateTime ?? DateTime.UtcNow.AddHours(8);

            _ = await _sessionService.CreateSessionAsync(
                user.Id,
                sessionToken,
                SessionType.Cookie,
                null, // Cookie认证不绑定设备
                null, // Cookie认证不使用刷新令牌
                GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                await GetLocationFromIp(GetClientIpAddress()),
                expiresAt);

            // 更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            _ = await _context.SaveChangesAsync();

            _logger.LogInformation("{Role} {Username} 登录成功", user.Role, user.Username);

            // 根据返回URL或用户角色重定向
            return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToRoleBasedHome(user.Role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录过程中发生错误");
            ModelState.AddModelError("", "登录失败，请稍后重试");
            return View(model);
        }
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    [HttpPost]
    [Route("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        try
        {
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null)
            {
                _logger.LogInformation("用户 {UserId} 退出登录", userIdClaim);
            }

            await HttpContext.SignOutAsync("Cookie");
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "退出登录时发生错误");
            return RedirectToAction(nameof(Login));
        }
    }

    /// <summary>
    /// 访问被拒绝页面
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [Route("AccessDenied")]
    public IActionResult AccessDenied()
    {
        return View("~/Views/AdminLogin/AccessDenied.cshtml");
    }

    /// <summary>
    /// 根据用户角色重定向到相应首页
    /// </summary>
    private IActionResult RedirectToRoleBasedHome(UserRole? role = null)
    {
        UserRole userRole = role ?? GetCurrentUserRole();

        return userRole switch
        {
            UserRole.Administrator => RedirectToAction("Index", "Home"),
            UserRole.Teacher => Redirect("/Teacher/Organization"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    /// <summary>
    /// 获取当前用户角色
    /// </summary>
    private UserRole GetCurrentUserRole()
    {
        string? roleString = User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse(roleString, out UserRole role) ? role : UserRole.Student;
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// 根据IP获取位置信息（模拟实现）
    /// </summary>
    private async Task<string> GetLocationFromIp(string ipAddress)
    {
        // 这里可以集成真实的IP地理位置服务
        await Task.Delay(1);
        return "未知位置";
    }
}
