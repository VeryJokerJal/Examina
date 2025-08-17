using ExaminaWebApplication.Models.Organization.Dto;
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
    private readonly ILogger<ClassMembersApiController> _logger;

    public ClassMembersApiController(
        IOrganizationService organizationService,
        ILogger<ClassMembersApiController> logger)
    {
        _organizationService = organizationService;
        _logger = logger;
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
