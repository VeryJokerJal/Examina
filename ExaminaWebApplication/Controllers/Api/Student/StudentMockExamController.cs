using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Services.Student;
using ExaminaWebApplication.Services;
using System.Security.Claims;

namespace ExaminaWebApplication.Controllers.Api.Student;

/// <summary>
/// 学生端模拟考试API控制器
/// </summary>
[ApiController]
[Route("api/student/mock-exams")]
[Authorize(Roles = "Student")]
public class StudentMockExamController : ControllerBase
{
    private readonly IStudentMockExamService _mockExamService;
    private readonly ILogger<StudentMockExamController> _logger;

    public StudentMockExamController(
        IStudentMockExamService mockExamService,
        ILogger<StudentMockExamController> logger)
    {
        _mockExamService = mockExamService;
        _logger = logger;
    }

    /// <summary>
    /// 快速开始模拟考试（使用预设规则自动生成）
    /// </summary>
    /// <returns>创建并开始的模拟考试</returns>
    [HttpPost("quick-start")]
    public async Task<ActionResult<StudentMockExamDto>> QuickStartMockExam()
    {
        try
        {
            // 获取当前学生用户ID
            int studentUserId = GetCurrentUserId();

            StudentMockExamDto? mockExam = await _mockExamService.QuickStartMockExamAsync(studentUserId);
            if (mockExam == null)
            {
                _logger.LogWarning("快速开始模拟考试失败，学生ID: {StudentId}", studentUserId);
                return BadRequest(new { message = "快速开始模拟考试失败，请检查题库或稍后重试" });
            }

            _logger.LogInformation("学生成功快速开始模拟考试，学生ID: {StudentId}, 模拟考试ID: {MockExamId}",
                studentUserId, mockExam.Id);

            return Ok(mockExam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "快速开始模拟考试时发生异常");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 创建模拟考试
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>创建的模拟考试</returns>
    [HttpPost]
    public async Task<ActionResult<StudentMockExamDto>> CreateMockExam([FromBody] CreateMockExamRequestDto request)
    {
        try
        {
            // 获取当前学生用户ID
            int studentUserId = GetCurrentUserId();

            StudentMockExamDto? mockExam = await _mockExamService.CreateMockExamAsync(request, studentUserId);
            if (mockExam == null)
            {
                _logger.LogWarning("创建模拟考试失败，学生ID: {StudentId}", studentUserId);
                return BadRequest(new { message = "创建模拟考试失败，请检查抽取规则或稍后重试" });
            }

            _logger.LogInformation("学生成功创建模拟考试，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, mockExam.Id);

            return Ok(mockExam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建模拟考试时发生异常");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取学生的模拟考试列表
    /// </summary>
    /// <param name="pageNumber">页码，默认为1</param>
    /// <param name="pageSize">页大小，默认为50，最大100</param>
    /// <returns>模拟考试列表</returns>
    [HttpGet]
    public async Task<ActionResult<List<StudentMockExamDto>>> GetMockExams(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            // 验证分页参数
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            int studentUserId = GetCurrentUserId();

            List<StudentMockExamDto> mockExams = await _mockExamService.GetStudentMockExamsAsync(
                studentUserId, pageNumber, pageSize);

            _logger.LogInformation("获取学生模拟考试列表成功，学生ID: {StudentId}, 页码: {PageNumber}, 页大小: {PageSize}, 返回数量: {Count}", 
                studentUserId, pageNumber, pageSize, mockExams.Count);

            return Ok(mockExams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模拟考试列表时发生异常");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取模拟考试详情
    /// </summary>
    /// <param name="id">模拟考试ID</param>
    /// <returns>模拟考试详情</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<StudentMockExamDto>> GetMockExamDetails(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            StudentMockExamDto? mockExam = await _mockExamService.GetMockExamDetailsAsync(id, studentUserId);
            if (mockExam == null)
            {
                _logger.LogWarning("模拟考试不存在或无权限访问，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                    studentUserId, id);
                return NotFound(new { message = "模拟考试不存在或您无权限访问" });
            }

            _logger.LogInformation("获取模拟考试详情成功，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, id);

            return Ok(mockExam);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模拟考试详情时发生异常，模拟考试ID: {MockExamId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 开始模拟考试
    /// </summary>
    /// <param name="id">模拟考试ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/start")]
    public async Task<ActionResult> StartMockExam(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _mockExamService.StartMockExamAsync(id, studentUserId);
            if (!success)
            {
                _logger.LogWarning("开始模拟考试失败，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                    studentUserId, id);
                return BadRequest(new { message = "无法开始模拟考试，请检查考试状态或权限" });
            }

            _logger.LogInformation("学生开始模拟考试成功，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, id);

            return Ok(new { message = "模拟考试已开始" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始模拟考试时发生异常，模拟考试ID: {MockExamId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 完成模拟考试
    /// </summary>
    /// <param name="id">模拟考试ID</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/complete")]
    public async Task<ActionResult> CompleteMockExam(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _mockExamService.CompleteMockExamAsync(id, studentUserId);
            if (!success)
            {
                _logger.LogWarning("完成模拟考试失败，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                    studentUserId, id);
                return BadRequest(new { message = "无法完成模拟考试，请检查考试状态或权限" });
            }

            _logger.LogInformation("学生完成模拟考试成功，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, id);

            return Ok(new { message = "模拟考试已完成" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成模拟考试时发生异常，模拟考试ID: {MockExamId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 删除模拟考试
    /// </summary>
    /// <param name="id">模拟考试ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMockExam(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _mockExamService.DeleteMockExamAsync(id, studentUserId);
            if (!success)
            {
                _logger.LogWarning("删除模拟考试失败，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                    studentUserId, id);
                return BadRequest(new { message = "无法删除模拟考试，请检查权限" });
            }

            _logger.LogInformation("学生删除模拟考试成功，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, id);

            return Ok(new { message = "模拟考试已删除" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除模拟考试时发生异常，模拟考试ID: {MockExamId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 检查模拟考试访问权限
    /// </summary>
    /// <param name="id">模拟考试ID</param>
    /// <returns>权限检查结果</returns>
    [HttpGet("{id}/access")]
    public async Task<ActionResult> CheckMockExamAccess(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool hasAccess = await _mockExamService.HasAccessToMockExamAsync(id, studentUserId);

            _logger.LogInformation("检查模拟考试访问权限，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 有权限: {HasAccess}", 
                studentUserId, id, hasAccess);

            return Ok(new { hasAccess });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查模拟考试访问权限时发生异常，模拟考试ID: {MockExamId}", id);
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取学生模拟考试总数
    /// </summary>
    /// <returns>模拟考试总数</returns>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetMockExamCount()
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            int count = await _mockExamService.GetStudentMockExamCountAsync(studentUserId);

            _logger.LogInformation("获取学生模拟考试总数成功，学生ID: {StudentId}, 总数: {Count}", 
                studentUserId, count);

            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模拟考试总数时发生异常");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>用户ID</returns>
    private int GetCurrentUserId()
    {
        string? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("无法获取用户身份信息");
        }
        return userId;
    }
}
