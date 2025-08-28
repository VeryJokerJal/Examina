using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Services.Student;
using ExaminaWebApplication.Services;
using ExaminaWebApplication.Data;
using Microsoft.EntityFrameworkCore;
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
    private readonly ApplicationDbContext _context;

    public StudentMockExamController(
        IStudentMockExamService mockExamService,
        ILogger<StudentMockExamController> logger,
        ApplicationDbContext context)
    {
        _mockExamService = mockExamService;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 快速开始模拟考试（使用预设规则自动生成）
    /// </summary>
    /// <returns>创建并开始的模拟考试</returns>
    [HttpPost("quick-start")]
    public async Task<ActionResult<MockExamComprehensiveTrainingDto>> QuickStartMockExam()
    {
        try
        {
            // 获取当前学生用户ID
            int studentUserId = GetCurrentUserId();

            MockExamComprehensiveTrainingDto? mockExam = await _mockExamService.QuickStartMockExamAsync(studentUserId);
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
    /// 提交模拟考试
    /// </summary>
    /// <param name="id">模拟考试ID</param>
    /// <param name="actualDurationSeconds">客户端实际用时（秒），可选参数</param>
    /// <returns>操作结果，包含时间状态信息</returns>
    [HttpPost("{id}/submit")]
    public async Task<ActionResult> SubmitMockExam(int id, [FromQuery] int? actualDurationSeconds = null)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            _logger.LogInformation("接收到模拟考试提交请求，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 客户端用时: {ClientDuration}秒",
                studentUserId, id, actualDurationSeconds);

            var result = await _mockExamService.SubmitMockExamAsync(id, studentUserId, actualDurationSeconds);
            if (!result.Success)
            {
                _logger.LogWarning("提交模拟考试失败，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 原因: {Message}",
                    studentUserId, id, result.Message);
                return BadRequest(result);
            }

            _logger.LogInformation("学生提交模拟考试成功，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 用时: {Duration}分钟",
                studentUserId, id, result.ActualDurationMinutes);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交模拟考试时发生异常，模拟考试ID: {MockExamId}", id);
            return StatusCode(500, new {
                success = false,
                message = "服务器内部错误",
                status = "Error",
                timeStatusDescription = "服务器处理异常"
            });
        }
    }

    /// <summary>
    /// 提交模拟考试成绩
    /// </summary>
    /// <param name="id">模拟考试ID</param>
    /// <param name="scoreRequest">成绩数据</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/score")]
    public async Task<ActionResult> SubmitMockExamScore(int id, [FromBody] SubmitMockExamScoreRequestDto scoreRequest)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            bool success = await _mockExamService.SubmitMockExamScoreAsync(id, studentUserId, scoreRequest);
            if (!success)
            {
                _logger.LogWarning("提交模拟考试成绩失败，学生ID: {StudentId}, 模拟考试ID: {MockExamId}",
                    studentUserId, id);
                return BadRequest(new { message = "无法提交模拟考试成绩，请检查考试状态或权限" });
            }

            _logger.LogInformation("学生提交模拟考试成绩成功，学生ID: {StudentId}, 模拟考试ID: {MockExamId}",
                studentUserId, id);

            return Ok(new { message = "模拟考试成绩已提交" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交模拟考试成绩时发生异常，模拟考试ID: {MockExamId}", id);
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
    /// 诊断模拟考试权限问题
    /// </summary>
    /// <param name="id">模拟考试ID</param>
    /// <returns>详细的诊断信息</returns>
    [HttpGet("{id}/diagnose")]
    public async Task<ActionResult> DiagnoseMockExamAccess(int id)
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            _logger.LogInformation("开始诊断模拟考试权限，学生ID: {StudentId}, 模拟考试ID: {MockExamId}",
                studentUserId, id);

            // 获取当前用户信息
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentUserId);

            // 获取模拟考试信息
            var mockExam = await _context.MockExams.FirstOrDefaultAsync(me => me.Id == id);

            // 检查权限
            bool hasAccess = await _mockExamService.HasAccessToMockExamAsync(id, studentUserId);

            var diagnosticInfo = new
            {
                RequestInfo = new
                {
                    RequestedMockExamId = id,
                    RequestingStudentId = studentUserId,
                    RequestTime = DateTime.Now
                },
                CurrentUser = currentUser != null ? new
                {
                    currentUser.Id,
                    currentUser.Username,
                    currentUser.Email,
                    currentUser.Role,
                    currentUser.IsActive,
                    currentUser.RealName,
                    currentUser.CreatedAt
                } : null,
                MockExam = mockExam != null ? new
                {
                    mockExam.Id,
                    mockExam.StudentId,
                    mockExam.Name,
                    mockExam.Status,
                    mockExam.CreatedAt,
                    mockExam.StartedAt,
                    mockExam.CompletedAt,
                    mockExam.DurationMinutes
                } : null,
                PermissionCheck = new
                {
                    HasAccess = hasAccess,
                    UserExists = currentUser != null,
                    UserIsStudent = currentUser?.Role == Models.UserRole.Student,
                    UserIsActive = currentUser?.IsActive == true,
                    MockExamExists = mockExam != null,
                    StudentIdMatches = mockExam?.StudentId == studentUserId,
                    Issues = new List<string>()
                }
            };

            // 添加问题诊断
            var issues = (List<string>)diagnosticInfo.PermissionCheck.Issues;

            if (currentUser == null)
                issues.Add("用户不存在");
            else if (currentUser.Role != Models.UserRole.Student)
                issues.Add($"用户角色不正确，当前角色: {currentUser.Role}，需要: Student");
            else if (!currentUser.IsActive)
                issues.Add("用户账号未激活");

            if (mockExam == null)
                issues.Add("模拟考试不存在");
            else if (mockExam.StudentId != studentUserId)
                issues.Add($"模拟考试不属于当前用户，考试所属学生ID: {mockExam.StudentId}，当前用户ID: {studentUserId}");

            if (issues.Count == 0)
                issues.Add("未发现明显问题，权限验证应该通过");

            _logger.LogInformation("模拟考试权限诊断完成，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 有权限: {HasAccess}, 问题数: {IssueCount}",
                studentUserId, id, hasAccess, issues.Count);

            return Ok(diagnosticInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "诊断模拟考试权限时发生异常，模拟考试ID: {MockExamId}", id);
            return StatusCode(500, new { message = "服务器内部错误", error = ex.Message });
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
    /// 获取学生已完成的模拟考试数量
    /// </summary>
    /// <returns>已完成的模拟考试数量</returns>
    [HttpGet("completed/count")]
    public async Task<ActionResult<int>> GetCompletedMockExamCount()
    {
        try
        {
            int studentUserId = GetCurrentUserId();

            int count = await _mockExamService.GetCompletedMockExamCountAsync(studentUserId);

            _logger.LogInformation("获取学生已完成模拟考试数量成功，学生ID: {StudentId}, 已完成数量: {Count}",
                studentUserId, count);

            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取已完成模拟考试数量时发生异常");
            return StatusCode(500, new { message = "服务器内部错误" });
        }
    }

    /// <summary>
    /// 获取学生模拟考试成绩列表
    /// </summary>
    /// <param name="pageNumber">页码，默认为1</param>
    /// <param name="pageSize">页大小，默认为20，最大100</param>
    /// <returns>模拟考试成绩列表</returns>
    [HttpGet("completions")]
    public async Task<ActionResult<List<MockExamCompletionDto>>> GetMockExamCompletions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // 验证分页参数
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            int studentUserId = GetCurrentUserId();

            List<MockExamCompletionDto> completions = await _mockExamService.GetMockExamCompletionsAsync(
                studentUserId, pageNumber, pageSize);

            _logger.LogInformation("获取学生模拟考试成绩列表成功，学生ID: {StudentId}, 页码: {PageNumber}, 页大小: {PageSize}, 返回数量: {Count}",
                studentUserId, pageNumber, pageSize, completions.Count);

            return Ok(completions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模拟考试成绩列表时发生异常");
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
