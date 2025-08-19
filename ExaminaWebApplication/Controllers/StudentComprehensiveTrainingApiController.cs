using System.Security.Claims;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Models.Dto;
using ExaminaWebApplication.Services.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 学生端综合训练API控制器
/// </summary>
[ApiController]
[Route("api/student/comprehensive-trainings")]
[Authorize(Roles = "Student")]
public class StudentComprehensiveTrainingApiController : ControllerBase
{
    private readonly IStudentComprehensiveTrainingService _studentComprehensiveTrainingService;
    private readonly ILogger<StudentComprehensiveTrainingApiController> _logger;

    public StudentComprehensiveTrainingApiController(
        IStudentComprehensiveTrainingService studentComprehensiveTrainingService,
        ILogger<StudentComprehensiveTrainingApiController> logger)
    {
        _studentComprehensiveTrainingService = studentComprehensiveTrainingService;
        _logger = logger;
    }

    /// <summary>
    /// 获取学生可访问的综合训练列表
    /// </summary>
    /// <param name="pageNumber">页码，默认为1</param>
    /// <param name="pageSize">页大小，默认为50</param>
    /// <returns>综合训练列表</returns>
    [HttpGet]
    public async Task<ActionResult<List<StudentComprehensiveTrainingDto>>> GetAvailableTrainings(
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

            List<StudentComprehensiveTrainingDto> trainings = await _studentComprehensiveTrainingService.GetAvailableTrainingsAsync(
                studentUserId, pageNumber, pageSize);

            _logger.LogInformation("学生获取可访问综合训练列表成功，学生ID: {StudentUserId}, 页码: {PageNumber}, 页大小: {PageSize}, 返回数量: {Count}",
                studentUserId, pageNumber, pageSize, trainings.Count);

            return Ok(trainings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问综合训练列表失败");
            return StatusCode(500, new { message = "获取综合训练列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取综合训练详情
    /// </summary>
    /// <param name="trainingId">综合训练ID</param>
    /// <returns>综合训练详情</returns>
    [HttpGet("{trainingId}")]
    public async Task<ActionResult<StudentComprehensiveTrainingDto>> GetTrainingDetails(int trainingId)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            StudentComprehensiveTrainingDto? training = await _studentComprehensiveTrainingService.GetTrainingDetailsAsync(trainingId, studentUserId);

            if (training == null)
            {
                _logger.LogWarning("综合训练不存在或学生无权限访问，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                    studentUserId, trainingId);
                return NotFound(new { message = "综合训练不存在或您无权限访问" });
            }

            _logger.LogInformation("学生获取综合训练详情成功，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                studentUserId, trainingId);

            return Ok(training);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取综合训练详情失败，训练ID: {TrainingId}", trainingId);
            return StatusCode(500, new { message = "获取综合训练详情失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 检查学生是否有权限访问指定综合训练
    /// </summary>
    /// <param name="trainingId">综合训练ID</param>
    /// <returns>权限检查结果</returns>
    [HttpGet("{trainingId}/access")]
    public async Task<ActionResult<bool>> CheckTrainingAccess(int trainingId)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool hasAccess = await _studentComprehensiveTrainingService.HasAccessToTrainingAsync(trainingId, studentUserId);

            _logger.LogInformation("学生综合训练权限检查完成，学生ID: {StudentUserId}, 训练ID: {TrainingId}, 有权限: {HasAccess}",
                studentUserId, trainingId, hasAccess);

            return Ok(hasAccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查综合训练访问权限失败，训练ID: {TrainingId}", trainingId);
            return StatusCode(500, new { message = "检查权限失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取学生可访问的综合训练总数
    /// </summary>
    /// <returns>综合训练总数</returns>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetAvailableTrainingCount()
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            int count = await _studentComprehensiveTrainingService.GetAvailableTrainingCountAsync(studentUserId);

            _logger.LogInformation("学生获取可访问综合训练总数成功，学生ID: {StudentUserId}, 总数: {Count}",
                studentUserId, count);

            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问综合训练总数失败");
            return StatusCode(500, new { message = "获取综合训练总数失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取学生综合训练进度统计
    /// </summary>
    /// <returns>综合训练进度统计</returns>
    [HttpGet("progress")]
    public async Task<ActionResult<ComprehensiveTrainingProgressDto>> GetTrainingProgress()
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            ComprehensiveTrainingProgressDto progress = await _studentComprehensiveTrainingService.GetTrainingProgressAsync(studentUserId);

            _logger.LogInformation("学生获取综合训练进度统计成功，学生ID: {StudentUserId}, 总数: {TotalCount}, 完成数: {CompletedCount}",
                studentUserId, progress.TotalCount, progress.CompletedCount);

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生综合训练进度统计失败");
            return StatusCode(500, new { message = "获取综合训练进度统计失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 标记综合训练为开始状态
    /// </summary>
    /// <param name="id">训练ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/start")]
    public async Task<ActionResult> StartTraining(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _studentComprehensiveTrainingService.MarkTrainingAsStartedAsync(studentUserId, id);

            if (success)
            {
                _logger.LogInformation("学生开始综合训练成功，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                    studentUserId, id);

                return Ok(new { message = "训练开始标记成功" });
            }
            else
            {
                return BadRequest(new { message = "训练开始标记失败，请检查训练是否存在或已完成" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记综合训练开始失败，训练ID: {TrainingId}", id);
            return StatusCode(500, new { message = "标记训练开始失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 标记综合训练为已完成
    /// </summary>
    /// <param name="id">训练ID</param>
    /// <param name="request">完成信息</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/complete")]
    public async Task<ActionResult> CompleteTraining(int id, [FromBody] CompleteTrainingRequest request)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _studentComprehensiveTrainingService.MarkTrainingAsCompletedAsync(
                studentUserId,
                id,
                request.Score,
                request.MaxScore,
                request.DurationSeconds,
                request.Notes);

            if (success)
            {
                _logger.LogInformation("学生完成综合训练成功，学生ID: {StudentUserId}, 训练ID: {TrainingId}, 得分: {Score}",
                    studentUserId, id, request.Score);

                return Ok(new { message = "训练完成标记成功" });
            }
            else
            {
                return BadRequest(new { message = "训练完成标记失败，请检查训练是否存在" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记综合训练完成失败，训练ID: {TrainingId}", id);
            return StatusCode(500, new { message = "标记训练完成失败", error = ex.Message });
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

/// <summary>
/// 完成训练请求DTO
/// </summary>
public class CompleteTrainingRequest
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
