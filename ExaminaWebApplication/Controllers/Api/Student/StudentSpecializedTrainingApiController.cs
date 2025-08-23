using System.Security.Claims;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Services.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExaminaWebApplication.Controllers.Api.Student;

/// <summary>
/// 学生端专项训练API控制器
/// </summary>
[ApiController]
[Route("api/student/specialized-trainings")]
[Authorize(Roles = "Student")]
public class StudentSpecializedTrainingApiController : ControllerBase
{
    private readonly IStudentSpecializedTrainingService _studentSpecializedTrainingService;
    private readonly ILogger<StudentSpecializedTrainingApiController> _logger;

    public StudentSpecializedTrainingApiController(
        IStudentSpecializedTrainingService studentSpecializedTrainingService,
        ILogger<StudentSpecializedTrainingApiController> logger)
    {
        _studentSpecializedTrainingService = studentSpecializedTrainingService;
        _logger = logger;
    }

    /// <summary>
    /// 获取学生可访问的专项训练列表（随机排序）
    /// </summary>
    /// <param name="pageNumber">页码，默认为1</param>
    /// <param name="pageSize">页大小，默认为50</param>
    /// <returns>随机排序的专项训练列表</returns>
    [HttpGet]
    public async Task<ActionResult<List<StudentSpecializedTrainingDto>>> GetAvailableTrainings(
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

            List<StudentSpecializedTrainingDto> trainings = await _studentSpecializedTrainingService.GetAvailableTrainingsAsync(
                studentUserId, pageNumber, pageSize);

            _logger.LogInformation("学生获取可访问专项训练列表成功，学生ID: {StudentUserId}, 页码: {PageNumber}, 页大小: {PageSize}, 返回数量: {Count}",
                studentUserId, pageNumber, pageSize, trainings.Count);

            return Ok(trainings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问专项训练列表失败");
            return StatusCode(500, new { message = "获取专项训练列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取专项训练详情
    /// </summary>
    /// <param name="trainingId">专项训练ID</param>
    /// <returns>专项训练详情</returns>
    [HttpGet("{trainingId}")]
    public async Task<ActionResult<StudentSpecializedTrainingDto>> GetTrainingDetails(int trainingId)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            StudentSpecializedTrainingDto? training = await _studentSpecializedTrainingService.GetTrainingDetailsAsync(trainingId, studentUserId);

            if (training == null)
            {
                _logger.LogWarning("专项训练不存在或学生无权限访问，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                    studentUserId, trainingId);
                return NotFound(new { message = "专项训练不存在或您无权限访问" });
            }

            _logger.LogInformation("学生获取专项训练详情成功，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                studentUserId, trainingId);

            return Ok(training);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取专项训练详情失败，训练ID: {TrainingId}", trainingId);
            return StatusCode(500, new { message = "获取专项训练详情失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 检查学生是否有权限访问指定专项训练
    /// </summary>
    /// <param name="trainingId">专项训练ID</param>
    /// <returns>权限检查结果</returns>
    [HttpGet("{trainingId}/access")]
    public async Task<ActionResult<bool>> CheckTrainingAccess(int trainingId)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool hasAccess = await _studentSpecializedTrainingService.HasAccessToTrainingAsync(trainingId, studentUserId);

            _logger.LogInformation("学生专项训练权限检查完成，学生ID: {StudentUserId}, 训练ID: {TrainingId}, 有权限: {HasAccess}",
                studentUserId, trainingId, hasAccess);

            return Ok(hasAccess);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查专项训练访问权限失败，训练ID: {TrainingId}", trainingId);
            return StatusCode(500, new { message = "检查权限失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取学生可访问的专项训练总数
    /// </summary>
    /// <returns>专项训练总数</returns>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetAvailableTrainingCount()
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            int count = await _studentSpecializedTrainingService.GetAvailableTrainingCountAsync(studentUserId);

            _logger.LogInformation("学生获取可访问专项训练总数成功，学生ID: {StudentUserId}, 总数: {Count}",
                studentUserId, count);

            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问专项训练总数失败");
            return StatusCode(500, new { message = "获取专项训练总数失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据模块类型获取专项训练列表
    /// </summary>
    /// <param name="moduleType">模块类型（如：Windows、Office、Programming等）</param>
    /// <param name="pageNumber">页码，默认为1</param>
    /// <param name="pageSize">页大小，默认为50</param>
    /// <returns>专项训练列表</returns>
    [HttpGet("by-module-type/{moduleType}")]
    public async Task<ActionResult<List<StudentSpecializedTrainingDto>>> GetTrainingsByModuleType(
        string moduleType,
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

            List<StudentSpecializedTrainingDto> trainings = await _studentSpecializedTrainingService.GetTrainingsByModuleTypeAsync(
                studentUserId, moduleType, pageNumber, pageSize);

            _logger.LogInformation("学生根据模块类型获取专项训练列表成功，学生ID: {StudentUserId}, 模块类型: {ModuleType}, 返回数量: {Count}",
                studentUserId, moduleType, trainings.Count);

            return Ok(trainings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据模块类型获取专项训练列表失败，模块类型: {ModuleType}", moduleType);
            return StatusCode(500, new { message = "获取专项训练列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据难度等级获取专项训练列表
    /// </summary>
    /// <param name="difficultyLevel">难度等级（1-5）</param>
    /// <param name="pageNumber">页码，默认为1</param>
    /// <param name="pageSize">页大小，默认为50</param>
    /// <returns>专项训练列表</returns>
    [HttpGet("by-difficulty/{difficultyLevel}")]
    public async Task<ActionResult<List<StudentSpecializedTrainingDto>>> GetTrainingsByDifficulty(
        int difficultyLevel,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            // 验证难度等级参数
            if (difficultyLevel is < 1 or > 5)
            {
                return BadRequest(new { message = "难度等级必须在1-5之间" });
            }

            // 验证分页参数
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            if (pageSize is < 1 or > 100)
            {
                pageSize = 50;
            }

            List<StudentSpecializedTrainingDto> trainings = await _studentSpecializedTrainingService.GetTrainingsByDifficultyAsync(
                studentUserId, difficultyLevel, pageNumber, pageSize);

            _logger.LogInformation("学生根据难度等级获取专项训练列表成功，学生ID: {StudentUserId}, 难度等级: {DifficultyLevel}, 返回数量: {Count}",
                studentUserId, difficultyLevel, trainings.Count);

            return Ok(trainings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据难度等级获取专项训练列表失败，难度等级: {DifficultyLevel}", difficultyLevel);
            return StatusCode(500, new { message = "获取专项训练列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 搜索专项训练
    /// </summary>
    /// <param name="keyword">搜索关键词</param>
    /// <param name="pageNumber">页码，默认为1</param>
    /// <param name="pageSize">页大小，默认为50</param>
    /// <returns>专项训练列表</returns>
    [HttpGet("search")]
    public async Task<ActionResult<List<StudentSpecializedTrainingDto>>> SearchTrainings(
        [FromQuery] string keyword,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return BadRequest(new { message = "搜索关键词不能为空" });
            }

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

            List<StudentSpecializedTrainingDto> trainings = await _studentSpecializedTrainingService.SearchTrainingsAsync(
                studentUserId, keyword, pageNumber, pageSize);

            _logger.LogInformation("学生搜索专项训练成功，学生ID: {StudentUserId}, 搜索关键词: {Keyword}, 返回数量: {Count}",
                studentUserId, keyword, trainings.Count);

            return Ok(trainings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索专项训练失败，搜索关键词: {Keyword}", keyword);
            return StatusCode(500, new { message = "搜索专项训练失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取所有可用的模块类型列表
    /// </summary>
    /// <returns>模块类型列表</returns>
    [HttpGet("module-types")]
    public async Task<ActionResult<List<string>>> GetAvailableModuleTypes()
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            List<string> moduleTypes = await _studentSpecializedTrainingService.GetAvailableModuleTypesAsync(studentUserId);

            _logger.LogInformation("学生获取可用模块类型列表成功，学生ID: {StudentUserId}, 模块类型数量: {Count}",
                studentUserId, moduleTypes.Count);

            return Ok(moduleTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用模块类型列表失败");
            return StatusCode(500, new { message = "获取模块类型列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 标记专项训练为开始状态
    /// </summary>
    /// <param name="trainingId">专项训练ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{trainingId}/start")]
    public async Task<ActionResult> StartSpecializedTraining(int trainingId)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _studentSpecializedTrainingService.MarkTrainingAsStartedAsync(studentUserId, trainingId);

            if (success)
            {
                _logger.LogInformation("学生开始专项训练成功，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                    studentUserId, trainingId);
                return Ok(new { message = "专项训练开始成功" });
            }
            else
            {
                _logger.LogWarning("学生开始专项训练失败，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                    studentUserId, trainingId);
                return BadRequest(new { message = "开始专项训练失败，请检查训练是否存在或您是否有权限访问" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始专项训练时发生异常，训练ID: {TrainingId}", trainingId);
            return StatusCode(500, new { message = "开始专项训练失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 标记专项训练为已完成
    /// </summary>
    /// <param name="trainingId">专项训练ID</param>
    /// <param name="request">完成信息</param>
    /// <returns>操作结果</returns>
    [HttpPost("{trainingId}/complete")]
    public async Task<ActionResult> CompleteSpecializedTraining(int trainingId, [FromBody] CompleteTrainingRequest request)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _studentSpecializedTrainingService.MarkTrainingAsCompletedAsync(
                studentUserId,
                trainingId,
                request.Score,
                request.MaxScore,
                request.DurationSeconds,
                request.Notes);

            if (success)
            {
                _logger.LogInformation("学生完成专项训练成功，学生ID: {StudentUserId}, 训练ID: {TrainingId}, 得分: {Score}/{MaxScore}",
                    studentUserId, trainingId, request.Score, request.MaxScore);
                return Ok(new { message = "专项训练完成成功" });
            }
            else
            {
                _logger.LogWarning("学生完成专项训练失败，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                    studentUserId, trainingId);
                return BadRequest(new { message = "完成专项训练失败，请检查训练是否存在或您是否有权限访问" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成专项训练时发生异常，训练ID: {TrainingId}", trainingId);
            return StatusCode(500, new { message = "完成专项训练失败", error = ex.Message });
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
