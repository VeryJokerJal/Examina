using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生专项练习服务实现
/// </summary>
public class StudentSpecialPracticeService : IStudentSpecialPracticeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StudentSpecialPracticeService> _logger;

    public StudentSpecialPracticeService(ApplicationDbContext context, ILogger<StudentSpecialPracticeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取学生专项练习进度统计
    /// </summary>
    public async Task<SpecialPracticeProgressDto> GetPracticeProgressAsync(int studentUserId)
    {
        try
        {
            // 验证学生用户存在且为学生角色
            Models.User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == Models.UserRole.Student && u.IsActive);

            if (student == null)
            {
                _logger.LogWarning("用户不存在或不是活跃学生，用户ID: {UserId}", studentUserId);
                return new SpecialPracticeProgressDto
                {
                    TotalCount = 0,
                    CompletedCount = 0,
                    CompletionPercentage = 0,
                    InProgressCount = 0,
                    NotStartedCount = 0
                };
            }

            // 获取所有启用的专项练习总数
            // 注意：这里假设专项练习存储在ImportedSpecializedTrainings表中
            // 如果实际的表名不同，需要相应调整
            int totalCount = await _context.ImportedSpecializedTrainings
                .CountAsync(t => t.IsEnabled);

            // 获取学生的练习完成记录
            var completionRecords = await _context.SpecialPracticeCompletions
                .Where(c => c.StudentUserId == studentUserId && c.IsActive)
                .ToListAsync();

            // 统计各种状态的练习数量
            int completedCount = completionRecords.Count(c => c.Status == SpecialPracticeCompletionStatus.Completed);
            int inProgressCount = completionRecords.Count(c => c.Status == SpecialPracticeCompletionStatus.InProgress);
            int notStartedCount = totalCount - completedCount - inProgressCount;

            double completionPercentage = totalCount > 0 ? (double)completedCount / totalCount * 100 : 0;

            // 获取最近完成的练习信息
            var lastCompletedRecord = completionRecords
                .Where(c => c.Status == SpecialPracticeCompletionStatus.Completed && c.CompletedAt.HasValue)
                .OrderByDescending(c => c.CompletedAt)
                .FirstOrDefault();

            string? lastCompletedPracticeName = null;
            DateTime? lastCompletedAt = null;

            if (lastCompletedRecord != null)
            {
                // 获取练习名称
                var practice = await _context.ImportedSpecializedTrainings
                    .FirstOrDefaultAsync(t => t.Id == lastCompletedRecord.PracticeId);
                lastCompletedPracticeName = practice?.Name;
                lastCompletedAt = lastCompletedRecord.CompletedAt;
            }

            SpecialPracticeProgressDto progress = new()
            {
                TotalCount = totalCount,
                CompletedCount = completedCount,
                CompletionPercentage = Math.Round(completionPercentage, 1),
                InProgressCount = inProgressCount,
                NotStartedCount = notStartedCount,
                LastCompletedPracticeName = lastCompletedPracticeName,
                LastCompletedAt = lastCompletedAt
            };

            _logger.LogInformation("获取学生专项练习进度统计成功，学生ID: {StudentUserId}, 总数: {TotalCount}, 完成数: {CompletedCount}",
                studentUserId, progress.TotalCount, progress.CompletedCount);

            return progress;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生专项练习进度统计失败，学生ID: {StudentUserId}", studentUserId);
            throw;
        }
    }

    /// <summary>
    /// 获取学生可访问的专项练习总数
    /// </summary>
    public async Task<int> GetAvailablePracticeCountAsync(int studentUserId)
    {
        try
        {
            // 目前简化权限验证：所有启用的专项练习都对学生可见
            int count = await _context.ImportedSpecializedTrainings
                .CountAsync(t => t.IsEnabled);

            _logger.LogInformation("获取学生可访问专项练习总数成功，学生ID: {StudentUserId}, 总数: {Count}",
                studentUserId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问专项练习总数失败，学生ID: {StudentUserId}", studentUserId);
            throw;
        }
    }

    /// <summary>
    /// 标记专项练习为已完成
    /// </summary>
    public async Task<bool> MarkPracticeAsCompletedAsync(int studentUserId, int practiceId, decimal? score = null, decimal? maxScore = null, int? durationSeconds = null, string? notes = null)
    {
        try
        {
            // 验证学生用户存在且为学生角色
            Models.User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == Models.UserRole.Student && u.IsActive);

            if (student == null)
            {
                _logger.LogWarning("用户不存在或不是活跃学生，用户ID: {UserId}", studentUserId);
                return false;
            }

            // 验证练习存在且启用
            var practice = await _context.ImportedSpecializedTrainings
                .FirstOrDefaultAsync(t => t.Id == practiceId && t.IsEnabled);

            if (practice == null)
            {
                _logger.LogWarning("专项练习不存在或未启用，练习ID: {PracticeId}", practiceId);
                return false;
            }

            // 查找现有的完成记录
            var existingRecord = await _context.SpecialPracticeCompletions
                .FirstOrDefaultAsync(c => c.StudentUserId == studentUserId && c.PracticeId == practiceId && c.IsActive);

            DateTime now = DateTime.UtcNow;

            if (existingRecord != null)
            {
                // 更新现有记录
                existingRecord.Status = SpecialPracticeCompletionStatus.Completed;
                existingRecord.CompletedAt = now;
                existingRecord.Score = score;
                existingRecord.MaxScore = maxScore;
                existingRecord.DurationSeconds = durationSeconds;
                existingRecord.Notes = notes;
                existingRecord.UpdatedAt = now;

                // 计算完成百分比
                if (score.HasValue && maxScore.HasValue && maxScore.Value > 0)
                {
                    existingRecord.CompletionPercentage = Math.Round((score.Value / maxScore.Value) * 100, 2);
                }

                _logger.LogInformation("更新专项练习完成记录，学生ID: {StudentUserId}, 练习ID: {PracticeId}", studentUserId, practiceId);
            }
            else
            {
                // 创建新的完成记录
                var newRecord = new SpecialPracticeCompletion
                {
                    StudentUserId = studentUserId,
                    PracticeId = practiceId,
                    Status = SpecialPracticeCompletionStatus.Completed,
                    StartedAt = now, // 假设开始时间就是完成时间（如果没有先标记为开始）
                    CompletedAt = now,
                    Score = score,
                    MaxScore = maxScore,
                    DurationSeconds = durationSeconds,
                    Notes = notes,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                // 计算完成百分比
                if (score.HasValue && maxScore.HasValue && maxScore.Value > 0)
                {
                    newRecord.CompletionPercentage = Math.Round((score.Value / maxScore.Value) * 100, 2);
                }

                _context.SpecialPracticeCompletions.Add(newRecord);
                _logger.LogInformation("创建新的专项练习完成记录，学生ID: {StudentUserId}, 练习ID: {PracticeId}", studentUserId, practiceId);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记专项练习为已完成失败，学生ID: {StudentUserId}, 练习ID: {PracticeId}", studentUserId, practiceId);
            return false;
        }
    }

    /// <summary>
    /// 标记专项练习为开始状态
    /// </summary>
    public async Task<bool> MarkPracticeAsStartedAsync(int studentUserId, int practiceId)
    {
        try
        {
            // 验证学生用户存在且为学生角色
            Models.User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == Models.UserRole.Student && u.IsActive);

            if (student == null)
            {
                _logger.LogWarning("用户不存在或不是活跃学生，用户ID: {UserId}", studentUserId);
                return false;
            }

            // 验证练习存在且启用
            var practice = await _context.ImportedSpecializedTrainings
                .FirstOrDefaultAsync(t => t.Id == practiceId && t.IsEnabled);

            if (practice == null)
            {
                _logger.LogWarning("专项练习不存在或未启用，练习ID: {PracticeId}", practiceId);
                return false;
            }

            // 查找现有的完成记录
            var existingRecord = await _context.SpecialPracticeCompletions
                .FirstOrDefaultAsync(c => c.StudentUserId == studentUserId && c.PracticeId == practiceId && c.IsActive);

            DateTime now = DateTime.UtcNow;

            if (existingRecord != null)
            {
                // 如果已经是完成状态，不允许重新标记为开始
                if (existingRecord.Status == SpecialPracticeCompletionStatus.Completed)
                {
                    _logger.LogInformation("专项练习已完成，不允许重新标记为开始，学生ID: {StudentUserId}, 练习ID: {PracticeId}", studentUserId, practiceId);
                    return false;
                }

                // 更新为进行中状态
                existingRecord.Status = SpecialPracticeCompletionStatus.InProgress;
                existingRecord.StartedAt = existingRecord.StartedAt ?? now; // 如果没有开始时间则设置
                existingRecord.UpdatedAt = now;

                _logger.LogInformation("更新专项练习为进行中状态，学生ID: {StudentUserId}, 练习ID: {PracticeId}", studentUserId, practiceId);
            }
            else
            {
                // 创建新的开始记录
                var newRecord = new SpecialPracticeCompletion
                {
                    StudentUserId = studentUserId,
                    PracticeId = practiceId,
                    Status = SpecialPracticeCompletionStatus.InProgress,
                    StartedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                _context.SpecialPracticeCompletions.Add(newRecord);
                _logger.LogInformation("创建新的专项练习开始记录，学生ID: {StudentUserId}, 练习ID: {PracticeId}", studentUserId, practiceId);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记专项练习为开始状态失败，学生ID: {StudentUserId}, 练习ID: {PracticeId}", studentUserId, practiceId);
            return false;
        }
    }

    /// <summary>
    /// 获取学生专项练习完成记录
    /// </summary>
    public async Task<List<SpecialPracticeCompletionDto>> GetPracticeCompletionsAsync(int studentUserId, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            // 由于SpecialPracticeCompletion没有直接的Practice导航属性，我们需要手动关联
            List<SpecialPracticeCompletion> completionEntities = await _context.SpecialPracticeCompletions
                .Where(c => c.StudentUserId == studentUserId && c.IsActive)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 获取所有相关的专项训练信息
            List<int> practiceIds = completionEntities.Select(c => c.PracticeId).Distinct().ToList();
            Dictionary<int, Models.ImportedSpecializedTraining.ImportedSpecializedTraining> practiceDict = await _context.ImportedSpecializedTrainings
                .Where(p => practiceIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            // 构建DTO
            List<SpecialPracticeCompletionDto> completions = completionEntities.Select(c => new SpecialPracticeCompletionDto
            {
                Id = c.Id,
                PracticeId = c.PracticeId,
                PracticeName = practiceDict.ContainsKey(c.PracticeId) ? practiceDict[c.PracticeId].Name : $"专项练习 {c.PracticeId}",
                PracticeDescription = practiceDict.ContainsKey(c.PracticeId) ? practiceDict[c.PracticeId].Description : null,
                Status = c.Status,
                Score = c.Score,
                MaxScore = c.MaxScore,
                CompletionPercentage = c.CompletionPercentage,
                DurationSeconds = c.DurationSeconds,
                Notes = c.Notes,
                StartedAt = c.StartedAt,
                CompletedAt = c.CompletedAt,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();

            _logger.LogInformation("获取学生专项练习完成记录成功，学生ID: {StudentUserId}, 页码: {PageNumber}, 页大小: {PageSize}, 记录数: {Count}",
                studentUserId, pageNumber, pageSize, completions.Count);

            return completions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生专项练习完成记录失败，学生ID: {StudentUserId}", studentUserId);
            return new List<SpecialPracticeCompletionDto>();
        }
    }
}
