using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Controllers;

/// <summary>
/// 诊断控制器 - 用于排查排行榜问题
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class DiagnosticController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DiagnosticController> _logger;

    public DiagnosticController(ApplicationDbContext context, ILogger<DiagnosticController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 检查模拟考试完成记录
    /// </summary>
    [HttpGet("mock-exam-completions")]
    public async Task<ActionResult> GetMockExamCompletions()
    {
        try
        {
            var completions = await _context.MockExamCompletions
                .Include(mec => mec.Student)
                .Include(mec => mec.MockExam)
                .OrderByDescending(mec => mec.CreatedAt)
                .Take(20)
                .Select(mec => new
                {
                    mec.Id,
                    mec.StudentUserId,
                    StudentUsername = mec.Student != null ? mec.Student.Username : "Unknown",
                    mec.MockExamId,
                    MockExamName = mec.MockExam != null ? mec.MockExam.Name : "Unknown",
                    mec.Status,
                    StatusText = mec.Status.ToString(),
                    mec.Score,
                    mec.MaxScore,
                    mec.CompletedAt,
                    mec.DurationSeconds,
                    mec.IsActive,
                    mec.CreatedAt
                })
                .ToListAsync();

            var summary = new
            {
                TotalRecords = await _context.MockExamCompletions.CountAsync(),
                CompletedRecords = await _context.MockExamCompletions.CountAsync(mec => mec.Status == MockExamCompletionStatus.Completed),
                ActiveRecords = await _context.MockExamCompletions.CountAsync(mec => mec.IsActive),
                RecordsWithScore = await _context.MockExamCompletions.CountAsync(mec => mec.Score.HasValue),
                RecordsWithCompletedAt = await _context.MockExamCompletions.CountAsync(mec => mec.CompletedAt.HasValue),
                RecentCompletions = completions
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查模拟考试完成记录失败");
            return StatusCode(500, new { message = "检查失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 检查综合实训完成记录
    /// </summary>
    [HttpGet("training-completions")]
    public async Task<ActionResult> GetTrainingCompletions()
    {
        try
        {
            var completions = await _context.ComprehensiveTrainingCompletions
                .Include(ctc => ctc.Student)
                .Include(ctc => ctc.Training)
                .OrderByDescending(ctc => ctc.CreatedAt)
                .Take(20)
                .Select(ctc => new
                {
                    ctc.Id,
                    ctc.StudentUserId,
                    StudentUsername = ctc.Student != null ? ctc.Student.Username : "Unknown",
                    ctc.TrainingId,
                    TrainingName = ctc.Training != null ? ctc.Training.Name : "Unknown",
                    ctc.Status,
                    StatusText = ctc.Status.ToString(),
                    ctc.Score,
                    ctc.MaxScore,
                    ctc.CompletedAt,
                    ctc.DurationSeconds,
                    ctc.IsActive,
                    ctc.CreatedAt
                })
                .ToListAsync();

            var summary = new
            {
                TotalRecords = await _context.ComprehensiveTrainingCompletions.CountAsync(),
                CompletedRecords = await _context.ComprehensiveTrainingCompletions.CountAsync(ctc => ctc.Status == ComprehensiveTrainingCompletionStatus.Completed),
                ActiveRecords = await _context.ComprehensiveTrainingCompletions.CountAsync(ctc => ctc.IsActive),
                RecordsWithScore = await _context.ComprehensiveTrainingCompletions.CountAsync(ctc => ctc.Score.HasValue),
                RecordsWithCompletedAt = await _context.ComprehensiveTrainingCompletions.CountAsync(ctc => ctc.CompletedAt.HasValue),
                RecentCompletions = completions
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查综合实训完成记录失败");
            return StatusCode(500, new { message = "检查失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 检查正式考试完成记录
    /// </summary>
    [HttpGet("exam-completions")]
    public async Task<ActionResult> GetExamCompletions()
    {
        try
        {
            var completions = await _context.ExamCompletions
                .Include(ec => ec.Student)
                .Include(ec => ec.Exam)
                .OrderByDescending(ec => ec.CreatedAt)
                .Take(20)
                .Select(ec => new
                {
                    ec.Id,
                    ec.StudentUserId,
                    StudentUsername = ec.Student != null ? ec.Student.Username : "Unknown",
                    ec.ExamId,
                    ExamName = ec.Exam != null ? ec.Exam.Name : "Unknown",
                    ec.Status,
                    StatusText = ec.Status.ToString(),
                    ec.Score,
                    ec.MaxScore,
                    ec.CompletedAt,
                    ec.DurationSeconds,
                    ec.IsActive,
                    ec.CreatedAt
                })
                .ToListAsync();

            var summary = new
            {
                TotalRecords = await _context.ExamCompletions.CountAsync(),
                CompletedRecords = await _context.ExamCompletions.CountAsync(ec => ec.Status == ExamCompletionStatus.Completed),
                ActiveRecords = await _context.ExamCompletions.CountAsync(ec => ec.IsActive),
                RecordsWithScore = await _context.ExamCompletions.CountAsync(ec => ec.Score.HasValue),
                RecordsWithCompletedAt = await _context.ExamCompletions.CountAsync(ec => ec.CompletedAt.HasValue),
                RecentCompletions = completions
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查正式考试完成记录失败");
            return StatusCode(500, new { message = "检查失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 检查排行榜查询条件
    /// </summary>
    [HttpGet("ranking-query-test")]
    public async Task<ActionResult> TestRankingQuery()
    {
        try
        {
            // 测试模拟考试排行榜查询条件
            var mockExamQuery = _context.MockExamCompletions
                .Include(mec => mec.Student)
                .Include(mec => mec.MockExam)
                .Where(mec => mec.Status == MockExamCompletionStatus.Completed &&
                             mec.IsActive &&
                             mec.Score.HasValue &&
                             mec.CompletedAt.HasValue);

            var mockExamCount = await mockExamQuery.CountAsync();
            var mockExamSample = await mockExamQuery.Take(5).ToListAsync();

            // 测试综合实训排行榜查询条件
            var trainingQuery = _context.ComprehensiveTrainingCompletions
                .Include(ctc => ctc.Student)
                .Include(ctc => ctc.Training)
                .Where(ctc => ctc.Status == ComprehensiveTrainingCompletionStatus.Completed &&
                             ctc.IsActive &&
                             ctc.Score.HasValue &&
                             ctc.CompletedAt.HasValue);

            var trainingCount = await trainingQuery.CountAsync();
            var trainingSample = await trainingQuery.Take(5).ToListAsync();

            var result = new
            {
                MockExamRanking = new
                {
                    QualifiedRecords = mockExamCount,
                    SampleRecords = mockExamSample.Select(mec => new
                    {
                        mec.Id,
                        mec.StudentUserId,
                        StudentUsername = mec.Student?.Username,
                        mec.MockExamId,
                        MockExamName = mec.MockExam?.Name,
                        mec.Status,
                        mec.Score,
                        mec.CompletedAt,
                        mec.IsActive
                    })
                },
                TrainingRanking = new
                {
                    QualifiedRecords = trainingCount,
                    SampleRecords = trainingSample.Select(ctc => new
                    {
                        ctc.Id,
                        ctc.StudentUserId,
                        StudentUsername = ctc.Student?.Username,
                        ctc.TrainingId,
                        TrainingName = ctc.Training?.Name,
                        ctc.Status,
                        ctc.Score,
                        ctc.CompletedAt,
                        ctc.IsActive
                    })
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试排行榜查询条件失败");
            return StatusCode(500, new { message = "测试失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 创建测试数据 - 用于验证修复效果
    /// </summary>
    [HttpPost("create-test-data")]
    public async Task<ActionResult> CreateTestData()
    {
        try
        {
            var now = DateTime.Now;

            // 创建测试模拟考试完成记录
            var mockExamCompletion = new MockExamCompletion
            {
                StudentUserId = 1, // 假设用户ID为1
                MockExamId = 1, // 假设模拟考试ID为1
                Status = MockExamCompletionStatus.Completed,
                StartedAt = now.AddMinutes(-30),
                CompletedAt = now,
                Score = 85,
                MaxScore = 100,
                DurationSeconds = 1800, // 30分钟
                Notes = "测试数据",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            };

            // 创建测试综合实训完成记录
            var trainingCompletion = new ComprehensiveTrainingCompletion
            {
                StudentUserId = 1, // 假设用户ID为1
                TrainingId = 1, // 假设实训ID为1
                Status = ComprehensiveTrainingCompletionStatus.Completed,
                StartedAt = now.AddMinutes(-45),
                CompletedAt = now,
                Score = 92,
                MaxScore = 100,
                DurationSeconds = 2700, // 45分钟
                Notes = "测试数据",
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            };

            _context.MockExamCompletions.Add(mockExamCompletion);
            _context.ComprehensiveTrainingCompletions.Add(trainingCompletion);

            await _context.SaveChangesAsync();

            return Ok(new {
                message = "测试数据创建成功",
                mockExamCompletionId = mockExamCompletion.Id,
                trainingCompletionId = trainingCompletion.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建测试数据失败");
            return StatusCode(500, new { message = "创建失败", error = ex.Message });
        }
    }
}
