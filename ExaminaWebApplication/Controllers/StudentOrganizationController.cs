using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Organization.Requests;
using ExaminaWebApplication.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 学生组织管理API控制器
/// </summary>
[ApiController]
[Route("api/student/organizations")]
[Authorize(Policy = "StudentPolicy")]
public class StudentOrganizationController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StudentOrganizationController> _logger;

    public StudentOrganizationController(
        IOrganizationService organizationService,
        ApplicationDbContext context,
        ILogger<StudentOrganizationController> logger)
    {
        _organizationService = organizationService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 通过邀请码加入组织
    /// </summary>
    /// <param name="request">加入组织请求</param>
    /// <returns>加入结果</returns>
    [HttpPost("join")]
    public async Task<ActionResult<StudentOrganizationDto>> JoinOrganization([FromBody] JoinOrganizationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 获取当前学生用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int studentUserId))
            {
                return Unauthorized(new { message = "无法获取用户信息" });
            }

            // 验证用户角色
            string? roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim != "Student")
            {
                return Forbid("只有学生可以加入组织");
            }

            // 加入组织
            JoinOrganizationResult result = await _organizationService.JoinOrganizationAsync(studentUserId, UserRole.Student, request.InvitationCode);

            if (!result.Success)
            {
                _logger.LogWarning("学生 {StudentUserId} 加入组织失败: {ErrorMessage}", studentUserId, result.ErrorMessage);
                return BadRequest(new { message = result.ErrorMessage });
            }

            _logger.LogInformation("学生 {StudentUserId} 通过邀请码 {InvitationCode} 加入组织成功",
                studentUserId, request.InvitationCode);

            return Ok(result.StudentOrganization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "学生加入组织时发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取学生已加入的组织列表
    /// </summary>
    /// <returns>学生组织列表</returns>
    [HttpGet]
    public async Task<ActionResult<List<StudentOrganizationDto>>> GetStudentOrganizations()
    {
        try
        {
            // 获取当前学生用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int studentUserId))
            {
                return Unauthorized(new { message = "无法获取用户信息" });
            }

            List<StudentOrganizationDto> organizations = await _organizationService.GetUserOrganizationsAsync(studentUserId);
            return Ok(organizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生组织列表时发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 退出组织
    /// </summary>
    /// <param name="id">组织ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}/leave")]
    public async Task<ActionResult> LeaveOrganization(int id)
    {
        try
        {
            // 获取当前学生用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int studentUserId))
            {
                return Unauthorized(new { message = "无法获取用户信息" });
            }

            // 检查学生是否在该组织中
            bool isInOrganization = await _organizationService.IsUserInOrganizationAsync(studentUserId, id);
            if (!isInOrganization)
            {
                return NotFound(new { message = "您不在该组织中" });
            }

            // 退出组织
            bool success = await _organizationService.LeaveOrganizationAsync(studentUserId, id);
            if (!success)
            {
                return BadRequest(new { message = "退出组织失败" });
            }

            _logger.LogInformation("学生 {StudentUserId} 退出组织 {OrganizationId} 成功", studentUserId, id);
            return Ok(new { message = "已成功退出组织" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "学生退出组织时发生错误: {OrganizationId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 检查学生是否在指定组织中
    /// </summary>
    /// <param name="id">组织ID</param>
    /// <returns>检查结果</returns>
    [HttpGet("{id}/membership")]
    public async Task<ActionResult<object>> CheckMembership(int id)
    {
        try
        {
            // 获取当前学生用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int studentUserId))
            {
                return Unauthorized(new { message = "无法获取用户信息" });
            }

            bool isInOrganization = await _organizationService.IsUserInOrganizationAsync(studentUserId, id);
            
            return Ok(new 
            { 
                organizationId = id,
                isMember = isInOrganization,
                studentId = studentUserId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查学生组织关系时发生错误: {OrganizationId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取可用的组织列表（用于展示）
    /// </summary>
    /// <returns>组织列表</returns>
    [HttpGet("available")]
    public async Task<ActionResult<List<OrganizationDto>>> GetAvailableOrganizations()
    {
        try
        {
            // 获取所有激活的组织
            List<OrganizationDto> organizations = await _organizationService.GetOrganizationsAsync(includeInactive: false);
            
            // 移除敏感信息，只返回基本信息
            var publicOrganizations = organizations.Select(org => new
            {
                org.Id,
                org.Name,
                org.StudentCount
            }).ToList();

            return Ok(publicOrganizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用组织列表时发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取学生的组织状态信息
    /// </summary>
    /// <returns>学生组织状态</returns>
    [HttpGet("status")]
    public async Task<ActionResult<object>> GetStudentOrganizationStatus()
    {
        try
        {
            // 获取当前学生用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int studentUserId))
            {
                return Unauthorized(new { message = "无法获取用户信息" });
            }

            // 获取学生用户信息
            User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == UserRole.Student);

            if (student == null)
            {
                return NotFound(new { message = "学生用户不存在" });
            }

            // 检查学生是否已加入任何组织
            bool hasJoinedOrganization = await _context.StudentOrganizations
                .AnyAsync(so => so.StudentId == studentUserId && so.IsActive);

            bool isInMemberList = false;

            // 如果未加入组织，检查是否在成员名单中
            if (!hasJoinedOrganization && !string.IsNullOrEmpty(student.RealName))
            {
                isInMemberList = await _context.OrganizationMembers
                    .AnyAsync(om => om.RealName == student.RealName &&
                                   om.OrganizationId == null &&
                                   om.IsActive);
            }

            var result = new
            {
                hasJoinedOrganization,
                isInMemberList,
                studentInfo = new
                {
                    id = student.Id,
                    username = student.Username,
                    realName = student.RealName,
                    phoneNumber = student.PhoneNumber
                }
            };

            _logger.LogInformation("获取学生 {StudentId} 组织状态: 已加入组织={HasJoined}, 在成员名单={InMemberList}",
                studentUserId, hasJoinedOrganization, isInMemberList);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生组织状态时发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }
}
