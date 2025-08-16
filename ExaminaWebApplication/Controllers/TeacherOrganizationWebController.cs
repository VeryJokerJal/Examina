using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Organization.Requests;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 教师网页端组织控制器
/// </summary>
[Authorize(Policy = "TeacherPolicy")]
public class TeacherOrganizationWebController : Controller
{
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<TeacherOrganizationWebController> _logger;

    public TeacherOrganizationWebController(
        IOrganizationService organizationService,
        ILogger<TeacherOrganizationWebController> logger)
    {
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// 教师组织管理首页
    /// </summary>
    [HttpGet]
    [Route("Teacher/Organization")]
    public async Task<IActionResult> Index()
    {
        try
        {
            // 获取当前教师用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int teacherId))
            {
                TempData["ErrorMessage"] = "无法获取用户信息";
                return View(new TeacherOrganizationViewModel());
            }

            // 获取教师已加入的组织
            List<StudentOrganizationDto> joinedOrganizations = await _organizationService.GetUserOrganizationsAsync(teacherId);

            // 获取可用的学校组织（教师只能加入学校组织）
            List<OrganizationDto> allOrganizations = await _organizationService.GetOrganizationsAsync(includeInactive: false);
            List<OrganizationDto> availableSchoolOrganizations = allOrganizations
                .Where(o => o.Type == OrganizationType.School)
                .ToList();

            var viewModel = new TeacherOrganizationViewModel
            {
                JoinedOrganizations = joinedOrganizations,
                AvailableSchoolOrganizations = availableSchoolOrganizations
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取教师组织信息时发生错误");
            TempData["ErrorMessage"] = "获取组织信息失败，请稍后重试";
            return View(new TeacherOrganizationViewModel());
        }
    }

    /// <summary>
    /// 加入组织页面
    /// </summary>
    [HttpGet]
    [Route("Teacher/Organization/Join")]
    public IActionResult Join()
    {
        return View(new JoinOrganizationRequest());
    }

    /// <summary>
    /// 通过邀请码加入组织
    /// </summary>
    [HttpPost]
    [Route("Teacher/Organization/Join")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(JoinOrganizationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            // 获取当前教师用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int teacherId))
            {
                ModelState.AddModelError("", "无法获取用户信息");
                return View(request);
            }

            // 验证用户角色
            string? roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim != "Teacher")
            {
                ModelState.AddModelError("", "只有教师可以使用此功能");
                return View(request);
            }

            // 加入组织
            JoinOrganizationResult result = await _organizationService.JoinOrganizationAsync(
                teacherId, UserRole.Teacher, request.InvitationCode);

            if (!result.Success)
            {
                _logger.LogWarning("教师 {TeacherId} 加入组织失败: {ErrorMessage}", teacherId, result.ErrorMessage);
                ModelState.AddModelError("", result.ErrorMessage);
                return View(request);
            }

            _logger.LogInformation("教师 {TeacherId} 通过邀请码 {InvitationCode} 加入组织成功",
                teacherId, request.InvitationCode);

            TempData["SuccessMessage"] = $"成功加入组织 '{result.StudentOrganization?.OrganizationName}'";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "教师加入组织时发生错误");
            ModelState.AddModelError("", "加入组织失败，请稍后重试");
            return View(request);
        }
    }

    /// <summary>
    /// 退出组织
    /// </summary>
    [HttpPost]
    [Route("Teacher/Organization/Leave/{organizationId}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int organizationId)
    {
        try
        {
            // 获取当前教师用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int teacherId))
            {
                TempData["ErrorMessage"] = "无法获取用户信息";
                return RedirectToAction(nameof(Index));
            }

            // 检查教师是否在该组织中
            bool isInOrganization = await _organizationService.IsUserInOrganizationAsync(teacherId, organizationId);
            if (!isInOrganization)
            {
                TempData["ErrorMessage"] = "您不在该组织中";
                return RedirectToAction(nameof(Index));
            }

            // 退出组织
            bool success = await _organizationService.LeaveOrganizationAsync(teacherId, organizationId);
            if (!success)
            {
                TempData["ErrorMessage"] = "退出组织失败";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("教师 {TeacherId} 退出组织 {OrganizationId} 成功", teacherId, organizationId);
            TempData["SuccessMessage"] = "已成功退出组织";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "教师退出组织时发生错误: {OrganizationId}", organizationId);
            TempData["ErrorMessage"] = "退出组织失败，请稍后重试";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// 组织详情页面
    /// </summary>
    [HttpGet]
    [Route("Teacher/Organization/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            // 获取当前教师用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int teacherId))
            {
                TempData["ErrorMessage"] = "无法获取用户信息";
                return RedirectToAction(nameof(Index));
            }

            // 检查教师是否在该组织中
            bool isInOrganization = await _organizationService.IsUserInOrganizationAsync(teacherId, id);
            if (!isInOrganization)
            {
                TempData["ErrorMessage"] = "您不在该组织中，无法查看详情";
                return RedirectToAction(nameof(Index));
            }

            OrganizationDto? organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "组织不存在";
                return RedirectToAction(nameof(Index));
            }

            // 获取组织成员列表
            var members = await _organizationService.GetOrganizationMembersAsync(id, includeInactive: false);

            var viewModel = new TeacherOrganizationDetailsViewModel
            {
                Organization = organization,
                Members = members,
                IsTeacherInOrganization = true
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
}

/// <summary>
/// 教师组织视图模型
/// </summary>
public class TeacherOrganizationViewModel
{
    /// <summary>
    /// 已加入的组织列表
    /// </summary>
    public List<StudentOrganizationDto> JoinedOrganizations { get; set; } = new();

    /// <summary>
    /// 可用的学校组织列表
    /// </summary>
    public List<OrganizationDto> AvailableSchoolOrganizations { get; set; } = new();
}

/// <summary>
/// 教师组织详情视图模型
/// </summary>
public class TeacherOrganizationDetailsViewModel
{
    /// <summary>
    /// 组织信息
    /// </summary>
    public OrganizationDto Organization { get; set; } = new();

    /// <summary>
    /// 组织成员列表
    /// </summary>
    public List<StudentOrganizationDto> Members { get; set; } = new();

    /// <summary>
    /// 教师是否在该组织中
    /// </summary>
    public bool IsTeacherInOrganization { get; set; }
}
