using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Organization.Requests;
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
    private readonly ILogger<StudentOrganizationController> _logger;

    public StudentOrganizationController(
        IOrganizationService organizationService,
        ILogger<StudentOrganizationController> logger)
    {
        _organizationService = organizationService;
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
                org.Type,
                org.TypeDisplayName,
                org.Description,
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
}
