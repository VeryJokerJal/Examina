using System.Security.Claims;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Organization;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Requests;
using ExaminaWebApplication.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClassMembersApiController> _logger;

    public ClassMembersApiController(
        IOrganizationService organizationService,
        INonOrganizationStudentService studentService,
        ApplicationDbContext context,
        ILogger<ClassMembersApiController> logger)
    {
        _organizationService = organizationService;
        _studentService = studentService;
        _context = context;
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

            // 检查手机号在当前班级中是否已存在
            bool phoneExistsInClass = await CheckPhoneExistsInClassAsync(request.PhoneNumber, classId);
            if (phoneExistsInClass)
            {
                return BadRequest(new { message = "该手机号码在当前班级中已存在，请使用其他手机号码" });
            }

            // 创建非组织学生记录
            NonOrganizationStudentDto? studentDto = await _studentService.CreateStudentAsync(
                request.RealName,
                request.PhoneNumber,
                operatorUserId,
                request.Notes);

            if (studentDto == null)
            {
                return BadRequest(new { message = "创建学生记录失败" });
            }

            // 将学生添加到班级
            StudentOrganizationDto? result = await CreateStudentOrganizationAsync(studentDto.Id, classId);

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
    /// 创建学生组织关系
    /// </summary>
    private async Task<StudentOrganizationDto?> CreateStudentOrganizationAsync(int nonOrgStudentId, int classId)
    {
        try
        {
            // 获取非组织学生信息
            NonOrganizationStudentDto? student = await _studentService.GetStudentByIdAsync(nonOrgStudentId);
            if (student == null)
            {
                return null;
            }

            // 获取当前用户ID
            int operatorUserId = GetCurrentUserId();

            // 尝试创建关联关系（如果关联表存在）
            int relationId = nonOrgStudentId; // 默认使用学生ID作为关系ID
            try
            {
                // 检查是否已存在关联关系
                bool relationExists = await _context.NonOrganizationStudentOrganizations
                    .AnyAsync(noso => noso.NonOrganizationStudentId == nonOrgStudentId &&
                                     noso.OrganizationId == classId &&
                                     noso.IsActive);

                if (relationExists)
                {
                    _logger.LogWarning("非组织学生已在班级中: {StudentId} -> {ClassId}", nonOrgStudentId, classId);
                    return null;
                }

                // 创建关联关系
                NonOrganizationStudentOrganization relation = new NonOrganizationStudentOrganization
                {
                    NonOrganizationStudentId = nonOrgStudentId,
                    OrganizationId = classId,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = operatorUserId
                };

                _context.NonOrganizationStudentOrganizations.Add(relation);
                await _context.SaveChangesAsync();

                relationId = relation.Id;
                _logger.LogInformation("成功创建非组织学生关联关系: {StudentId} -> {ClassId}, 关系ID: {RelationId}",
                    nonOrgStudentId, classId, relationId);
            }
            catch (Exception ex)
            {
                // 如果关联表不存在，记录警告但继续执行
                _logger.LogWarning(ex, "创建非组织学生关联关系时出错，可能表尚未创建: {StudentId} -> {ClassId}", nonOrgStudentId, classId);
            }

            _logger.LogInformation("非组织学生添加到班级成功: {StudentName}({PhoneNumber}) -> {ClassId}",
                student.RealName, student.PhoneNumber, classId);

            // 构造返回的DTO
            return new StudentOrganizationDto
            {
                Id = relationId, // 使用关联关系ID或学生ID
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
    /// 检查手机号在指定班级中是否已存在
    /// </summary>
    private async Task<bool> CheckPhoneExistsInClassAsync(string phoneNumber, int classId)
    {
        try
        {
            // 检查注册用户学生
            bool existsInRegisteredStudents = await _context.StudentOrganizations
                .Include(so => so.Student)
                .AnyAsync(so => so.OrganizationId == classId &&
                               so.IsActive &&
                               so.Student.PhoneNumber == phoneNumber);

            if (existsInRegisteredStudents)
            {
                return true;
            }

            // 检查非组织学生（如果关联表存在）
            try
            {
                bool existsInNonOrgStudents = await _context.NonOrganizationStudentOrganizations
                    .Include(noso => noso.NonOrganizationStudent)
                    .AnyAsync(noso => noso.OrganizationId == classId &&
                                     noso.IsActive &&
                                     noso.NonOrganizationStudent.PhoneNumber == phoneNumber);

                return existsInNonOrgStudents;
            }
            catch (Exception ex)
            {
                // 如果关联表不存在，记录警告但不影响主流程
                _logger.LogWarning(ex, "检查非组织学生关联表时出错，可能表尚未创建: {PhoneNumber}, 班级: {ClassId}", phoneNumber, classId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查手机号在班级中是否存在时出错: {PhoneNumber}, 班级: {ClassId}", phoneNumber, classId);
            return false;
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
