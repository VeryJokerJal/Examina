using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Models.Dto;
using ExaminaWebApplication.Models.ImportedComprehensiveTraining;
using Microsoft.EntityFrameworkCore;
using ImportedComprehensiveTrainingEntity = ExaminaWebApplication.Models.ImportedComprehensiveTraining.ImportedComprehensiveTraining;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生端综合训练服务实现
/// </summary>
public class StudentComprehensiveTrainingService : IStudentComprehensiveTrainingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StudentComprehensiveTrainingService> _logger;

    public StudentComprehensiveTrainingService(ApplicationDbContext context, ILogger<StudentComprehensiveTrainingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取学生可访问的综合训练列表（随机排序）
    /// </summary>
    public async Task<List<StudentComprehensiveTrainingDto>> GetAvailableTrainingsAsync(int studentUserId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            // 目前简化权限验证：所有启用的综合训练都对学生可见
            // 后续可以根据组织关系、权限设置等进行更细粒度的权限控制

            // 首先获取总数，用于性能优化决策
            int totalCount = await _context.ImportedComprehensiveTrainings
                .Where(t => t.IsEnabled)
                .CountAsync();

            List<ImportedComprehensiveTrainingEntity> trainings;

            // 性能优化：如果总数较少（小于1000），使用内存随机排序
            // 如果总数较多，使用数据库层面的随机排序
            if (totalCount <= 1000)
            {
                // 小数据量：在内存中进行真正的随机排序
                List<ImportedComprehensiveTrainingEntity> allTrainings = await _context.ImportedComprehensiveTrainings
                    .Where(t => t.IsEnabled)
                    .ToListAsync();

                // 使用随机数生成器进行随机排序
                Random random = new();
                trainings = allTrainings
                    .OrderBy(x => random.Next())
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("使用内存随机排序获取综合训练列表，学生ID: {StudentUserId}, 总数: {TotalCount}, 返回数量: {Count}, 页码: {PageNumber}",
                    studentUserId, totalCount, trainings.Count, pageNumber);
            }
            else
            {
                // 大数据量：使用数据库层面的随机排序（性能更好但随机性稍弱）
                trainings = await _context.ImportedComprehensiveTrainings
                    .Where(t => t.IsEnabled)
                    .OrderBy(x => Guid.NewGuid()) // 使用GUID进行随机排序
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("使用数据库随机排序获取综合训练列表，学生ID: {StudentUserId}, 总数: {TotalCount}, 返回数量: {Count}, 页码: {PageNumber}",
                    studentUserId, totalCount, trainings.Count, pageNumber);
            }

            List<StudentComprehensiveTrainingDto> result = trainings.Select(MapToStudentComprehensiveTrainingDto).ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问综合训练列表失败，学生ID: {StudentUserId}", studentUserId);
            throw;
        }
    }

    /// <summary>
    /// 获取综合训练详情
    /// </summary>
    public async Task<StudentComprehensiveTrainingDto?> GetTrainingDetailsAsync(int trainingId, int studentUserId)
    {
        try
        {
            // 检查权限
            if (!await HasAccessToTrainingAsync(trainingId, studentUserId))
            {
                _logger.LogWarning("学生无权限访问综合训练，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                    studentUserId, trainingId);
                return null;
            }

            ImportedComprehensiveTrainingEntity? training = await _context.ImportedComprehensiveTrainings
                .Include(t => t.Subjects)
                    .ThenInclude(s => s.Questions)
                        .ThenInclude(q => q.OperationPoints)
                            .ThenInclude(op => op.Parameters)
                .Include(t => t.Modules)
                    .ThenInclude(m => m.Questions)
                        .ThenInclude(q => q.OperationPoints)
                            .ThenInclude(op => op.Parameters)
                .FirstOrDefaultAsync(t => t.Id == trainingId && t.IsEnabled);

            if (training == null)
            {
                _logger.LogWarning("综合训练不存在或已禁用，训练ID: {TrainingId}", trainingId);
                return null;
            }

            StudentComprehensiveTrainingDto result = MapToStudentComprehensiveTrainingDtoWithDetails(training);

            _logger.LogInformation("获取综合训练详情成功，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                studentUserId, trainingId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取综合训练详情失败，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                studentUserId, trainingId);
            throw;
        }
    }

    /// <summary>
    /// 检查学生是否有权限访问指定综合训练
    /// </summary>
    public async Task<bool> HasAccessToTrainingAsync(int trainingId, int studentUserId)
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

            // 验证综合训练存在且启用
            bool trainingExists = await _context.ImportedComprehensiveTrainings
                .AnyAsync(t => t.Id == trainingId && t.IsEnabled);

            if (!trainingExists)
            {
                _logger.LogWarning("综合训练不存在或已禁用，训练ID: {TrainingId}", trainingId);
                return false;
            }

            // 目前简化权限验证：所有启用的综合训练都对学生可见
            // 后续可以根据组织关系、权限设置等进行更细粒度的权限控制
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查综合训练访问权限失败，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                studentUserId, trainingId);
            return false;
        }
    }

    /// <summary>
    /// 获取学生可访问的综合训练总数
    /// </summary>
    public async Task<int> GetAvailableTrainingCountAsync(int studentUserId)
    {
        try
        {
            // 目前简化权限验证：所有启用的综合训练都对学生可见
            int count = await _context.ImportedComprehensiveTrainings
                .CountAsync(t => t.IsEnabled);

            _logger.LogInformation("获取学生可访问综合训练总数成功，学生ID: {StudentUserId}, 总数: {Count}",
                studentUserId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问综合训练总数失败，学生ID: {StudentUserId}", studentUserId);
            throw;
        }
    }

    /// <summary>
    /// 获取学生综合训练进度统计
    /// </summary>
    public async Task<ComprehensiveTrainingProgressDto> GetTrainingProgressAsync(int studentUserId)
    {
        try
        {
            // 验证学生用户存在且为学生角色
            Models.User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == Models.UserRole.Student && u.IsActive);

            if (student == null)
            {
                _logger.LogWarning("用户不存在或不是活跃学生，用户ID: {UserId}", studentUserId);
                return new ComprehensiveTrainingProgressDto
                {
                    TotalCount = 0,
                    CompletedCount = 0,
                    CompletionPercentage = 0,
                    InProgressCount = 0,
                    NotStartedCount = 0
                };
            }

            // 获取所有启用的综合训练总数
            int totalCount = await _context.ImportedComprehensiveTrainings
                .CountAsync(t => t.IsEnabled);

            // 获取学生的训练完成记录
            List<ComprehensiveTrainingCompletion> completionRecords = await _context.ComprehensiveTrainingCompletions
                .Where(c => c.StudentUserId == studentUserId && c.IsActive)
                .ToListAsync();

            // 统计各种状态的训练数量
            int completedCount = completionRecords.Count(c => c.Status == ComprehensiveTrainingCompletionStatus.Completed);
            int inProgressCount = completionRecords.Count(c => c.Status == ComprehensiveTrainingCompletionStatus.InProgress);
            int notStartedCount = totalCount - completedCount - inProgressCount;

            double completionPercentage = totalCount > 0 ? (double)completedCount / totalCount * 100 : 0;

            // 获取最近完成的训练信息
            ComprehensiveTrainingCompletion? lastCompletedRecord = completionRecords
                .Where(c => c.Status == ComprehensiveTrainingCompletionStatus.Completed && c.CompletedAt.HasValue)
                .OrderByDescending(c => c.CompletedAt)
                .FirstOrDefault();

            string? lastCompletedTrainingName = null;
            DateTime? lastCompletedAt = null;

            if (lastCompletedRecord != null)
            {
                // 获取训练名称
                ImportedComprehensiveTrainingEntity? training = await _context.ImportedComprehensiveTrainings
                    .FirstOrDefaultAsync(t => t.Id == lastCompletedRecord.TrainingId);
                lastCompletedTrainingName = training?.Name;
                lastCompletedAt = lastCompletedRecord.CompletedAt;
            }

            ComprehensiveTrainingProgressDto progress = new()
            {
                TotalCount = totalCount,
                CompletedCount = completedCount,
                CompletionPercentage = Math.Round(completionPercentage, 1),
                InProgressCount = inProgressCount,
                NotStartedCount = notStartedCount,
                LastCompletedTrainingName = lastCompletedTrainingName,
                LastCompletedAt = lastCompletedAt
            };

            _logger.LogInformation("获取学生综合训练进度统计成功，学生ID: {StudentUserId}, 总数: {TotalCount}, 完成数: {CompletedCount}",
                studentUserId, progress.TotalCount, progress.CompletedCount);

            return progress;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生综合训练进度统计失败，学生ID: {StudentUserId}", studentUserId);
            throw;
        }
    }

    /// <summary>
    /// 映射到学生端综合训练DTO（不包含详细信息）
    /// </summary>
    private static StudentComprehensiveTrainingDto MapToStudentComprehensiveTrainingDto(ImportedComprehensiveTrainingEntity training)
    {
        return new StudentComprehensiveTrainingDto
        {
            Id = training.Id,
            Name = training.Name,
            Description = training.Description,
            Status = training.Status,
            TotalScore = (int)training.TotalScore,
            DurationMinutes = training.DurationMinutes,
            StartTime = training.StartTime,
            EndTime = training.EndTime,
            AllowRetake = training.AllowRetake,
            MaxRetakeCount = training.MaxRetakeCount,
            PassingScore = (int)training.PassingScore,
            RandomizeQuestions = training.RandomizeQuestions,
            ShowScore = training.ShowScore,
            ShowAnswers = training.ShowAnswers,
            Tags = training.Tags,
            Subjects = [],
            Modules = []
        };
    }

    /// <summary>
    /// 映射到学生端综合训练DTO（包含完整详细信息）
    /// </summary>
    private static StudentComprehensiveTrainingDto MapToStudentComprehensiveTrainingDtoWithDetails(ImportedComprehensiveTrainingEntity training)
    {
        StudentComprehensiveTrainingDto dto = MapToStudentComprehensiveTrainingDto(training);

        // 映射科目
        dto.Subjects = training.Subjects.Select(subject => new StudentComprehensiveTrainingSubjectDto
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
            Questions = subject.Questions.Select(MapToStudentComprehensiveTrainingQuestionDto).ToList()
        }).ToList();

        // 映射模块
        dto.Modules = training.Modules.Select(module => new StudentComprehensiveTrainingModuleDto
        {
            Id = module.Id,
            Name = module.Name,
            Type = module.Type,
            Description = module.Description,
            Score = module.Score,
            Order = module.Order,
            Questions = module.Questions.Select(MapToStudentComprehensiveTrainingQuestionDto).ToList()
        }).ToList();

        return dto;
    }

    /// <summary>
    /// 映射到学生端综合训练题目DTO
    /// </summary>
    private static StudentComprehensiveTrainingQuestionDto MapToStudentComprehensiveTrainingQuestionDto(ImportedComprehensiveTrainingQuestion question)
    {
        return new StudentComprehensiveTrainingQuestionDto
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
            OperationPoints = question.OperationPoints.Select(op => new StudentComprehensiveTrainingOperationPointDto
            {
                Id = op.Id,
                Name = op.Name,
                Description = op.Description,
                ModuleType = op.ModuleType,
                Score = (int)op.Score,
                Order = op.Order,
                Parameters = op.Parameters.Select(param => new StudentComprehensiveTrainingParameterDto
                {
                    Id = param.Id,
                    Name = param.Name,
                    Description = param.Description,
                    ParameterType = param.Type,
                    DefaultValue = param.DefaultValue,
                    MinValue = param.MinValue?.ToString(),
                    MaxValue = param.MaxValue?.ToString()
                }).ToList()
            }).ToList()
        };
    }

    /// <summary>
    /// 标记综合训练为已完成
    /// </summary>
    public async Task<bool> MarkTrainingAsCompletedAsync(int studentUserId, int trainingId, decimal? score = null, decimal? maxScore = null, int? durationSeconds = null, string? notes = null)
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

            // 验证训练存在且启用
            ImportedComprehensiveTrainingEntity? training = await _context.ImportedComprehensiveTrainings
                .FirstOrDefaultAsync(t => t.Id == trainingId && t.IsEnabled);

            if (training == null)
            {
                _logger.LogWarning("综合训练不存在或未启用，训练ID: {TrainingId}", trainingId);
                return false;
            }

            // 查找现有的完成记录
            ComprehensiveTrainingCompletion? existingRecord = await _context.ComprehensiveTrainingCompletions
                .FirstOrDefaultAsync(c => c.StudentUserId == studentUserId && c.TrainingId == trainingId && c.IsActive);

            DateTime now = DateTime.UtcNow;

            if (existingRecord != null)
            {
                // 更新现有记录
                existingRecord.Status = ComprehensiveTrainingCompletionStatus.Completed;
                existingRecord.CompletedAt = now;
                existingRecord.Score = score;
                existingRecord.MaxScore = maxScore;
                existingRecord.DurationSeconds = durationSeconds;
                existingRecord.Notes = notes;
                existingRecord.UpdatedAt = now;

                // 计算完成百分比
                if (score.HasValue && maxScore.HasValue && maxScore.Value > 0)
                {
                    existingRecord.CompletionPercentage = Math.Round(score.Value / maxScore.Value * 100, 2);
                }

                _logger.LogInformation("更新综合训练完成记录，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
            }
            else
            {
                // 创建新的完成记录
                ComprehensiveTrainingCompletion newRecord = new()
                {
                    StudentUserId = studentUserId,
                    TrainingId = trainingId,
                    Status = ComprehensiveTrainingCompletionStatus.Completed,
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
                    newRecord.CompletionPercentage = Math.Round(score.Value / maxScore.Value * 100, 2);
                }

                _ = _context.ComprehensiveTrainingCompletions.Add(newRecord);
                _logger.LogInformation("创建新的综合训练完成记录，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
            }

            _ = await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记综合训练为已完成失败，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
            return false;
        }
    }

    /// <summary>
    /// 标记综合训练为开始状态
    /// </summary>
    public async Task<bool> MarkTrainingAsStartedAsync(int studentUserId, int trainingId)
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

            // 验证训练存在且启用
            ImportedComprehensiveTrainingEntity? training = await _context.ImportedComprehensiveTrainings
                .FirstOrDefaultAsync(t => t.Id == trainingId && t.IsEnabled);

            if (training == null)
            {
                _logger.LogWarning("综合训练不存在或未启用，训练ID: {TrainingId}", trainingId);
                return false;
            }

            // 查找现有的完成记录
            ComprehensiveTrainingCompletion? existingRecord = await _context.ComprehensiveTrainingCompletions
                .FirstOrDefaultAsync(c => c.StudentUserId == studentUserId && c.TrainingId == trainingId && c.IsActive);

            DateTime now = DateTime.UtcNow;

            if (existingRecord != null)
            {
                // 如果已经是完成状态，不允许重新标记为开始
                if (existingRecord.Status == ComprehensiveTrainingCompletionStatus.Completed)
                {
                    _logger.LogInformation("综合训练已完成，不允许重新标记为开始，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
                    return false;
                }

                // 更新为进行中状态
                existingRecord.Status = ComprehensiveTrainingCompletionStatus.InProgress;
                existingRecord.StartedAt ??= now; // 如果没有开始时间则设置
                existingRecord.UpdatedAt = now;

                _logger.LogInformation("更新综合训练为进行中状态，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
            }
            else
            {
                // 创建新的开始记录
                ComprehensiveTrainingCompletion newRecord = new()
                {
                    StudentUserId = studentUserId,
                    TrainingId = trainingId,
                    Status = ComprehensiveTrainingCompletionStatus.InProgress,
                    StartedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                _ = _context.ComprehensiveTrainingCompletions.Add(newRecord);
                _logger.LogInformation("创建新的综合训练开始记录，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
            }

            _ = await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记综合训练为开始状态失败，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
            return false;
        }
    }

    /// <summary>
    /// 获取学生综合训练完成记录
    /// </summary>
    public async Task<List<ComprehensiveTrainingCompletionDto>> GetTrainingCompletionsAsync(int studentUserId, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            // 由于ComprehensiveTrainingCompletion有Training导航属性，我们可以直接使用
            List<ComprehensiveTrainingCompletionDto> completions = await _context.ComprehensiveTrainingCompletions
                .Where(c => c.StudentUserId == studentUserId && c.IsActive)
                .Include(c => c.Training)
                .OrderByDescending(c => c.UpdatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ComprehensiveTrainingCompletionDto
                {
                    Id = c.Id,
                    TrainingId = c.TrainingId,
                    TrainingName = c.Training!.Name, // 使用Name而不是Title
                    TrainingDescription = c.Training.Description,
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
                })
                .ToListAsync();

            _logger.LogInformation("获取学生综合训练完成记录成功，学生ID: {StudentUserId}, 页码: {PageNumber}, 页大小: {PageSize}, 记录数: {Count}",
                studentUserId, pageNumber, pageSize, completions.Count);

            return completions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生综合训练完成记录失败，学生ID: {StudentUserId}", studentUserId);
            return new List<ComprehensiveTrainingCompletionDto>();
        }
    }
}
