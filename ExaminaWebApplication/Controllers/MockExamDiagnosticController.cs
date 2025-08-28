using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Services.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MockExam = ExaminaWebApplication.Models.MockExam.MockExam;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 模拟考试诊断控制器 - 用于排查权限问题
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MockExamDiagnosticController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IStudentMockExamService _mockExamService;
    private readonly ILogger<MockExamDiagnosticController> _logger;

    public MockExamDiagnosticController(
        ApplicationDbContext context,
        IStudentMockExamService mockExamService,
        ILogger<MockExamDiagnosticController> logger)
    {
        _context = context;
        _mockExamService = mockExamService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户的所有模拟考试
    /// </summary>
    [HttpGet("my-mock-exams")]
    public async Task<ActionResult> GetMyMockExams()
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            var mockExams = await _context.MockExams
                .Where(me => me.StudentId == studentUserId)
                .OrderByDescending(me => me.CreatedAt)
                .Select(me => new
                {
                    me.Id,
                    me.Name,
                    me.Status,
                    me.StudentId,
                    me.CreatedAt,
                    me.StartedAt,
                    me.CompletedAt,
                    me.DurationMinutes
                })
                .ToListAsync();

            return Ok(new
            {
                CurrentUserId = studentUserId,
                MockExamCount = mockExams.Count,
                MockExams = mockExams
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户模拟考试列表失败");
            return StatusCode(500, new { message = "服务器内部错误", error = ex.Message });
        }
    }

    /// <summary>
    /// 检查特定模拟考试的详细信息
    /// </summary>
    [HttpGet("mock-exam/{id}/details")]
    public async Task<ActionResult> GetMockExamDetails(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            var mockExam = await _context.MockExams
                .FirstOrDefaultAsync(me => me.Id == id);

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId);

            bool hasAccess = await _mockExamService.HasAccessToMockExamAsync(id, studentUserId);

            return Ok(new
            {
                RequestInfo = new
                {
                    MockExamId = id,
                    RequestingUserId = studentUserId,
                    RequestTime = DateTime.Now
                },
                MockExam = mockExam != null ? new
                {
                    mockExam.Id,
                    mockExam.StudentId,
                    mockExam.Name,
                    mockExam.Status,
                    mockExam.CreatedAt,
                    mockExam.StartedAt,
                    mockExam.CompletedAt,
                    mockExam.DurationMinutes,
                    mockExam.TotalScore
                } : null,
                CurrentUser = currentUser != null ? new
                {
                    currentUser.Id,
                    currentUser.Username,
                    currentUser.Email,
                    currentUser.Role,
                    currentUser.IsActive,
                    currentUser.RealName
                } : null,
                AccessCheck = new
                {
                    HasAccess = hasAccess,
                    MockExamExists = mockExam != null,
                    UserExists = currentUser != null,
                    StudentIdMatches = mockExam?.StudentId == studentUserId,
                    UserIsStudent = currentUser?.Role == UserRole.Student,
                    UserIsActive = currentUser?.IsActive == true
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模拟考试详细信息失败，模拟考试ID: {MockExamId}", id);
            return StatusCode(500, new { message = "服务器内部错误", error = ex.Message });
        }
    }

    /// <summary>
    /// 修复模拟考试权限问题（仅限开发环境）
    /// </summary>
    [HttpPost("mock-exam/{id}/fix-permission")]
    public async Task<ActionResult> FixMockExamPermission(int id, [FromQuery] bool dryRun = true)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            var mockExam = await _context.MockExams
                .FirstOrDefaultAsync(me => me.Id == id);

            if (mockExam == null)
            {
                return NotFound(new { message = "模拟考试不存在" });
            }

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId);

            if (currentUser == null)
            {
                return BadRequest(new { message = "当前用户不存在" });
            }

            var fixActions = new List<string>();

            // 检查需要修复的问题
            if (mockExam.StudentId != studentUserId)
            {
                fixActions.Add($"将模拟考试的StudentId从 {mockExam.StudentId} 修改为 {studentUserId}");
                
                if (!dryRun)
                {
                    mockExam.StudentId = studentUserId;
                }
            }

            if (currentUser.Role != UserRole.Student)
            {
                fixActions.Add($"将用户角色从 {currentUser.Role} 修改为 Student");
                
                if (!dryRun)
                {
                    currentUser.Role = UserRole.Student;
                }
            }

            if (!currentUser.IsActive)
            {
                fixActions.Add("激活用户账号");
                
                if (!dryRun)
                {
                    currentUser.IsActive = true;
                }
            }

            if (!dryRun && fixActions.Count > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("修复模拟考试权限问题，模拟考试ID: {MockExamId}, 用户ID: {UserId}, 修复操作: {Actions}",
                    id, studentUserId, string.Join(", ", fixActions));
            }

            return Ok(new
            {
                MockExamId = id,
                UserId = studentUserId,
                DryRun = dryRun,
                FixActions = fixActions,
                Message = dryRun ? "预览修复操作（未实际执行）" : 
                         fixActions.Count > 0 ? "权限问题已修复" : "未发现需要修复的问题"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "修复模拟考试权限问题失败，模拟考试ID: {MockExamId}", id);
            return StatusCode(500, new { message = "服务器内部错误", error = ex.Message });
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
