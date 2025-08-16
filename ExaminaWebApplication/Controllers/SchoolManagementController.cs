using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Requests;
using ExaminaWebApplication.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 学校管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,Teacher")]
public class SchoolManagementController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<SchoolManagementController> _logger;

    public SchoolManagementController(
        IOrganizationService organizationService,
        ILogger<SchoolManagementController> logger)
    {
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// 创建学校
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrganizationDto>> CreateSchool([FromBody] CreateSchoolRequest request)
    {
        try
        {
            int creatorUserId = GetCurrentUserId();
            OrganizationDto school = await _organizationService.CreateSchoolAsync(request.Name, creatorUserId);
            
            _logger.LogInformation("学校创建成功: {SchoolName}, 创建者: {CreatorUserId}", request.Name, creatorUserId);
            return Ok(school);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建学校失败: {SchoolName}", request.Name);
            return StatusCode(500, new { message = "创建学校失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取学校列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<OrganizationDto>>> GetSchools([FromQuery] bool includeInactive = false)
    {
        try
        {
            List<OrganizationDto> schools = await _organizationService.GetSchoolsAsync(includeInactive);
            return Ok(schools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学校列表失败");
            return StatusCode(500, new { message = "获取学校列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取学校详情
    /// </summary>
    [HttpGet("{schoolId}")]
    public async Task<ActionResult<OrganizationDto>> GetSchoolById(int schoolId)
    {
        try
        {
            OrganizationDto? school = await _organizationService.GetOrganizationByIdAsync(schoolId);
            if (school == null)
            {
                return NotFound(new { message = "学校不存在" });
            }

            return Ok(school);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学校详情失败: {SchoolId}", schoolId);
            return StatusCode(500, new { message = "获取学校详情失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新学校信息
    /// </summary>
    [HttpPut("{schoolId}")]
    public async Task<ActionResult<OrganizationDto>> UpdateSchool(int schoolId, [FromBody] CreateSchoolRequest request)
    {
        try
        {
            OrganizationDto? school = await _organizationService.UpdateOrganizationAsync(schoolId, request.Name);
            if (school == null)
            {
                return NotFound(new { message = "学校不存在或更新失败" });
            }

            _logger.LogInformation("学校更新成功: {SchoolId}, 新名称: {SchoolName}", schoolId, request.Name);
            return Ok(school);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新学校失败: {SchoolId}", schoolId);
            return StatusCode(500, new { message = "更新学校失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 停用学校
    /// </summary>
    [HttpDelete("{schoolId}")]
    public async Task<ActionResult> DeactivateSchool(int schoolId)
    {
        try
        {
            bool success = await _organizationService.DeactivateOrganizationAsync(schoolId);
            if (!success)
            {
                return NotFound(new { message = "学校不存在或停用失败" });
            }

            _logger.LogInformation("学校停用成功: {SchoolId}", schoolId);
            return Ok(new { message = "学校停用成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用学校失败: {SchoolId}", schoolId);
            return StatusCode(500, new { message = "停用学校失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取学校下的班级列表
    /// </summary>
    [HttpGet("{schoolId}/classes")]
    public async Task<ActionResult<List<OrganizationDto>>> GetClassesBySchool(int schoolId, [FromQuery] bool includeInactive = false)
    {
        try
        {
            List<OrganizationDto> classes = await _organizationService.GetClassesBySchoolAsync(schoolId, includeInactive);
            return Ok(classes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学校班级列表失败: {SchoolId}", schoolId);
            return StatusCode(500, new { message = "获取学校班级列表失败", error = ex.Message });
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
