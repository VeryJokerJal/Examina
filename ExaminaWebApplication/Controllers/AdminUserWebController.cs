using System.Security.Claims;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Admin.Requests;
using ExaminaWebApplication.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 管理员网页端用户管理（创建用户）
/// </summary>
[Authorize(Policy = "AdminPolicy")]
public class AdminUserWebController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IInvitationCodeService _invitationCodeService;
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<AdminUserWebController> _logger;

    public AdminUserWebController(
        ApplicationDbContext context,
        IInvitationCodeService invitationCodeService,
        IOrganizationService organizationService,
        ILogger<AdminUserWebController> logger)
    {
        _context = context;
        _invitationCodeService = invitationCodeService;
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// 创建用户页面
    /// </summary>
    [HttpGet]
    [Route("Admin/User/Create")]
    public IActionResult Create()
    {
        return View(new CreateUserRequest());
    }

    /// <summary>
    /// 提交创建用户
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("Admin/User/Create")]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            // 基础重复性检查
            bool usernameExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
            if (usernameExists)
            {
                ModelState.AddModelError("Username", "该用户名已存在");
                return View(request);
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "该邮箱已被使用");
                return View(request);
            }

            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                bool phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == request.PhoneNumber);
                if (phoneExists)
                {
                    ModelState.AddModelError("PhoneNumber", "该手机号已被使用");
                    return View(request);
                }
            }

            // 老师邀请码校验：必填 + 必须属于学校组织 + 可用
            if (request.Role == UserRole.Teacher)
            {
                if (string.IsNullOrWhiteSpace(request.InvitationCode))
                {
                    ModelState.AddModelError("InvitationCode", "教师必须填写邀请码");
                    return View(request);
                }

                ExaminaWebApplication.Models.Organization.InvitationCode? invitation = await _invitationCodeService.ValidateInvitationCodeAsync(request.InvitationCode);
                if (invitation == null)
                {
                    ModelState.AddModelError("InvitationCode", "邀请码不存在");
                    return View(request);
                }

                if (!_invitationCodeService.IsInvitationCodeAvailable(invitation))
                {
                    ModelState.AddModelError("InvitationCode", "邀请码不可用或已过期/达到上限");
                    return View(request);
                }

                // 移除组织类型限制，所有组织都可以接受教师
            }

            // 学生邀请码可选：如果填写则需要验证有效性（可用即可，不强制学校类型）
            if (request.Role == UserRole.Student && !string.IsNullOrWhiteSpace(request.InvitationCode))
            {
                ExaminaWebApplication.Models.Organization.InvitationCode? invitation = await _invitationCodeService.ValidateInvitationCodeAsync(request.InvitationCode);
                if (invitation == null || !_invitationCodeService.IsInvitationCodeAvailable(invitation))
                {
                    ModelState.AddModelError("InvitationCode", "学生填写的邀请码无效");
                    return View(request);
                }
            }

            // 创建用户
            User newUser = new()
            {
                Username = request.Username,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                RealName = request.RealName,
                StudentId = request.StudentId,
                IsFirstLogin = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                AllowMultipleDevices = request.Role == UserRole.Student ? false : true,
                MaxDeviceCount = request.Role == UserRole.Student ? 1 : 5
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // 如填写邀请码，加入对应组织（学生/教师）
            if (!string.IsNullOrWhiteSpace(request.InvitationCode))
            {
                JoinOrganizationResult joinResult = await _organizationService.JoinOrganizationAsync(newUser.Id, request.Role, request.InvitationCode);
                if (!joinResult.Success)
                {
                    // 虽然不阻止账号创建，但提示加入组织失败信息
                    TempData["WarningMessage"] = $"用户创建成功，但加入组织失败：{joinResult.ErrorMessage}";
                }
            }

            _logger.LogInformation("管理员创建{Role}用户成功：{Username}", request.Role, request.Username);
            TempData["SuccessMessage"] = $"用户 '{request.Username}' 创建成功";
            return RedirectToAction(nameof(Create));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "管理员创建用户失败");
            ModelState.AddModelError("", "创建用户失败，请稍后重试");
            return View(request);
        }
    }
}

