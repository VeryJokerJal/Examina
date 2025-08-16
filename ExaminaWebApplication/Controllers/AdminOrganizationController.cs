using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExaminaWebApplication.Services.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Organization.Requests;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 管理员组织管理API控制器
/// </summary>
[ApiController]
[Route("api/admin/organizations")]
[Authorize(Policy = "AdminPolicy")]
public class AdminOrganizationController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly IInvitationCodeService _invitationCodeService;
    private readonly ILogger<AdminOrganizationController> _logger;

    public AdminOrganizationController(
        IOrganizationService organizationService,
        IInvitationCodeService invitationCodeService,
        ILogger<AdminOrganizationController> logger)
    {
        _organizationService = organizationService;
        _invitationCodeService = invitationCodeService;
        _logger = logger;
    }

    /// <summary>
    /// 创建组织
    /// </summary>
    /// <param name="request">创建组织请求</param>
    /// <returns>创建的组织信息</returns>
    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 获取当前管理员用户ID
            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int adminUserId))
            {
                return Unauthorized(new { message = "无法获取用户信息" });
            }

            OrganizationDto organization = await _organizationService.CreateOrganizationAsync(request, adminUserId);

            _logger.LogInformation("管理员 {AdminUserId} 创建组织成功: {OrganizationName}", adminUserId, organization.Name);
            return Ok(organization);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("创建组织失败: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建组织时发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取组织列表
    /// </summary>
    /// <param name="includeInactive">是否包含非激活的组织</param>
    /// <returns>组织列表</returns>
    [HttpGet]
    public async Task<ActionResult<List<OrganizationDto>>> GetOrganizations([FromQuery] bool includeInactive = false)
    {
        try
        {
            List<OrganizationDto> organizations = await _organizationService.GetOrganizationsAsync(includeInactive);
            return Ok(organizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织列表时发生错误");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 根据ID获取组织详情
    /// </summary>
    /// <param name="id">组织ID</param>
    /// <returns>组织详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationDto>> GetOrganization(int id)
    {
        try
        {
            OrganizationDto? organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                return NotFound(new { message = "组织不存在" });
            }

            return Ok(organization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织详情时发生错误: {OrganizationId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 更新组织信息
    /// </summary>
    /// <param name="id">组织ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新后的组织信息</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<OrganizationDto>> UpdateOrganization(int id, [FromBody] UpdateOrganizationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            OrganizationDto? organization = await _organizationService.UpdateOrganizationAsync(id, request.Name, request.Description);
            if (organization == null)
            {
                return NotFound(new { message = "组织不存在" });
            }

            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("管理员 {AdminUserId} 更新组织 {OrganizationId} 成功", userIdClaim, id);

            return Ok(organization);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("更新组织失败: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新组织时发生错误: {OrganizationId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 停用组织
    /// </summary>
    /// <param name="id">组织ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeactivateOrganization(int id)
    {
        try
        {
            bool success = await _organizationService.DeactivateOrganizationAsync(id);
            if (!success)
            {
                return NotFound(new { message = "组织不存在" });
            }

            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("管理员 {AdminUserId} 停用组织 {OrganizationId} 成功", userIdClaim, id);

            return Ok(new { message = "组织已停用" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用组织时发生错误: {OrganizationId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 为组织生成邀请码
    /// </summary>
    /// <param name="id">组织ID</param>
    /// <param name="request">生成邀请码请求</param>
    /// <returns>生成的邀请码信息</returns>
    [HttpPost("{id}/invitation-codes")]
    public async Task<ActionResult<InvitationCodeDto>> CreateInvitationCode(int id, [FromBody] CreateInvitationCodeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 检查组织是否存在
            OrganizationDto? organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                return NotFound(new { message = "组织不存在" });
            }

            // 创建邀请码
            var invitationCode = await _invitationCodeService.CreateInvitationCodeAsync(
                id, request.ExpiresAt, request.MaxUsage);

            // 转换为DTO
            InvitationCodeDto dto = new()
            {
                Id = invitationCode.Id,
                Code = invitationCode.Code,
                OrganizationId = invitationCode.OrganizationId,
                OrganizationName = organization.Name,
                CreatedAt = invitationCode.CreatedAt,
                ExpiresAt = invitationCode.ExpiresAt,
                IsActive = invitationCode.IsActive,
                UsageCount = invitationCode.UsageCount,
                MaxUsage = invitationCode.MaxUsage
            };

            string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("管理员 {AdminUserId} 为组织 {OrganizationId} 生成邀请码 {InvitationCode} 成功",
                userIdClaim, id, invitationCode.Code);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成邀请码时发生错误: {OrganizationId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取组织的邀请码列表
    /// </summary>
    /// <param name="id">组织ID</param>
    /// <param name="includeInactive">是否包含非激活的邀请码</param>
    /// <returns>邀请码列表</returns>
    [HttpGet("{id}/invitation-codes")]
    public async Task<ActionResult<List<InvitationCodeDto>>> GetOrganizationInvitationCodes(int id, [FromQuery] bool includeInactive = false)
    {
        try
        {
            // 检查组织是否存在
            OrganizationDto? organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                return NotFound(new { message = "组织不存在" });
            }

            var invitationCodes = await _invitationCodeService.GetOrganizationInvitationCodesAsync(id, includeInactive);

            List<InvitationCodeDto> dtos = invitationCodes.Select(ic => new InvitationCodeDto
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
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织邀请码列表时发生错误: {OrganizationId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取组织的学生列表
    /// </summary>
    /// <param name="id">组织ID</param>
    /// <param name="includeInactive">是否包含非激活的关系</param>
    /// <returns>学生列表</returns>
    [HttpGet("{id}/students")]
    public async Task<ActionResult<List<StudentOrganizationDto>>> GetOrganizationStudents(int id, [FromQuery] bool includeInactive = false)
    {
        try
        {
            // 检查组织是否存在
            OrganizationDto? organization = await _organizationService.GetOrganizationByIdAsync(id);
            if (organization == null)
            {
                return NotFound(new { message = "组织不存在" });
            }

            List<StudentOrganizationDto> students = await _organizationService.GetOrganizationStudentsAsync(id, includeInactive);
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组织学生列表时发生错误: {OrganizationId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }
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
