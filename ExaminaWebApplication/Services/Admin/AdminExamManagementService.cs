using Microsoft.EntityFrameworkCore;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Api.Admin;
using ExaminaWebApplication.Models.ImportedExam;
using ExaminaWebApplication.Models;
using ImportedExamEntity = ExaminaWebApplication.Models.ImportedExam.ImportedExam;

namespace ExaminaWebApplication.Services.Admin;

/// <summary>
/// 管理员考试管理服务实现
/// </summary>
public class AdminExamManagementService : IAdminExamManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminExamManagementService> _logger;

    public AdminExamManagementService(
        ApplicationDbContext context,
        ILogger<AdminExamManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取管理员的考试列表
    /// </summary>
    public async Task<List<AdminExamDto>> GetExamsAsync(int userId, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            IQueryable<ImportedExamEntity> query = _context.ImportedExams
                .Where(e => e.ImportedBy == userId)
                .Include(e => e.Subjects)
                .Include(e => e.Modules)
                .OrderByDescending(e => e.ImportedAt);

            List<ImportedExamEntity> exams = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<AdminExamDto> result = new();

            foreach (ImportedExamEntity exam in exams)
            {
                // 获取统计信息
                int participantCount = await _context.ExamCompletions
                    .Where(ec => ec.ExamId == exam.Id)
                    .Select(ec => ec.StudentUserId)
                    .Distinct()
                    .CountAsync();

                int completedCount = await _context.ExamCompletions
                    .Where(ec => ec.ExamId == exam.Id && ec.CompletedAt.HasValue)
                    .CountAsync();

                int questionCount = exam.Subjects.Sum(s => s.Questions.Count) + 
                                  exam.Modules.Sum(m => m.Questions.Count);

                AdminExamDto dto = new()
                {
                    Id = exam.Id,
                    Name = exam.Name,
                    Description = exam.Description,
                    ExamType = exam.ExamType,
                    Status = exam.Status,
                    TotalScore = exam.TotalScore,
                    DurationMinutes = exam.DurationMinutes,
                    StartTime = exam.StartTime,
                    EndTime = exam.EndTime,
                    AllowRetake = exam.AllowRetake,
                    MaxRetakeCount = exam.MaxRetakeCount,
                    PassingScore = exam.PassingScore,
                    RandomizeQuestions = exam.RandomizeQuestions,
                    ShowScore = exam.ShowScore,
                    ShowAnswers = exam.ShowAnswers,
                    IsEnabled = exam.IsEnabled,
                    ExamCategory = exam.ExamCategory,
                    Tags = exam.Tags,
                    ImportedAt = exam.ImportedAt,
                    ImportFileName = exam.ImportFileName,
                    SubjectCount = exam.Subjects.Count,
                    ModuleCount = exam.Modules.Count,
                    QuestionCount = questionCount,
                    ParticipantCount = participantCount,
                    CompletedCount = completedCount
                };

                result.Add(dto);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试列表失败，用户ID: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// 获取考试详情
    /// </summary>
    public async Task<AdminExamDto?> GetExamDetailsAsync(int examId, int userId)
    {
        try
        {
            ImportedExamEntity? exam = await _context.ImportedExams
                .Include(e => e.Subjects)
                    .ThenInclude(s => s.Questions)
                .Include(e => e.Modules)
                    .ThenInclude(m => m.Questions)
                .FirstOrDefaultAsync(e => e.Id == examId && e.ImportedBy == userId);

            if (exam == null)
            {
                return null;
            }

            // 获取统计信息
            int participantCount = await _context.ExamCompletions
                .Where(ec => ec.ExamId == examId)
                .Select(ec => ec.StudentUserId)
                .Distinct()
                .CountAsync();

            int completedCount = await _context.ExamCompletions
                .Where(ec => ec.ExamId == examId && ec.CompletedAt.HasValue)
                .CountAsync();

            int questionCount = exam.Subjects.Sum(s => s.Questions.Count) + 
                              exam.Modules.Sum(m => m.Questions.Count);

            AdminExamDto dto = new()
            {
                Id = exam.Id,
                Name = exam.Name,
                Description = exam.Description,
                ExamType = exam.ExamType,
                Status = exam.Status,
                TotalScore = exam.TotalScore,
                DurationMinutes = exam.DurationMinutes,
                StartTime = exam.StartTime,
                EndTime = exam.EndTime,
                AllowRetake = exam.AllowRetake,
                MaxRetakeCount = exam.MaxRetakeCount,
                PassingScore = exam.PassingScore,
                RandomizeQuestions = exam.RandomizeQuestions,
                ShowScore = exam.ShowScore,
                ShowAnswers = exam.ShowAnswers,
                IsEnabled = exam.IsEnabled,
                ExamCategory = exam.ExamCategory,
                Tags = exam.Tags,
                ImportedAt = exam.ImportedAt,
                ImportFileName = exam.ImportFileName,
                SubjectCount = exam.Subjects.Count,
                ModuleCount = exam.Modules.Count,
                QuestionCount = questionCount,
                ParticipantCount = participantCount,
                CompletedCount = completedCount
            };

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试详情失败，考试ID: {ExamId}, 用户ID: {UserId}", examId, userId);
            throw;
        }
    }

    /// <summary>
    /// 设置考试时间
    /// </summary>
    public async Task<bool> SetExamScheduleAsync(int examId, int userId, DateTime startTime, DateTime endTime)
    {
        try
        {
            ImportedExamEntity? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId && e.ImportedBy == userId);

            if (exam == null)
            {
                return false;
            }

            // 检查是否可以修改时间
            if (exam.Status != "Draft" && exam.Status != "Scheduled")
            {
                _logger.LogWarning("考试状态不允许修改时间，考试ID: {ExamId}, 状态: {Status}", examId, exam.Status);
                return false;
            }

            exam.StartTime = startTime;
            exam.EndTime = endTime;
            exam.Status = "Scheduled";

            await _context.SaveChangesAsync();

            _logger.LogInformation("设置考试时间成功，考试ID: {ExamId}, 开始时间: {StartTime}, 结束时间: {EndTime}",
                examId, startTime, endTime);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置考试时间失败，考试ID: {ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 更新考试状态
    /// </summary>
    public async Task<bool> UpdateExamStatusAsync(int examId, int userId, string status)
    {
        try
        {
            ImportedExamEntity? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId && e.ImportedBy == userId);

            if (exam == null)
            {
                return false;
            }

            // 验证状态转换是否合法
            if (!IsValidStatusTransition(exam.Status, status))
            {
                _logger.LogWarning("无效的状态转换，考试ID: {ExamId}, 当前状态: {CurrentStatus}, 目标状态: {TargetStatus}",
                    examId, exam.Status, status);
                return false;
            }

            exam.Status = status;
            await _context.SaveChangesAsync();

            _logger.LogInformation("更新考试状态成功，考试ID: {ExamId}, 新状态: {Status}", examId, status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试状态失败，考试ID: {ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 更新考试类型
    /// </summary>
    public async Task<bool> UpdateExamCategoryAsync(int examId, int userId, ExamCategory category)
    {
        try
        {
            ImportedExamEntity? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId && e.ImportedBy == userId);

            if (exam == null)
            {
                return false;
            }

            exam.ExamCategory = category;
            await _context.SaveChangesAsync();

            _logger.LogInformation("更新考试类型成功，考试ID: {ExamId}, 新类型: {Category}", examId, category);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新考试类型失败，考试ID: {ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 发布考试
    /// </summary>
    public async Task<bool> PublishExamAsync(int examId, int userId)
    {
        try
        {
            ImportedExamEntity? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId && e.ImportedBy == userId);

            if (exam == null)
            {
                return false;
            }

            // 检查是否可以发布
            if (exam.Status != "Draft" && exam.Status != "Scheduled")
            {
                _logger.LogWarning("考试状态不允许发布，考试ID: {ExamId}, 状态: {Status}", examId, exam.Status);
                return false;
            }

            if (!exam.StartTime.HasValue || !exam.EndTime.HasValue)
            {
                _logger.LogWarning("考试时间未设置，无法发布，考试ID: {ExamId}", examId);
                return false;
            }

            exam.Status = "Published";
            await _context.SaveChangesAsync();

            _logger.LogInformation("发布考试成功，考试ID: {ExamId}", examId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布考试失败，考试ID: {ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 开始考试
    /// </summary>
    public async Task<bool> StartExamAsync(int examId, int userId)
    {
        return await UpdateExamStatusAsync(examId, userId, "InProgress");
    }

    /// <summary>
    /// 结束考试
    /// </summary>
    public async Task<bool> EndExamAsync(int examId, int userId)
    {
        return await UpdateExamStatusAsync(examId, userId, "Completed");
    }

    /// <summary>
    /// 取消考试
    /// </summary>
    public async Task<bool> CancelExamAsync(int examId, int userId)
    {
        return await UpdateExamStatusAsync(examId, userId, "Cancelled");
    }

    /// <summary>
    /// 检查用户是否有权限管理指定考试
    /// </summary>
    public async Task<bool> HasManagePermissionAsync(int examId, int userId)
    {
        try
        {
            return await _context.ImportedExams
                .AnyAsync(e => e.Id == examId && e.ImportedBy == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查管理权限失败，考试ID: {ExamId}, 用户ID: {UserId}", examId, userId);
            return false;
        }
    }

    /// <summary>
    /// 获取考试统计信息
    /// </summary>
    public async Task<ExamStatisticsDto?> GetExamStatisticsAsync(int examId, int userId)
    {
        try
        {
            ImportedExamEntity? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId && e.ImportedBy == userId);

            if (exam == null)
            {
                return null;
            }

            List<ExamCompletion> completions = await _context.ExamCompletions
                .Where(ec => ec.ExamId == examId)
                .ToListAsync();

            int totalParticipants = completions.Select(c => c.StudentUserId).Distinct().Count();
            int completedCount = completions.Count(c => c.CompletedAt.HasValue);
            int inProgressCount = completions.Count(c => c.StartedAt.HasValue && !c.CompletedAt.HasValue);
            int notStartedCount = totalParticipants - completions.Count(c => c.StartedAt.HasValue);

            List<decimal> scores = completions
                .Where(c => c.Score.HasValue)
                .Select(c => c.Score!.Value)
                .ToList();

            decimal? averageScore = scores.Any() ? scores.Average() : null;
            decimal? highestScore = scores.Any() ? scores.Max() : null;
            decimal? lowestScore = scores.Any() ? scores.Min() : null;

            int passedCount = scores.Count(s => s >= exam.PassingScore);
            decimal passRate = scores.Any() ? (decimal)passedCount / scores.Count * 100 : 0;
            decimal completionRate = totalParticipants > 0 ? (decimal)completedCount / totalParticipants * 100 : 0;

            return new ExamStatisticsDto
            {
                ExamId = examId,
                ExamName = exam.Name,
                TotalParticipants = totalParticipants,
                CompletedCount = completedCount,
                InProgressCount = inProgressCount,
                NotStartedCount = notStartedCount,
                AverageScore = averageScore,
                HighestScore = highestScore,
                LowestScore = lowestScore,
                PassRate = passRate,
                CompletionRate = completionRate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试统计信息失败，考试ID: {ExamId}", examId);
            throw;
        }
    }

    /// <summary>
    /// 验证状态转换是否合法
    /// </summary>
    private static bool IsValidStatusTransition(string currentStatus, string targetStatus)
    {
        return currentStatus switch
        {
            "Draft" => targetStatus is "Scheduled" or "Published" or "Cancelled",
            "Scheduled" => targetStatus is "Published" or "Cancelled" or "Draft",
            "Published" => targetStatus is "InProgress" or "Cancelled",
            "InProgress" => targetStatus is "Completed" or "Cancelled",
            "Completed" => false, // 已完成的考试不能再改变状态
            "Cancelled" => targetStatus == "Draft", // 已取消的考试只能回到草稿状态
            _ => false
        };
    }
}
