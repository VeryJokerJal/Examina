using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Models.ImportedExam;
using ExaminaWebApplication.Services.School;
using Microsoft.EntityFrameworkCore;
using ImportedExamEntity = ExaminaWebApplication.Models.ImportedExam.ImportedExam;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生端考试服务实现
/// </summary>
public class StudentExamService : IStudentExamService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StudentExamService> _logger;
    private readonly ISchoolPermissionService _schoolPermissionService;

    public StudentExamService(
        ApplicationDbContext context,
        ILogger<StudentExamService> logger,
        ISchoolPermissionService schoolPermissionService)
    {
        _context = context;
        _logger = logger;
        _schoolPermissionService = schoolPermissionService;
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
            List<ImportedExamEntity> exams = await _context.ImportedExams
                .Where(e => e.IsEnabled)
                .OrderByDescending(e => e.ImportedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<StudentExamDto> result = [.. exams.Select(MapToStudentExamDto)];

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

            ImportedExamEntity? exam = await _context.ImportedExams
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
            ImportedExamEntity? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId && e.IsEnabled);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在或已禁用，考试ID: {ExamId}", examId);
                return false;
            }

            // 根据考试类型进行权限验证
            if (exam.ExamCategory == ExamCategory.School)
            {
                // 学校统考：需要验证学生是否属于有权限的学校
                bool hasSchoolAccess = await _schoolPermissionService.HasAccessToSchoolExamAsync(studentUserId, examId);
                _logger.LogInformation("学校统考权限验证结果，学生ID: {StudentUserId}, 考试ID: {ExamId}, 有权限: {HasAccess}",
                    studentUserId, examId, hasSchoolAccess);
                return hasSchoolAccess;
            }
            else if (exam.ExamCategory == ExamCategory.Provincial)
            {
                // 全省统考：所有学生都可以访问
                _logger.LogInformation("全省统考权限验证通过，学生ID: {StudentUserId}, 考试ID: {ExamId}",
                    studentUserId, examId);
                return true;
            }
            else
            {
                _logger.LogWarning("未知的考试类型，考试ID: {ExamId}, 类型: {ExamCategory}", examId, exam.ExamCategory);
                return false;
            }
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
    /// 按考试类型获取学生可访问的考试列表
    /// </summary>
    public async Task<List<StudentExamDto>> GetAvailableExamsByCategoryAsync(int studentUserId, ExamCategory examCategory, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            List<ImportedExamEntity> exams;

            if (examCategory == ExamCategory.School)
            {
                // 学校统考：只返回学生所属学校有权限的考试
                List<int> accessibleExamIds = await _schoolPermissionService.GetAccessibleSchoolExamIdsAsync(studentUserId);

                exams = await _context.ImportedExams
                    .Where(e => e.IsEnabled &&
                               e.ExamCategory == examCategory &&
                               accessibleExamIds.Contains(e.Id))
                    .OrderByDescending(e => e.ImportedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("获取学校统考列表，学生ID: {StudentUserId}, 可访问考试数: {AccessibleCount}, 返回数量: {Count}",
                    studentUserId, accessibleExamIds.Count, exams.Count);
            }
            else if (examCategory == ExamCategory.Provincial)
            {
                // 全省统考：所有启用的全省统考都对学生可见
                exams = await _context.ImportedExams
                    .Where(e => e.IsEnabled && e.ExamCategory == examCategory)
                    .OrderByDescending(e => e.ImportedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("获取全省统考列表，学生ID: {StudentUserId}, 返回数量: {Count}",
                    studentUserId, exams.Count);
            }
            else
            {
                _logger.LogWarning("未知的考试类型，学生ID: {StudentUserId}, 考试类型: {ExamCategory}", studentUserId, examCategory);
                return [];
            }

            List<StudentExamDto> result = [.. exams.Select(MapToStudentExamDto)];

            _logger.LogInformation("按类型获取学生可访问考试列表成功，学生ID: {StudentUserId}, 考试类型: {ExamCategory}, 返回数量: {Count}",
                studentUserId, examCategory, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取学生可访问考试列表失败，学生ID: {StudentUserId}, 考试类型: {ExamCategory}", studentUserId, examCategory);
            throw;
        }
    }

    /// <summary>
    /// 按考试类型获取学生可访问的考试总数
    /// </summary>
    public async Task<int> GetAvailableExamCountByCategoryAsync(int studentUserId, ExamCategory examCategory)
    {
        try
        {
            int count;

            if (examCategory == ExamCategory.School)
            {
                // 学校统考：只计算学生所属学校有权限的考试
                List<int> accessibleExamIds = await _schoolPermissionService.GetAccessibleSchoolExamIdsAsync(studentUserId);

                count = await _context.ImportedExams
                    .CountAsync(e => e.IsEnabled &&
                                    e.ExamCategory == examCategory &&
                                    accessibleExamIds.Contains(e.Id));

                _logger.LogInformation("获取学校统考总数，学生ID: {StudentUserId}, 可访问考试总数: {Count}",
                    studentUserId, count);
            }
            else if (examCategory == ExamCategory.Provincial)
            {
                // 全省统考：所有启用的全省统考都对学生可见
                count = await _context.ImportedExams
                    .CountAsync(e => e.IsEnabled && e.ExamCategory == examCategory);

                _logger.LogInformation("获取全省统考总数，学生ID: {StudentUserId}, 总数: {Count}",
                    studentUserId, count);
            }
            else
            {
                _logger.LogWarning("未知的考试类型，学生ID: {StudentUserId}, 考试类型: {ExamCategory}", studentUserId, examCategory);
                return 0;
            }

            _logger.LogInformation("按类型获取学生可访问考试总数成功，学生ID: {StudentUserId}, 考试类型: {ExamCategory}, 总数: {Count}",
                studentUserId, examCategory, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "按类型获取学生可访问考试总数失败，学生ID: {StudentUserId}, 考试类型: {ExamCategory}", studentUserId, examCategory);
            throw;
        }
    }

    /// <summary>
    /// 映射到学生端考试DTO（不包含详细信息）
    /// </summary>
    private static StudentExamDto MapToStudentExamDto(ImportedExamEntity exam)
    {
        // 添加调试输出
        System.Diagnostics.Debug.WriteLine($"=== MapToStudentExamDto 调试 ===");
        System.Diagnostics.Debug.WriteLine($"考试ID: {exam.Id}, 考试名称: {exam.Name}");
        System.Diagnostics.Debug.WriteLine($"数据库中的AllowRetake: {exam.AllowRetake}");
        System.Diagnostics.Debug.WriteLine($"数据库中的AllowPractice: {exam.AllowPractice}");
        System.Diagnostics.Debug.WriteLine($"数据库中的MaxRetakeCount: {exam.MaxRetakeCount}");
        System.Diagnostics.Debug.WriteLine($"=== MapToStudentExamDto 调试结束 ===");

        return new StudentExamDto
        {
            Id = exam.Id,
            Name = exam.Name,
            Description = exam.Description,
            ExamType = exam.ExamType,
            Status = exam.Status,
            TotalScore = (int)exam.TotalScore,
            DurationMinutes = exam.DurationMinutes,
            StartTime = exam.StartTime,
            EndTime = exam.EndTime,
            // 使用数据库中的实际配置
            AllowRetake = exam.AllowRetake,
            AllowPractice = exam.AllowPractice,
            MaxRetakeCount = exam.MaxRetakeCount,
            PassingScore = (int)exam.PassingScore,
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
    private static StudentExamDto MapToStudentExamDtoWithDetails(ImportedExamEntity exam)
    {
        StudentExamDto dto = MapToStudentExamDto(exam);

        // 映射科目
        dto.Subjects = [.. exam.Subjects.Select(subject => new StudentSubjectDto
        {
            Id = subject.Id,
            SubjectType = subject.SubjectType,
            SubjectName = subject.SubjectName,
            Description = subject.Description,
            Score = (int)subject.Score,
            DurationMinutes = subject.DurationMinutes,
            SortOrder = subject.SortOrder,
            IsRequired = subject.IsRequired,
            MinScore = subject.MinScore.HasValue ? (int)subject.MinScore.Value : 0,
            Weight = subject.Weight,
            QuestionCount = subject.QuestionCount,
            Questions = [.. subject.Questions.Select(MapToStudentQuestionDto)]
        })];

        // 映射模块
        dto.Modules = [.. exam.Modules.Select(module => new StudentModuleDto
        {
            Id = module.Id,
            Name = module.Name,
            Type = module.Type,
            Description = module.Description,
            Score = module.Score,
            Order = module.Order,
            Questions = [.. module.Questions.Select(MapToStudentQuestionDto)]
        })];

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
            Score = (int)question.Score,
            EstimatedMinutes = question.EstimatedMinutes,
            SortOrder = question.SortOrder,
            IsRequired = question.IsRequired,
            QuestionConfig = question.QuestionConfig,
            AnswerValidationRules = question.AnswerValidationRules,
            Tags = question.Tags,
            Remarks = question.Remarks,
            ProgramInput = question.ProgramInput,
            ExpectedOutput = question.ExpectedOutput,
            OperationPoints = [.. question.OperationPoints.Select(op => new StudentOperationPointDto
            {
                Id = op.Id,
                Name = op.Name,
                Description = op.Description,
                ModuleType = op.ModuleType,
                Score = (int)op.Score,
                Order = op.Order,
                Parameters = [.. op.Parameters.Select(param => new StudentParameterDto
                {
                    Id = param.Id,
                    Name = param.Name,
                    Description = param.Description,
                    ParameterType = param.Type,
                    DefaultValue = param.DefaultValue,
                    MinValue = param.MinValue?.ToString(),
                    MaxValue = param.MaxValue?.ToString()
                })]
            })]
        };
    }

    /// <summary>
    /// 开始正式考试
    /// </summary>
    public async Task<bool> StartExamAsync(int examId, int studentUserId)
    {
        try
        {
            _logger.LogInformation("开始正式考试，考试ID: {ExamId}, 学生ID: {StudentId}", examId, studentUserId);

            // 检查权限
            if (!await HasAccessToExamAsync(examId, studentUserId))
            {
                _logger.LogWarning("学生无权限访问考试，学生ID: {StudentId}, 考试ID: {ExamId}", studentUserId, examId);
                return false;
            }

            // 检查考试是否存在
            Models.ImportedExam.ImportedExam? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId && e.IsEnabled);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在或未启用，考试ID: {ExamId}", examId);
                return false;
            }

            DateTime now = DateTime.Now;

            // 检查是否已有完成记录
            ExamCompletion? existingCompletion = await _context.ExamCompletions
                .FirstOrDefaultAsync(ec => ec.ExamId == examId && ec.StudentUserId == studentUserId);

            if (existingCompletion != null)
            {
                // 如果已有记录且状态为未开始，更新为进行中
                if (existingCompletion.Status == ExamCompletionStatus.NotStarted)
                {
                    existingCompletion.Status = ExamCompletionStatus.InProgress;
                    existingCompletion.StartedAt = now;
                    existingCompletion.UpdatedAt = now;
                }
                else
                {
                    _logger.LogWarning("考试已开始或完成，无法重新开始，学生ID: {StudentId}, 考试ID: {ExamId}, 当前状态: {Status}",
                        studentUserId, examId, existingCompletion.Status);
                    return false;
                }
            }
            else
            {
                // 创建新的开始记录
                ExamCompletion newCompletion = new()
                {
                    StudentUserId = studentUserId,
                    ExamId = examId,
                    Status = ExamCompletionStatus.InProgress,
                    StartedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                _context.ExamCompletions.Add(newCompletion);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("正式考试开始成功，学生ID: {StudentId}, 考试ID: {ExamId}", studentUserId, examId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始正式考试失败，考试ID: {ExamId}, 学生ID: {StudentId}", examId, studentUserId);
            return false;
        }
    }

    /// <summary>
    /// 提交正式考试成绩
    /// </summary>
    public async Task<bool> SubmitExamScoreAsync(int examId, int studentUserId, SubmitExamScoreRequestDto scoreRequest)
    {
        try
        {
            _logger.LogInformation("开始提交正式考试成绩，考试ID: {ExamId}, 学生ID: {StudentId}", examId, studentUserId);

            // 检查权限
            if (!await HasAccessToExamAsync(examId, studentUserId))
            {
                _logger.LogWarning("学生无权限访问考试，学生ID: {StudentId}, 考试ID: {ExamId}", studentUserId, examId);
                return false;
            }

            // 检查考试是否存在
            Models.ImportedExam.ImportedExam? exam = await _context.ImportedExams
                .FirstOrDefaultAsync(e => e.Id == examId && e.IsEnabled);

            if (exam == null)
            {
                _logger.LogWarning("考试不存在或未启用，考试ID: {ExamId}", examId);
                return false;
            }

            DateTime now = DateTime.Now;
            DateTime completedAt = scoreRequest.CompletedAt ?? now;

            // 查找现有的完成记录
            ExamCompletion? existingCompletion = await _context.ExamCompletions
                .FirstOrDefaultAsync(ec => ec.ExamId == examId && ec.StudentUserId == studentUserId);

            if (existingCompletion != null)
            {
                // 更新现有记录
                existingCompletion.Status = ExamCompletionStatus.Completed;
                existingCompletion.CompletedAt = completedAt;
                existingCompletion.Score = scoreRequest.Score;
                existingCompletion.MaxScore = scoreRequest.MaxScore;
                existingCompletion.DurationSeconds = scoreRequest.DurationSeconds;
                existingCompletion.Notes = scoreRequest.Notes;
                existingCompletion.BenchSuiteScoringResult = scoreRequest.BenchSuiteScoringResult;
                existingCompletion.UpdatedAt = now;

                // 计算完成百分比
                if (scoreRequest.Score.HasValue && scoreRequest.MaxScore.HasValue && scoreRequest.MaxScore.Value > 0)
                {
                    existingCompletion.CompletionPercentage = Math.Round((scoreRequest.Score.Value / scoreRequest.MaxScore.Value) * 100, 2);
                }

                _logger.LogInformation("更新正式考试完成记录，学生ID: {StudentId}, 考试ID: {ExamId}", studentUserId, examId);
            }
            else
            {
                // 创建新的完成记录
                ExamCompletion newCompletion = new()
                {
                    StudentUserId = studentUserId,
                    ExamId = examId,
                    Status = ExamCompletionStatus.Completed,
                    StartedAt = completedAt, // 如果没有开始记录，假设开始时间就是完成时间
                    CompletedAt = completedAt,
                    Score = scoreRequest.Score,
                    MaxScore = scoreRequest.MaxScore,
                    DurationSeconds = scoreRequest.DurationSeconds,
                    Notes = scoreRequest.Notes,
                    BenchSuiteScoringResult = scoreRequest.BenchSuiteScoringResult,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                // 计算完成百分比
                if (scoreRequest.Score.HasValue && scoreRequest.MaxScore.HasValue && scoreRequest.MaxScore.Value > 0)
                {
                    newCompletion.CompletionPercentage = Math.Round((scoreRequest.Score.Value / scoreRequest.MaxScore.Value) * 100, 2);
                }

                _context.ExamCompletions.Add(newCompletion);
                _logger.LogInformation("创建新的正式考试完成记录，学生ID: {StudentId}, 考试ID: {ExamId}", studentUserId, examId);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("正式考试成绩提交成功，学生ID: {StudentId}, 考试ID: {ExamId}, 得分: {Score}/{MaxScore}",
                studentUserId, examId, scoreRequest.Score, scoreRequest.MaxScore);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交正式考试成绩异常，考试ID: {ExamId}, 学生ID: {StudentId}", examId, studentUserId);
            return false;
        }
    }

    /// <summary>
    /// 标记正式考试为已完成（不包含成绩）
    /// </summary>
    public async Task<bool> CompleteExamAsync(int examId, int studentUserId)
    {
        try
        {
            _logger.LogInformation("标记正式考试为已完成，考试ID: {ExamId}, 学生ID: {StudentId}", examId, studentUserId);

            // 检查权限
            if (!await HasAccessToExamAsync(examId, studentUserId))
            {
                _logger.LogWarning("学生无权限访问考试，学生ID: {StudentId}, 考试ID: {ExamId}", studentUserId, examId);
                return false;
            }

            DateTime now = DateTime.Now;

            // 查找现有的完成记录
            ExamCompletion? existingCompletion = await _context.ExamCompletions
                .FirstOrDefaultAsync(ec => ec.ExamId == examId && ec.StudentUserId == studentUserId);

            if (existingCompletion != null)
            {
                // 更新现有记录状态
                existingCompletion.Status = ExamCompletionStatus.Completed;
                existingCompletion.CompletedAt = now;
                existingCompletion.UpdatedAt = now;

                // 如果有开始时间，计算用时
                if (existingCompletion.StartedAt.HasValue)
                {
                    TimeSpan duration = now - existingCompletion.StartedAt.Value;
                    existingCompletion.DurationSeconds = (int)Math.Ceiling(duration.TotalSeconds);
                }

                _logger.LogInformation("更新正式考试完成状态，学生ID: {StudentId}, 考试ID: {ExamId}", studentUserId, examId);
            }
            else
            {
                // 创建新的完成记录
                ExamCompletion newCompletion = new()
                {
                    StudentUserId = studentUserId,
                    ExamId = examId,
                    Status = ExamCompletionStatus.Completed,
                    StartedAt = now, // 假设开始时间就是完成时间
                    CompletedAt = now,
                    DurationSeconds = 0, // 没有开始记录，用时为0
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                _context.ExamCompletions.Add(newCompletion);
                _logger.LogInformation("创建新的正式考试完成记录，学生ID: {StudentId}, 考试ID: {ExamId}", studentUserId, examId);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("正式考试完成标记成功，学生ID: {StudentId}, 考试ID: {ExamId}", studentUserId, examId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记正式考试完成失败，考试ID: {ExamId}, 学生ID: {StudentId}", examId, studentUserId);
            return false;
        }
    }

    /// <summary>
    /// 获取学生的考试完成记录
    /// </summary>
    public async Task<List<ExamCompletion>> GetExamCompletionsAsync(int studentUserId, int? examId = null)
    {
        try
        {
            IQueryable<ExamCompletion> query = _context.ExamCompletions
                .Include(ec => ec.Student)
                .Include(ec => ec.Exam)
                .Where(ec => ec.StudentUserId == studentUserId && ec.IsActive);

            if (examId.HasValue)
            {
                query = query.Where(ec => ec.ExamId == examId.Value);
            }

            List<ExamCompletion> completions = await query
                .OrderByDescending(ec => ec.CompletedAt)
                .ToListAsync();

            _logger.LogInformation("获取学生考试完成记录成功，学生ID: {StudentId}, 记录数: {Count}", studentUserId, completions.Count);
            return completions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生考试完成记录失败，学生ID: {StudentId}", studentUserId);
            return [];
        }
    }
}
