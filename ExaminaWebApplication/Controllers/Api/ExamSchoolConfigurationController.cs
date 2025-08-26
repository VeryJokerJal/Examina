using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Services.School;

namespace ExaminaWebApplication.Controllers.Api;

/// <summary>
/// 考试学校配置API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,Teacher")]
public class ExamSchoolConfigurationController : ControllerBase
{
    private readonly ISchoolPermissionService _schoolPermissionService;
    private readonly ILogger<ExamSchoolConfigurationController> _logger;

    public ExamSchoolConfigurationController(
        ISchoolPermissionService schoolPermissionService,
        ILogger<ExamSchoolConfigurationController> logger)
    {
        _schoolPermissionService = schoolPermissionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取考试关联的学校列表
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>学校列表</returns>
    [HttpGet("exam/{examId}/schools")]
    public async Task<ActionResult<List<OrganizationDto>>> GetExamAssociatedSchools(int examId)
    {
        try
        {
            List<Organization> schools = await _schoolPermissionService.GetExamAssociatedSchoolsAsync(examId);

            List<OrganizationDto> result = schools.Select(s => new OrganizationDto
            {
                Id = s.Id,
                Name = s.Name,
                Type = s.Type,
                ParentOrganizationId = s.ParentOrganizationId,
                CreatedAt = s.CreatedAt,
                CreatedBy = s.CreatedBy,
                IsActive = s.IsActive
            }).ToList();

            _logger.LogInformation("获取考试关联学校列表成功，考试ID: {ExamId}, 学校数量: {Count}", 
                examId, result.Count);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试关联学校列表失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "获取学校列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 为考试添加学校关联
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="request">添加学校关联请求</param>
    /// <returns>添加结果</returns>
    [HttpPost("exam/{examId}/schools")]
    public async Task<ActionResult> AddExamSchoolAssociation(int examId, [FromBody] AddSchoolAssociationRequest request)
    {
        try
        {
            int currentUserId = GetCurrentUserId();

            bool success = await _schoolPermissionService.AddExamSchoolAssociationAsync(
                examId, request.SchoolId, currentUserId, request.Remarks);

            if (success)
            {
                _logger.LogInformation("考试学校关联添加成功，考试ID: {ExamId}, 学校ID: {SchoolId}, 操作者: {UserId}", 
                    examId, request.SchoolId, currentUserId);

                return Ok(new { message = "学校关联添加成功" });
            }
            else
            {
                return BadRequest(new { message = "学校关联添加失败，可能已存在或参数无效" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加考试学校关联失败，考试ID: {ExamId}, 学校ID: {SchoolId}", 
                examId, request.SchoolId);
            return StatusCode(500, new { message = "添加学校关联失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 移除考试的学校关联
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="schoolId">学校ID</param>
    /// <returns>移除结果</returns>
    [HttpDelete("exam/{examId}/schools/{schoolId}")]
    public async Task<ActionResult> RemoveExamSchoolAssociation(int examId, int schoolId)
    {
        try
        {
            bool success = await _schoolPermissionService.RemoveExamSchoolAssociationAsync(examId, schoolId);

            if (success)
            {
                _logger.LogInformation("考试学校关联移除成功，考试ID: {ExamId}, 学校ID: {SchoolId}", 
                    examId, schoolId);

                return Ok(new { message = "学校关联移除成功" });
            }
            else
            {
                return NotFound(new { message = "学校关联不存在" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移除考试学校关联失败，考试ID: {ExamId}, 学校ID: {SchoolId}", 
                examId, schoolId);
            return StatusCode(500, new { message = "移除学校关联失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 批量添加考试的学校关联
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="request">批量添加请求</param>
    /// <returns>添加结果</returns>
    [HttpPost("exam/{examId}/schools/batch")]
    public async Task<ActionResult> BatchAddExamSchoolAssociations(int examId, [FromBody] BatchAddSchoolAssociationsRequest request)
    {
        try
        {
            int currentUserId = GetCurrentUserId();

            int successCount = await _schoolPermissionService.BatchAddExamSchoolAssociationsAsync(
                examId, request.SchoolIds, currentUserId, request.Remarks);

            _logger.LogInformation("批量添加考试学校关联完成，考试ID: {ExamId}, 成功数量: {SuccessCount}/{TotalCount}, 操作者: {UserId}", 
                examId, successCount, request.SchoolIds.Count, currentUserId);

            return Ok(new 
            { 
                message = $"批量添加完成，成功 {successCount}/{request.SchoolIds.Count} 个学校关联",
                successCount = successCount,
                totalCount = request.SchoolIds.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量添加考试学校关联失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "批量添加学校关联失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 批量移除考试的学校关联
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="request">批量移除请求</param>
    /// <returns>移除结果</returns>
    [HttpDelete("exam/{examId}/schools/batch")]
    public async Task<ActionResult> BatchRemoveExamSchoolAssociations(int examId, [FromBody] BatchRemoveSchoolAssociationsRequest request)
    {
        try
        {
            int successCount = await _schoolPermissionService.BatchRemoveExamSchoolAssociationsAsync(
                examId, request.SchoolIds);

            _logger.LogInformation("批量移除考试学校关联完成，考试ID: {ExamId}, 成功数量: {SuccessCount}/{TotalCount}", 
                examId, successCount, request.SchoolIds.Count);

            return Ok(new 
            { 
                message = $"批量移除完成，成功 {successCount}/{request.SchoolIds.Count} 个学校关联",
                successCount = successCount,
                totalCount = request.SchoolIds.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量移除考试学校关联失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "批量移除学校关联失败", error = ex.Message });
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
            throw new UnauthorizedAccessException("无法获取当前用户ID");
        }
        return userId;
    }
}

/// <summary>
/// 添加学校关联请求模型
/// </summary>
public class AddSchoolAssociationRequest
{
    /// <summary>
    /// 学校ID
    /// </summary>
    public int SchoolId { get; set; }

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Remarks { get; set; }
}

/// <summary>
/// 批量添加学校关联请求模型
/// </summary>
public class BatchAddSchoolAssociationsRequest
{
    /// <summary>
    /// 学校ID列表
    /// </summary>
    public List<int> SchoolIds { get; set; } = [];

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Remarks { get; set; }
}

/// <summary>
/// 批量移除学校关联请求模型
/// </summary>
public class BatchRemoveSchoolAssociationsRequest
{
    /// <summary>
    /// 学校ID列表
    /// </summary>
    public List<int> SchoolIds { get; set; } = [];
}

/// <summary>
/// 组织DTO模型
/// </summary>
public class OrganizationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public OrganizationType Type { get; set; }
    public int? ParentOrganizationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public bool IsActive { get; set; }
}
