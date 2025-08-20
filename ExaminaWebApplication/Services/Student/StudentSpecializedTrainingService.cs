using ExaminaWebApplication.Data;
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
    /// 获取学生可访问的专项训练列表
    /// </summary>
    public async Task<List<StudentSpecializedTrainingDto>> GetAvailableTrainingsAsync(int studentUserId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            // 目前简化权限验证：所有启用的专项训练都对学生可见
            // 后续可以根据组织关系、权限设置等进行更细粒度的权限控制
            List<ImportedSpecializedTrainingEntity> trainings = await _context.ImportedSpecializedTrainings
                .Where(t => t.IsEnabled)
                .OrderByDescending(t => t.ImportedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(t => t.Modules)
                .Include(t => t.Questions)
                .ToListAsync();

            List<StudentSpecializedTrainingDto> result = trainings.Select(MapToStudentSpecializedTrainingDto).ToList();

            _logger.LogInformation("获取学生可访问专项训练列表成功，学生ID: {StudentUserId}, 返回数量: {Count}",
                studentUserId, result.Count);

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

            StudentSpecializedTrainingDto result = MapToStudentSpecializedTrainingDtoWithDetails(training);

            _logger.LogInformation("获取专项训练详情成功，学生ID: {StudentUserId}, 训练ID: {TrainingId}",
                studentUserId, trainingId);

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
            Models.User? student = await _context.Users
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

            // 目前简化权限验证：所有启用的专项训练都对学生可见
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
            // 目前简化权限验证：所有启用的专项训练都对学生可见
            int count = await _context.ImportedSpecializedTrainings
                .CountAsync(t => t.IsEnabled);

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

            List<StudentSpecializedTrainingDto> result = trainings.Select(MapToStudentSpecializedTrainingDto).ToList();

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

            List<StudentSpecializedTrainingDto> result = trainings.Select(MapToStudentSpecializedTrainingDto).ToList();

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

            List<StudentSpecializedTrainingDto> result = trainings.Select(MapToStudentSpecializedTrainingDto).ToList();

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
        return new StudentSpecializedTrainingDto
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
            QuestionCount = training.Questions?.Count ?? 0
        };
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
            dto.Modules = training.Modules.Select(MapToStudentSpecializedTrainingModuleDto).ToList();
        }

        // 映射题目信息
        if (training.Questions != null)
        {
            dto.Questions = training.Questions.Select(MapToStudentSpecializedTrainingQuestionDto).ToList();
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
            ProgramInput = null, // 专项训练题目没有ProgramInput属性
            ExpectedOutput = null, // 专项训练题目没有ExpectedOutput属性
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
            MinValue = parameter.MinValue?.ToString(), // 转换为字符串
            MaxValue = parameter.MaxValue?.ToString() // 转换为字符串
        };
    }
}
