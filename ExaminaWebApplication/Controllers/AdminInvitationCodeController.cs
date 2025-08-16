using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using System.Security.Claims;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 管理员邀请码管理API控制器
/// </summary>
[ApiController]
[Route("api/admin/invitation-codes")]
[Authorize(Policy = "AdminPolicy")]
public class AdminInvitationCodeController : ControllerBase
{
    private readonly IInvitationCodeService _invitationCodeService;
    private readonly ILogger<AdminInvitationCodeController> _logger;

    public AdminInvitationCodeController(
        IInvitationCodeService invitationCodeService,
        ILogger<AdminInvitationCodeController> logger)
    {
        _invitationCodeService = invitationCodeService;
        _logger = logger;
    }

    /// <summary>
    /// 停用邀请码
    /// </summary>
    /// <param name="id">邀请码ID</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/deactivate")]
    public async Task<ActionResult> DeactivateInvitationCode(int id)
    {
        try
        {
            bool success = await _invitationCodeService.DeactivateInvitationCodeAsync(id);
            if (!success)
            {
                return NotFound(new { message = "邀请码不存在" });
            }

            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("管理员 {AdminUserId} 停用邀请码 {InvitationCodeId} 成功", userIdClaim, id);

            return Ok(new { message = "邀请码已停用" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用邀请码时发生错误: {InvitationCodeId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 验证邀请码
    /// </summary>
    /// <param name="code">邀请码</param>
    /// <returns>邀请码信息</returns>
    [HttpGet("validate/{code}")]
    public async Task<ActionResult<InvitationCodeDto>> ValidateInvitationCode(string code)
    {
        try
        {
            var invitationCode = await _invitationCodeService.ValidateInvitationCodeAsync(code);
            if (invitationCode == null)
            {
                return NotFound(new { message = "邀请码不存在" });
            }

            InvitationCodeDto dto = new()
            {
                Id = invitationCode.Id,
                Code = invitationCode.Code,
                OrganizationId = invitationCode.OrganizationId,
                OrganizationName = invitationCode.Organization?.Name ?? "未知",
                CreatedAt = invitationCode.CreatedAt,
                ExpiresAt = invitationCode.ExpiresAt,
                IsActive = invitationCode.IsActive,
                UsageCount = invitationCode.UsageCount,
                MaxUsage = invitationCode.MaxUsage
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证邀请码时发生错误: {Code}", code);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }
}
