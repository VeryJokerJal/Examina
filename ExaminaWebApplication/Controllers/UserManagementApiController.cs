using System.Security.Claims;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Organization.Dto;
using ExaminaWebApplication.Models.Requests;
using ExaminaWebApplication.Services.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 用户管理API控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator,Teacher")]
public class UserManagementApiController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<UserManagementApiController> _logger;

    public UserManagementApiController(
        IUserManagementService userManagementService,
        ILogger<UserManagementApiController> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            int creatorUserId = GetCurrentUserId();
            UserDto? user = null;

            if (request.Role == UserRole.Student)
            {
                user = await _userManagementService.CreateStudentUserAsync(
                    request.Username,
                    request.Email,
                    request.PhoneNumber,
                    request.Password,
                    request.RealName,
                    creatorUserId);
            }
            else if (request.Role == UserRole.Teacher)
            {
                user = await _userManagementService.CreateTeacherUserAsync(
                    request.Username,
                    request.Email,
                    request.PhoneNumber,
                    request.Password,
                    request.RealName,
                    request.SchoolId,
                    request.ClassIds,
                    creatorUserId);
            }
            else
            {
                return BadRequest(new { message = "不支持的用户角色" });
            }

            if (user == null)
            {
                return BadRequest(new { message = "创建用户失败，可能是用户名或邮箱已存在" });
            }

            _logger.LogInformation("用户创建成功: {Username}, 角色: {Role}, 创建者: {CreatorUserId}",
                request.Username, request.Role, creatorUserId);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建用户失败: {Username}, 角色: {Role}", request.Username, request.Role);
            return StatusCode(500, new { message = "创建用户失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers(
        [FromQuery] UserRole? role = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            List<UserDto> users = await _userManagementService.GetUsersAsync(role, includeInactive, pageNumber, pageSize);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户列表失败");
            return StatusCode(500, new { message = "获取用户列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取用户详情
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserDto>> GetUserById(int userId)
    {
        try
        {
            UserDto? user = await _userManagementService.GetUserByIdAsync(userId);
            return user == null ? (ActionResult<UserDto>)NotFound(new { message = "用户不存在" }) : (ActionResult<UserDto>)Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户详情失败: {UserId}", userId);
            return StatusCode(500, new { message = "获取用户详情失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    [HttpPut("{userId}")]
    public async Task<ActionResult<UserDto>> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            int updaterUserId = GetCurrentUserId();
            UserDto? user = await _userManagementService.UpdateUserAsync(
                userId,
                request.Email,
                request.PhoneNumber,
                request.RealName,
                updaterUserId);

            if (user == null)
            {
                return NotFound(new { message = "用户不存在或更新失败" });
            }

            _logger.LogInformation("用户更新成功: {UserId}, 更新者: {UpdaterUserId}", userId, updaterUserId);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新用户失败: {UserId}", userId);
            return StatusCode(500, new { message = "更新用户失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 停用用户
    /// </summary>
    [HttpPost("{userId}/deactivate")]
    public async Task<ActionResult> DeactivateUser(int userId)
    {
        try
        {
            int updaterUserId = GetCurrentUserId();
            bool success = await _userManagementService.DeactivateUserAsync(userId, updaterUserId);
            if (!success)
            {
                return NotFound(new { message = "用户不存在或停用失败" });
            }

            _logger.LogInformation("用户停用成功: {UserId}, 操作者: {UpdaterUserId}", userId, updaterUserId);
            return Ok(new { message = "用户停用成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停用用户失败: {UserId}", userId);
            return StatusCode(500, new { message = "停用用户失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 激活用户
    /// </summary>
    [HttpPost("{userId}/activate")]
    public async Task<ActionResult> ActivateUser(int userId)
    {
        try
        {
            int updaterUserId = GetCurrentUserId();
            bool success = await _userManagementService.ActivateUserAsync(userId, updaterUserId);
            if (!success)
            {
                return NotFound(new { message = "用户不存在或激活失败" });
            }

            _logger.LogInformation("用户激活成功: {UserId}, 操作者: {UpdaterUserId}", userId, updaterUserId);
            return Ok(new { message = "用户激活成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "激活用户失败: {UserId}", userId);
            return StatusCode(500, new { message = "激活用户失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 重置用户密码
    /// </summary>
    [HttpPost("{userId}/reset-password")]
    public async Task<ActionResult> ResetUserPassword(int userId, [FromBody] ResetPasswordRequest request)
    {
        try
        {
            int updaterUserId = GetCurrentUserId();
            bool success = await _userManagementService.ResetUserPasswordAsync(userId, request.NewPassword, updaterUserId);
            if (!success)
            {
                return NotFound(new { message = "用户不存在或重置密码失败" });
            }

            _logger.LogInformation("用户密码重置成功: {UserId}, 操作者: {UpdaterUserId}", userId, updaterUserId);
            return Ok(new { message = "密码重置成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置用户密码失败: {UserId}", userId);
            return StatusCode(500, new { message = "重置密码失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 搜索用户
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<UserDto>>> SearchUsers(
        [FromQuery] string keyword,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { message = "搜索关键词不能为空" });
            }

            List<UserDto> users = await _userManagementService.SearchUsersAsync(keyword, role, includeInactive);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索用户失败: {Keyword}", keyword);
            return StatusCode(500, new { message = "搜索用户失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取教师用户列表
    /// </summary>
    [HttpGet("teachers")]
    public async Task<ActionResult<List<UserDto>>> GetTeachers([FromQuery] bool includeInactive = false)
    {
        try
        {
            List<UserDto> teachers = await _userManagementService.GetTeachersAsync(includeInactive);
            return Ok(teachers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取教师用户列表失败");
            return StatusCode(500, new { message = "获取教师用户列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取学生用户列表
    /// </summary>
    [HttpGet("students")]
    public async Task<ActionResult<List<UserDto>>> GetStudents([FromQuery] bool includeInactive = false)
    {
        try
        {
            List<UserDto> students = await _userManagementService.GetStudentsAsync(includeInactive);
            return Ok(students);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生用户列表失败");
            return StatusCode(500, new { message = "获取学生用户列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 切换用户的组织成员身份
    /// </summary>
    [HttpPost("{userId}/toggle-organization-membership")]
    public async Task<ActionResult> ToggleOrganizationMembership(int userId)
    {
        try
        {
            _logger.LogInformation("开始切换用户组织成员身份: 用户ID: {UserId}", userId);

            int operatorUserId = GetCurrentUserId();
            _logger.LogInformation("操作者用户ID: {OperatorUserId}", operatorUserId);

            (bool success, string message) = await _userManagementService.ToggleOrganizationMembershipAsync(userId, operatorUserId);

            if (success)
            {
                _logger.LogInformation("组织成员身份切换成功: 用户ID: {UserId}, 操作者: {OperatorUserId}", userId, operatorUserId);
                return Ok(new { message });
            }
            else
            {
                _logger.LogWarning("组织成员身份切换失败: 用户ID: {UserId}, 操作者: {OperatorUserId}, 原因: {Message}", userId, operatorUserId, message);
                return BadRequest(new { message });
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "权限不足: {Message}", ex.Message);
            return Unauthorized(new { message = "权限不足" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换用户组织成员身份失败: 用户ID: {UserId}", userId);
            return StatusCode(500, new { message = "切换组织成员身份失败", error = ex.Message });
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
