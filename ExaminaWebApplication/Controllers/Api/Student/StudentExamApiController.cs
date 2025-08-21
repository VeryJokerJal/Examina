using System.Security.Claims;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Services.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers.Api.Student;

/// <summary>
/// 学生端考试API控制器
/// </summary>
[ApiController]
[Route("api/student/exams")]
[Authorize(Roles = "Student")]
public class StudentExamApiController : ControllerBase
{
    private readonly IStudentExamService _studentExamService;
    private readonly ILogger<StudentExamApiController> _logger;

    public StudentExamApiController(
        IStudentExamService studentExamService,
        ILogger<StudentExamApiController> logger)
    {
        _studentExamService = studentExamService;
        _logger = logger;
    }

    /// <summary>
    /// 获取学生可访问的考试列表
    /// </summary>
    /// <param name="pageNumber">页码，默认为1</param>
    /// <param name="pageSize">页大小，默认为50</param>
    /// <returns>考试列表</returns>
    [HttpGet]
    public async Task<ActionResult<List<StudentExamDto>>> GetAvailableExams(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            // 验证分页参数
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize is < 1 or > 100)
            {
                pageSize = 50;
            }

            List<StudentExamDto> exams = await _studentExamService.GetAvailableExamsAsync(
                studentUserId, pageNumber, pageSize);

            _logger.LogInformation("学生获取可访问考试列表成功，学生ID: {StudentUserId}, 页码: {PageNumber}, 页大小: {PageSize}, 返回数量: {Count}",
                studentUserId, pageNumber, pageSize, exams.Count);

            return Ok(exams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问考试列表失败");
            return StatusCode(500, new { message = "获取考试列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取考试详情
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试详情</returns>
    [HttpGet("{examId}")]
    public async Task<ActionResult<StudentExamDto>> GetExamDetails(int examId)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            StudentExamDto? exam = await _studentExamService.GetExamDetailsAsync(examId, studentUserId);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在或学生无权限访问，学生ID: {StudentUserId}, 考试ID: {ExamId}",
                    studentUserId, examId);
                return NotFound(new { message = "考试不存在或您无权限访问" });
            }

            _logger.LogInformation("学生获取考试详情成功，学生ID: {StudentUserId}, 考试ID: {ExamId}",
                studentUserId, examId);

            return Ok(exam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试详情失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "获取考试详情失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 检查学生是否有权限访问指定考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>权限检查结果</returns>
    [HttpGet("{examId}/access")]
    public async Task<ActionResult<bool>> CheckExamAccess(int examId)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool hasAccess = await _studentExamService.HasAccessToExamAsync(examId, studentUserId);

            _logger.LogInformation("学生考试权限检查完成，学生ID: {StudentUserId}, 考试ID: {ExamId}, 有权限: {HasAccess}",
                studentUserId, examId, hasAccess);

            return Ok(hasAccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查考试访问权限失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "检查权限失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取学生可访问的考试总数
    /// </summary>
    /// <returns>考试总数</returns>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetAvailableExamCount()
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            int count = await _studentExamService.GetAvailableExamCountAsync(studentUserId);

            _logger.LogInformation("学生获取可访问考试总数成功，学生ID: {StudentUserId}, 总数: {Count}",
                studentUserId, count);

            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问考试总数失败");
            return StatusCode(500, new { message = "获取考试总数失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>用户ID</returns>
    /// <exception cref="UnauthorizedAccessException">无法获取用户信息时抛出</exception>
    private int GetCurrentUserId()
    {
        string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)
            ? throw new UnauthorizedAccessException("无法获取当前用户信息")
            : userId;
    }
}
