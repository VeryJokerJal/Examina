using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Organization.Requests;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

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

    public AdminOrganizationWebController(
        IOrganizationService organizationService,
        IInvitationCodeService invitationCodeService,
        ILogger<AdminOrganizationWebController> logger)
    {
        _organizationService = organizationService;
        _invitationCodeService = invitationCodeService;
        _logger = logger;
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
                Name = organization.Name,
                Description = organization.Description
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

            OrganizationDto? organization = await _organizationService.UpdateOrganizationAsync(id, request.Name, request.Description);
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
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 组织描述
    /// </summary>
    [StringLength(500, ErrorMessage = "组织描述长度不能超过500个字符")]
    public string? Description { get; set; }
}
