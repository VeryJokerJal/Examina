using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Exam;

namespace ExaminaWebApplication.Services.Exam;

/// <summary>
/// 试卷科目管理服务类
/// </summary>
public partial class ExamSubjectService
{
    private readonly ApplicationDbContext _context;

    public ExamSubjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取试卷的所有科目
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <returns></returns>
    public async Task<List<ExamSubject>> GetExamSubjectsAsync(int examId)
    {
        return await _context.ExamSubjects
            .Include(s => s.Questions.OrderBy(q => q.SortOrder))
            .Where(s => s.ExamId == examId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();
    }

    /// <summary>
    /// 根据ID获取科目详情
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<ExamSubject?> GetSubjectByIdAsync(int subjectId)
    {
        return await _context.ExamSubjects
            .Include(s => s.Exam)
            .Include(s => s.Questions.OrderBy(q => q.SortOrder))
            .FirstOrDefaultAsync(s => s.Id == subjectId);
    }

    /// <summary>
    /// 创建试卷科目
    /// </summary>
    /// <param name="subject">科目信息</param>
    /// <returns></returns>
    public async Task<ExamSubject> CreateSubjectAsync(ExamSubject subject)
    {
        // 验证试卷是否存在
        bool examExists = await _context.Exams.AnyAsync(e => e.Id == subject.ExamId);
        if (!examExists)
        {
            throw new ArgumentException($"试卷 ID {subject.ExamId} 不存在");
        }

        // 检查试卷状态
        Models.Exam.Exam? exam = await _context.Exams.FindAsync(subject.ExamId);
        if (exam != null && (exam.Status == ExamStatus.Published || exam.Status == ExamStatus.InProgress))
        {
            throw new InvalidOperationException("已发布或进行中的试卷不能添加科目");
        }

        // 获取下一个排序号
        int nextSortOrder = await GetNextSortOrderAsync(subject.ExamId);
        subject.SortOrder = nextSortOrder;
        subject.CreatedAt = DateTime.UtcNow;

        _context.ExamSubjects.Add(subject);
        await _context.SaveChangesAsync();

        return subject;
    }

    /// <summary>
    /// 更新科目信息
    /// </summary>
    /// <param name="subject">科目信息</param>
    /// <returns></returns>
    public async Task<ExamSubject> UpdateSubjectAsync(ExamSubject subject)
    {
        ExamSubject? existingSubject = await _context.ExamSubjects.FindAsync(subject.Id);
        if (existingSubject == null)
        {
            throw new ArgumentException($"科目 ID {subject.Id} 不存在");
        }

        // 检查试卷状态
        Models.Exam.Exam? exam = await _context.Exams.FindAsync(existingSubject.ExamId);
        if (exam != null && (exam.Status == ExamStatus.Published || exam.Status == ExamStatus.InProgress))
        {
            throw new InvalidOperationException("已发布或进行中的试卷科目不能编辑");
        }

        // 更新科目信息
        existingSubject.SubjectName = subject.SubjectName;
        existingSubject.Description = subject.Description;
        existingSubject.Score = subject.Score;
        existingSubject.DurationMinutes = subject.DurationMinutes;
        existingSubject.IsRequired = subject.IsRequired;
        existingSubject.MinScore = subject.MinScore;
        existingSubject.Weight = subject.Weight;
        existingSubject.SubjectConfig = subject.SubjectConfig;
        existingSubject.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingSubject;
    }

    /// <summary>
    /// 删除科目
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<bool> DeleteSubjectAsync(int subjectId)
    {
        ExamSubject? subject = await _context.ExamSubjects.FindAsync(subjectId);
        if (subject == null)
        {
            return false;
        }

        // 检查试卷状态
        Models.Exam.Exam? exam = await _context.Exams.FindAsync(subject.ExamId);
        if (exam != null && (exam.Status == ExamStatus.Published || exam.Status == ExamStatus.InProgress))
        {
            throw new InvalidOperationException("已发布或进行中的试卷科目不能删除");
        }

        // 检查是否有题目
        bool hasQuestions = await _context.ExamQuestions.AnyAsync(q => q.ExamSubjectId == subjectId);
        if (hasQuestions)
        {
            throw new InvalidOperationException("包含题目的科目不能删除，请先删除所有题目");
        }

        _context.ExamSubjects.Remove(subject);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 调整科目顺序
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <param name="subjectOrders">科目顺序列表</param>
    /// <returns></returns>
    public async Task ReorderSubjectsAsync(int examId, List<SubjectOrderItem> subjectOrders)
    {
        List<ExamSubject> subjects = await _context.ExamSubjects
            .Where(s => s.ExamId == examId)
            .ToListAsync();

        foreach (SubjectOrderItem orderItem in subjectOrders)
        {
            ExamSubject? subject = subjects.FirstOrDefault(s => s.Id == orderItem.SubjectId);
            if (subject != null)
            {
                subject.SortOrder = orderItem.SortOrder;
                subject.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 创建标准五科目试卷结构
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <param name="config">科目配置</param>
    /// <returns></returns>
    public async Task<List<ExamSubject>> CreateStandardSubjectsAsync(int examId, StandardSubjectsConfig? config = null)
    {
        // 验证试卷是否存在
        bool examExists = await _context.Exams.AnyAsync(e => e.Id == examId);
        if (!examExists)
        {
            throw new ArgumentException($"试卷 ID {examId} 不存在");
        }

        // 检查是否已有科目
        bool hasSubjects = await _context.ExamSubjects.AnyAsync(s => s.ExamId == examId);
        if (hasSubjects)
        {
            throw new InvalidOperationException("试卷已包含科目，不能重复创建");
        }

        config ??= new StandardSubjectsConfig();

        List<ExamSubject> subjects = new List<ExamSubject>
        {
            new ExamSubject
            {
                ExamId = examId,
                SubjectType = SubjectType.Excel,
                SubjectName = "Excel",
                Description = "Excel电子表格操作",
                Score = config.ExcelScore,
                DurationMinutes = config.ExcelDuration,
                SortOrder = 1,
                IsRequired = true,
                Weight = 1.0m,
                SubjectConfig = JsonSerializer.Serialize(new ExcelSubjectConfig()),
                CreatedAt = DateTime.UtcNow
            },
            new ExamSubject
            {
                ExamId = examId,
                SubjectType = SubjectType.PowerPoint,
                SubjectName = "PowerPoint",
                Description = "PowerPoint演示文稿操作",
                Score = config.PowerPointScore,
                DurationMinutes = config.PowerPointDuration,
                SortOrder = 2,
                IsRequired = true,
                Weight = 1.0m,
                IsEnabled = false, // 暂未实现，设为禁用
                CreatedAt = DateTime.UtcNow
            },
            new ExamSubject
            {
                ExamId = examId,
                SubjectType = SubjectType.Word,
                SubjectName = "Word",
                Description = "Word文档处理操作",
                Score = config.WordScore,
                DurationMinutes = config.WordDuration,
                SortOrder = 3,
                IsRequired = true,
                Weight = 1.0m,
                IsEnabled = false, // 暂未实现，设为禁用
                CreatedAt = DateTime.UtcNow
            },
            new ExamSubject
            {
                ExamId = examId,
                SubjectType = SubjectType.Windows,
                SubjectName = "Windows",
                Description = "Windows文件系统操作",
                Score = config.WindowsScore,
                DurationMinutes = config.WindowsDuration,
                SortOrder = 4,
                IsRequired = true,
                Weight = 1.0m,
                SubjectConfig = JsonSerializer.Serialize(new WindowsSubjectConfig()),
                IsEnabled = true, // 已实现Windows操作功能
                CreatedAt = DateTime.UtcNow
            },
            new ExamSubject
            {
                ExamId = examId,
                SubjectType = SubjectType.CSharp,
                SubjectName = "C#",
                Description = "C#编程语言操作",
                Score = config.CSharpScore,
                DurationMinutes = config.CSharpDuration,
                SortOrder = 5,
                IsRequired = true,
                Weight = 1.0m,
                IsEnabled = false, // 暂未实现，设为禁用
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.ExamSubjects.AddRange(subjects);
        await _context.SaveChangesAsync();

        return subjects;
    }

    /// <summary>
    /// 获取科目统计信息
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<SubjectStatistics> GetSubjectStatisticsAsync(int subjectId)
    {
        ExamSubject? subject = await _context.ExamSubjects
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == subjectId);

        if (subject == null)
        {
            throw new ArgumentException($"科目 ID {subjectId} 不存在");
        }

        List<ExamQuestion> questions = subject.Questions.ToList();

        SubjectStatistics statistics = new SubjectStatistics
        {
            SubjectType = subject.SubjectType,
            SubjectName = subject.SubjectName,
            TotalScore = questions.Sum(q => q.Score),
            AverageDifficulty = questions.Count != 0 ? (decimal)questions.Average(q => q.DifficultyLevel) : 0,
            DifficultyDistribution = questions
                .GroupBy(q => q.DifficultyLevel)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return statistics;
    }

    /// <summary>
    /// 获取下一个排序号
    /// </summary>
    /// <param name="examId">试卷ID</param>
    /// <returns></returns>
    private async Task<int> GetNextSortOrderAsync(int examId)
    {
        int? maxOrder = await _context.ExamSubjects
            .Where(s => s.ExamId == examId)
            .MaxAsync(s => (int?)s.SortOrder);

        return (maxOrder ?? 0) + 1;
    }
}

/// <summary>
/// 科目顺序项
/// </summary>
public class SubjectOrderItem
{
    /// <summary>
    /// 科目ID
    /// </summary>
    public int SubjectId { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// 标准科目配置
/// </summary>
public class StandardSubjectsConfig
{
    /// <summary>
    /// Excel科目分值
    /// </summary>
    public decimal ExcelScore { get; set; } = 20.0m;

    /// <summary>
    /// Excel科目时长
    /// </summary>
    public int ExcelDuration { get; set; } = 30;

    /// <summary>
    /// PowerPoint科目分值
    /// </summary>
    public decimal PowerPointScore { get; set; } = 20.0m;

    /// <summary>
    /// PowerPoint科目时长
    /// </summary>
    public int PowerPointDuration { get; set; } = 25;

    /// <summary>
    /// Word科目分值
    /// </summary>
    public decimal WordScore { get; set; } = 20.0m;

    /// <summary>
    /// Word科目时长
    /// </summary>
    public int WordDuration { get; set; } = 25;

    /// <summary>
    /// Windows科目分值
    /// </summary>
    public decimal WindowsScore { get; set; } = 20.0m;

    /// <summary>
    /// Windows科目时长
    /// </summary>
    public int WindowsDuration { get; set; } = 20;

    /// <summary>
    /// C#科目分值
    /// </summary>
    public decimal CSharpScore { get; set; } = 20.0m;

    /// <summary>
    /// C#科目时长
    /// </summary>
    public int CSharpDuration { get; set; } = 20;
}

public partial class ExamSubjectService
{
    /// <summary>
    /// 获取科目的操作点配置
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<List<ExamSubjectOperationPoint>> GetSubjectOperationPointsAsync(int subjectId)
    {
        return await _context.ExamSubjectOperationPoints
            .Where(op => op.ExamSubjectId == subjectId)
            .OrderBy(op => op.SortOrder)
            .ToListAsync();
    }



    /// <summary>
    /// 获取科目操作点统计
    /// </summary>
    /// <param name="subjectId">科目ID</param>
    /// <returns></returns>
    public async Task<OperationPointStatistics> GetOperationPointStatisticsAsync(int subjectId)
    {
        ExamSubject? subject = await GetSubjectByIdAsync(subjectId);
        if (subject == null)
        {
            throw new ArgumentException($"科目 ID {subjectId} 不存在");
        }

        List<ExamSubjectOperationPoint> operationPoints = await _context.ExamSubjectOperationPoints
            .Where(op => op.ExamSubjectId == subjectId)
            .ToListAsync();

        return new OperationPointStatistics
        {
            SubjectId = subjectId,
            SubjectName = subject.SubjectName,
            SubjectType = subject.SubjectType,
            TotalOperationPoints = operationPoints.Count,
            EnabledOperationPoints = operationPoints.Count(op => op.IsEnabled),
            TotalWeight = operationPoints.Where(op => op.IsEnabled).Sum(op => op.Weight),
            LastUpdated = operationPoints.Count != 0 ? operationPoints.Max(op => op.UpdatedAt ?? op.CreatedAt) : null
        };
    }
}
