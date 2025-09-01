using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Models.ImportedSpecializedTraining;
using Microsoft.EntityFrameworkCore;
using ImportedSpecializedTrainingEntity = ExaminaWebApplication.Models.ImportedSpecializedTraining.ImportedSpecializedTraining;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生端专项训练服务实现
/// </summary>
public class StudentSpecializedTrainingService : IStudentSpecializedTrainingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StudentSpecializedTrainingService> _logger;

    public StudentSpecializedTrainingService(
        ApplicationDbContext context,
        ILogger<StudentSpecializedTrainingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 获取学生可访问的专项训练列表（随机排序）
    /// </summary>
    public async Task<List<StudentSpecializedTrainingDto>> GetAvailableTrainingsAsync(int studentUserId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            // 只显示启用且开放试用功能的专项训练
            // 后续可以根据组织关系、权限设置等进行更细粒度的权限控制

            // 首先获取总数，用于性能优化决策
            int totalCount = await _context.ImportedSpecializedTrainings
                .Where(t => t.IsEnabled && t.EnableTrial)
                .CountAsync();

            List<ImportedSpecializedTrainingEntity> trainings;

            // 性能优化：如果总数较少（小于1000），使用内存随机排序
            // 如果总数较多，使用数据库层面的随机排序
            if (totalCount <= 1000)
            {
                // 小数据量：在内存中进行真正的随机排序
                List<ImportedSpecializedTrainingEntity> allTrainings = await _context.ImportedSpecializedTrainings
                    .Where(t => t.IsEnabled && t.EnableTrial)
                    .Include(t => t.Modules)
                    .Include(t => t.Questions)
                    .ToListAsync();

                // 使用随机数生成器进行随机排序
                Random random = new();
                trainings = [.. allTrainings
                    .OrderBy(x => random.Next())
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)];

                _logger.LogInformation("使用内存随机排序获取专项训练列表，学生ID: {StudentUserId}, 总数: {TotalCount}, 返回数量: {Count}, 页码: {PageNumber}",
                    studentUserId, totalCount, trainings.Count, pageNumber);
            }
            else
            {
                // 大数据量：使用数据库层面的随机排序（性能更好但随机性稍弱）
                trainings = await _context.ImportedSpecializedTrainings
                    .Where(t => t.IsEnabled && t.EnableTrial)
                    .Include(t => t.Modules)
                    .Include(t => t.Questions)
                    .OrderBy(x => Guid.NewGuid()) // 使用GUID进行随机排序
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("使用数据库随机排序获取专项训练列表，学生ID: {StudentUserId}, 总数: {TotalCount}, 返回数量: {Count}, 页码: {PageNumber}",
                    studentUserId, totalCount, trainings.Count, pageNumber);
            }

            List<StudentSpecializedTrainingDto> result = [.. trainings.Select(MapToStudentSpecializedTrainingDto)];

            // 调试信息：输出每个训练的EnableTrial状态
            foreach (var training in trainings)
            {
                System.Diagnostics.Debug.WriteLine($"[SpecializedTrainingService] 数据库查询结果: {training.Name}, EnableTrial: {training.EnableTrial}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问专项训练列表失败，学生ID: {StudentUserId}", studentUserId);
            throw;
        }
    }

    /// <summary>
    /// 获取专项训练详情
    /// </summary>
    public async Task<StudentSpecializedTrainingDto?> GetTrainingDetailsAsync(int trainingId, int studentUserId)
    {
        try
        {
            // 检查权限
            if (!await HasAccessToTrainingAsync(trainingId, studentUserId))
            {
                _logger.LogWarning("学生无权限访问专项训练，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                    studentUserId, trainingId);
                return null;
            }

            ImportedSpecializedTrainingEntity? training = await _context.ImportedSpecializedTrainings
                .Include(t => t.Modules)
                    .ThenInclude(m => m.Questions)
                        .ThenInclude(q => q.OperationPoints)
                            .ThenInclude(op => op.Parameters)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.OperationPoints)
                        .ThenInclude(op => op.Parameters)
                .FirstOrDefaultAsync(t => t.Id == trainingId && t.IsEnabled);

            if (training == null)
            {
                _logger.LogWarning("专项训练不存在或已禁用，训练ID: {TrainingId}", trainingId);
                return null;
            }

            _logger.LogInformation("从数据库获取训练数据成功，训练ID: {TrainingId}, 模块数量: {ModuleCount}, 题目数量: {QuestionCount}",
                trainingId, training.Modules?.Count ?? 0, training.Questions?.Count ?? 0);

            StudentSpecializedTrainingDto result = MapToStudentSpecializedTrainingDtoWithDetails(training);

            _logger.LogInformation("获取专项训练详情成功，学生ID: {StudentUserId}, 训练ID: {TrainingId}, 模块数量: {ModuleCount}, 题目数量: {QuestionCount}",
                studentUserId, trainingId, result.Modules.Count, result.Questions.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取专项训练详情失败，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                studentUserId, trainingId);
            throw;
        }
    }

    /// <summary>
    /// 检查学生是否有权限访问指定专项训练
    /// </summary>
    public async Task<bool> HasAccessToTrainingAsync(int trainingId, int studentUserId)
    {
        try
        {
            // 验证学生用户存在且为学生角色
            User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == Models.UserRole.Student && u.IsActive);

            if (student == null)
            {
                _logger.LogWarning("用户不存在或不是活跃学生，用户ID: {UserId}", studentUserId);
                return false;
            }

            // 验证训练存在且启用
            ImportedSpecializedTrainingEntity? training = await _context.ImportedSpecializedTrainings
                .FirstOrDefaultAsync(t => t.Id == trainingId && t.IsEnabled);

            if (training == null)
            {
                _logger.LogWarning("专项训练不存在或已禁用，训练ID: {TrainingId}", trainingId);
                return false;
            }

            // 检查试用功能设置
            if (!training.EnableTrial)
            {
                _logger.LogWarning("专项训练试用功能已禁用，学生无法访问，训练ID: {TrainingId}, 学生ID: {StudentUserId}",
                    trainingId, studentUserId);
                return false;
            }

            // 目前简化权限验证：所有启用试用功能的专项训练都对学生可见
            // 后续可以根据组织关系、权限设置等进行更细粒度的权限控制
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查专项训练访问权限失败，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                studentUserId, trainingId);
            return false;
        }
    }

    /// <summary>
    /// 获取学生可访问的专项训练总数
    /// </summary>
    public async Task<int> GetAvailableTrainingCountAsync(int studentUserId)
    {
        try
        {
            // 只统计启用且开放试用功能的专项训练
            int count = await _context.ImportedSpecializedTrainings
                .CountAsync(t => t.IsEnabled && t.EnableTrial);

            _logger.LogInformation("获取学生可访问专项训练总数成功，学生ID: {StudentUserId}, 总数: {Count}",
                studentUserId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生可访问专项训练总数失败，学生ID: {StudentUserId}", studentUserId);
            throw;
        }
    }

    /// <summary>
    /// 根据模块类型获取专项训练列表
    /// </summary>
    public async Task<List<StudentSpecializedTrainingDto>> GetTrainingsByModuleTypeAsync(int studentUserId, string moduleType, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            List<ImportedSpecializedTrainingEntity> trainings = await _context.ImportedSpecializedTrainings
                .Where(t => t.IsEnabled && t.ModuleType == moduleType)
                .OrderByDescending(t => t.ImportedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(t => t.Modules)
                .Include(t => t.Questions)
                .ToListAsync();

            List<StudentSpecializedTrainingDto> result = [.. trainings.Select(MapToStudentSpecializedTrainingDto)];

            _logger.LogInformation("根据模块类型获取专项训练列表成功，学生ID: {StudentUserId}, 模块类型: {ModuleType}, 返回数量: {Count}",
                studentUserId, moduleType, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据模块类型获取专项训练列表失败，学生ID: {StudentUserId}, 模块类型: {ModuleType}",
                studentUserId, moduleType);
            throw;
        }
    }

    /// <summary>
    /// 根据难度等级获取专项训练列表
    /// </summary>
    public async Task<List<StudentSpecializedTrainingDto>> GetTrainingsByDifficultyAsync(int studentUserId, int difficultyLevel, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            List<ImportedSpecializedTrainingEntity> trainings = await _context.ImportedSpecializedTrainings
                .Where(t => t.IsEnabled && t.DifficultyLevel == difficultyLevel)
                .OrderByDescending(t => t.ImportedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(t => t.Modules)
                .Include(t => t.Questions)
                .ToListAsync();

            List<StudentSpecializedTrainingDto> result = [.. trainings.Select(MapToStudentSpecializedTrainingDto)];

            _logger.LogInformation("根据难度等级获取专项训练列表成功，学生ID: {StudentUserId}, 难度等级: {DifficultyLevel}, 返回数量: {Count}",
                studentUserId, difficultyLevel, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据难度等级获取专项训练列表失败，学生ID: {StudentUserId}, 难度等级: {DifficultyLevel}",
                studentUserId, difficultyLevel);
            throw;
        }
    }

    /// <summary>
    /// 搜索专项训练
    /// </summary>
    public async Task<List<StudentSpecializedTrainingDto>> SearchTrainingsAsync(int studentUserId, string searchKeyword, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            List<ImportedSpecializedTrainingEntity> trainings = await _context.ImportedSpecializedTrainings
                .Where(t => t.IsEnabled && 
                    (t.Name.Contains(searchKeyword) || 
                     (t.Description != null && t.Description.Contains(searchKeyword)) ||
                     (t.Tags != null && t.Tags.Contains(searchKeyword))))
                .OrderByDescending(t => t.ImportedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(t => t.Modules)
                .Include(t => t.Questions)
                .ToListAsync();

            List<StudentSpecializedTrainingDto> result = [.. trainings.Select(MapToStudentSpecializedTrainingDto)];

            _logger.LogInformation("搜索专项训练成功，学生ID: {StudentUserId}, 搜索关键词: {SearchKeyword}, 返回数量: {Count}",
                studentUserId, searchKeyword, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索专项训练失败，学生ID: {StudentUserId}, 搜索关键词: {SearchKeyword}",
                studentUserId, searchKeyword);
            throw;
        }
    }

    /// <summary>
    /// 获取所有可用的模块类型列表
    /// </summary>
    public async Task<List<string>> GetAvailableModuleTypesAsync(int studentUserId)
    {
        try
        {
            List<string> moduleTypes = await _context.ImportedSpecializedTrainings
                .Where(t => t.IsEnabled)
                .Select(t => t.ModuleType)
                .Distinct()
                .OrderBy(mt => mt)
                .ToListAsync();

            _logger.LogInformation("获取可用模块类型列表成功，学生ID: {StudentUserId}, 模块类型数量: {Count}",
                studentUserId, moduleTypes.Count);

            return moduleTypes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用模块类型列表失败，学生ID: {StudentUserId}", studentUserId);
            throw;
        }
    }

    /// <summary>
    /// 将ImportedSpecializedTraining映射为StudentSpecializedTrainingDto（基本信息）
    /// </summary>
    private static StudentSpecializedTrainingDto MapToStudentSpecializedTrainingDto(ImportedSpecializedTrainingEntity training)
    {
        var dto = new StudentSpecializedTrainingDto
        {
            Id = training.Id,
            Name = training.Name,
            Description = training.Description,
            ModuleType = training.ModuleType,
            TotalScore = training.TotalScore,
            Duration = training.Duration,
            DifficultyLevel = training.DifficultyLevel,
            RandomizeQuestions = training.RandomizeQuestions,
            Tags = training.Tags,
            OriginalCreatedTime = training.OriginalCreatedTime,
            OriginalLastModifiedTime = training.OriginalLastModifiedTime,
            ImportedAt = training.ImportedAt,
            ModuleCount = training.Modules?.Count ?? 0,
            QuestionCount = training.Questions?.Count ?? 0,
            EnableTrial = training.EnableTrial
        };

        System.Diagnostics.Debug.WriteLine($"[SpecializedTrainingService] 映射训练: {dto.Name}, EnableTrial: {training.EnableTrial} -> {dto.EnableTrial}");
        return dto;
    }

    /// <summary>
    /// 将ImportedSpecializedTraining映射为StudentSpecializedTrainingDto（包含详细信息）
    /// </summary>
    private static StudentSpecializedTrainingDto MapToStudentSpecializedTrainingDtoWithDetails(ImportedSpecializedTrainingEntity training)
    {
        StudentSpecializedTrainingDto dto = MapToStudentSpecializedTrainingDto(training);

        // 映射模块信息
        if (training.Modules != null)
        {
            System.Diagnostics.Debug.WriteLine($"映射模块信息，原始模块数量: {training.Modules.Count}");
            dto.Modules = [.. training.Modules.Select(MapToStudentSpecializedTrainingModuleDto)];
            System.Diagnostics.Debug.WriteLine($"映射后模块数量: {dto.Modules.Count}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("原始训练数据中Modules为null");
        }

        // 映射题目信息
        if (training.Questions != null)
        {
            System.Diagnostics.Debug.WriteLine($"映射题目信息，原始题目数量: {training.Questions.Count}");
            dto.Questions = [.. training.Questions.Select(MapToStudentSpecializedTrainingQuestionDto)];
            System.Diagnostics.Debug.WriteLine($"映射后题目数量: {dto.Questions.Count}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("原始训练数据中Questions为null");
        }

        return dto;
    }

    /// <summary>
    /// 将ImportedSpecializedTrainingModule映射为StudentSpecializedTrainingModuleDto
    /// </summary>
    private static StudentSpecializedTrainingModuleDto MapToStudentSpecializedTrainingModuleDto(ImportedSpecializedTrainingModule module)
    {
        return new StudentSpecializedTrainingModuleDto
        {
            Id = module.Id,
            Name = module.Name,
            Type = module.Type,
            Description = module.Description,
            Score = module.Score,
            Order = module.Order,
            Questions = module.Questions?.Select(MapToStudentSpecializedTrainingQuestionDto).ToList() ?? []
        };
    }

    /// <summary>
    /// 将ImportedSpecializedTrainingQuestion映射为StudentSpecializedTrainingQuestionDto
    /// </summary>
    private static StudentSpecializedTrainingQuestionDto MapToStudentSpecializedTrainingQuestionDto(ImportedSpecializedTrainingQuestion question)
    {
        return new StudentSpecializedTrainingQuestionDto
        {
            Id = question.Id,
            Title = question.Title,
            Content = question.Content,
            QuestionType = question.QuestionType,
            Score = question.Score,
            DifficultyLevel = question.DifficultyLevel,
            EstimatedMinutes = question.EstimatedMinutes,
            Order = question.Order,
            IsRequired = question.IsRequired,
            QuestionConfig = null, // 专项训练题目没有QuestionConfig属性
            AnswerValidationRules = null, // 专项训练题目没有AnswerValidationRules属性
            Tags = question.Tags,
            Remarks = null, // 专项训练题目没有Remarks属性
            ProgramInput = question.ProgramInput,
            ExpectedOutput = question.ExpectedOutput,
            CSharpQuestionType = question.CSharpQuestionType,
            CodeFilePath = question.CodeFilePath,
            CSharpDirectScore = question.CSharpDirectScore,
            CodeBlanks = question.CodeBlanks,
            TemplateCode = question.TemplateCode,
            DocumentFilePath = question.DocumentFilePath,
            OperationPoints = question.OperationPoints?.Select(MapToStudentSpecializedTrainingOperationPointDto).ToList() ?? []
        };
    }

    /// <summary>
    /// 将ImportedSpecializedTrainingOperationPoint映射为StudentSpecializedTrainingOperationPointDto
    /// </summary>
    private static StudentSpecializedTrainingOperationPointDto MapToStudentSpecializedTrainingOperationPointDto(ImportedSpecializedTrainingOperationPoint operationPoint)
    {
        return new StudentSpecializedTrainingOperationPointDto
        {
            Id = operationPoint.Id,
            Name = operationPoint.Name,
            Description = operationPoint.Description,
            ModuleType = operationPoint.ModuleType,
            Score = operationPoint.Score,
            Order = operationPoint.Order,
            Parameters = operationPoint.Parameters?.Select(MapToStudentSpecializedTrainingParameterDto).ToList() ?? []
        };
    }

    /// <summary>
    /// 将ImportedSpecializedTrainingParameter映射为StudentSpecializedTrainingParameterDto
    /// </summary>
    private static StudentSpecializedTrainingParameterDto MapToStudentSpecializedTrainingParameterDto(ImportedSpecializedTrainingParameter parameter)
    {
        return new StudentSpecializedTrainingParameterDto
        {
            Id = parameter.Id,
            Name = parameter.Name,
            Description = parameter.Description,
            ParameterType = parameter.Type, // 使用Type属性而不是ParameterType
            DefaultValue = parameter.DefaultValue?.ToString(), // 转换为字符串
            Value = parameter.Value, // 添加Value字段映射
            MinValue = parameter.MinValue?.ToString(), // 转换为字符串
            MaxValue = parameter.MaxValue?.ToString() // 转换为字符串
        };
    }

    /// <summary>
    /// 标记专项训练为开始状态
    /// </summary>
    public async Task<bool> MarkTrainingAsStartedAsync(int studentUserId, int trainingId)
    {
        try
        {
            // 验证学生用户存在且为学生角色
            User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == Models.UserRole.Student && u.IsActive);

            if (student == null)
            {
                _logger.LogWarning("用户不存在或不是活跃学生，用户ID: {UserId}", studentUserId);
                return false;
            }

            // 验证专项训练存在且启用
            ImportedSpecializedTrainingEntity? training = await _context.ImportedSpecializedTrainings
                .FirstOrDefaultAsync(t => t.Id == trainingId && t.IsEnabled);

            if (training == null)
            {
                _logger.LogWarning("专项训练不存在或未启用，训练ID: {TrainingId}", trainingId);
                return false;
            }

            // 查找现有的完成记录
            SpecialPracticeCompletion? existingRecord = await _context.SpecialPracticeCompletions
                .FirstOrDefaultAsync(c => c.StudentUserId == studentUserId && c.PracticeId == trainingId && c.IsActive);

            DateTime now = DateTime.UtcNow;

            if (existingRecord != null)
            {
                // 如果已经是完成状态，不允许重新标记为开始
                if (existingRecord.Status == SpecialPracticeCompletionStatus.Completed)
                {
                    _logger.LogInformation("专项训练已完成，不允许重新标记为开始，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
                    return false;
                }

                // 更新为进行中状态
                existingRecord.Status = SpecialPracticeCompletionStatus.InProgress;
                existingRecord.StartedAt ??= now; // 如果没有开始时间则设置
                existingRecord.UpdatedAt = now;

                _logger.LogInformation("更新专项训练为进行中状态，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
            }
            else
            {
                // 创建新的开始记录
                SpecialPracticeCompletion newRecord = new()
                {
                    StudentUserId = studentUserId,
                    PracticeId = trainingId,
                    Status = SpecialPracticeCompletionStatus.InProgress,
                    StartedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                _ = _context.SpecialPracticeCompletions.Add(newRecord);
                _logger.LogInformation("创建新的专项训练开始记录，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
            }

            _ = await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记专项训练为开始状态失败，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
            return false;
        }
    }

    /// <summary>
    /// 标记专项训练为已完成
    /// </summary>
    public async Task<bool> MarkTrainingAsCompletedAsync(int studentUserId, int trainingId, double? score = null, double? maxScore = null, int? durationSeconds = null, string? notes = null)
    {
        try
        {
            // 验证学生用户存在且为学生角色
            User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == Models.UserRole.Student && u.IsActive);

            if (student == null)
            {
                _logger.LogWarning("用户不存在或不是活跃学生，用户ID: {UserId}", studentUserId);
                return false;
            }

            // 验证专项训练存在且启用
            ImportedSpecializedTrainingEntity? training = await _context.ImportedSpecializedTrainings
                .FirstOrDefaultAsync(t => t.Id == trainingId && t.IsEnabled);

            if (training == null)
            {
                _logger.LogWarning("专项训练不存在或未启用，训练ID: {TrainingId}", trainingId);
                return false;
            }

            // 查找现有的完成记录
            SpecialPracticeCompletion? existingRecord = await _context.SpecialPracticeCompletions
                .FirstOrDefaultAsync(c => c.StudentUserId == studentUserId && c.PracticeId == trainingId && c.IsActive);

            DateTime now = DateTime.UtcNow;

            if (existingRecord != null)
            {
                // 更新现有记录为完成状态
                existingRecord.Status = SpecialPracticeCompletionStatus.Completed;
                existingRecord.CompletedAt = now;
                existingRecord.UpdatedAt = now;

                // 更新评分信息
                if (score.HasValue) existingRecord.Score = score.Value;
                if (maxScore.HasValue) existingRecord.MaxScore = maxScore.Value;
                if (durationSeconds.HasValue) existingRecord.DurationSeconds = durationSeconds.Value;
                if (!string.IsNullOrWhiteSpace(notes)) existingRecord.Notes = notes;

                // 计算完成百分比
                if (score.HasValue && maxScore.HasValue && maxScore.Value > 0)
                {
                    existingRecord.CompletionPercentage = Math.Round((score.Value / maxScore.Value) * 100, 2);
                }

                _logger.LogInformation("更新专项训练为完成状态，学生ID: {StudentUserId}, 训练ID: {TrainingId}, 得分: {Score}/{MaxScore}",
                    studentUserId, trainingId, score, maxScore);
            }
            else
            {
                // 创建新的完成记录（直接标记为完成）
                SpecialPracticeCompletion newRecord = new()
                {
                    StudentUserId = studentUserId,
                    PracticeId = trainingId,
                    Status = SpecialPracticeCompletionStatus.Completed,
                    StartedAt = now, // 假设开始时间就是现在
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

                _ = _context.SpecialPracticeCompletions.Add(newRecord);
                _logger.LogInformation("创建新的专项训练完成记录，学生ID: {StudentUserId}, 训练ID: {TrainingId}, 得分: {Score}/{MaxScore}",
                    studentUserId, trainingId, score, maxScore);
            }

            _ = await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记专项训练为完成状态失败，学生ID: {StudentUserId}, 训练ID: {TrainingId}", studentUserId, trainingId);
            return false;
        }
    }
}
