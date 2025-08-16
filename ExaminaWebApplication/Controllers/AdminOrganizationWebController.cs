using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Organization.Requests;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 管理员网页端组织管理控制器
/// </summary>
[Authorize(Policy = "AdminPolicy")]
public class AdminOrganizationWebController : Controller
{
    private readonly IOrganizationService _organizationService;
    private readonly IInvitationCodeService _invitationCodeService;
    private readonly ILogger<AdminOrganizationWebController> _logger;
    private readonly ApplicationDbContext _context;

    public AdminOrganizationWebController(
        IOrganizationService organizationService,
        IInvitationCodeService invitationCodeService,
        ILogger<AdminOrganizationWebController> logger,
        ApplicationDbContext context)
    {
        _organizationService = organizationService;
        _invitationCodeService = invitationCodeService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 组织管理首页
    /// </summary>
    [HttpGet]
    [Route("Admin/Organization")]
    public async Task<IActionResult> Index()
    {
        try
        {
            List<OrganizationDto> organizations = await _organizationService.GetOrganizationsAsync(includeInactive: false);
            return View(organizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织列表时发生错误");
            TempData["ErrorMessage"] = "获取组织列表失败，请稍后重试";
            return View(new List<OrganizationDto>());
        }
    }

    /// <summary>
    /// 创建组织页面
    /// </summary>
    [HttpGet]
    [Route("Admin/Organization/Create")]
    public IActionResult Create()
    {
        return View(new CreateOrganizationRequest());
    }

    /// <summary>
    /// 创建组织
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrganizationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            // 获取当前管理员用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int adminUserId))
            {
                TempData["ErrorMessage"] = "无法获取用户信息";
                return View(request);
            }

            OrganizationDto organization = await _organizationService.CreateOrganizationAsync(request, adminUserId);

            _logger.LogInformation("管理员 {AdminUserId} 创建组织成功: {OrganizationName}", adminUserId, organization.Name);
            TempData["SuccessMessage"] = $"组织 '{organization.Name}' 创建成功";
            
            return RedirectToAction(nameof(Details), new { id = organization.Id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("创建组织失败: {Message}", ex.Message);
            ModelState.AddModelError("", ex.Message);
            return View(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建组织时发生错误");
            ModelState.AddModelError("", "创建组织失败，请稍后重试");
            return View(request);
        }
    }

    /// <summary>
    /// 组织详情页面
    /// </summary>
    [HttpGet]
    [Route("Admin/Organization/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            OrganizationDto? organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "组织不存在";
                return RedirectToAction(nameof(Index));
            }

            // 获取邀请码列表
            var invitationCodes = await _invitationCodeService.GetOrganizationInvitationCodesAsync(id, includeInactive: true);
            
            // 获取成员列表
            var members = await _organizationService.GetOrganizationMembersAsync(id, includeInactive: false);

            // 添加调试日志
            _logger.LogInformation("组织 {OrganizationId} 的成员数量: {MemberCount}", id, members.Count);
            foreach (var member in members)
            {
                _logger.LogInformation("成员: {Username}, 手机号: {Phone}", member.StudentUsername, member.StudentPhoneNumber ?? "未设置");
            }

            var viewModel = new OrganizationDetailsViewModel
            {
                Organization = organization,
                InvitationCodes = invitationCodes.Select(ic => new InvitationCodeDto
                {
                    Id = ic.Id,
                    Code = ic.Code,
                    OrganizationId = ic.OrganizationId,
                    OrganizationName = organization.Name,
                    CreatedAt = ic.CreatedAt,
                    ExpiresAt = ic.ExpiresAt,
                    IsActive = ic.IsActive,
                    UsageCount = ic.UsageCount,
                    MaxUsage = ic.MaxUsage
                }).ToList(),
                Members = members
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织详情时发生错误: {OrganizationId}", id);
            TempData["ErrorMessage"] = "获取组织详情失败，请稍后重试";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// 编辑组织页面
    /// </summary>
    [HttpGet]
    [Route("Admin/Organization/Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            OrganizationDto? organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "组织不存在";
                return RedirectToAction(nameof(Index));
            }

            var request = new UpdateOrganizationRequest
            {
                Name = organization.Name
            };

            ViewBag.OrganizationId = id;
            return View(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织编辑页面时发生错误: {OrganizationId}", id);
            TempData["ErrorMessage"] = "获取组织信息失败，请稍后重试";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// 更新组织信息
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateOrganizationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                ViewBag.OrganizationId = id;
                return View(request);
            }

            OrganizationDto? organization = await _organizationService.UpdateOrganizationAsync(id, request.Name);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "组织不存在";
                return RedirectToAction(nameof(Index));
            }

            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("管理员 {AdminUserId} 更新组织 {OrganizationId} 成功", userIdClaim, id);

            TempData["SuccessMessage"] = "组织信息更新成功";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("更新组织失败: {Message}", ex.Message);
            ModelState.AddModelError("", ex.Message);
            ViewBag.OrganizationId = id;
            return View(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新组织时发生错误: {OrganizationId}", id);
            ModelState.AddModelError("", "更新组织失败，请稍后重试");
            ViewBag.OrganizationId = id;
            return View(request);
        }
    }

    /// <summary>
    /// 生成邀请码
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/{id}/CreateInvitationCode")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInvitationCode(int id, CreateInvitationCodeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "请求参数无效";
                return RedirectToAction(nameof(Details), new { id });
            }

            // 检查组织是否存在
            OrganizationDto? organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "组织不存在";
                return RedirectToAction(nameof(Index));
            }

            // 创建邀请码
            var invitationCode = await _invitationCodeService.CreateInvitationCodeAsync(
                id, request.ExpiresAt, request.MaxUsage);

            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("管理员 {AdminUserId} 为组织 {OrganizationId} 生成邀请码 {InvitationCode} 成功",
                userIdClaim, id, invitationCode.Code);

            TempData["SuccessMessage"] = $"邀请码 '{invitationCode.Code}' 生成成功";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成邀请码时发生错误: {OrganizationId}", id);
            TempData["ErrorMessage"] = "生成邀请码失败，请稍后重试";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    /// <summary>
    /// 停用邀请码
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/DeactivateInvitationCode/{invitationCodeId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateInvitationCode(int invitationCodeId, int organizationId)
    {
        try
        {
            bool success = await _invitationCodeService.DeactivateInvitationCodeAsync(invitationCodeId);
            if (!success)
            {
                TempData["ErrorMessage"] = "邀请码不存在";
            }
            else
            {
                string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("管理员 {AdminUserId} 停用邀请码 {InvitationCodeId} 成功", userIdClaim, invitationCodeId);
                TempData["SuccessMessage"] = "邀请码已停用";
            }

            return RedirectToAction(nameof(Details), new { id = organizationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用邀请码时发生错误: {InvitationCodeId}", invitationCodeId);
            TempData["ErrorMessage"] = "停用邀请码失败，请稍后重试";
            return RedirectToAction(nameof(Details), new { id = organizationId });
        }
    }

    /// <summary>
    /// 停用组织
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/Deactivate/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            bool success = await _organizationService.DeactivateOrganizationAsync(id);
            if (!success)
            {
                TempData["ErrorMessage"] = "组织不存在";
            }
            else
            {
                string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("管理员 {AdminUserId} 停用组织 {OrganizationId} 成功", userIdClaim, id);
                TempData["SuccessMessage"] = "组织已停用";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用组织时发生错误: {OrganizationId}", id);
            TempData["ErrorMessage"] = "停用组织失败，请稍后重试";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    /// <summary>
    /// 更新成员手机号
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/UpdateMemberPhone")]
    public async Task<IActionResult> UpdateMemberPhone([FromBody] UpdateMemberPhoneRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "请求参数无效" });
            }

            // 查找用户
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.StudentId);
            if (user == null)
            {
                return NotFound(new { message = "用户不存在" });
            }

            // 检查手机号是否已被其他用户使用（如果不为空）
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                bool phoneExists = await _context.Users
                    .AnyAsync(u => u.PhoneNumber == request.PhoneNumber && u.Id != request.StudentId);
                if (phoneExists)
                {
                    return BadRequest(new { message = "该手机号已被其他用户使用" });
                }
            }

            // 更新手机号
            user.PhoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? null : request.PhoneNumber;
            await _context.SaveChangesAsync();

            _logger.LogInformation("管理员更新用户手机号成功: 用户ID: {UserId}, 新手机号: {PhoneNumber}",
                request.StudentId, request.PhoneNumber ?? "空");

            return Ok(new { message = "手机号更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户手机号失败: 用户ID: {UserId}", request.StudentId);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 批量更新成员手机号
    /// </summary>
    [HttpPost]
    [Route("Admin/Organization/BatchUpdateMemberPhone")]
    public async Task<IActionResult> BatchUpdateMemberPhone([FromBody] BatchUpdateMemberPhoneRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "请求参数无效" });
            }

            int successCount = 0;
            int failureCount = 0;
            List<string> errors = new();

            foreach (PhoneEntry entry in request.PhoneEntries)
            {
                try
                {
                    // 根据用户名查找用户
                    User? user = await _context.Users.FirstOrDefaultAsync(u => u.Username == entry.Username);
                    if (user == null)
                    {
                        errors.Add($"用户 {entry.Username} 不存在");
                        failureCount++;
                        continue;
                    }

                    // 检查是否需要覆盖现有手机号
                    if (!request.OverwriteExisting && !string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        errors.Add($"用户 {entry.Username} 已有手机号，跳过");
                        failureCount++;
                        continue;
                    }

                    // 检查手机号是否已被其他用户使用
                    bool phoneExists = await _context.Users
                        .AnyAsync(u => u.PhoneNumber == entry.Phone && u.Id != user.Id);
                    if (phoneExists)
                    {
                        errors.Add($"手机号 {entry.Phone} 已被其他用户使用");
                        failureCount++;
                        continue;
                    }

                    // 更新手机号
                    user.PhoneNumber = entry.Phone;
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量更新手机号时处理用户 {Username} 失败", entry.Username);
                    errors.Add($"处理用户 {entry.Username} 时发生错误");
                    failureCount++;
                }
            }

            // 保存所有更改
            if (successCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("批量更新手机号完成: 成功 {SuccessCount}, 失败 {FailureCount}",
                successCount, failureCount);

            return Ok(new
            {
                message = "批量更新完成",
                successCount,
                failureCount,
                errors = errors.Take(10).ToList() // 只返回前10个错误
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量更新用户手机号失败");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 调试方法：检查组织成员数据
    /// </summary>
    [HttpGet]
    [Route("Admin/Organization/Debug/{id}")]
    public async Task<IActionResult> DebugMembers(int id)
    {
        try
        {
            // 直接查询数据库
            var rawMembers = await _context.StudentOrganizations
                .Include(so => so.Student)
                .Include(so => so.Organization)
                .Include(so => so.InvitationCode)
                .Where(so => so.OrganizationId == id)
                .ToListAsync();

            var debugInfo = new
            {
                OrganizationId = id,
                TotalMembersInDb = rawMembers.Count,
                ActiveMembers = rawMembers.Count(m => m.IsActive),
                Members = rawMembers.Select(m => new
                {
                    Id = m.Id,
                    StudentId = m.StudentId,
                    StudentUsername = m.Student?.Username ?? "NULL",
                    StudentRealName = m.Student?.RealName ?? "NULL",
                    StudentPhoneNumber = m.Student?.PhoneNumber ?? "NULL",
                    IsActive = m.IsActive,
                    JoinedAt = m.JoinedAt
                }).ToList()
            };

            return Json(debugInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调试组织成员数据失败: {OrganizationId}", id);
            return Json(new { error = ex.Message });
        }
    }
}

/// <summary>
/// 组织详情视图模型
/// </summary>
public class OrganizationDetailsViewModel
{
    public OrganizationDto Organization { get; set; } = new();
    public List<InvitationCodeDto> InvitationCodes { get; set; } = new();
    public List<StudentOrganizationDto> Members { get; set; } = new();
}

/// <summary>
/// 更新组织请求模型
/// </summary>
public class UpdateOrganizationRequest
{
    /// <summary>
    /// 组织名称
    /// </summary>
    [Required(ErrorMessage = "组织名称不能为空")]
    [StringLength(100, ErrorMessage = "组织名称长度不能超过100个字符")]
    [Display(Name = "组织名称")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 更新成员手机号请求模型
/// </summary>
public class UpdateMemberPhoneRequest
{
    /// <summary>
    /// 学生用户ID
    /// </summary>
    [Required(ErrorMessage = "学生ID不能为空")]
    public int StudentId { get; set; }

    /// <summary>
    /// 手机号（可为空表示清除）
    /// </summary>
    [Phone(ErrorMessage = "手机号格式不正确")]
    [StringLength(20, ErrorMessage = "手机号长度不能超过20个字符")]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// 批量更新成员手机号请求模型
/// </summary>
public class BatchUpdateMemberPhoneRequest
{
    /// <summary>
    /// 手机号条目列表
    /// </summary>
    [Required(ErrorMessage = "手机号条目不能为空")]
    public List<PhoneEntry> PhoneEntries { get; set; } = new();

    /// <summary>
    /// 是否覆盖现有手机号
    /// </summary>
    public bool OverwriteExisting { get; set; }
}

/// <summary>
/// 手机号条目
/// </summary>
public class PhoneEntry
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    [Required(ErrorMessage = "手机号不能为空")]
    [Phone(ErrorMessage = "手机号格式不正确")]
    public string Phone { get; set; } = string.Empty;
}
