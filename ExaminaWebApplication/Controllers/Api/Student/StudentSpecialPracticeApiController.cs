using System.Security.Claims;
using ExaminaWebApplication.Models.Dto;
using ExaminaWebApplication.Services.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers.Api.Student;

/// <summary>
/// 学生专项练习API控制器
/// </summary>
[ApiController]
[Route("api/student/special-practices")]
[Authorize(Roles = "Student")]
public class StudentSpecialPracticeApiController : ControllerBase
{
    private readonly IStudentSpecialPracticeService _studentSpecialPracticeService;
    private readonly ILogger<StudentSpecialPracticeApiController> _logger;

    public StudentSpecialPracticeApiController(
        IStudentSpecialPracticeService studentSpecialPracticeService,
        ILogger<StudentSpecialPracticeApiController> logger)
    {
        _studentSpecialPracticeService = studentSpecialPracticeService;
        _logger = logger;
    }

    /// <summary>
    /// 获取学生可访问的专项练习总数
    /// </summary>
    /// <returns>专项练习总数</returns>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetAvailablePracticeCount()
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            int count = await _studentSpecialPracticeService.GetAvailablePracticeCountAsync(studentUserId);

            _logger.LogInformation("学生获取可访问专项练习总数成功，学生ID: {StudentUserId}, 总数: {Count}",
                studentUserId, count);

            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问专项练习总数失败");
            return StatusCode(500, new { message = "获取专项练习总数失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取学生专项练习进度统计
    /// </summary>
    /// <returns>专项练习进度统计</returns>
    [HttpGet("progress")]
    public async Task<ActionResult<SpecialPracticeProgressDto>> GetPracticeProgress()
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            SpecialPracticeProgressDto progress = await _studentSpecialPracticeService.GetPracticeProgressAsync(studentUserId);

            _logger.LogInformation("学生获取专项练习进度统计成功，学生ID: {StudentUserId}, 总数: {TotalCount}, 完成数: {CompletedCount}",
                studentUserId, progress.TotalCount, progress.CompletedCount);

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生专项练习进度统计失败");
            return StatusCode(500, new { message = "获取专项练习进度统计失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取学生专项练习完成记录
    /// </summary>
    /// <param name="pageNumber">页码，默认为1</param>
    /// <param name="pageSize">页大小，默认为20</param>
    /// <returns>专项练习完成记录列表</returns>
    [HttpGet("completions")]
    public async Task<ActionResult<List<SpecialPracticeCompletionDto>>> GetPracticeCompletions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("开始获取专项练习完成记录，页码: {PageNumber}, 页大小: {PageSize}", pageNumber, pageSize);

            int studentUserId = GetCurrentUserId();
            _logger.LogInformation("获取到当前用户ID: {StudentUserId}", studentUserId);

            // 验证分页参数
            if (pageNumber < 1)
            {
                _logger.LogWarning("页码参数无效: {PageNumber}，重置为1", pageNumber);
                pageNumber = 1;
            }

            if (pageSize is < 1 or > 100)
            {
                _logger.LogWarning("页大小参数无效: {PageSize}，重置为20", pageSize);
                pageSize = 20;
            }

            _logger.LogInformation("调用服务层获取专项练习完成记录，学生ID: {StudentUserId}, 页码: {PageNumber}, 页大小: {PageSize}",
                studentUserId, pageNumber, pageSize);

            List<SpecialPracticeCompletionDto> completions = await _studentSpecialPracticeService.GetPracticeCompletionsAsync(studentUserId, pageNumber, pageSize);

            _logger.LogInformation("学生获取专项练习完成记录成功，学生ID: {StudentUserId}, 页码: {PageNumber}, 页大小: {PageSize}, 记录数: {Count}",
                studentUserId, pageNumber, pageSize, completions.Count);

            return Ok(completions);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "获取专项练习完成记录时用户认证失败");
            return Unauthorized(new { message = "用户认证失败", error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生专项练习完成记录失败");
            return StatusCode(500, new { message = "获取专项练习完成记录失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 标记专项练习为开始状态
    /// </summary>
    /// <param name="id">练习ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/start")]
    public async Task<ActionResult> StartPractice(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _studentSpecialPracticeService.MarkPracticeAsStartedAsync(studentUserId, id);

            if (success)
            {
                _logger.LogInformation("学生开始专项练习成功，学生ID: {StudentUserId}, 练习ID: {PracticeId}",
                    studentUserId, id);

                return Ok(new { message = "练习开始标记成功" });
            }
            else
            {
                return BadRequest(new { message = "练习开始标记失败，请检查练习是否存在或已完成" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记专项练习开始失败，练习ID: {PracticeId}", id);
            return StatusCode(500, new { message = "标记练习开始失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 标记专项练习为已完成
    /// </summary>
    /// <param name="id">练习ID</param>
    /// <param name="request">完成信息</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/complete")]
    public async Task<ActionResult> CompletePractice(int id, [FromBody] CompletePracticeRequest request)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _studentSpecialPracticeService.MarkPracticeAsCompletedAsync(
                studentUserId,
                id,
                request.Score,
                request.MaxScore,
                request.DurationSeconds,
                request.Notes);

            if (success)
            {
                _logger.LogInformation("学生完成专项练习成功，学生ID: {StudentUserId}, 练习ID: {PracticeId}, 得分: {Score}",
                    studentUserId, id, request.Score);

                return Ok(new { message = "练习完成标记成功" });
            }
            else
            {
                return BadRequest(new { message = "练习完成标记失败，请检查练习是否存在" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记专项练习完成失败，练习ID: {PracticeId}", id);
            return StatusCode(500, new { message = "标记练习完成失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private int GetCurrentUserId()
    {
        string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)
            ? throw new UnauthorizedAccessException("无法获取当前用户ID")
            : userId;
    }
}

/// <summary>
/// 完成练习请求DTO
/// </summary>
public class CompletePracticeRequest
{
    /// <summary>
    /// 得分（可选）
    /// </summary>
    public decimal? Score { get; set; }

    /// <summary>
    /// 最大得分（可选）
    /// </summary>
    public decimal? MaxScore { get; set; }

    /// <summary>
    /// 用时（秒，可选）
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// 备注（可选）
    /// </summary>
    public string? Notes { get; set; }
}
