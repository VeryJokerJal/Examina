using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Requests;
using ExaminaWebApplication.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 班级管理API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,Teacher")]
public class ClassManagementApiController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly IInvitationCodeService _invitationCodeService;
    private readonly ILogger<ClassManagementApiController> _logger;

    public ClassManagementApiController(
        IOrganizationService organizationService,
        IInvitationCodeService invitationCodeService,
        ILogger<ClassManagementApiController> logger)
    {
        _organizationService = organizationService;
        _invitationCodeService = invitationCodeService;
        _logger = logger;
    }

    /// <summary>
    /// 创建班级
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> CreateClass([FromBody] CreateClassRequest request)
    {
        try
        {
            int creatorUserId = GetCurrentUserId();
            OrganizationDto classOrg = await _organizationService.CreateClassAsync(
                request.Name, 
                request.SchoolId, 
                creatorUserId, 
                request.GenerateInvitationCode);
            
            _logger.LogInformation("班级创建成功: {ClassName}, 学校ID: {SchoolId}, 创建者: {CreatorUserId}", 
                request.Name, request.SchoolId, creatorUserId);
            return Ok(classOrg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建班级失败: {ClassName}, 学校ID: {SchoolId}", request.Name, request.SchoolId);
            return StatusCode(500, new { message = "创建班级失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取所有班级列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<OrganizationDto>>> GetClasses([FromQuery] bool includeInactive = false)
    {
        try
        {
            List<OrganizationDto> classes = await _organizationService.GetClassesAsync(includeInactive);
            return Ok(classes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取班级列表失败");
            return StatusCode(500, new { message = "获取班级列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取班级详情
    /// </summary>
    [HttpGet("{classId}")]
    public async Task<ActionResult<OrganizationDto>> GetClassById(int classId)
    {
        try
        {
            OrganizationDto? classOrg = await _organizationService.GetOrganizationByIdAsync(classId);
            if (classOrg == null)
            {
                return NotFound(new { message = "班级不存在" });
            }

            return Ok(classOrg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取班级详情失败: {ClassId}", classId);
            return StatusCode(500, new { message = "获取班级详情失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新班级信息
    /// </summary>
    [HttpPut("{classId}")]
    public async Task<ActionResult<OrganizationDto>> UpdateClass(int classId, [FromBody] CreateClassRequest request)
    {
        try
        {
            OrganizationDto? classOrg = await _organizationService.UpdateOrganizationAsync(classId, request.Name);
            if (classOrg == null)
            {
                return NotFound(new { message = "班级不存在或更新失败" });
            }

            _logger.LogInformation("班级更新成功: {ClassId}, 新名称: {ClassName}", classId, request.Name);
            return Ok(classOrg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新班级失败: {ClassId}", classId);
            return StatusCode(500, new { message = "更新班级失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 停用班级
    /// </summary>
    [HttpDelete("{classId}")]
    public async Task<ActionResult> DeactivateClass(int classId)
    {
        try
        {
            bool success = await _organizationService.DeactivateOrganizationAsync(classId);
            if (!success)
            {
                return NotFound(new { message = "班级不存在或停用失败" });
            }

            _logger.LogInformation("班级停用成功: {ClassId}", classId);
            return Ok(new { message = "班级停用成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用班级失败: {ClassId}", classId);
            return StatusCode(500, new { message = "停用班级失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取班级的邀请码列表
    /// </summary>
    [HttpGet("{classId}/invitation-codes")]
    public async Task<ActionResult<List<InvitationCode>>> GetClassInvitationCodes(int classId, [FromQuery] bool includeInactive = false)
    {
        try
        {
            List<InvitationCode> invitationCodes = await _invitationCodeService.GetOrganizationInvitationCodesAsync(classId, includeInactive);
            return Ok(invitationCodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取班级邀请码列表失败: {ClassId}", classId);
            return StatusCode(500, new { message = "获取班级邀请码列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 为班级创建新的邀请码
    /// </summary>
    [HttpPost("{classId}/invitation-codes")]
    public async Task<ActionResult<InvitationCode>> CreateInvitationCode(int classId, [FromBody] CreateInvitationCodeRequest? request = null)
    {
        try
        {
            InvitationCode invitationCode = await _invitationCodeService.CreateInvitationCodeAsync(
                classId, 
                request?.ExpiresAt, 
                request?.MaxUsage);
            
            _logger.LogInformation("班级邀请码创建成功: {ClassId}, 邀请码: {Code}", classId, invitationCode.Code);
            return Ok(invitationCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建班级邀请码失败: {ClassId}", classId);
            return StatusCode(500, new { message = "创建班级邀请码失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取班级成员列表
    /// </summary>
    [HttpGet("{classId}/members")]
    public async Task<ActionResult<List<StudentOrganizationDto>>> GetClassMembers(int classId, [FromQuery] bool includeInactive = false)
    {
        try
        {
            List<StudentOrganizationDto> members = await _organizationService.GetOrganizationMembersAsync(classId, includeInactive);
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取班级成员列表失败: {ClassId}", classId);
            return StatusCode(500, new { message = "获取班级成员列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private int GetCurrentUserId()
    {
        string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("无法获取当前用户信息");
        }
        return userId;
    }
}

/// <summary>
/// 创建邀请码请求模型
/// </summary>
public class CreateInvitationCodeRequest
{
    /// <summary>
    /// 过期时间（可选）
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// 最大使用次数（可选）
    /// </summary>
    public int? MaxUsage { get; set; }
}
