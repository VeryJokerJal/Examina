using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Api.Student;
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
    /// 获取学生可访问的综合训练列表
    /// </summary>
    public async Task<List<StudentComprehensiveTrainingDto>> GetAvailableTrainingsAsync(int studentUserId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            // 目前简化权限验证：所有启用的综合训练都对学生可见
            // 后续可以根据组织关系、权限设置等进行更细粒度的权限控制
            List<ImportedComprehensiveTrainingEntity> trainings = await _context.ImportedComprehensiveTrainings
                .Where(t => t.IsEnabled)
                .OrderByDescending(t => t.ImportedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<StudentComprehensiveTrainingDto> result = trainings.Select(MapToStudentComprehensiveTrainingDto).ToList();

            _logger.LogInformation("获取学生可访问综合训练列表成功，学生ID: {StudentUserId}, 返回数量: {Count}", 
                studentUserId, result.Count);

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
    /// 映射到学生端综合训练DTO（不包含详细信息）
    /// </summary>
    private static StudentComprehensiveTrainingDto MapToStudentComprehensiveTrainingDto(ImportedComprehensiveTrainingEntity training)
    {
        return new StudentComprehensiveTrainingDto
        {
            Id = training.Id,
            Name = training.Name,
            Description = training.Description,
            TrainingType = training.TrainingType,
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
}
