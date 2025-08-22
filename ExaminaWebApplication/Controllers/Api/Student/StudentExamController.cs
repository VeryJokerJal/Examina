using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Services.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ExaminaWebApplication.Models;

namespace ExaminaWebApplication.Controllers.Api.Student;

/// <summary>
/// 学生正式考试API控制器
/// </summary>
[ApiController]
[Route("api/student/exams")]
[Authorize(Roles = "Student")]
public class StudentExamController : ControllerBase
{
    private readonly IStudentExamService _examService;
    private readonly ILogger<StudentExamController> _logger;

    public StudentExamController(IStudentExamService examService, ILogger<StudentExamController> logger)
    {
        _examService = examService;
        _logger = logger;
    }

    /// <summary>
    /// 获取学生可访问的考试列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>考试列表</returns>
    [HttpGet]
    public async Task<ActionResult<List<StudentExamDto>>> GetAvailableExams(int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            int studentUserId = GetCurrentUserId();
            List<StudentExamDto> exams = await _examService.GetAvailableExamsAsync(studentUserId, pageNumber, pageSize);

            _logger.LogInformation("获取学生可访问考试列表成功，学生ID: {StudentId}, 考试数量: {Count}",
                studentUserId, exams.Count);

            return Ok(exams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问考试列表失败");
            return StatusCode(500, new { message = "获取考试列表失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取考试详情
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>考试详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<StudentExamDto>> GetExamDetails(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();
            StudentExamDto? exam = await _examService.GetExamDetailsAsync(id, studentUserId);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在或学生无权限访问，学生ID: {StudentId}, 考试ID: {ExamId}",
                    studentUserId, id);
                return NotFound(new { message = "考试不存在或您无权限访问" });
            }

            _logger.LogInformation("获取考试详情成功，学生ID: {StudentId}, 考试ID: {ExamId}",
                studentUserId, id);

            return Ok(exam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试详情失败，考试ID: {ExamId}", id);
            return StatusCode(500, new { message = "获取考试详情失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 开始正式考试
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/start")]
    public async Task<ActionResult> StartExam(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _examService.StartExamAsync(id, studentUserId);
            if (!success)
            {
                _logger.LogWarning("开始正式考试失败，学生ID: {StudentId}, 考试ID: {ExamId}",
                    studentUserId, id);
                return BadRequest(new { message = "无法开始考试，请检查考试状态或权限" });
            }

            _logger.LogInformation("学生开始正式考试成功，学生ID: {StudentId}, 考试ID: {ExamId}",
                studentUserId, id);

            return Ok(new { message = "考试已开始" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始正式考试异常，考试ID: {ExamId}", id);
            return StatusCode(500, new { message = "开始考试失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 提交正式考试成绩
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <param name="scoreRequest">成绩数据</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/score")]
    public async Task<ActionResult> SubmitExamScore(int id, [FromBody] SubmitExamScoreRequestDto scoreRequest)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _examService.SubmitExamScoreAsync(id, studentUserId, scoreRequest);
            if (!success)
            {
                _logger.LogWarning("提交正式考试成绩失败，学生ID: {StudentId}, 考试ID: {ExamId}",
                    studentUserId, id);
                return BadRequest(new { message = "无法提交考试成绩，请检查考试状态或权限" });
            }

            _logger.LogInformation("学生提交正式考试成绩成功，学生ID: {StudentId}, 考试ID: {ExamId}",
                studentUserId, id);

            return Ok(new { message = "考试成绩已提交" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交正式考试成绩异常，考试ID: {ExamId}", id);
            return StatusCode(500, new { message = "提交成绩失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 完成正式考试（不包含成绩）
    /// </summary>
    /// <param name="id">考试ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/complete")]
    public async Task<ActionResult> CompleteExam(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _examService.CompleteExamAsync(id, studentUserId);
            if (!success)
            {
                _logger.LogWarning("完成正式考试失败，学生ID: {StudentId}, 考试ID: {ExamId}",
                    studentUserId, id);
                return BadRequest(new { message = "无法完成考试，请检查考试状态或权限" });
            }

            _logger.LogInformation("学生完成正式考试成功，学生ID: {StudentId}, 考试ID: {ExamId}",
                studentUserId, id);

            return Ok(new { message = "考试已完成" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成正式考试异常，考试ID: {ExamId}", id);
            return StatusCode(500, new { message = "完成考试失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取学生的考试完成记录
    /// </summary>
    /// <param name="examId">考试ID（可选）</param>
    /// <returns>考试完成记录列表</returns>
    [HttpGet("completions")]
    public async Task<ActionResult<List<ExamCompletion>>> GetExamCompletions(int? examId = null)
    {
        try
        {
            int studentUserId = GetCurrentUserId();
            List<ExamCompletion> completions = await _examService.GetExamCompletionsAsync(studentUserId, examId);

            _logger.LogInformation("获取学生考试完成记录成功，学生ID: {StudentId}, 记录数: {Count}",
                studentUserId, completions.Count);

            return Ok(completions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生考试完成记录失败");
            return StatusCode(500, new { message = "获取完成记录失败，请稍后重试" });
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
