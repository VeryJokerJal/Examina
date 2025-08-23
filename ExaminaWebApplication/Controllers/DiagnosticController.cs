using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MockExam = ExaminaWebApplication.Models.MockExam.MockExam;

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

    /// <summary>
    /// 检查重复的模拟考试完成记录
    /// </summary>
    [HttpGet("duplicate-mock-exam-completions")]
    public async Task<ActionResult> GetDuplicateMockExamCompletions()
    {
        try
        {
            var duplicates = await _context.MockExamCompletions
                .GroupBy(mec => new { mec.StudentUserId, mec.MockExamId })
                .Where(g => g.Count() > 1)
                .Select(g => new
                {
                    StudentUserId = g.Key.StudentUserId,
                    MockExamId = g.Key.MockExamId,
                    Count = g.Count(),
                    Records = g.Select(mec => new
                    {
                        mec.Id,
                        mec.Status,
                        mec.Score,
                        mec.DurationSeconds,
                        mec.CompletedAt,
                        mec.CreatedAt,
                        mec.IsActive
                    }).OrderByDescending(r => r.Score).ThenBy(r => r.DurationSeconds)
                })
                .ToListAsync();

            var summary = new
            {
                TotalDuplicateGroups = duplicates.Count,
                TotalDuplicateRecords = duplicates.Sum(d => d.Count),
                DuplicateGroups = duplicates
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查重复模拟考试完成记录失败");
            return StatusCode(500, new { message = "检查失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 清理重复的模拟考试完成记录
    /// </summary>
    [HttpPost("cleanup-duplicate-mock-exam-completions")]
    public async Task<ActionResult> CleanupDuplicateMockExamCompletions([FromQuery] bool dryRun = true)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var duplicateGroups = await _context.MockExamCompletions
                .GroupBy(mec => new { mec.StudentUserId, mec.MockExamId })
                .Where(g => g.Count() > 1)
                .ToListAsync();

            var cleanupResults = new List<object>();
            int totalRecordsToDelete = 0;

            foreach (var group in duplicateGroups)
            {
                var records = group.OrderByDescending(mec => mec.Score)
                                  .ThenBy(mec => mec.DurationSeconds)
                                  .ThenBy(mec => mec.CompletedAt)
                                  .ToList();

                var bestRecord = records.First();
                var recordsToDelete = records.Skip(1).ToList();

                cleanupResults.Add(new
                {
                    StudentUserId = group.Key.StudentUserId,
                    MockExamId = group.Key.MockExamId,
                    TotalRecords = records.Count,
                    BestRecord = new
                    {
                        bestRecord.Id,
                        bestRecord.Score,
                        bestRecord.DurationSeconds,
                        bestRecord.CompletedAt
                    },
                    RecordsToDelete = recordsToDelete.Select(r => new
                    {
                        r.Id,
                        r.Score,
                        r.DurationSeconds,
                        r.CompletedAt
                    })
                });

                totalRecordsToDelete += recordsToDelete.Count;

                if (!dryRun)
                {
                    _context.MockExamCompletions.RemoveRange(recordsToDelete);
                }
            }

            if (!dryRun)
            {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("成功清理重复的模拟考试完成记录，删除记录数: {Count}", totalRecordsToDelete);
            }
            else
            {
                await transaction.RollbackAsync();
            }

            return Ok(new
            {
                DryRun = dryRun,
                TotalDuplicateGroups = duplicateGroups.Count,
                TotalRecordsToDelete = totalRecordsToDelete,
                CleanupResults = cleanupResults,
                Message = dryRun ? "预览模式：未实际删除数据" : $"成功清理 {totalRecordsToDelete} 条重复记录"
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "清理重复模拟考试完成记录失败");
            return StatusCode(500, new { message = "清理失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 时间诊断API - 检查模拟考试的时间记录和计算
    /// </summary>
    [HttpGet("time-diagnosis/{mockExamId}")]
    public async Task<ActionResult> DiagnoseMockExamTime(int mockExamId)
    {
        try
        {
            // 获取模拟考试信息
            var mockExam = await _context.MockExams
                .FirstOrDefaultAsync(me => me.Id == mockExamId);

            if (mockExam == null)
            {
                return NotFound(new { message = "模拟考试不存在" });
            }

            // 获取完成记录
            var completions = await _context.MockExamCompletions
                .Where(mec => mec.MockExamId == mockExamId)
                .OrderByDescending(mec => mec.CreatedAt)
                .ToListAsync();

            var diagnosis = new
            {
                MockExam = new
                {
                    mockExam.Id,
                    mockExam.Name,
                    mockExam.Status,
                    mockExam.CreatedAt,
                    mockExam.StartedAt,
                    mockExam.CompletedAt,
                    DurationMinutes = mockExam.DurationMinutes
                },
                Completions = completions.Select(completion => new
                {
                    completion.Id,
                    completion.StudentUserId,
                    completion.Status,
                    completion.StartedAt,
                    completion.CompletedAt,
                    completion.DurationSeconds,
                    completion.CreatedAt,
                    completion.UpdatedAt,

                    // 计算各种时间差异
                    TimeAnalysis = AnalyzeTimeData(completion, mockExam)
                }),
                Summary = new
                {
                    TotalCompletions = completions.Count,
                    CompletedCount = completions.Count(c => c.Status == MockExamCompletionStatus.Completed),
                    AverageDurationSeconds = completions.Where(c => c.DurationSeconds.HasValue).Average(c => c.DurationSeconds),
                    MinDurationSeconds = completions.Where(c => c.DurationSeconds.HasValue).Min(c => c.DurationSeconds),
                    MaxDurationSeconds = completions.Where(c => c.DurationSeconds.HasValue).Max(c => c.DurationSeconds)
                }
            };

            return Ok(diagnosis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "时间诊断失败，模拟考试ID: {MockExamId}", mockExamId);
            return StatusCode(500, new { message = "诊断失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 分析时间数据
    /// </summary>
    private static object AnalyzeTimeData(MockExamCompletion completion, MockExam mockExam)
    {
        var analysis = new
        {
            // 基础时间信息
            StoredDurationSeconds = completion.DurationSeconds,

            // 使用模拟考试的StartedAt计算（服务端逻辑）
            ServerCalculation = mockExam.StartedAt.HasValue && completion.CompletedAt.HasValue
                ? new
                {
                    StartTime = mockExam.StartedAt.Value,
                    EndTime = completion.CompletedAt.Value,
                    CalculatedSeconds = (int)Math.Ceiling((completion.CompletedAt.Value - mockExam.StartedAt.Value).TotalSeconds),
                    CalculatedMinutes = (int)Math.Ceiling((completion.CompletedAt.Value - mockExam.StartedAt.Value).TotalMinutes)
                }
                : null,

            // 使用完成记录的StartedAt计算（可能的客户端逻辑）
            ClientCalculation = completion.StartedAt.HasValue && completion.CompletedAt.HasValue
                ? new
                {
                    StartTime = completion.StartedAt.Value,
                    EndTime = completion.CompletedAt.Value,
                    CalculatedSeconds = (int)Math.Ceiling((completion.CompletedAt.Value - completion.StartedAt.Value).TotalSeconds),
                    CalculatedMinutes = (int)Math.Ceiling((completion.CompletedAt.Value - completion.StartedAt.Value).TotalMinutes)
                }
                : null,

            // 时间差异分析
            TimeDifferences = new
            {
                MockExamVsCompletionStartTime = mockExam.StartedAt.HasValue && completion.StartedAt.HasValue
                    ? (completion.StartedAt.Value - mockExam.StartedAt.Value).TotalSeconds
                    : (double?)null,

                ServerVsStoredDuration = mockExam.StartedAt.HasValue && completion.CompletedAt.HasValue && completion.DurationSeconds.HasValue
                    ? (int)Math.Ceiling((completion.CompletedAt.Value - mockExam.StartedAt.Value).TotalSeconds) - completion.DurationSeconds.Value
                    : (int?)null,

                ClientVsStoredDuration = completion.StartedAt.HasValue && completion.CompletedAt.HasValue && completion.DurationSeconds.HasValue
                    ? (int)Math.Ceiling((completion.CompletedAt.Value - completion.StartedAt.Value).TotalSeconds) - completion.DurationSeconds.Value
                    : (int?)null
            },

            // 可能的问题标识
            PotentialIssues = IdentifyTimeIssues(completion, mockExam)
        };

        return analysis;
    }

    /// <summary>
    /// 识别时间相关的潜在问题
    /// </summary>
    private static List<string> IdentifyTimeIssues(MockExamCompletion completion, MockExam mockExam)
    {
        var issues = new List<string>();

        // 检查时间戳一致性
        if (mockExam.StartedAt.HasValue && completion.StartedAt.HasValue)
        {
            var timeDiff = Math.Abs((completion.StartedAt.Value - mockExam.StartedAt.Value).TotalSeconds);
            if (timeDiff > 10) // 超过10秒差异
            {
                issues.Add($"模拟考试和完成记录的开始时间差异过大: {timeDiff:F1}秒");
            }
        }

        // 检查存储的用时与计算用时的差异
        if (mockExam.StartedAt.HasValue && completion.CompletedAt.HasValue && completion.DurationSeconds.HasValue)
        {
            var calculatedDuration = (int)Math.Ceiling((completion.CompletedAt.Value - mockExam.StartedAt.Value).TotalSeconds);
            var storedDuration = completion.DurationSeconds.Value;
            var diff = Math.Abs(calculatedDuration - storedDuration);

            if (diff > 2) // 超过2秒差异
            {
                issues.Add($"存储用时与计算用时差异: 存储{storedDuration}秒, 计算{calculatedDuration}秒, 差异{diff}秒");
            }
        }

        // 检查是否使用了Math.Ceiling导致的向上取整问题
        if (completion.DurationSeconds.HasValue && completion.DurationSeconds.Value % 1 == 0)
        {
            // 这是整数，可能是Math.Ceiling的结果
            if (mockExam.StartedAt.HasValue && completion.CompletedAt.HasValue)
            {
                var exactDuration = (completion.CompletedAt.Value - mockExam.StartedAt.Value).TotalSeconds;
                var ceilingDuration = Math.Ceiling(exactDuration);
                var diff = ceilingDuration - exactDuration;

                if (diff > 0.1) // 向上取整导致的差异
                {
                    issues.Add($"Math.Ceiling向上取整导致时间增加: 实际{exactDuration:F2}秒, 取整后{ceilingDuration}秒, 增加{diff:F2}秒");
                }
            }
        }

        return issues;
    }
}
