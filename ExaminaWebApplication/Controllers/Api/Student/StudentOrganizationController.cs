using System.Security.Claims;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Controllers.Api.Student;

/// <summary>
/// 学生组织管理控制器
/// </summary>
[ApiController]
[Route("api/student/organization")]
[Authorize(Policy = "StudentPolicy")]
public class StudentOrganizationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IOrganizationService _organizationService;
    private readonly IInvitationCodeService _invitationCodeService;
    private readonly ILogger<StudentOrganizationController> _logger;

    public StudentOrganizationController(
        ApplicationDbContext context,
        IOrganizationService organizationService,
        IInvitationCodeService invitationCodeService,
        ILogger<StudentOrganizationController> logger)
    {
        _context = context;
        _organizationService = organizationService;
        _invitationCodeService = invitationCodeService;
        _logger = logger;
    }

    /// <summary>
    /// 学生加入组织
    /// </summary>
    /// <param name="request">加入组织请求</param>
    /// <returns>加入结果</returns>
    [HttpPost("join")]
    public async Task<ActionResult<JoinOrganizationResponse>> JoinOrganization([FromBody] JoinOrganizationRequest request)
    {
        try
        {
            // 获取当前用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("无法获取当前用户ID");
                return Unauthorized("用户身份验证失败");
            }

            _logger.LogInformation("学生 {UserId} 尝试使用邀请码 {InvitationCode} 加入组织", userId, request.InvitationCode);

            // 验证用户是否存在且为学生角色
            User? user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && u.Role == UserRole.Student);

            if (user == null)
            {
                _logger.LogWarning("用户 {UserId} 不存在或不是学生角色", userId);
                return BadRequest(JoinOrganizationResponse.CreateFailure("用户不存在或权限不足"));
            }

            // 检查学生是否已经加入其他学校组织
            bool hasJoinedSchool = await _context.StudentOrganizations
                .Include(so => so.Organization)
                .AnyAsync(so => so.StudentId == userId &&
                               so.IsActive &&
                               so.Organization.Type == OrganizationType.School &&
                               so.Organization.IsActive);

            if (hasJoinedSchool)
            {
                _logger.LogInformation("学生 {UserId} 已经加入了其他学校组织", userId);
                return BadRequest(JoinOrganizationResponse.CreateFailure("您已经加入了其他学校，无法重复加入"));
            }

            // 验证邀请码
            InvitationCode? invitationCode = await _invitationCodeService.ValidateInvitationCodeAsync(request.InvitationCode);
            if (invitationCode == null)
            {
                _logger.LogInformation("邀请码 {InvitationCode} 无效", request.InvitationCode);
                return BadRequest(JoinOrganizationResponse.CreateFailure("邀请码无效"));
            }

            // 检查邀请码是否可用
            if (!_invitationCodeService.IsInvitationCodeAvailable(invitationCode))
            {
                _logger.LogInformation("邀请码 {InvitationCode} 不可用（已过期或达到使用上限）", request.InvitationCode);
                return BadRequest(JoinOrganizationResponse.CreateFailure("邀请码已过期或已达到使用上限"));
            }

            // 获取组织信息
            Organization? organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == invitationCode.OrganizationId && o.IsActive);

            if (organization == null)
            {
                _logger.LogWarning("邀请码 {InvitationCode} 对应的组织不存在或已停用", request.InvitationCode);
                return BadRequest(JoinOrganizationResponse.CreateFailure("组织不存在或已停用"));
            }

            // 检查是否已经在该组织中
            bool alreadyInOrganization = await _context.StudentOrganizations
                .AnyAsync(so => so.StudentId == userId && so.OrganizationId == organization.Id && so.IsActive);

            if (alreadyInOrganization)
            {
                _logger.LogInformation("学生 {UserId} 已经在组织 {OrganizationId} 中", userId, organization.Id);
                return BadRequest(JoinOrganizationResponse.CreateFailure("您已经在该组织中"));
            }

            // 调用组织服务加入组织
            JoinOrganizationResult joinResult = await _organizationService.JoinOrganizationAsync(userId, UserRole.Student, request.InvitationCode);

            if (!joinResult.Success)
            {
                _logger.LogWarning("学生 {UserId} 加入组织失败: {ErrorMessage}", userId, joinResult.ErrorMessage);
                return BadRequest(JoinOrganizationResponse.CreateFailure(joinResult.ErrorMessage ?? "加入组织失败"));
            }

            if (joinResult.StudentOrganization == null)
            {
                _logger.LogError("加入组织成功但返回的学生组织信息为空");
                return StatusCode(500, JoinOrganizationResponse.CreateFailure("系统错误，请稍后重试"));
            }

            _logger.LogInformation("学生 {UserId} 成功加入组织 {OrganizationName} (ID: {OrganizationId})",
                userId, joinResult.StudentOrganization.OrganizationName, joinResult.StudentOrganization.OrganizationId);

            return Ok(JoinOrganizationResponse.CreateSuccess(joinResult.StudentOrganization));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "学生加入组织时发生异常");
            return StatusCode(500, JoinOrganizationResponse.CreateFailure("系统错误，请稍后重试"));
        }
    }

    /// <summary>
    /// 获取学生已加入的组织列表
    /// </summary>
    /// <returns>学生组织列表</returns>
    [HttpGet("my-organizations")]
    public async Task<ActionResult<List<StudentOrganizationDto>>> GetMyOrganizations()
    {
        try
        {
            // 获取当前用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("无法获取当前用户ID");
                return Unauthorized("用户身份验证失败");
            }

            _logger.LogInformation("获取学生 {UserId} 的组织列表", userId);

            List<StudentOrganizationDto> organizations = await _organizationService.GetUserOrganizationsAsync(userId);

            _logger.LogInformation("学生 {UserId} 共有 {Count} 个组织", userId, organizations.Count);

            return Ok(organizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生组织列表时发生异常");
            return StatusCode(500, "系统错误，请稍后重试");
        }
    }

    /// <summary>
    /// 检查学生是否已加入学校组织
    /// </summary>
    /// <returns>是否已加入学校</returns>
    [HttpGet("school-status")]
    public async Task<ActionResult<object>> GetSchoolStatus()
    {
        try
        {
            // 获取当前用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("无法获取当前用户ID");
                return Unauthorized("用户身份验证失败");
            }

            _logger.LogInformation("检查学生 {UserId} 的学校绑定状态", userId);

            // 查找学生加入的学校组织
            StudentOrganization? schoolOrganization = await _context.StudentOrganizations
                .Include(so => so.Organization)
                .FirstOrDefaultAsync(so => so.StudentId == userId &&
                                          so.IsActive &&
                                          so.Organization.Type == OrganizationType.School &&
                                          so.Organization.IsActive);

            bool isSchoolBound = schoolOrganization != null;
            string? currentSchool = schoolOrganization?.Organization.Name;

            _logger.LogInformation("学生 {UserId} 学校绑定状态: {IsSchoolBound}, 当前学校: {CurrentSchool}",
                userId, isSchoolBound, currentSchool);

            return Ok(new
            {
                IsSchoolBound = isSchoolBound,
                CurrentSchool = currentSchool,
                SchoolId = schoolOrganization?.OrganizationId,
                schoolOrganization?.JoinedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查学生学校状态时发生异常");
            return StatusCode(500, "系统错误，请稍后重试");
        }
    }
}
