using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Models.MockExam;
using ExaminaWebApplication.Models.ImportedComprehensiveTraining;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExaminaWebApplication.Services.Student;

/// <summary>
/// 学生端模拟考试服务实现
/// </summary>
public class StudentMockExamService : IStudentMockExamService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StudentMockExamService> _logger;
    private readonly Random _random = new();

    /// <summary>
    /// 统一的JSON序列化选项配置
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StudentMockExamService(ApplicationDbContext context, ILogger<StudentMockExamService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 快速开始模拟考试（使用预设规则自动生成并开始）
    /// </summary>
    public async Task<StudentMockExamDto?> QuickStartMockExamAsync(int studentUserId)
    {
        try
        {
            // 验证学生用户存在且为学生角色
            Models.User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == Models.UserRole.Student && u.IsActive);

            if (student == null)
            {
                _logger.LogWarning("用户不存在或不是活跃学生，用户ID: {UserId}", studentUserId);
                return null;
            }

            // 使用预设的抽取规则
            CreateMockExamRequestDto request = CreateDefaultMockExamRequest();

            // 验证抽取规则的总分值是否匹配
            int totalScoreFromRules = request.ExtractionRules.Sum(r => r.Count * r.ScorePerQuestion);
            if (totalScoreFromRules != request.TotalScore)
            {
                _logger.LogWarning("预设抽取规则的总分值({TotalFromRules})与请求的总分值({RequestTotal})不匹配",
                    totalScoreFromRules, request.TotalScore);
                return null;
            }

            // 从综合训练中抽取题目
            List<ExtractedQuestionInfo> extractedQuestions = await ExtractQuestionsAsync(request.ExtractionRules);

            _logger.LogInformation("题目抽取结果：抽取到 {ExtractedCount} 道题目，需要 {RequiredCount} 道题目",
                extractedQuestions.Count, request.ExtractionRules.Sum(r => r.Count));

            // 如果抽取的题目数量不足，尝试使用备用策略
            int requiredQuestionCount = request.ExtractionRules.Sum(r => r.Count);
            if (extractedQuestions.Count < requiredQuestionCount)
            {
                _logger.LogWarning("抽取的题目数量不足，尝试使用备用策略。当前抽取：{ExtractedCount}道，需要：{RequiredCount}道",
                    extractedQuestions.Count, requiredQuestionCount);

                // 尝试备用抽取策略：不限制题目类型和难度
                List<ExtractedQuestionInfo> fallbackQuestions = await ExtractQuestionsWithFallbackAsync(requiredQuestionCount - extractedQuestions.Count);
                extractedQuestions.AddRange(fallbackQuestions);

                _logger.LogInformation("备用策略抽取了 {FallbackCount} 道题目，总计：{TotalCount}道",
                    fallbackQuestions.Count, extractedQuestions.Count);
            }

            if (extractedQuestions.Count == 0)
            {
                _logger.LogWarning("无法从综合训练中抽取到任何题目，请检查题库是否为空");
                return null;
            }

            // 如果需要随机排序
            if (request.RandomizeQuestions)
            {
                extractedQuestions = extractedQuestions.OrderBy(x => _random.Next()).ToList();
            }

            // 重新设置排序顺序
            for (int i = 0; i < extractedQuestions.Count; i++)
            {
                extractedQuestions[i].SortOrder = i + 1;
            }

            // 创建模拟考试实例并立即开始
            MockExam mockExam = new()
            {
                StudentId = studentUserId,
                Name = request.Name,
                Description = request.Description,
                DurationMinutes = request.DurationMinutes,
                TotalScore = request.TotalScore,
                PassingScore = request.PassingScore,
                RandomizeQuestions = request.RandomizeQuestions,
                ExtractedQuestions = JsonSerializer.Serialize(extractedQuestions, JsonOptions),
                Status = "InProgress", // 直接设置为进行中
                CreatedAt = DateTime.UtcNow,
                StartedAt = DateTime.UtcNow, // 立即开始
                ExpiresAt = DateTime.UtcNow.AddHours(request.DurationMinutes / 60.0 + 1) // 考试时长+1小时缓冲
            };

            _context.MockExams.Add(mockExam);
            await _context.SaveChangesAsync();

            _logger.LogInformation("成功快速开始模拟考试，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 题目数量: {QuestionCount}",
                studentUserId, mockExam.Id, extractedQuestions.Count);

            return MapToStudentMockExamDto(mockExam, extractedQuestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "快速开始模拟考试失败，学生ID: {StudentId}", studentUserId);
            return null;
        }
    }

    /// <summary>
    /// 创建模拟考试
    /// </summary>
    public async Task<StudentMockExamDto?> CreateMockExamAsync(CreateMockExamRequestDto request, int studentUserId)
    {
        try
        {
            // 验证学生用户存在且为学生角色
            Models.User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == Models.UserRole.Student && u.IsActive);

            if (student == null)
            {
                _logger.LogWarning("用户不存在或不是活跃学生，用户ID: {UserId}", studentUserId);
                return null;
            }

            // 验证抽取规则的总分值是否匹配
            int totalScoreFromRules = request.ExtractionRules.Sum(r => r.Count * r.ScorePerQuestion);
            if (totalScoreFromRules != request.TotalScore)
            {
                _logger.LogWarning("抽取规则的总分值({TotalFromRules})与请求的总分值({RequestTotal})不匹配", 
                    totalScoreFromRules, request.TotalScore);
                return null;
            }

            // 从综合训练中抽取题目
            List<ExtractedQuestionInfo> extractedQuestions = await ExtractQuestionsAsync(request.ExtractionRules);
            if (extractedQuestions.Count == 0)
            {
                _logger.LogWarning("无法从综合训练中抽取到足够的题目");
                return null;
            }

            // 如果需要随机排序
            if (request.RandomizeQuestions)
            {
                extractedQuestions = extractedQuestions.OrderBy(x => _random.Next()).ToList();
            }

            // 重新设置排序顺序
            for (int i = 0; i < extractedQuestions.Count; i++)
            {
                extractedQuestions[i].SortOrder = i + 1;
            }

            // 创建模拟考试实例
            MockExam mockExam = new()
            {
                StudentId = studentUserId,
                Name = request.Name,
                Description = request.Description,
                DurationMinutes = request.DurationMinutes,
                TotalScore = request.TotalScore,
                PassingScore = request.PassingScore,
                RandomizeQuestions = request.RandomizeQuestions,
                ExtractedQuestions = JsonSerializer.Serialize(extractedQuestions, JsonOptions),
                Status = "Created",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7) // 7天后过期
            };

            _context.MockExams.Add(mockExam);
            await _context.SaveChangesAsync();

            _logger.LogInformation("成功创建模拟考试，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 题目数量: {QuestionCount}", 
                studentUserId, mockExam.Id, extractedQuestions.Count);

            return MapToStudentMockExamDto(mockExam, extractedQuestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建模拟考试失败，学生ID: {StudentId}", studentUserId);
            return null;
        }
    }

    /// <summary>
    /// 获取学生的模拟考试列表
    /// </summary>
    public async Task<List<StudentMockExamDto>> GetStudentMockExamsAsync(int studentUserId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            List<MockExam> mockExams = await _context.MockExams
                .Where(me => me.StudentId == studentUserId)
                .OrderByDescending(me => me.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<StudentMockExamDto> result = [];
            foreach (MockExam mockExam in mockExams)
            {
                List<ExtractedQuestionInfo> questions = [];
                if (!string.IsNullOrEmpty(mockExam.ExtractedQuestions))
                {
                    questions = JsonSerializer.Deserialize<List<ExtractedQuestionInfo>>(mockExam.ExtractedQuestions, JsonOptions) ?? [];
                }

                result.Add(MapToStudentMockExamDto(mockExam, questions));
            }

            _logger.LogInformation("获取学生模拟考试列表成功，学生ID: {StudentId}, 返回数量: {Count}", 
                studentUserId, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生模拟考试列表失败，学生ID: {StudentId}", studentUserId);
            return [];
        }
    }

    /// <summary>
    /// 获取模拟考试详情
    /// </summary>
    public async Task<StudentMockExamDto?> GetMockExamDetailsAsync(int mockExamId, int studentUserId)
    {
        try
        {
            // 检查权限
            if (!await HasAccessToMockExamAsync(mockExamId, studentUserId))
            {
                _logger.LogWarning("学生无权限访问模拟考试，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                    studentUserId, mockExamId);
                return null;
            }

            MockExam? mockExam = await _context.MockExams
                .FirstOrDefaultAsync(me => me.Id == mockExamId);

            if (mockExam == null)
            {
                _logger.LogWarning("模拟考试不存在，模拟考试ID: {MockExamId}", mockExamId);
                return null;
            }

            List<ExtractedQuestionInfo> questions = [];
            if (!string.IsNullOrEmpty(mockExam.ExtractedQuestions))
            {
                questions = JsonSerializer.Deserialize<List<ExtractedQuestionInfo>>(mockExam.ExtractedQuestions, JsonOptions) ?? [];
            }

            _logger.LogInformation("获取模拟考试详情成功，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, mockExamId);

            return MapToStudentMockExamDto(mockExam, questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模拟考试详情失败，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, mockExamId);
            return null;
        }
    }

    /// <summary>
    /// 开始模拟考试
    /// </summary>
    public async Task<bool> StartMockExamAsync(int mockExamId, int studentUserId)
    {
        try
        {
            // 检查权限
            if (!await HasAccessToMockExamAsync(mockExamId, studentUserId))
            {
                return false;
            }

            MockExam? mockExam = await _context.MockExams
                .FirstOrDefaultAsync(me => me.Id == mockExamId && me.StudentId == studentUserId);

            if (mockExam == null || mockExam.Status != "Created")
            {
                return false;
            }

            // 检查是否已过期
            if (mockExam.ExpiresAt.HasValue && mockExam.ExpiresAt.Value < DateTime.UtcNow)
            {
                mockExam.Status = "Expired";
                await _context.SaveChangesAsync();
                return false;
            }

            mockExam.Status = "InProgress";
            mockExam.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("学生开始模拟考试，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, mockExamId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始模拟考试失败，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, mockExamId);
            return false;
        }
    }

    /// <summary>
    /// 完成模拟考试
    /// </summary>
    public async Task<bool> CompleteMockExamAsync(int mockExamId, int studentUserId)
    {
        try
        {
            // 检查权限
            if (!await HasAccessToMockExamAsync(mockExamId, studentUserId))
            {
                return false;
            }

            MockExam? mockExam = await _context.MockExams
                .FirstOrDefaultAsync(me => me.Id == mockExamId && me.StudentId == studentUserId);

            if (mockExam == null || mockExam.Status != "InProgress")
            {
                return false;
            }

            mockExam.Status = "Completed";
            mockExam.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("学生完成模拟考试，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, mockExamId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成模拟考试失败，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, mockExamId);
            return false;
        }
    }

    /// <summary>
    /// 提交模拟考试（与完成模拟考试功能相同，但语义更明确）
    /// </summary>
    public async Task<bool> SubmitMockExamAsync(int mockExamId, int studentUserId)
    {
        try
        {
            _logger.LogInformation("开始提交模拟考试，模拟考试ID: {MockExamId}, 学生ID: {StudentId}",
                mockExamId, studentUserId);

            // 提交模拟考试实际上就是完成模拟考试
            bool result = await CompleteMockExamAsync(mockExamId, studentUserId);

            if (result)
            {
                _logger.LogInformation("模拟考试提交成功，模拟考试ID: {MockExamId}, 学生ID: {StudentId}",
                    mockExamId, studentUserId);
            }
            else
            {
                _logger.LogWarning("模拟考试提交失败，模拟考试ID: {MockExamId}, 学生ID: {StudentId}",
                    mockExamId, studentUserId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交模拟考试异常，模拟考试ID: {MockExamId}, 学生ID: {StudentId}",
                mockExamId, studentUserId);
            return false;
        }
    }

    /// <summary>
    /// 删除模拟考试
    /// </summary>
    public async Task<bool> DeleteMockExamAsync(int mockExamId, int studentUserId)
    {
        try
        {
            // 检查权限
            if (!await HasAccessToMockExamAsync(mockExamId, studentUserId))
            {
                return false;
            }

            MockExam? mockExam = await _context.MockExams
                .FirstOrDefaultAsync(me => me.Id == mockExamId && me.StudentId == studentUserId);

            if (mockExam == null)
            {
                return false;
            }

            _context.MockExams.Remove(mockExam);
            await _context.SaveChangesAsync();

            _logger.LogInformation("学生删除模拟考试，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, mockExamId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除模拟考试失败，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, mockExamId);
            return false;
        }
    }

    /// <summary>
    /// 获取学生可访问的模拟考试总数
    /// </summary>
    public async Task<int> GetStudentMockExamCountAsync(int studentUserId)
    {
        try
        {
            int count = await _context.MockExams
                .CountAsync(me => me.StudentId == studentUserId);

            _logger.LogInformation("获取学生模拟考试总数成功，学生ID: {StudentId}, 总数: {Count}", 
                studentUserId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生模拟考试总数失败，学生ID: {StudentId}", studentUserId);
            return 0;
        }
    }

    /// <summary>
    /// 检查是否有权限访问指定模拟考试
    /// </summary>
    public async Task<bool> HasAccessToMockExamAsync(int mockExamId, int studentUserId)
    {
        try
        {
            // 验证学生用户存在且为学生角色
            Models.User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == Models.UserRole.Student && u.IsActive);

            if (student == null)
            {
                return false;
            }

            // 验证模拟考试存在且属于该学生
            bool hasAccess = await _context.MockExams
                .AnyAsync(me => me.Id == mockExamId && me.StudentId == studentUserId);

            return hasAccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查模拟考试访问权限失败，学生ID: {StudentId}, 模拟考试ID: {MockExamId}", 
                studentUserId, mockExamId);
            return false;
        }
    }

    /// <summary>
    /// 从综合训练中抽取题目
    /// </summary>
    private async Task<List<ExtractedQuestionInfo>> ExtractQuestionsAsync(List<QuestionExtractionRuleDto> rules)
    {
        List<ExtractedQuestionInfo> extractedQuestions = [];

        foreach (QuestionExtractionRuleDto rule in rules)
        {
            // 构建查询条件
            IQueryable<ImportedComprehensiveTrainingQuestion> query = _context.ImportedComprehensiveTrainingQuestions
                .Include(q => q.OperationPoints)
                    .ThenInclude(op => op.Parameters)
                .Include(q => q.Subject)
                .Include(q => q.Module)
                .Where(q => q.IsEnabled);

            // 按科目类型过滤
            if (!string.IsNullOrEmpty(rule.SubjectType))
            {
                query = query.Where(q => q.Subject != null && q.Subject.SubjectType == rule.SubjectType);
            }

            // 按题目类型过滤
            if (!string.IsNullOrEmpty(rule.QuestionType))
            {
                query = query.Where(q => q.QuestionType == rule.QuestionType);
            }

            // 按难度等级过滤
            if (!string.IsNullOrEmpty(rule.DifficultyLevel))
            {
                int difficultyLevelInt = ConvertDifficultyLevelToInt(rule.DifficultyLevel);
                query = query.Where(q => q.DifficultyLevel == difficultyLevelInt);
            }

            // 获取符合条件的题目
            List<ImportedComprehensiveTrainingQuestion> availableQuestions = await query.ToListAsync();

            _logger.LogInformation("抽取规则 {QuestionType}({DifficultyLevel})：找到 {AvailableCount} 道可用题目，需要 {RequiredCount} 道",
                rule.QuestionType, rule.DifficultyLevel, availableQuestions.Count, rule.Count);

            // 随机抽取指定数量的题目
            List<ImportedComprehensiveTrainingQuestion> selectedQuestions = availableQuestions
                .OrderBy(x => _random.Next())
                .Take(rule.Count)
                .ToList();

            _logger.LogInformation("抽取规则 {QuestionType}({DifficultyLevel})：成功抽取 {SelectedCount} 道题目",
                rule.QuestionType, rule.DifficultyLevel, selectedQuestions.Count);

            // 转换为ExtractedQuestionInfo
            foreach (ImportedComprehensiveTrainingQuestion question in selectedQuestions)
            {
                ExtractedQuestionInfo extractedQuestion = new()
                {
                    OriginalQuestionId = question.Id,
                    ComprehensiveTrainingId = question.ComprehensiveTrainingId,
                    SubjectId = question.SubjectId,
                    ModuleId = question.ModuleId,
                    Title = question.Title,
                    Content = question.Content,
                    QuestionType = question.QuestionType,
                    Score = rule.ScorePerQuestion, // 使用规则中指定的分值
                    DifficultyLevel = ConvertDifficultyLevelToString(question.DifficultyLevel),
                    EstimatedMinutes = question.EstimatedMinutes,
                    SortOrder = question.SortOrder,
                    QuestionConfig = question.QuestionConfig,
                    AnswerValidationRules = question.AnswerValidationRules,
                    Tags = question.Tags,
                    Remarks = question.Remarks,
                    ProgramInput = question.ProgramInput,
                    ExpectedOutput = question.ExpectedOutput,
                    OperationPoints = question.OperationPoints.Select(op => new ExtractedOperationPointInfo
                    {
                        Id = op.Id,
                        Name = op.Name,
                        Description = op.Description,
                        ModuleType = op.ModuleType,
                        Score = (int)op.Score,
                        Order = op.Order,
                        Parameters = op.Parameters.Select(p => new ExtractedParameterInfo
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Description = p.Description,
                            ParameterType = p.Type,
                            DefaultValue = p.DefaultValue?.ToString() ?? string.Empty,
                            MinValue = p.MinValue?.ToString() ?? string.Empty,
                            MaxValue = p.MaxValue?.ToString() ?? string.Empty
                        }).ToList()
                    }).ToList()
                };

                extractedQuestions.Add(extractedQuestion);
            }

            _logger.LogInformation("抽取题目完成，规则: {Rule}, 可用题目数: {Available}, 抽取数量: {Selected}",
                JsonSerializer.Serialize(rule, JsonOptions), availableQuestions.Count, selectedQuestions.Count);
        }

        return extractedQuestions;
    }

    /// <summary>
    /// 备用题目抽取策略：不限制题目类型和难度
    /// </summary>
    private async Task<List<ExtractedQuestionInfo>> ExtractQuestionsWithFallbackAsync(int count)
    {
        try
        {
            _logger.LogInformation("执行备用抽取策略，需要抽取 {Count} 道题目", count);

            // 查询所有可用的题目，不限制类型和难度
            List<ImportedComprehensiveTrainingQuestion> availableQuestions = await _context.ImportedComprehensiveTrainingQuestions
                .Include(q => q.OperationPoints)
                    .ThenInclude(op => op.Parameters)
                .Include(q => q.Subject)
                .Include(q => q.Module)
                .Where(q => q.IsEnabled)
                .ToListAsync();

            _logger.LogInformation("备用策略找到 {AvailableCount} 道可用题目", availableQuestions.Count);

            if (availableQuestions.Count == 0)
            {
                _logger.LogWarning("备用策略：数据库中没有可用的题目");
                return [];
            }

            // 随机抽取指定数量的题目
            List<ImportedComprehensiveTrainingQuestion> selectedQuestions = availableQuestions
                .OrderBy(x => _random.Next())
                .Take(count)
                .ToList();

            List<ExtractedQuestionInfo> extractedQuestions = [];

            // 使用现有的转换逻辑
            foreach (ImportedComprehensiveTrainingQuestion question in selectedQuestions)
            {
                ExtractedQuestionInfo extractedQuestion = new()
                {
                    OriginalQuestionId = question.Id,
                    ComprehensiveTrainingId = question.ComprehensiveTrainingId,
                    SubjectId = question.SubjectId,
                    ModuleId = question.ModuleId,
                    Title = question.Title,
                    Content = question.Content,
                    QuestionType = question.QuestionType,
                    Score = 10, // 备用策略使用固定分值
                    DifficultyLevel = ConvertDifficultyLevelToString(question.DifficultyLevel),
                    EstimatedMinutes = question.EstimatedMinutes,
                    SortOrder = question.SortOrder,
                    QuestionConfig = question.QuestionConfig,
                    AnswerValidationRules = question.AnswerValidationRules,
                    Tags = question.Tags,
                    Remarks = question.Remarks,
                    OperationPoints = [] // 简化：不包含操作点
                };

                extractedQuestions.Add(extractedQuestion);
            }

            _logger.LogInformation("备用策略成功抽取 {ExtractedCount} 道题目", extractedQuestions.Count);
            return extractedQuestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "备用抽取策略执行失败");
            return [];
        }
    }

    /// <summary>
    /// 将MockExam和题目信息映射为StudentMockExamDto
    /// </summary>
    private static StudentMockExamDto MapToStudentMockExamDto(MockExam mockExam, List<ExtractedQuestionInfo> questions)
    {
        return new StudentMockExamDto
        {
            Id = mockExam.Id,
            Name = mockExam.Name,
            Description = mockExam.Description,
            DurationMinutes = mockExam.DurationMinutes,
            TotalScore = mockExam.TotalScore,
            PassingScore = mockExam.PassingScore,
            RandomizeQuestions = mockExam.RandomizeQuestions,
            Status = mockExam.Status,
            CreatedAt = mockExam.CreatedAt,
            StartedAt = mockExam.StartedAt,
            CompletedAt = mockExam.CompletedAt,
            ExpiresAt = mockExam.ExpiresAt,
            Questions = questions.Select(q => new StudentMockExamQuestionDto
            {
                OriginalQuestionId = q.OriginalQuestionId,
                Title = q.Title,
                Content = q.Content,
                QuestionType = q.QuestionType,
                Score = q.Score,
                DifficultyLevel = q.DifficultyLevel,
                EstimatedMinutes = q.EstimatedMinutes,
                SortOrder = q.SortOrder,
                QuestionConfig = q.QuestionConfig,
                AnswerValidationRules = q.AnswerValidationRules,
                Tags = q.Tags,
                Remarks = q.Remarks,
                ProgramInput = q.ProgramInput,
                ExpectedOutput = q.ExpectedOutput,
                OperationPoints = q.OperationPoints.Select(op => new StudentMockExamOperationPointDto
                {
                    Id = op.Id,
                    Name = op.Name,
                    Description = op.Description,
                    ModuleType = op.ModuleType,
                    Score = op.Score,
                    Order = op.Order,
                    Parameters = op.Parameters.Select(p => new StudentMockExamParameterDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        ParameterType = p.ParameterType,
                        DefaultValue = p.DefaultValue,
                        MinValue = p.MinValue,
                        MaxValue = p.MaxValue
                    }).ToList()
                }).ToList()
            }).ToList()
        };
    }

    /// <summary>
    /// 创建默认的模拟考试请求
    /// </summary>
    private static CreateMockExamRequestDto CreateDefaultMockExamRequest()
    {
        return new CreateMockExamRequestDto
        {
            Name = $"模拟考试 - {DateTime.Now:yyyy年MM月dd日 HH:mm}",
            Description = "系统自动生成的模拟考试，从综合训练题库中随机抽取题目组成。",
            DurationMinutes = 120, // 2小时
            TotalScore = 100,
            PassingScore = 60,
            RandomizeQuestions = true,
            ExtractionRules = new List<QuestionExtractionRuleDto>
            {
                // 编程题 - 5道，每道15分（不限制难度）
                new()
                {
                    QuestionType = "编程题",
                    DifficultyLevel = "", // 不限制难度
                    Count = 5,
                    ScorePerQuestion = 15,
                    IsRequired = true
                },
                // 操作题 - 5道，每道5分（不限制难度）
                new()
                {
                    QuestionType = "操作题",
                    DifficultyLevel = "", // 不限制难度
                    Count = 5,
                    ScorePerQuestion = 5,
                    IsRequired = true
                }
            }
        };
    }

    /// <summary>
    /// 将难度级别字符串转换为整数
    /// </summary>
    private static int ConvertDifficultyLevelToInt(string difficultyLevel)
    {
        return difficultyLevel?.ToLower() switch
        {
            "简单" or "easy" => 1,
            "中等" or "medium" => 2,
            "困难" or "hard" => 3,
            "很难" or "very hard" => 4,
            "极难" or "extreme" => 5,
            _ => 1 // 默认为简单
        };
    }

    /// <summary>
    /// 将难度级别整数转换为字符串
    /// </summary>
    private static string ConvertDifficultyLevelToString(int difficultyLevel)
    {
        return difficultyLevel switch
        {
            1 => "简单",
            2 => "中等",
            3 => "困难",
            4 => "很难",
            5 => "极难",
            _ => "简单" // 默认为简单
        };
    }
}
