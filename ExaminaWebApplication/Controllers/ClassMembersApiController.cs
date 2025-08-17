using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Requests;
using ExaminaWebApplication.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 班级成员管理API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,Teacher")]
public class ClassMembersApiController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly IInvitationCodeService _invitationCodeService;
    private readonly ILogger<ClassMembersApiController> _logger;

    public ClassMembersApiController(
        IOrganizationService organizationService,
        IInvitationCodeService invitationCodeService,
        ILogger<ClassMembersApiController> logger)
    {
        _organizationService = organizationService;
        _invitationCodeService = invitationCodeService;
        _logger = logger;
    }

    /// <summary>
    /// 添加学生到班级
    /// </summary>
    [HttpPost("{classId}/members")]
    public async Task<ActionResult<StudentOrganizationDto>> AddMemberToClass(int classId, [FromBody] AddClassMemberRequest request)
    {
        try
        {
            int operatorUserId = GetCurrentUserId();
            
            // 验证班级是否存在
            OrganizationDto? classInfo = await _organizationService.GetOrganizationByIdAsync(classId);
            if (classInfo == null)
            {
                return NotFound(new { message = "班级不存在" });
            }

            // 如果没有指定邀请码，使用班级的默认邀请码
            int invitationCodeId = request.InvitationCodeId ?? await GetDefaultInvitationCodeId(classId);
            
            // 添加学生到班级
            StudentOrganizationDto? result = await _organizationService.JoinOrganizationAsync(
                request.StudentId, 
                classId, 
                invitationCodeId);

            if (result == null)
            {
                return BadRequest(new { message = "添加成员失败，可能是学生已在班级中或邀请码无效" });
            }

            _logger.LogInformation("学生添加到班级成功: {StudentId} -> {ClassId}, 操作者: {OperatorUserId}", 
                request.StudentId, classId, operatorUserId);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加学生到班级失败: {StudentId} -> {ClassId}", request.StudentId, classId);
            return StatusCode(500, new { message = "添加成员失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 移除班级成员
    /// </summary>
    [HttpDelete("{classId}/members/{memberId}")]
    public async Task<ActionResult> RemoveMemberFromClass(int classId, int memberId)
    {
        try
        {
            int operatorUserId = GetCurrentUserId();
            
            // 移除成员（软删除）
            bool success = await _organizationService.RemoveOrganizationMemberAsync(memberId, operatorUserId);
            if (!success)
            {
                return NotFound(new { message = "成员不存在或移除失败" });
            }

            _logger.LogInformation("班级成员移除成功: {MemberId}, 班级: {ClassId}, 操作者: {OperatorUserId}", 
                memberId, classId, operatorUserId);
            
            return Ok(new { message = "成员移除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除班级成员失败: {MemberId}, 班级: {ClassId}", memberId, classId);
            return StatusCode(500, new { message = "移除成员失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 恢复班级成员
    /// </summary>
    [HttpPost("{classId}/members/{memberId}/restore")]
    public async Task<ActionResult> RestoreMemberToClass(int classId, int memberId)
    {
        try
        {
            int operatorUserId = GetCurrentUserId();
            
            // 恢复成员
            bool success = await _organizationService.RestoreOrganizationMemberAsync(memberId, operatorUserId);
            if (!success)
            {
                return NotFound(new { message = "成员不存在或恢复失败" });
            }

            _logger.LogInformation("班级成员恢复成功: {MemberId}, 班级: {ClassId}, 操作者: {OperatorUserId}", 
                memberId, classId, operatorUserId);
            
            return Ok(new { message = "成员恢复成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复班级成员失败: {MemberId}, 班级: {ClassId}", memberId, classId);
            return StatusCode(500, new { message = "恢复成员失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取班级的默认邀请码ID
    /// </summary>
    private async Task<int> GetDefaultInvitationCodeId(int classId)
    {
        try
        {
            var invitationCodes = await _invitationCodeService.GetOrganizationInvitationCodesAsync(classId, false);
            var activeCode = invitationCodes.FirstOrDefault(c => c.IsActive);
            
            if (activeCode != null)
            {
                return activeCode.Id;
            }

            // 如果没有活跃的邀请码，创建一个默认的
            var newCode = await _invitationCodeService.CreateInvitationCodeAsync(classId);
            return newCode?.Id ?? throw new InvalidOperationException("无法创建默认邀请码");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取或创建默认邀请码失败: {ClassId}", classId);
            throw;
        }
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private int GetCurrentUserId()
    {
        string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)
            ? throw new UnauthorizedAccessException("无法获取当前用户信息")
            : userId;
    }
}
