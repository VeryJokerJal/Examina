using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 管理员和教师网页端登录控制器
/// </summary>
public class AdminLoginController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly SessionService _sessionService;
    private readonly ILogger<AdminLoginController> _logger;

    public AdminLoginController(
        ApplicationDbContext context,
        SessionService sessionService,
        ILogger<AdminLoginController> logger)
    {
        _context = context;
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// 登录页面
    /// </summary>
    [HttpGet]
    [Route("Admin/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        // 如果已经登录，重定向到相应页面
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToRoleBasedHome();
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new AdminLoginViewModel());
    }

    /// <summary>
    /// 处理登录请求
    /// </summary>
    [HttpPost]
    [Route("Admin/Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model, string? returnUrl = null)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 查找用户（支持用户名、邮箱登录）
            User? user = await _context.Users
                .FirstOrDefaultAsync(u => 
                    (u.Username == model.Username || u.Email == model.Username) 
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
                return View(model);
            }

            // 创建Claims
            List<Claim> claims = new()
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

            // 更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("{Role} {Username} 登录成功", user.Role, user.Username);

            // 根据返回URL或用户角色重定向
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToRoleBasedHome(user.Role);
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
    [Route("Admin/Logout")]
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
    [HttpGet]
    [Route("Admin/AccessDenied")]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// 根据用户角色重定向到相应首页
    /// </summary>
    private IActionResult RedirectToRoleBasedHome(UserRole? role = null)
    {
        UserRole userRole = role ?? GetCurrentUserRole();

        return userRole switch
        {
            UserRole.Administrator => Redirect("/Admin/Organization"),
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
        return Enum.TryParse<UserRole>(roleString, out UserRole role) ? role : UserRole.Student;
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

/// <summary>
/// 管理员登录视图模型
/// </summary>
public class AdminLoginViewModel
{
    /// <summary>
    /// 用户名或邮箱
    /// </summary>
    [Required(ErrorMessage = "请输入用户名或邮箱")]
    [Display(Name = "用户名/邮箱")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required(ErrorMessage = "请输入密码")]
    [DataType(DataType.Password)]
    [Display(Name = "密码")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 记住我
    /// </summary>
    [Display(Name = "记住我")]
    public bool RememberMe { get; set; }
}
