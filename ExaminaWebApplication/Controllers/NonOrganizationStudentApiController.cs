using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Requests;
using ExaminaWebApplication.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 非组织学生管理API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,Teacher")]
public class NonOrganizationStudentApiController : ControllerBase
{
    private readonly INonOrganizationStudentService _studentService;
    private readonly ILogger<NonOrganizationStudentApiController> _logger;

    public NonOrganizationStudentApiController(
        INonOrganizationStudentService studentService,
        ILogger<NonOrganizationStudentApiController> logger)
    {
        _studentService = studentService;
        _logger = logger;
    }

    /// <summary>
    /// 创建非组织学生
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<NonOrganizationStudentDto>> CreateStudent([FromBody] CreateNonOrganizationStudentRequest request)
    {
        try
        {
            int creatorUserId = GetCurrentUserId();
            NonOrganizationStudentDto? student = await _studentService.CreateStudentAsync(
                request.RealName, 
                request.PhoneNumber, 
                creatorUserId, 
                request.Notes);

            if (student == null)
            {
                return BadRequest(new { message = "创建学生失败，可能是手机号已存在" });
            }

            _logger.LogInformation("非组织学生创建成功: {RealName}, {PhoneNumber}, 创建者: {CreatorUserId}", 
                request.RealName, request.PhoneNumber, creatorUserId);
            return Ok(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建非组织学生失败: {RealName}, {PhoneNumber}", request.RealName, request.PhoneNumber);
            return StatusCode(500, new { message = "创建学生失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取非组织学生列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NonOrganizationStudentDto>>> GetStudents(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            List<NonOrganizationStudentDto> students = await _studentService.GetStudentsAsync(includeInactive, pageNumber, pageSize);
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取非组织学生列表失败");
            return StatusCode(500, new { message = "获取学生列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取非组织学生详情
    /// </summary>
    [HttpGet("{studentId}")]
    public async Task<ActionResult<NonOrganizationStudentDto>> GetStudentById(int studentId)
    {
        try
        {
            NonOrganizationStudentDto? student = await _studentService.GetStudentByIdAsync(studentId);
            if (student == null)
            {
                return NotFound(new { message = "学生不存在" });
            }

            return Ok(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取非组织学生详情失败: {StudentId}", studentId);
            return StatusCode(500, new { message = "获取学生详情失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新非组织学生信息
    /// </summary>
    [HttpPut("{studentId}")]
    public async Task<ActionResult<NonOrganizationStudentDto>> UpdateStudent(int studentId, [FromBody] CreateNonOrganizationStudentRequest request)
    {
        try
        {
            int updaterUserId = GetCurrentUserId();
            NonOrganizationStudentDto? student = await _studentService.UpdateStudentAsync(
                studentId, 
                request.RealName, 
                request.PhoneNumber, 
                updaterUserId, 
                request.Notes);

            if (student == null)
            {
                return NotFound(new { message = "学生不存在或更新失败" });
            }

            _logger.LogInformation("非组织学生更新成功: {StudentId}, 更新者: {UpdaterUserId}", studentId, updaterUserId);
            return Ok(student);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新非组织学生失败: {StudentId}", studentId);
            return StatusCode(500, new { message = "更新学生失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除非组织学生（软删除）
    /// </summary>
    [HttpDelete("{studentId}")]
    public async Task<ActionResult> DeleteStudent(int studentId)
    {
        try
        {
            int updaterUserId = GetCurrentUserId();
            bool success = await _studentService.DeleteStudentAsync(studentId, updaterUserId);
            if (!success)
            {
                return NotFound(new { message = "学生不存在或删除失败" });
            }

            _logger.LogInformation("非组织学生删除成功: {StudentId}, 操作者: {UpdaterUserId}", studentId, updaterUserId);
            return Ok(new { message = "学生删除成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除非组织学生失败: {StudentId}", studentId);
            return StatusCode(500, new { message = "删除学生失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据手机号搜索非组织学生
    /// </summary>
    [HttpGet("search/phone")]
    public async Task<ActionResult<List<NonOrganizationStudentDto>>> SearchStudentsByPhone(
        [FromQuery] string phoneNumber,
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return BadRequest(new { message = "手机号不能为空" });
            }

            List<NonOrganizationStudentDto> students = await _studentService.SearchStudentsByPhoneAsync(phoneNumber, includeInactive);
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据手机号搜索非组织学生失败: {PhoneNumber}", phoneNumber);
            return StatusCode(500, new { message = "搜索学生失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据姓名搜索非组织学生
    /// </summary>
    [HttpGet("search/name")]
    public async Task<ActionResult<List<NonOrganizationStudentDto>>> SearchStudentsByName(
        [FromQuery] string realName,
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(realName))
            {
                return BadRequest(new { message = "姓名不能为空" });
            }

            List<NonOrganizationStudentDto> students = await _studentService.SearchStudentsByNameAsync(realName, includeInactive);
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据姓名搜索非组织学生失败: {RealName}", realName);
            return StatusCode(500, new { message = "搜索学生失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取非组织学生总数
    /// </summary>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetStudentCount([FromQuery] bool includeInactive = false)
    {
        try
        {
            int count = await _studentService.GetStudentCountAsync(includeInactive);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取非组织学生总数失败");
            return StatusCode(500, new { message = "获取学生总数失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 关联非组织学生到已注册用户
    /// </summary>
    [HttpPost("{studentId}/link-user/{userId}")]
    public async Task<ActionResult> LinkStudentToUser(int studentId, int userId)
    {
        try
        {
            int updaterUserId = GetCurrentUserId();
            bool success = await _studentService.LinkStudentToUserAsync(studentId, userId, updaterUserId);
            if (!success)
            {
                return BadRequest(new { message = "关联失败，学生或用户不存在" });
            }

            _logger.LogInformation("非组织学生关联用户成功: {StudentId} -> {UserId}, 操作者: {UpdaterUserId}", 
                studentId, userId, updaterUserId);
            return Ok(new { message = "关联成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关联非组织学生到用户失败: {StudentId} -> {UserId}", studentId, userId);
            return StatusCode(500, new { message = "关联失败", error = ex.Message });
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
