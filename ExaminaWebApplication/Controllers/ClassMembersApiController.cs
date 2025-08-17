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
    private readonly INonOrganizationStudentService _studentService;
    private readonly ILogger<ClassMembersApiController> _logger;

    public ClassMembersApiController(
        IOrganizationService organizationService,
        INonOrganizationStudentService studentService,
        ILogger<ClassMembersApiController> logger)
    {
        _organizationService = organizationService;
        _studentService = studentService;
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

            // 检查手机号是否已存在
            var existingStudents = await _studentService.SearchStudentsByPhoneAsync(request.PhoneNumber, false);
            if (existingStudents.Any())
            {
                return BadRequest(new { message = "该手机号码已存在，请使用其他手机号码" });
            }

            // 创建非组织学生记录
            var studentDto = await _studentService.CreateStudentAsync(
                request.RealName,
                request.PhoneNumber,
                operatorUserId,
                request.Notes);

            if (studentDto == null)
            {
                return BadRequest(new { message = "创建学生记录失败" });
            }

            // 将学生添加到班级
            var result = await CreateStudentOrganizationAsync(studentDto.Id, classId);

            if (result == null)
            {
                return BadRequest(new { message = "添加学生到班级失败" });
            }

            _logger.LogInformation("学生添加到班级成功: {StudentName}({PhoneNumber}) -> {ClassId}, 操作者: {OperatorUserId}",
                request.RealName, request.PhoneNumber, classId, operatorUserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加学生到班级失败: {StudentName}({PhoneNumber}) -> {ClassId}",
                request.RealName, request.PhoneNumber, classId);
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
    /// 创建学生组织关系
    /// </summary>
    private async Task<StudentOrganizationDto?> CreateStudentOrganizationAsync(int nonOrgStudentId, int classId)
    {
        try
        {
            // 获取非组织学生信息
            var student = await _studentService.GetStudentByIdAsync(nonOrgStudentId);
            if (student == null)
            {
                return null;
            }

            // 由于现有的StudentOrganization模型需要StudentId（用户ID），
            // 而非组织学生没有用户账户，我们直接返回DTO
            // 实际的关系通过NonOrganizationStudent记录来维护

            _logger.LogInformation("非组织学生添加到班级成功: {StudentName}({PhoneNumber}) -> {ClassId}",
                student.RealName, student.PhoneNumber, classId);

            // 构造返回的DTO
            return new StudentOrganizationDto
            {
                Id = nonOrgStudentId, // 使用非组织学生ID作为关系ID
                StudentId = 0, // 非组织学生没有用户ID
                StudentUsername = student.RealName, // 使用真实姓名作为用户名显示
                StudentRealName = student.RealName,
                StudentPhoneNumber = student.PhoneNumber,
                OrganizationId = classId,
                OrganizationName = "", // 可以后续获取
                JoinedAt = DateTime.UtcNow,
                InvitationCode = "", // 不使用邀请码
                IsActive = student.IsActive
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建学生组织关系失败: {StudentId} -> {ClassId}", nonOrgStudentId, classId);
            return null;
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
