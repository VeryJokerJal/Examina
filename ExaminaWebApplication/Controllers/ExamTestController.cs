using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Services.ImportedExam;
using ExaminaWebApplication.Models.ImportedExam;
using ExaminaWebApplication.Models;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 考试测试API控制器 - 为前端提供统计和测试数据
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExamTestController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ExamImportService _examImportService;
    private readonly ILogger<ExamTestController> _logger;

    public ExamTestController(
        ApplicationDbContext context,
        ExamImportService examImportService,
        ILogger<ExamTestController> logger)
    {
        _context = context;
        _examImportService = examImportService;
        _logger = logger;
    }

    /// <summary>
    /// 获取系统统计信息
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            _logger.LogInformation("API请求: 获取系统统计信息");

            // 获取考试统计
            int userId = 1; // 使用管理员用户ID
            List<ImportedExam> exams = await _examImportService.GetImportedExamsAsync(userId);

            // 获取用户统计
            int totalUsers = await _context.Users.CountAsync();
            int activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            int studentUsers = await _context.Users.CountAsync(u => u.Role == UserRole.Student);
            int teacherUsers = await _context.Users.CountAsync(u => u.Role == UserRole.Teacher);

            // 构建响应数据，匹配前端期望的格式
            object response = new
            {
                ExamStatistics = new
                {
                    TotalExams = exams.Count,
                    TotalSubjects = exams.Sum(e => e.Subjects.Count),
                    TotalModules = exams.Sum(e => e.Modules.Count),
                    TotalQuestions = exams.Sum(e => e.Modules.Sum(m => m.Questions.Count)),
                    RecentImports = exams.OrderByDescending(e => e.ImportedAt)
                        .Take(5)
                        .Select(e => new
                        {
                            Id = e.Id,
                            Name = e.Name,
                            ImportedAt = e.ImportedAt.ToString("yyyy-MM-dd HH:mm"),
                            Status = e.ImportStatus
                        })
                },
                UserStatistics = new
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    StudentUsers = studentUsers,
                    TeacherUsers = teacherUsers
                },
                SystemInfo = new
                {
                    ServerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
                }
            };

            _logger.LogInformation("统计信息获取成功: 考试数量={ExamCount}, 用户数量={UserCount}", 
                exams.Count, totalUsers);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统统计信息失败");
            return StatusCode(500, new { 
                error = "获取统计信息失败", 
                message = ex.Message 
            });
        }
    }

    /// <summary>
    /// 创建示例考试数据
    /// </summary>
    [HttpPost("create-sample-exam")]
    public async Task<IActionResult> CreateSampleExam()
    {
        try
        {
            _logger.LogInformation("API请求: 创建示例考试数据");

            // 检查是否已存在示例考试
            bool hasSampleExam = await _context.Set<ImportedExam>()
                .AnyAsync(e => e.Name.Contains("示例考试") || e.Name.Contains("Sample Exam"));

            if (hasSampleExam)
            {
                return Ok(new { 
                    Message = "示例考试已存在，无需重复创建",
                    Success = true 
                });
            }

            // 创建示例考试数据
            ImportedExam sampleExam = new()
            {
                Name = "计算机应用基础示例考试",
                OriginalFileName = "sample-exam.json",
                ImportedAt = DateTime.UtcNow,
                ImportedBy = 1, // 管理员用户ID
                ImportStatus = ImportStatus.Success,
                Subjects = new List<ImportedSubject>
                {
                    new()
                    {
                        Name = "计算机基础",
                        Description = "计算机应用基础知识",
                        Order = 1
                    }
                },
                Modules = new List<ImportedModule>
                {
                    new()
                    {
                        Name = "Windows操作系统",
                        Description = "Windows基本操作",
                        ModuleType = "Windows",
                        Score = 30,
                        Order = 1,
                        Questions = new List<ImportedQuestion>
                        {
                            new()
                            {
                                Title = "文件管理操作",
                                Description = "完成指定的文件和文件夹操作",
                                Score = 15,
                                Order = 1,
                                QuestionType = "Operation"
                            },
                            new()
                            {
                                Title = "系统设置配置",
                                Description = "配置Windows系统相关设置",
                                Score = 15,
                                Order = 2,
                                QuestionType = "Operation"
                            }
                        }
                    },
                    new()
                    {
                        Name = "Office应用",
                        Description = "Office办公软件应用",
                        ModuleType = "Office",
                        Score = 70,
                        Order = 2,
                        Questions = new List<ImportedQuestion>
                        {
                            new()
                            {
                                Title = "Word文档编辑",
                                Description = "完成Word文档的格式化和编辑",
                                Score = 25,
                                Order = 1,
                                QuestionType = "Operation"
                            },
                            new()
                            {
                                Title = "Excel数据处理",
                                Description = "使用Excel进行数据分析和图表制作",
                                Score = 25,
                                Order = 2,
                                QuestionType = "Operation"
                            },
                            new()
                            {
                                Title = "PowerPoint演示文稿",
                                Description = "制作专业的PowerPoint演示文稿",
                                Score = 20,
                                Order = 3,
                                QuestionType = "Operation"
                            }
                        }
                    }
                }
            };

            _context.Set<ImportedExam>().Add(sampleExam);
            await _context.SaveChangesAsync();

            _logger.LogInformation("示例考试创建成功: ID={ExamId}, 名称={ExamName}", 
                sampleExam.Id, sampleExam.Name);

            return Ok(new { 
                Message = $"示例考试 '{sampleExam.Name}' 创建成功！包含 {sampleExam.Modules.Count} 个模块，{sampleExam.Modules.Sum(m => m.Questions.Count)} 道题目",
                Success = true,
                ExamId = sampleExam.Id,
                ExamName = sampleExam.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建示例考试失败");
            return StatusCode(500, new { 
                Message = "创建示例考试失败: " + ex.Message,
                Success = false 
            });
        }
    }

    /// <summary>
    /// 健康检查端点
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { 
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}
