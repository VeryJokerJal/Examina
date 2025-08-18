using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Models.ImportedExam;
using Microsoft.EntityFrameworkCore;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生端考试服务实现
/// </summary>
public class StudentExamService : IStudentExamService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StudentExamService> _logger;

    public StudentExamService(ApplicationDbContext context, ILogger<StudentExamService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取学生可访问的考试列表
    /// </summary>
    public async Task<List<StudentExamDto>> GetAvailableExamsAsync(int studentUserId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            // 目前简化权限验证：所有启用的考试都对学生可见
            // 后续可以根据组织关系、权限设置等进行更细粒度的权限控制
            List<ImportedExam> exams = await _context.ImportedExams
                .Where(e => e.IsEnabled)
                .OrderByDescending(e => e.ImportedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<StudentExamDto> result = exams.Select(MapToStudentExamDto).ToList();

            _logger.LogInformation("获取学生可访问考试列表成功，学生ID: {StudentUserId}, 返回数量: {Count}", 
                studentUserId, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问考试列表失败，学生ID: {StudentUserId}", studentUserId);
            throw;
        }
    }

    /// <summary>
    /// 获取考试详情
    /// </summary>
    public async Task<StudentExamDto?> GetExamDetailsAsync(int examId, int studentUserId)
    {
        try
        {
            // 检查权限
            if (!await HasAccessToExamAsync(examId, studentUserId))
            {
                _logger.LogWarning("学生无权限访问考试，学生ID: {StudentUserId}, 考试ID: {ExamId}", 
                    studentUserId, examId);
                return null;
            }

            ImportedExam? exam = await _context.ImportedExams
                .Include(e => e.Subjects)
                    .ThenInclude(s => s.Questions)
                        .ThenInclude(q => q.OperationPoints)
                            .ThenInclude(op => op.Parameters)
                .Include(e => e.Modules)
                    .ThenInclude(m => m.Questions)
                        .ThenInclude(q => q.OperationPoints)
                            .ThenInclude(op => op.Parameters)
                .FirstOrDefaultAsync(e => e.Id == examId && e.IsEnabled);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在或已禁用，考试ID: {ExamId}", examId);
                return null;
            }

            StudentExamDto result = MapToStudentExamDtoWithDetails(exam);

            _logger.LogInformation("获取考试详情成功，学生ID: {StudentUserId}, 考试ID: {ExamId}", 
                studentUserId, examId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试详情失败，学生ID: {StudentUserId}, 考试ID: {ExamId}", 
                studentUserId, examId);
            throw;
        }
    }

    /// <summary>
    /// 检查学生是否有权限访问指定考试
    /// </summary>
    public async Task<bool> HasAccessToExamAsync(int examId, int studentUserId)
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

            // 验证考试存在且启用
            bool examExists = await _context.ImportedExams
                .AnyAsync(e => e.Id == examId && e.IsEnabled);

            if (!examExists)
            {
                _logger.LogWarning("考试不存在或已禁用，考试ID: {ExamId}", examId);
                return false;
            }

            // 目前简化权限验证：所有启用的考试都对学生可见
            // 后续可以根据组织关系、权限设置等进行更细粒度的权限控制
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查考试访问权限失败，学生ID: {StudentUserId}, 考试ID: {ExamId}", 
                studentUserId, examId);
            return false;
        }
    }

    /// <summary>
    /// 获取学生可访问的考试总数
    /// </summary>
    public async Task<int> GetAvailableExamCountAsync(int studentUserId)
    {
        try
        {
            // 目前简化权限验证：所有启用的考试都对学生可见
            int count = await _context.ImportedExams
                .CountAsync(e => e.IsEnabled);

            _logger.LogInformation("获取学生可访问考试总数成功，学生ID: {StudentUserId}, 总数: {Count}", 
                studentUserId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问考试总数失败，学生ID: {StudentUserId}", studentUserId);
            throw;
        }
    }

    /// <summary>
    /// 映射到学生端考试DTO（不包含详细信息）
    /// </summary>
    private static StudentExamDto MapToStudentExamDto(ImportedExam exam)
    {
        return new StudentExamDto
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
            Tags = exam.Tags,
            Subjects = [],
            Modules = []
        };
    }

    /// <summary>
    /// 映射到学生端考试DTO（包含完整详细信息）
    /// </summary>
    private static StudentExamDto MapToStudentExamDtoWithDetails(ImportedExam exam)
    {
        StudentExamDto dto = MapToStudentExamDto(exam);

        // 映射科目
        dto.Subjects = exam.Subjects.Select(subject => new StudentSubjectDto
        {
            Id = subject.Id,
            SubjectType = subject.SubjectType,
            SubjectName = subject.SubjectName,
            Description = subject.Description,
            Score = subject.Score,
            DurationMinutes = subject.DurationMinutes,
            SortOrder = subject.SortOrder,
            IsRequired = subject.IsRequired,
            MinScore = subject.MinScore,
            Weight = subject.Weight,
            QuestionCount = subject.QuestionCount,
            Questions = subject.Questions.Select(MapToStudentQuestionDto).ToList()
        }).ToList();

        // 映射模块
        dto.Modules = exam.Modules.Select(module => new StudentModuleDto
        {
            Id = module.Id,
            Name = module.Name,
            Type = module.Type,
            Description = module.Description,
            Score = module.Score,
            Order = module.Order,
            Questions = module.Questions.Select(MapToStudentQuestionDto).ToList()
        }).ToList();

        return dto;
    }

    /// <summary>
    /// 映射到学生端题目DTO
    /// </summary>
    private static StudentQuestionDto MapToStudentQuestionDto(ImportedQuestion question)
    {
        return new StudentQuestionDto
        {
            Id = question.Id,
            Title = question.Title,
            Content = question.Content,
            QuestionType = question.QuestionType,
            Score = question.Score,
            DifficultyLevel = question.DifficultyLevel,
            EstimatedMinutes = question.EstimatedMinutes,
            SortOrder = question.SortOrder,
            IsRequired = question.IsRequired,
            QuestionConfig = question.QuestionConfig,
            AnswerValidationRules = question.AnswerValidationRules,
            Tags = question.Tags,
            Remarks = question.Remarks,
            ProgramInput = question.ProgramInput,
            ExpectedOutput = question.ExpectedOutput,
            OperationPoints = question.OperationPoints.Select(op => new StudentOperationPointDto
            {
                Id = op.Id,
                Name = op.Name,
                Description = op.Description,
                ModuleType = op.ModuleType,
                Score = op.Score,
                Order = op.Order,
                Parameters = op.Parameters.Select(param => new StudentParameterDto
                {
                    Id = param.Id,
                    Name = param.Name,
                    Description = param.Description,
                    ParameterType = param.ParameterType,
                    DefaultValue = param.DefaultValue,
                    MinValue = param.MinValue,
                    MaxValue = param.MaxValue
                }).ToList()
            }).ToList()
        };
    }
}
