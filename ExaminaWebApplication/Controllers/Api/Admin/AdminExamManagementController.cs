using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExaminaWebApplication.Services.Admin;
using ExaminaWebApplication.Models.Api.Admin;
using ExaminaWebApplication.Models.ImportedExam;

namespace ExaminaWebApplication.Controllers.Api.Admin;

/// <summary>
/// 管理员考试管理API控制器
/// </summary>
[ApiController]
[Route("api/admin/exam-management")]
[Authorize(Roles = "Admin,Teacher")]
public class AdminExamManagementController : ControllerBase
{
    private readonly IAdminExamManagementService _adminExamManagementService;
    private readonly ILogger<AdminExamManagementController> _logger;

    public AdminExamManagementController(
        IAdminExamManagementService adminExamManagementService,
        ILogger<AdminExamManagementController> logger)
    {
        _adminExamManagementService = adminExamManagementService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户的考试列表
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>考试列表</returns>
    [HttpGet]
    public async Task<ActionResult<List<AdminExamDto>>> GetExams(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            int userId = GetCurrentUserId();
            
            // 验证分页参数
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize is < 1 or > 100) pageSize = 20;

            List<AdminExamDto> exams = await _adminExamManagementService.GetExamsAsync(
                userId, pageNumber, pageSize);

            _logger.LogInformation("管理员获取考试列表成功，用户ID: {UserId}, 页码: {PageNumber}, 页大小: {PageSize}, 结果数量: {Count}",
                userId, pageNumber, pageSize, exams.Count);

            return Ok(exams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试列表失败");
            return StatusCode(500, new { message = "获取考试列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取考试详情
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>考试详情</returns>
    [HttpGet("{examId}")]
    public async Task<ActionResult<AdminExamDto>> GetExamDetails(int examId)
    {
        try
        {
            int userId = GetCurrentUserId();

            AdminExamDto? exam = await _adminExamManagementService.GetExamDetailsAsync(examId, userId);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在或无权限访问，用户ID: {UserId}, 考试ID: {ExamId}", userId, examId);
                return NotFound(new { message = "考试不存在或您无权限访问" });
            }

            _logger.LogInformation("管理员获取考试详情成功，用户ID: {UserId}, 考试ID: {ExamId}", userId, examId);

            return Ok(exam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试详情失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "获取考试详情失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 设置考试时间
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="request">时间设置请求</param>
    /// <returns>操作结果</returns>
    [HttpPut("{examId}/schedule")]
    public async Task<ActionResult> SetExamSchedule(int examId, [FromBody] SetExamScheduleRequestDto request)
    {
        try
        {
            int userId = GetCurrentUserId();

            // 验证请求数据
            if (request.StartTime >= request.EndTime)
            {
                return BadRequest(new { message = "开始时间必须早于结束时间" });
            }

            if (request.StartTime <= DateTime.Now)
            {
                return BadRequest(new { message = "开始时间必须晚于当前时间" });
            }

            bool success = await _adminExamManagementService.SetExamScheduleAsync(
                examId, userId, request.StartTime, request.EndTime);

            if (!success)
            {
                _logger.LogWarning("设置考试时间失败，用户ID: {UserId}, 考试ID: {ExamId}", userId, examId);
                return BadRequest(new { message = "设置考试时间失败，考试不存在或您无权限操作" });
            }

            _logger.LogInformation("设置考试时间成功，用户ID: {UserId}, 考试ID: {ExamId}, 开始时间: {StartTime}, 结束时间: {EndTime}",
                userId, examId, request.StartTime, request.EndTime);

            return Ok(new { message = "考试时间设置成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置考试时间失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "设置考试时间失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新考试状态
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="request">状态更新请求</param>
    /// <returns>操作结果</returns>
    [HttpPut("{examId}/status")]
    public async Task<ActionResult> UpdateExamStatus(int examId, [FromBody] UpdateExamStatusRequestDto request)
    {
        try
        {
            int userId = GetCurrentUserId();

            bool success = await _adminExamManagementService.UpdateExamStatusAsync(
                examId, userId, request.Status);

            if (!success)
            {
                _logger.LogWarning("更新考试状态失败，用户ID: {UserId}, 考试ID: {ExamId}, 状态: {Status}",
                    userId, examId, request.Status);
                return BadRequest(new { message = "更新考试状态失败，考试不存在或您无权限操作" });
            }

            _logger.LogInformation("更新考试状态成功，用户ID: {UserId}, 考试ID: {ExamId}, 状态: {Status}",
                userId, examId, request.Status);

            return Ok(new { message = "考试状态更新成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试状态失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "更新考试状态失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新考试类型（全省统考/学校统考）
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="request">类型更新请求</param>
    /// <returns>操作结果</returns>
    [HttpPut("{examId}/category")]
    public async Task<ActionResult> UpdateExamCategory(int examId, [FromBody] UpdateExamCategoryRequestDto request)
    {
        try
        {
            int userId = GetCurrentUserId();

            bool success = await _adminExamManagementService.UpdateExamCategoryAsync(
                examId, userId, request.Category);

            if (!success)
            {
                _logger.LogWarning("更新考试类型失败，用户ID: {UserId}, 考试ID: {ExamId}, 类型: {Category}",
                    userId, examId, request.Category);
                return BadRequest(new { message = "更新考试类型失败，考试不存在或您无权限操作" });
            }

            string categoryName = request.Category == ExamCategory.Provincial ? "全省统考" : "学校统考";
            _logger.LogInformation("更新考试类型成功，用户ID: {UserId}, 考试ID: {ExamId}, 类型: {Category}",
                userId, examId, request.Category);

            return Ok(new { message = $"考试类型已更新为：{categoryName}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试类型失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "更新考试类型失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 发布考试
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{examId}/publish")]
    public async Task<ActionResult> PublishExam(int examId)
    {
        try
        {
            int userId = GetCurrentUserId();

            bool success = await _adminExamManagementService.PublishExamAsync(examId, userId);

            if (!success)
            {
                _logger.LogWarning("发布考试失败，用户ID: {UserId}, 考试ID: {ExamId}", userId, examId);
                return BadRequest(new { message = "发布考试失败，考试不存在、您无权限操作或考试状态不允许发布" });
            }

            _logger.LogInformation("发布考试成功，用户ID: {UserId}, 考试ID: {ExamId}", userId, examId);

            return Ok(new { message = "考试发布成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布考试失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new { message = "发布考试失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新试卷名称
    /// </summary>
    /// <param name="examId">考试ID</param>
    /// <param name="request">名称更新请求</param>
    /// <returns>操作结果</returns>
    [HttpPut("{examId}/name")]
    public async Task<ActionResult<UpdateExamNameResponseDto>> UpdateExamName(int examId, [FromBody] UpdateExamNameRequestDto request)
    {
        try
        {
            int userId = GetCurrentUserId();

            // 验证请求数据
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new UpdateExamNameResponseDto
                {
                    Success = false,
                    Message = "试卷名称不能为空"
                });
            }

            bool success = await _adminExamManagementService.UpdateExamNameAsync(
                examId, userId, request.Name);

            if (!success)
            {
                _logger.LogWarning("更新试卷名称失败，用户ID: {UserId}, 考试ID: {ExamId}, 新名称: {NewName}",
                    userId, examId, request.Name);
                return BadRequest(new UpdateExamNameResponseDto
                {
                    Success = false,
                    Message = "更新试卷名称失败，试卷不存在、您无权限操作、名称已存在或包含非法字符"
                });
            }

            _logger.LogInformation("更新试卷名称成功，用户ID: {UserId}, 考试ID: {ExamId}, 新名称: {NewName}",
                userId, examId, request.Name);

            return Ok(new UpdateExamNameResponseDto
            {
                Success = true,
                Message = "试卷名称更新成功",
                UpdatedName = request.Name.Trim(),
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新试卷名称失败，考试ID: {ExamId}", examId);
            return StatusCode(500, new UpdateExamNameResponseDto
            {
                Success = false,
                Message = "更新试卷名称失败，服务器内部错误"
            });
        }
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private int GetCurrentUserId()
    {
        string? userIdClaim = User.FindFirst("UserId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("无效的用户身份");
        }
        return userId;
    }
}
