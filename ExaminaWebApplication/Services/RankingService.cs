using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Ranking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrganizationEntity = ExaminaWebApplication.Models.Organization.Organization;

namespace ExaminaWebApplication.Services;

/// <summary>
/// 排行榜服务
/// </summary>
public class RankingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RankingService> _logger;

    public RankingService(ApplicationDbContext context, ILogger<RankingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取排行榜数据
    /// </summary>
    public async Task<RankingResponseDto> GetRankingAsync(RankingQueryDto query)
    {
        try
        {
            _logger.LogInformation("获取排行榜数据，类型: {Type}, 页码: {Page}, 每页: {PageSize}, 试卷ID: {ExamId}, 开始时间: {StartDate}, 结束时间: {EndDate}",
                query.Type, query.Page, query.PageSize, query.ExamId, query.StartDate, query.EndDate);

            RankingResponseDto response = query.Type switch
            {
                RankingType.ExamRanking => await GetExamRankingAsync(query),
                RankingType.MockExamRanking => await GetMockExamRankingAsync(query),
                RankingType.TrainingRanking => await GetTrainingRankingAsync(query),
                _ => throw new ArgumentException($"不支持的排行榜类型: {query.Type}")
            };

            _logger.LogInformation("成功获取排行榜数据，类型: {Type}, 记录数: {Count}, 总数: {TotalCount}",
                query.Type, response.Entries.Count, response.TotalCount);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取排行榜数据失败，类型: {Type}", query.Type);
            throw;
        }
    }

    /// <summary>
    /// 获取上机统考排行榜
    /// </summary>
    private async Task<RankingResponseDto> GetExamRankingAsync(RankingQueryDto query)
    {
        IQueryable<ExamCompletion> baseQuery = _context.ExamCompletions
            .Include(ec => ec.Student)
            .Include(ec => ec.Exam)
            .Where(ec => ec.Status == ExamCompletionStatus.Completed && 
                        ec.IsActive && 
                        ec.Score.HasValue &&
                        ec.CompletedAt.HasValue);

        // 应用时间过滤
        if (query.StartDate.HasValue)
        {
            baseQuery = baseQuery.Where(ec => ec.CompletedAt >= query.StartDate.Value);
        }
        if (query.EndDate.HasValue)
        {
            baseQuery = baseQuery.Where(ec => ec.CompletedAt <= query.EndDate.Value);
        }

        // 应用试卷筛选
        if (query.ExamId.HasValue)
        {
            baseQuery = baseQuery.Where(ec => ec.ExamId == query.ExamId.Value);
        }

        // 获取总数
        int totalCount = await baseQuery.CountAsync();
        _logger.LogInformation("上机统考排行榜查询，总记录数: {TotalCount}, 试卷筛选: {ExamId}", totalCount, query.ExamId);

        // 排序并分页
        List<ExamCompletion> completions = await baseQuery
            .OrderByDescending(ec => ec.Score)
            .ThenBy(ec => ec.DurationSeconds)
            .ThenBy(ec => ec.CompletedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        _logger.LogInformation("上机统考排行榜查询完成，返回记录数: {Count}", completions.Count);

        // 转换为DTO并添加排名
        List<RankingEntryDto> entries = [];
        for (int i = 0; i < completions.Count; i++)
        {
            ExamCompletion completion = completions[i];
            RankingEntryDto entry = await ConvertToRankingEntryAsync(completion, query.Page, query.PageSize, i);
            entries.Add(entry);
        }

        return new RankingResponseDto
        {
            Type = RankingType.ExamRanking,
            TypeName = "上机统考排行榜",
            Entries = entries,
            TotalCount = totalCount,
            CurrentPage = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
        };
    }

    /// <summary>
    /// 获取模拟考试排行榜
    /// </summary>
    private async Task<RankingResponseDto> GetMockExamRankingAsync(RankingQueryDto query)
    {
        IQueryable<MockExamCompletion> baseQuery = _context.MockExamCompletions
            .Include(mec => mec.Student)
            .Include(mec => mec.MockExam)
            .Where(mec => mec.Status == MockExamCompletionStatus.Completed && 
                         mec.IsActive && 
                         mec.Score.HasValue &&
                         mec.CompletedAt.HasValue);

        // 应用时间过滤
        if (query.StartDate.HasValue)
        {
            baseQuery = baseQuery.Where(mec => mec.CompletedAt >= query.StartDate.Value);
        }
        if (query.EndDate.HasValue)
        {
            baseQuery = baseQuery.Where(mec => mec.CompletedAt <= query.EndDate.Value);
        }

        // 应用试卷筛选（模拟考试使用MockExamId）
        if (query.ExamId.HasValue)
        {
            baseQuery = baseQuery.Where(mec => mec.MockExamId == query.ExamId.Value);
        }

        // 获取总数
        int totalCount = await baseQuery.CountAsync();

        // 添加详细的诊断日志
        int totalMockExamCompletions = await _context.MockExamCompletions.CountAsync();
        int completedRecords = await _context.MockExamCompletions.CountAsync(mec => mec.Status == MockExamCompletionStatus.Completed);
        int activeRecords = await _context.MockExamCompletions.CountAsync(mec => mec.IsActive);
        int recordsWithScore = await _context.MockExamCompletions.CountAsync(mec => mec.Score.HasValue);
        int recordsWithCompletedAt = await _context.MockExamCompletions.CountAsync(mec => mec.CompletedAt.HasValue);

        _logger.LogInformation("模拟考试排行榜查询诊断 - 总记录: {Total}, 已完成: {Completed}, 活跃: {Active}, 有分数: {WithScore}, 有完成时间: {WithCompletedAt}, 符合条件: {Qualified}, 试卷筛选: {ExamId}",
            totalMockExamCompletions, completedRecords, activeRecords, recordsWithScore, recordsWithCompletedAt, totalCount, query.ExamId);

        // 排序并分页
        List<MockExamCompletion> completions = await baseQuery
            .OrderByDescending(mec => mec.Score)
            .ThenBy(mec => mec.DurationSeconds)
            .ThenBy(mec => mec.CompletedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        _logger.LogInformation("模拟考试排行榜查询完成，返回记录数: {Count}", completions.Count);

        // 转换为DTO并添加排名
        List<RankingEntryDto> entries = [];
        for (int i = 0; i < completions.Count; i++)
        {
            MockExamCompletion completion = completions[i];
            RankingEntryDto entry = await ConvertToRankingEntryAsync(completion, query.Page, query.PageSize, i);
            entries.Add(entry);
        }

        return new RankingResponseDto
        {
            Type = RankingType.MockExamRanking,
            TypeName = "模拟考试排行榜",
            Entries = entries,
            TotalCount = totalCount,
            CurrentPage = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
        };
    }

    /// <summary>
    /// 获取综合实训排行榜
    /// </summary>
    private async Task<RankingResponseDto> GetTrainingRankingAsync(RankingQueryDto query)
    {
        IQueryable<ComprehensiveTrainingCompletion> baseQuery = _context.ComprehensiveTrainingCompletions
            .Include(ctc => ctc.Student)
            .Include(ctc => ctc.Training)
            .Where(ctc => ctc.Status == ComprehensiveTrainingCompletionStatus.Completed && 
                         ctc.IsActive && 
                         ctc.Score.HasValue &&
                         ctc.CompletedAt.HasValue);

        // 应用时间过滤
        if (query.StartDate.HasValue)
        {
            baseQuery = baseQuery.Where(ctc => ctc.CompletedAt >= query.StartDate.Value);
        }
        if (query.EndDate.HasValue)
        {
            baseQuery = baseQuery.Where(ctc => ctc.CompletedAt <= query.EndDate.Value);
        }

        // 应用试卷筛选（综合实训使用TrainingId）
        if (query.ExamId.HasValue)
        {
            baseQuery = baseQuery.Where(ctc => ctc.TrainingId == query.ExamId.Value);
        }

        // 获取总数
        int totalCount = await baseQuery.CountAsync();

        // 添加详细的诊断日志
        int totalTrainingCompletions = await _context.ComprehensiveTrainingCompletions.CountAsync();
        int completedRecords = await _context.ComprehensiveTrainingCompletions.CountAsync(ctc => ctc.Status == ComprehensiveTrainingCompletionStatus.Completed);
        int activeRecords = await _context.ComprehensiveTrainingCompletions.CountAsync(ctc => ctc.IsActive);
        int recordsWithScore = await _context.ComprehensiveTrainingCompletions.CountAsync(ctc => ctc.Score.HasValue);
        int recordsWithCompletedAt = await _context.ComprehensiveTrainingCompletions.CountAsync(ctc => ctc.CompletedAt.HasValue);

        _logger.LogInformation("综合实训排行榜查询诊断 - 总记录: {Total}, 已完成: {Completed}, 活跃: {Active}, 有分数: {WithScore}, 有完成时间: {WithCompletedAt}, 符合条件: {Qualified}, 试卷筛选: {ExamId}",
            totalTrainingCompletions, completedRecords, activeRecords, recordsWithScore, recordsWithCompletedAt, totalCount, query.ExamId);

        // 排序并分页
        List<ComprehensiveTrainingCompletion> completions = await baseQuery
            .OrderByDescending(ctc => ctc.Score)
            .ThenBy(ctc => ctc.DurationSeconds)
            .ThenBy(ctc => ctc.CompletedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        _logger.LogInformation("综合实训排行榜查询完成，返回记录数: {Count}", completions.Count);

        // 转换为DTO并添加排名
        List<RankingEntryDto> entries = [];
        for (int i = 0; i < completions.Count; i++)
        {
            ComprehensiveTrainingCompletion completion = completions[i];
            RankingEntryDto entry = await ConvertToRankingEntryAsync(completion, query.Page, query.PageSize, i);
            entries.Add(entry);
        }

        return new RankingResponseDto
        {
            Type = RankingType.TrainingRanking,
            TypeName = "综合实训排行榜",
            Entries = entries,
            TotalCount = totalCount,
            CurrentPage = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
        };
    }

    /// <summary>
    /// 将ExamCompletion转换为RankingEntryDto
    /// </summary>
    private async Task<RankingEntryDto> ConvertToRankingEntryAsync(ExamCompletion completion, int page, int pageSize, int indexInPage)
    {
        // 获取学校和班级信息
        (string? schoolName, string? className) = await GetUserOrganizationInfoAsync(completion.StudentUserId);

        return new RankingEntryDto
        {
            Rank = (page - 1) * pageSize + indexInPage + 1,
            StudentUserId = completion.StudentUserId,
            Username = completion.Student?.Username ?? "未知用户",
            RealName = completion.Student?.RealName,
            Score = completion.Score ?? 0,
            MaxScore = completion.MaxScore ?? 0,
            CompletionPercentage = completion.CompletionPercentage ?? 0,
            DurationSeconds = completion.DurationSeconds ?? 0,
            DurationText = FormatDuration(completion.DurationSeconds ?? 0),
            CompletedAt = completion.CompletedAt ?? DateTime.MinValue,
            ExamName = completion.Exam?.Name ?? "未知考试",
            SchoolName = schoolName,
            ClassName = className
        };
    }

    /// <summary>
    /// 将MockExamCompletion转换为RankingEntryDto
    /// </summary>
    private async Task<RankingEntryDto> ConvertToRankingEntryAsync(MockExamCompletion completion, int page, int pageSize, int indexInPage)
    {
        // 获取学校和班级信息
        (string? schoolName, string? className) = await GetUserOrganizationInfoAsync(completion.StudentUserId);

        return new RankingEntryDto
        {
            Rank = (page - 1) * pageSize + indexInPage + 1,
            StudentUserId = completion.StudentUserId,
            Username = completion.Student?.Username ?? "未知用户",
            RealName = completion.Student?.RealName,
            Score = completion.Score ?? 0,
            MaxScore = completion.MaxScore ?? 0,
            CompletionPercentage = completion.CompletionPercentage ?? 0,
            DurationSeconds = completion.DurationSeconds ?? 0,
            DurationText = FormatDuration(completion.DurationSeconds ?? 0),
            CompletedAt = completion.CompletedAt ?? DateTime.MinValue,
            ExamName = completion.MockExam?.Name ?? "未知模拟考试",
            SchoolName = schoolName,
            ClassName = className
        };
    }

    /// <summary>
    /// 将ComprehensiveTrainingCompletion转换为RankingEntryDto
    /// </summary>
    private async Task<RankingEntryDto> ConvertToRankingEntryAsync(ComprehensiveTrainingCompletion completion, int page, int pageSize, int indexInPage)
    {
        // 获取学校和班级信息
        (string? schoolName, string? className) = await GetUserOrganizationInfoAsync(completion.StudentUserId);

        return new RankingEntryDto
        {
            Rank = (page - 1) * pageSize + indexInPage + 1,
            StudentUserId = completion.StudentUserId,
            Username = completion.Student?.Username ?? "未知用户",
            RealName = completion.Student?.RealName,
            Score = completion.Score ?? 0,
            MaxScore = completion.MaxScore ?? 0,
            CompletionPercentage = completion.CompletionPercentage ?? 0,
            DurationSeconds = completion.DurationSeconds ?? 0,
            DurationText = FormatDuration(completion.DurationSeconds ?? 0),
            CompletedAt = completion.CompletedAt ?? DateTime.MinValue,
            ExamName = completion.Training?.Name ?? "未知综合实训",
            SchoolName = schoolName,
            ClassName = className
        };
    }

    /// <summary>
    /// 获取用户的组织信息（学校和班级）
    /// </summary>
    private async Task<(string? schoolName, string? className)> GetUserOrganizationInfoAsync(int userId)
    {
        try
        {
            // 查找学生组织关系
            ExaminaWebApplication.Models.Organization.StudentOrganization? studentOrg = await _context.StudentOrganizations
                .Include(so => so.Organization)
                .ThenInclude(o => o.ParentOrganization)
                .Where(so => so.StudentId == userId && so.IsActive)
                .FirstOrDefaultAsync();

            if (studentOrg?.Organization != null)
            {
                OrganizationEntity org = studentOrg.Organization;
                if (org.Type == ExaminaWebApplication.Models.Organization.OrganizationType.Class && org.ParentOrganization != null)
                {
                    // 用户在班级中，返回学校和班级名称
                    return (org.ParentOrganization.Name, org.Name);
                }
                else if (org.Type == ExaminaWebApplication.Models.Organization.OrganizationType.School)
                {
                    // 用户直接在学校中
                    return (org.Name, null);
                }
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取用户组织信息失败，用户ID: {UserId}", userId);
            return (null, null);
        }
    }

    /// <summary>
    /// 格式化时长显示
    /// </summary>
    private static string FormatDuration(int durationSeconds)
    {
        if (durationSeconds <= 0)
        {
            return "00:00:00";
        }

        TimeSpan timeSpan = TimeSpan.FromSeconds(durationSeconds);
        return timeSpan.ToString(@"hh\:mm\:ss");
    }
}
