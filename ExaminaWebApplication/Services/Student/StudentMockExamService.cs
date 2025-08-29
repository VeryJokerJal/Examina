using ExaminaWebApplication.Data;
using ExaminaWebApplication.Models;
using ExaminaWebApplication.Models.Api.Student;
using ExaminaWebApplication.Models.MockExam;
using ExaminaWebApplication.Models.ImportedComprehensiveTraining;
using ExaminaWebApplication.DTOs.MockExam;
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
    public async Task<MockExamComprehensiveTrainingDto?> QuickStartMockExamAsync(int studentUserId)
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
            double totalScoreFromRules = request.ExtractionRules.Sum(r => r.Count * r.ScorePerQuestion);
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
                extractedQuestions = [.. extractedQuestions.OrderBy(x => _random.Next())];
            }

            // 重新设置排序顺序
            for (int i = 0; i < extractedQuestions.Count; i++)
            {
                extractedQuestions[i].SortOrder = i + 1;
            }

            // 创建或获取默认配置
            MockExamConfiguration configuration = await GetOrCreateDefaultConfigurationAsync(request, studentUserId);

            // 创建模拟考试实例并立即开始
            DateTime now = DateTime.Now; // 使用本地时间
            MockExam mockExam = new()
            {
                ConfigurationId = configuration.Id,
                StudentId = studentUserId,
                Name = request.Name,
                Description = request.Description,
                DurationMinutes = request.DurationMinutes,
                TotalScore = request.TotalScore,
                PassingScore = request.PassingScore,
                RandomizeQuestions = request.RandomizeQuestions,
                ExtractedQuestions = JsonSerializer.Serialize(extractedQuestions, JsonOptions),
                Status = "InProgress", // 直接设置为进行中
                CreatedAt = now,
                StartedAt = now, // 立即开始
                ExpiresAt = now.AddHours(request.DurationMinutes / 60.0 + 1) // 考试时长+1小时缓冲
            };

            _context.MockExams.Add(mockExam);
            await _context.SaveChangesAsync();

            _logger.LogInformation("成功快速开始模拟考试，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 题目数量: {QuestionCount}",
                studentUserId, mockExam.Id, extractedQuestions.Count);

            return await MapToMockExamComprehensiveTrainingDtoAsync(mockExam, extractedQuestions);
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
            double totalScoreFromRules = request.ExtractionRules.Sum(r => r.Count * r.ScorePerQuestion);
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
                extractedQuestions = [.. extractedQuestions.OrderBy(x => _random.Next())];
            }

            // 重新设置排序顺序
            for (int i = 0; i < extractedQuestions.Count; i++)
            {
                extractedQuestions[i].SortOrder = i + 1;
            }

            // 创建或获取默认配置
            MockExamConfiguration configuration = await GetOrCreateDefaultConfigurationAsync(request, studentUserId);

            // 创建模拟考试实例
            DateTime now = DateTime.Now; // 使用本地时间
            MockExam mockExam = new()
            {
                ConfigurationId = configuration.Id,
                StudentId = studentUserId,
                Name = request.Name,
                Description = request.Description,
                DurationMinutes = request.DurationMinutes,
                TotalScore = request.TotalScore,
                PassingScore = request.PassingScore,
                RandomizeQuestions = request.RandomizeQuestions,
                ExtractedQuestions = JsonSerializer.Serialize(extractedQuestions, JsonOptions),
                Status = "Created",
                CreatedAt = now,
                ExpiresAt = now.AddDays(7) // 7天后过期
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
    public async Task<MockExamSubmissionResponseDto> SubmitMockExamAsync(int mockExamId, int studentUserId, int? clientActualDurationSeconds = null)
    {
        // 使用执行策略来处理事务，避免与MySQL重试策略冲突
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("开始提交模拟考试，模拟考试ID: {MockExamId}, 学生ID: {StudentId}",
                    mockExamId, studentUserId);

                // 检查权限
                _logger.LogInformation("开始权限验证，模拟考试ID: {MockExamId}, 学生ID: {StudentId}",
                    mockExamId, studentUserId);

                bool hasAccess = await HasAccessToMockExamAsync(mockExamId, studentUserId);

                if (!hasAccess)
                {
                    _logger.LogWarning("权限验证失败，拒绝提交模拟考试，模拟考试ID: {MockExamId}, 学生ID: {StudentId}",
                        mockExamId, studentUserId);

                    return new MockExamSubmissionResponseDto
                    {
                        Success = false,
                        Message = "无权限访问该模拟考试",
                        Status = "Unauthorized",
                        TimeStatusDescription = "权限验证失败"
                    };
                }

                _logger.LogInformation("权限验证通过，继续提交流程，模拟考试ID: {MockExamId}, 学生ID: {StudentId}",
                    mockExamId, studentUserId);

            MockExam? mockExam = await _context.MockExams
                .FirstOrDefaultAsync(me => me.Id == mockExamId && me.StudentId == studentUserId);

            if (mockExam == null)
            {
                return new MockExamSubmissionResponseDto
                {
                    Success = false,
                    Message = "模拟考试不存在",
                    Status = "NotFound",
                    TimeStatusDescription = "考试记录未找到"
                };
            }

            if (mockExam.Status != "InProgress")
            {
                return new MockExamSubmissionResponseDto
                {
                    Success = false,
                    Message = "考试状态不正确，无法提交",
                    Status = mockExam.Status,
                    StartedAt = mockExam.StartedAt,
                    CompletedAt = mockExam.CompletedAt,
                    TimeStatusDescription = $"考试当前状态为: {mockExam.Status}"
                };
            }

            DateTime now = DateTime.Now; // 使用本地时间

            // 使用锁定查询防止并发问题，创建或更新完成记录
            MockExamCompletion? existingCompletion = await _context.MockExamCompletions
                .Where(mec => mec.MockExamId == mockExamId && mec.StudentUserId == studentUserId)
                .FirstOrDefaultAsync();

            // 计算用时（秒）- 优先使用客户端传递的时间，避免网络延迟影响
            int? durationSeconds = null;
            string timeCalculationMethod = "";

            if (clientActualDurationSeconds.HasValue && clientActualDurationSeconds.Value > 0)
            {
                // 优先使用客户端传递的实际用时
                durationSeconds = clientActualDurationSeconds.Value;
                timeCalculationMethod = "客户端传递";

                _logger.LogInformation("使用客户端传递的用时 - 客户端用时: {ClientDuration}秒",
                    durationSeconds);
            }
            else if (mockExam.StartedAt.HasValue)
            {
                // 备用方案：服务端计算，使用四舍五入减少差异
                TimeSpan duration = now - mockExam.StartedAt.Value;
                durationSeconds = (int)Math.Round(duration.TotalSeconds);
                timeCalculationMethod = "服务端计算";

                _logger.LogInformation("服务端时间计算 - 开始时间: {StartTime}, 结束时间: {EndTime}, 精确用时: {ExactDuration}秒, 四舍五入后: {RoundedDuration}秒",
                    mockExam.StartedAt.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    duration.TotalSeconds,
                    durationSeconds);
            }

            _logger.LogInformation("时间计算方式: {Method}, 最终用时: {Duration}秒",
                timeCalculationMethod, durationSeconds);

            if (existingCompletion == null)
            {
                MockExamCompletion newCompletion = new()
                {
                    StudentUserId = studentUserId,
                    MockExamId = mockExamId,
                    Status = MockExamCompletionStatus.Completed,
                    StartedAt = mockExam.StartedAt ?? now,
                    CompletedAt = now,
                    Score = 0, // 设置默认分数，确保排行榜查询不会过滤掉这条记录
                    MaxScore = 100, // 设置默认最大分数
                    DurationSeconds = durationSeconds, // 设置用时（秒）
                    Notes = "基本提交（无BenchSuite评分）",
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsActive = true
                };

                _context.MockExamCompletions.Add(newCompletion);
                _logger.LogInformation("创建基本的模拟考试完成记录，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 用时: {Duration}秒, 默认分数: 0/100",
                    studentUserId, mockExamId, durationSeconds);
            }
            else
            {
                // 智能合并逻辑：保留最佳记录
                bool shouldUpdate = false;
                string updateReason = "";

                // 如果现有记录未完成，直接更新
                if (existingCompletion.Status != MockExamCompletionStatus.Completed)
                {
                    shouldUpdate = true;
                    updateReason = "状态更新为已完成";
                }
                // 如果现有记录已完成，比较用时（保留最短用时）
                else if (existingCompletion.DurationSeconds.HasValue && durationSeconds < existingCompletion.DurationSeconds.Value)
                {
                    shouldUpdate = true;
                    updateReason = $"更新为更短用时（{durationSeconds}秒 < {existingCompletion.DurationSeconds}秒）";
                }
                // 如果现有记录没有用时，更新用时
                else if (!existingCompletion.DurationSeconds.HasValue)
                {
                    shouldUpdate = true;
                    updateReason = "添加用时信息";
                }

                if (shouldUpdate)
                {
                    existingCompletion.Status = MockExamCompletionStatus.Completed;
                    existingCompletion.CompletedAt = now;
                    existingCompletion.DurationSeconds = durationSeconds;
                    existingCompletion.UpdatedAt = now;

                    // 如果没有分数，设置默认分数
                    if (!existingCompletion.Score.HasValue)
                    {
                        existingCompletion.Score = 0;
                        existingCompletion.MaxScore = 100;
                        existingCompletion.Notes = "基本提交（无BenchSuite评分）";
                    }

                    _logger.LogInformation("智能更新模拟考试完成记录，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 原因: {Reason}, 用时: {Duration}秒, 分数: {Score}/{MaxScore}",
                        studentUserId, mockExamId, updateReason, durationSeconds, existingCompletion.Score, existingCompletion.MaxScore);
                }
                else
                {
                    _logger.LogInformation("保留现有模拟考试完成记录，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 现有用时: {ExistingDuration}秒, 新用时: {NewDuration}秒",
                        studentUserId, mockExamId, existingCompletion.DurationSeconds, durationSeconds);
                }
            }

            // 更新模拟考试状态
            mockExam.Status = "Completed";
            mockExam.CompletedAt = now;

            // 保存所有更改
            await _context.SaveChangesAsync();

            // 提交事务
            await transaction.CommitAsync();

            // 计算实际用时
            int? actualDurationMinutes = null;
            string timeStatusDescription = "考试已成功提交";

            if (mockExam.StartedAt.HasValue)
            {
                TimeSpan actualDuration = now - mockExam.StartedAt.Value;
                actualDurationMinutes = (int)Math.Ceiling(actualDuration.TotalMinutes);

                // 格式化用时显示
                if (actualDurationMinutes >= 60)
                {
                    int hours = actualDurationMinutes.Value / 60;
                    int minutes = actualDurationMinutes.Value % 60;
                    timeStatusDescription = $"考试用时: {hours}小时{minutes}分钟";
                }
                else
                {
                    timeStatusDescription = $"考试用时: {actualDurationMinutes}分钟";
                }

                _logger.LogInformation("计算考试用时 - 开始时间: {StartTime}, 结束时间: {EndTime}, 用时: {Duration}分钟",
                    mockExam.StartedAt.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    now.ToString("yyyy-MM-dd HH:mm:ss"),
                    actualDurationMinutes);
            }
            else
            {
                timeStatusDescription = "考试已提交（开始时间未记录）";
                _logger.LogWarning("模拟考试StartedAt为空，无法计算用时，模拟考试ID: {MockExamId}", mockExamId);
            }

            _logger.LogInformation("模拟考试提交成功，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 用时: {Duration}分钟",
                studentUserId, mockExamId, actualDurationMinutes);

                // 提交事务
                await transaction.CommitAsync();

                return new MockExamSubmissionResponseDto
                {
                    Success = true,
                    Message = "模拟考试已成功提交",
                    StartedAt = mockExam.StartedAt,
                    CompletedAt = now,
                    ActualDurationMinutes = actualDurationMinutes,
                    Status = "Completed",
                    TimeStatusDescription = timeStatusDescription
                };
            }
            catch (Exception ex)
            {
                // 回滚事务
                await transaction.RollbackAsync();

                _logger.LogError(ex, "提交模拟考试异常，模拟考试ID: {MockExamId}, 学生ID: {StudentId}",
                    mockExamId, studentUserId);

                return new MockExamSubmissionResponseDto
                {
                    Success = false,
                    Message = "提交模拟考试时发生异常",
                    Status = "Error",
                    TimeStatusDescription = "服务器处理异常"
                };
            }
        });
    }

    /// <summary>
    /// 提交模拟考试成绩
    /// </summary>
    public async Task<bool> SubmitMockExamScoreAsync(int mockExamId, int studentUserId, SubmitMockExamScoreRequestDto scoreRequest)
    {
        // 使用执行策略来处理事务，避免与MySQL重试策略冲突
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
            _logger.LogInformation("开始提交模拟考试成绩，模拟考试ID: {MockExamId}, 学生ID: {StudentId}",
                mockExamId, studentUserId);

            // 检查权限
            if (!await HasAccessToMockExamAsync(mockExamId, studentUserId))
            {
                _logger.LogWarning("学生无权限访问模拟考试，学生ID: {StudentId}, 模拟考试ID: {MockExamId}",
                    studentUserId, mockExamId);
                return false;
            }

            // 检查模拟考试是否存在且状态正确
            MockExam? mockExam = await _context.MockExams
                .FirstOrDefaultAsync(me => me.Id == mockExamId && me.StudentId == studentUserId);

            if (mockExam == null)
            {
                _logger.LogWarning("模拟考试不存在，学生ID: {StudentId}, 模拟考试ID: {MockExamId}",
                    studentUserId, mockExamId);
                return false;
            }

            if (mockExam.Status != "InProgress")
            {
                _logger.LogWarning("模拟考试状态不正确，当前状态: {Status}, 学生ID: {StudentId}, 模拟考试ID: {MockExamId}",
                    mockExam.Status, studentUserId, mockExamId);
                return false;
            }

            DateTime now = DateTime.UtcNow;

            // 检查是否已存在完成记录
            MockExamCompletion? existingCompletion = await _context.MockExamCompletions
                .FirstOrDefaultAsync(mec => mec.MockExamId == mockExamId && mec.StudentUserId == studentUserId);

            if (existingCompletion != null)
            {
                // 智能合并逻辑：保留最佳分数和最短用时
                bool shouldUpdate = false;
                string updateReason = "";

                // 如果现有记录未完成，直接更新
                if (existingCompletion.Status != MockExamCompletionStatus.Completed)
                {
                    shouldUpdate = true;
                    updateReason = "状态更新为已完成";
                }
                // 如果新分数更高，更新记录
                else if (scoreRequest.Score.HasValue &&
                        (!existingCompletion.Score.HasValue || scoreRequest.Score.Value > existingCompletion.Score.Value))
                {
                    shouldUpdate = true;
                    updateReason = $"更新为更高分数（{scoreRequest.Score} > {existingCompletion.Score}）";
                }
                // 如果分数相同但用时更短，更新记录
                else if (scoreRequest.Score.HasValue && existingCompletion.Score.HasValue &&
                        scoreRequest.Score.Value == existingCompletion.Score.Value &&
                        scoreRequest.DurationSeconds.HasValue && existingCompletion.DurationSeconds.HasValue &&
                        scoreRequest.DurationSeconds.Value < existingCompletion.DurationSeconds.Value)
                {
                    shouldUpdate = true;
                    updateReason = $"相同分数但更短用时（{scoreRequest.DurationSeconds}秒 < {existingCompletion.DurationSeconds}秒）";
                }
                // 如果现有记录没有BenchSuite评分结果，但新记录有，则更新
                else if (string.IsNullOrEmpty(existingCompletion.BenchSuiteScoringResult) &&
                        !string.IsNullOrEmpty(scoreRequest.BenchSuiteScoringResult))
                {
                    shouldUpdate = true;
                    updateReason = "添加BenchSuite评分结果";
                }

                if (shouldUpdate)
                {
                    existingCompletion.Status = MockExamCompletionStatus.Completed;
                    existingCompletion.CompletedAt = now;
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

                    _logger.LogInformation("智能更新模拟考试完成记录，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 原因: {Reason}, 分数: {Score}/{MaxScore}",
                        studentUserId, mockExamId, updateReason, scoreRequest.Score, scoreRequest.MaxScore);
                }
                else
                {
                    _logger.LogInformation("保留现有模拟考试完成记录，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 现有分数: {ExistingScore}, 新分数: {NewScore}",
                        studentUserId, mockExamId, existingCompletion.Score, scoreRequest.Score);
                }
            }
            else
            {
                // 创建新的完成记录
                MockExamCompletion newCompletion = new()
                {
                    StudentUserId = studentUserId,
                    MockExamId = mockExamId,
                    Status = MockExamCompletionStatus.Completed,
                    StartedAt = mockExam.StartedAt ?? now,
                    CompletedAt = now,
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

                _context.MockExamCompletions.Add(newCompletion);
                _logger.LogInformation("创建新的模拟考试完成记录，学生ID: {StudentId}, 模拟考试ID: {MockExamId}",
                    studentUserId, mockExamId);
            }

            // 更新模拟考试状态
            mockExam.Status = "Completed";
            mockExam.CompletedAt = now;

            // 保存所有更改
            await _context.SaveChangesAsync();

            // 提交事务
            await transaction.CommitAsync();

                _logger.LogInformation("模拟考试成绩提交成功，学生ID: {StudentId}, 模拟考试ID: {MockExamId}, 得分: {Score}/{MaxScore}",
                    studentUserId, mockExamId, scoreRequest.Score, scoreRequest.MaxScore);

                return true;
            }
            catch (Exception ex)
            {
                // 回滚事务
                await transaction.RollbackAsync();

                _logger.LogError(ex, "提交模拟考试成绩异常，模拟考试ID: {MockExamId}, 学生ID: {StudentId}",
                    mockExamId, studentUserId);
                return false;
            }
        });
    }

    /// <summary>
    /// 获取学生模拟考试成绩列表
    /// </summary>
    public async Task<List<MockExamCompletionDto>> GetMockExamCompletionsAsync(int studentUserId, int pageNumber = 1, int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("开始获取学生模拟考试成绩列表，学生ID: {StudentId}, 页码: {PageNumber}, 页大小: {PageSize}",
                studentUserId, pageNumber, pageSize);

            List<MockExamCompletion> completions = await _context.MockExamCompletions
                .Include(mec => mec.MockExam)
                .Where(mec => mec.StudentUserId == studentUserId && mec.IsActive)
                .OrderByDescending(mec => mec.CompletedAt ?? mec.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            List<MockExamCompletionDto> result = [];
            foreach (MockExamCompletion completion in completions)
            {
                MockExamCompletionDto dto = new()
                {
                    Id = completion.Id,
                    MockExamId = completion.MockExamId,
                    MockExamName = completion.MockExam?.Name ?? "未知考试",
                    MockExamDescription = completion.MockExam?.Description,
                    MockExamTotalScore = (int)Math.Round(completion.MockExam?.TotalScore ?? 0),
                    Status = completion.Status,
                    StartedAt = completion.StartedAt,
                    CompletedAt = completion.CompletedAt,
                    Score = completion.Score,
                    MaxScore = completion.MaxScore,
                    CompletionPercentage = completion.CompletionPercentage,
                    DurationSeconds = completion.DurationSeconds,
                    Notes = completion.Notes,
                    CreatedAt = completion.CreatedAt
                };

                // 计算是否及格
                if (completion.Score.HasValue && completion.MockExam?.PassingScore != null)
                {
                    dto.IsPassed = completion.Score.Value >= completion.MockExam.PassingScore;
                }

                // 格式化完成时间
                if (completion.CompletedAt.HasValue)
                {
                    dto.FormattedCompletedAt = completion.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm");
                }

                // 格式化得分文本
                if (completion.Score.HasValue && completion.MaxScore.HasValue)
                {
                    dto.ScoreText = $"{completion.Score:F1}/{completion.MaxScore:F1}";
                }
                else if (completion.Score.HasValue)
                {
                    dto.ScoreText = $"{completion.Score:F1}";
                }
                else
                {
                    dto.ScoreText = "未评分";
                }

                // 计算和格式化用时文本
                int? calculatedDurationSeconds = completion.DurationSeconds;

                // 如果DurationSeconds为null，但有开始和完成时间，则动态计算
                if (!calculatedDurationSeconds.HasValue &&
                    completion.StartedAt.HasValue &&
                    completion.CompletedAt.HasValue)
                {
                    TimeSpan duration = completion.CompletedAt.Value - completion.StartedAt.Value;
                    calculatedDurationSeconds = (int)Math.Ceiling(duration.TotalSeconds);
                    dto.DurationSeconds = calculatedDurationSeconds; // 更新DTO中的值
                }

                if (calculatedDurationSeconds.HasValue)
                {
                    int totalSeconds = calculatedDurationSeconds.Value;
                    int hours = totalSeconds / 3600;
                    int minutes = (totalSeconds % 3600) / 60;
                    int seconds = totalSeconds % 60;

                    if (hours > 0)
                    {
                        dto.DurationText = minutes > 0 ? $"{hours}小时{minutes}分钟" : $"{hours}小时";
                    }
                    else if (minutes > 0)
                    {
                        dto.DurationText = seconds > 0 ? $"{minutes}分{seconds}秒" : $"{minutes}分钟";
                    }
                    else
                    {
                        dto.DurationText = $"{seconds}秒";
                    }
                }
                else
                {
                    dto.DurationText = "用时未知";
                }

                result.Add(dto);
            }

            _logger.LogInformation("获取学生模拟考试成绩列表成功，学生ID: {StudentId}, 返回数量: {Count}",
                studentUserId, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生模拟考试成绩列表失败，学生ID: {StudentId}",
                studentUserId);
            return [];
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
    /// 获取学生已完成的模拟考试数量
    /// </summary>
    public async Task<int> GetCompletedMockExamCountAsync(int studentUserId)
    {
        try
        {
            int count = await _context.MockExams
                .CountAsync(me => me.StudentId == studentUserId && me.Status == "Completed");

            _logger.LogInformation("获取学生已完成模拟考试数量成功，学生ID: {StudentId}, 已完成数量: {Count}",
                studentUserId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学生已完成模拟考试数量失败，学生ID: {StudentId}", studentUserId);
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
            _logger.LogInformation("开始检查模拟考试访问权限，学生ID: {StudentId}, 模拟考试ID: {MockExamId}",
                studentUserId, mockExamId);

            // 验证学生用户存在且为学生角色
            Models.User? student = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == studentUserId && u.Role == Models.UserRole.Student && u.IsActive);

            if (student == null)
            {
                _logger.LogWarning("权限验证失败：用户不存在或不是活跃学生，用户ID: {UserId}", studentUserId);

                // 进一步检查用户是否存在
                Models.User? anyUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentUserId);
                if (anyUser == null)
                {
                    _logger.LogWarning("用户完全不存在，用户ID: {UserId}", studentUserId);
                }
                else
                {
                    _logger.LogWarning("用户存在但条件不符，用户ID: {UserId}, 角色: {Role}, 活跃状态: {IsActive}",
                        studentUserId, anyUser.Role, anyUser.IsActive);
                }

                return false;
            }

            _logger.LogInformation("用户验证通过，用户ID: {UserId}, 用户名: {Username}, 角色: {Role}",
                student.Id, student.Username, student.Role);

            // 验证模拟考试存在且属于该学生
            MockExam? mockExam = await _context.MockExams
                .FirstOrDefaultAsync(me => me.Id == mockExamId);

            if (mockExam == null)
            {
                _logger.LogWarning("权限验证失败：模拟考试不存在，模拟考试ID: {MockExamId}", mockExamId);
                return false;
            }

            _logger.LogInformation("模拟考试存在，模拟考试ID: {MockExamId}, 所属学生ID: {StudentId}, 状态: {Status}, 创建时间: {CreatedAt}",
                mockExam.Id, mockExam.StudentId, mockExam.Status, mockExam.CreatedAt);

            bool hasAccess = mockExam.StudentId == studentUserId;

            if (hasAccess)
            {
                _logger.LogInformation("权限验证成功：学生有权限访问模拟考试，学生ID: {StudentId}, 模拟考试ID: {MockExamId}",
                    studentUserId, mockExamId);
            }
            else
            {
                _logger.LogWarning("权限验证失败：模拟考试不属于该学生，请求学生ID: {RequestStudentId}, 模拟考试所属学生ID: {MockExamStudentId}, 模拟考试ID: {MockExamId}",
                    studentUserId, mockExam.StudentId, mockExamId);
            }

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
            List<ImportedComprehensiveTrainingQuestion> selectedQuestions = [.. availableQuestions
                .OrderBy(x => _random.Next())
                .Take(rule.Count)];

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
                    CSharpQuestionType = question.CSharpQuestionType,
                    CodeFilePath = question.CodeFilePath,
                    CSharpDirectScore = question.CSharpDirectScore,
                    CodeBlanks = question.CodeBlanks,
                    TemplateCode = question.TemplateCode,
                    DocumentFilePath = question.DocumentFilePath,
                    OperationPoints = [.. question.OperationPoints.Select(op => new ExtractedOperationPointInfo
                    {
                        Id = op.Id,
                        Name = op.Name,
                        Description = op.Description,
                        ModuleType = op.ModuleType,
                        Score = (int)op.Score,
                        Order = op.Order,
                        Parameters = [.. op.Parameters.Select(p => new ExtractedParameterInfo
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Description = p.Description,
                            ParameterType = p.Type,
                            DefaultValue = p.DefaultValue?.ToString() ?? string.Empty,
                            Value = p.Value ?? string.Empty,
                            MinValue = p.MinValue?.ToString() ?? string.Empty,
                            MaxValue = p.MaxValue?.ToString() ?? string.Empty
                        })]
                    })]
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
            List<ImportedComprehensiveTrainingQuestion> selectedQuestions = [.. availableQuestions
                .OrderBy(x => _random.Next())
                .Take(count)];

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
                    ProgramInput = question.ProgramInput,
                    ExpectedOutput = question.ExpectedOutput,
                    CodeFilePath = question.CodeFilePath,
                    DocumentFilePath = question.DocumentFilePath,
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
            Questions = [.. questions.Select(q => new StudentMockExamQuestionDto
            {
                OriginalQuestionId = q.OriginalQuestionId,
                Title = q.Title,
                Content = q.Content,
                Score = q.Score,
                SortOrder = q.SortOrder,
                QuestionConfig = q.QuestionConfig,
                AnswerValidationRules = q.AnswerValidationRules,
                Tags = q.Tags,
                Remarks = q.Remarks,
                ProgramInput = q.ProgramInput,
                ExpectedOutput = q.ExpectedOutput,
                CSharpQuestionType = q.CSharpQuestionType,
                CodeFilePath = q.CodeFilePath,
                CSharpDirectScore = q.CSharpDirectScore,
                CodeBlanks = q.CodeBlanks,
                TemplateCode = q.TemplateCode,
                DocumentFilePath = q.DocumentFilePath,
                OperationPoints = [.. q.OperationPoints.Select(op => new StudentMockExamOperationPointDto
                {
                    Id = op.Id,
                    Name = op.Name,
                    Description = op.Description,
                    ModuleType = op.ModuleType,
                    Score = op.Score,
                    Order = op.Order,
                    Parameters = [.. op.Parameters.Select(p => new StudentMockExamParameterDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        ParameterType = p.ParameterType,
                        DefaultValue = p.DefaultValue,
                        Value = p.Value,
                        MinValue = p.MinValue,
                        MaxValue = p.MaxValue
                    })]
                })]
            })]
        };
    }

    /// <summary>
    /// 获取或创建默认的模拟考试配置
    /// </summary>
    private async Task<MockExamConfiguration> GetOrCreateDefaultConfigurationAsync(CreateMockExamRequestDto request, int createdBy)
    {
        try
        {
            // 查找是否已存在相同的默认配置
            string extractionRulesJson = JsonSerializer.Serialize(request.ExtractionRules, JsonOptions);

            MockExamConfiguration? existingConfig = await _context.MockExamConfigurations
                .FirstOrDefaultAsync(c =>
                    c.Name == request.Name &&
                    c.DurationMinutes == request.DurationMinutes &&
                    c.TotalScore == request.TotalScore &&
                    c.PassingScore == request.PassingScore &&
                    c.RandomizeQuestions == request.RandomizeQuestions &&
                    c.IsEnabled);

            if (existingConfig != null)
            {
                _logger.LogInformation("使用现有的模拟考试配置，配置ID: {ConfigurationId}", existingConfig.Id);
                return existingConfig;
            }

            // 创建新的配置
            MockExamConfiguration newConfig = new()
            {
                Name = request.Name,
                Description = request.Description,
                DurationMinutes = request.DurationMinutes,
                TotalScore = request.TotalScore,
                PassingScore = request.PassingScore,
                RandomizeQuestions = request.RandomizeQuestions,
                ExtractionRules = extractionRulesJson,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsEnabled = true
            };

            _context.MockExamConfigurations.Add(newConfig);
            await _context.SaveChangesAsync();

            _logger.LogInformation("创建新的模拟考试配置，配置ID: {ConfigurationId}", newConfig.Id);
            return newConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取或创建默认配置失败");
            throw;
        }
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

    /// <summary>
    /// 将MockExam和题目信息映射为MockExamComprehensiveTrainingDto（匹配ImportedComprehensiveTraining结构）
    /// </summary>
    private async Task<MockExamComprehensiveTrainingDto> MapToMockExamComprehensiveTrainingDtoAsync(MockExam mockExam, List<ExtractedQuestionInfo> extractedQuestions)
    {
        try
        {
            // 获取相关的ImportedComprehensiveTraining数据
            List<int> comprehensiveTrainingIds = [.. extractedQuestions
                .Select(q => q.ComprehensiveTrainingId)
                .Distinct()];

            // 查询相关的综合训练数据
            List<Models.ImportedComprehensiveTraining.ImportedComprehensiveTraining> comprehensiveTrainings =
                await _context.ImportedComprehensiveTrainings
                    .Include(ct => ct.Subjects)
                        .ThenInclude(s => s.Questions)
                            .ThenInclude(q => q.OperationPoints)
                                .ThenInclude(op => op.Parameters)
                    .Include(ct => ct.Modules)
                        .ThenInclude(m => m.Questions)
                            .ThenInclude(q => q.OperationPoints)
                                .ThenInclude(op => op.Parameters)
                    .Where(ct => comprehensiveTrainingIds.Contains(ct.Id))
                    .ToListAsync();

            // 使用第一个综合训练作为基础模板（如果有多个，合并它们）
            Models.ImportedComprehensiveTraining.ImportedComprehensiveTraining? baseTraining = comprehensiveTrainings.FirstOrDefault();

            if (baseTraining == null)
            {
                // 如果没有找到基础训练，创建一个默认的结构
                return CreateDefaultMockExamComprehensiveTrainingDto(mockExam, extractedQuestions);
            }

            // 创建主DTO
            MockExamComprehensiveTrainingDto dto = new()
            {
                Id = mockExam.Id, // 使用MockExam的ID
                OriginalComprehensiveTrainingId = $"MockExam_{mockExam.Id}",
                Name = mockExam.Name,
                Description = mockExam.Description,
                ComprehensiveTrainingType = "MockExam",
                Status = mockExam.Status,
                TotalScore = mockExam.TotalScore,
                DurationMinutes = mockExam.DurationMinutes,
                StartTime = mockExam.StartedAt,
                EndTime = mockExam.CompletedAt,
                AllowRetake = false,
                MaxRetakeCount = 0,
                PassingScore = mockExam.PassingScore,
                RandomizeQuestions = mockExam.RandomizeQuestions,
                ShowScore = true,
                ShowAnswers = false,
                IsEnabled = true,
                EnableTrial = true,
                Tags = null,
                ExtendedConfig = null,
                ImportedBy = mockExam.StudentId,
                ImportedAt = mockExam.CreatedAt,
                OriginalCreatedBy = mockExam.StudentId,
                OriginalCreatedAt = mockExam.CreatedAt,
                OriginalUpdatedAt = null,
                OriginalPublishedAt = mockExam.StartedAt,
                OriginalPublishedBy = mockExam.StudentId,
                ImportFileName = $"MockExam_{mockExam.Id}.json",
                ImportFileSize = 0,
                ImportVersion = "1.0",
                ImportStatus = "Success",
                ImportErrorMessage = null
            };

            // 按模块和科目组织题目
            OrganizeQuestionsIntoModulesAndSubjects(dto, extractedQuestions, comprehensiveTrainings);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "映射MockExamComprehensiveTrainingDto失败");
            return CreateDefaultMockExamComprehensiveTrainingDto(mockExam, extractedQuestions);
        }
    }

    /// <summary>
    /// 创建默认的MockExamComprehensiveTrainingDto
    /// </summary>
    private static MockExamComprehensiveTrainingDto CreateDefaultMockExamComprehensiveTrainingDto(MockExam mockExam, List<ExtractedQuestionInfo> extractedQuestions)
    {
        return new MockExamComprehensiveTrainingDto
        {
            Id = mockExam.Id,
            OriginalComprehensiveTrainingId = $"MockExam_{mockExam.Id}",
            Name = mockExam.Name,
            Description = mockExam.Description,
            ComprehensiveTrainingType = "MockExam",
            Status = mockExam.Status,
            TotalScore = mockExam.TotalScore,
            DurationMinutes = mockExam.DurationMinutes,
            StartTime = mockExam.StartedAt,
            EndTime = mockExam.CompletedAt,
            AllowRetake = false,
            MaxRetakeCount = 0,
            PassingScore = mockExam.PassingScore,
            RandomizeQuestions = mockExam.RandomizeQuestions,
            ShowScore = true,
            ShowAnswers = false,
            IsEnabled = true,
            EnableTrial = true,
            Tags = null,
            ExtendedConfig = null,
            ImportedBy = mockExam.StudentId,
            ImportedAt = mockExam.CreatedAt,
            OriginalCreatedBy = mockExam.StudentId,
            OriginalCreatedAt = mockExam.CreatedAt,
            OriginalUpdatedAt = null,
            OriginalPublishedAt = mockExam.StartedAt,
            OriginalPublishedBy = mockExam.StudentId,
            ImportFileName = $"MockExam_{mockExam.Id}.json",
            ImportFileSize = 0,
            ImportVersion = "1.0",
            ImportStatus = "Success",
            ImportErrorMessage = null,
            Subjects = [],
            Modules = [],
            Questions = extractedQuestions.Select(eq => new MockExamQuestionDto
            {
                Id = eq.OriginalQuestionId,
                OriginalQuestionId = eq.OriginalQuestionId.ToString(),
                ComprehensiveTrainingId = eq.ComprehensiveTrainingId,
                SubjectId = eq.SubjectId,
                ModuleId = eq.ModuleId,
                Title = eq.Title,
                Content = eq.Content,
                Score = eq.Score,
                SortOrder = eq.SortOrder,
                IsRequired = true,
                IsEnabled = true,
                QuestionConfig = eq.QuestionConfig,
                AnswerValidationRules = eq.AnswerValidationRules,
                StandardAnswer = null,
                ScoringRules = null,
                Tags = eq.Tags,
                Remarks = eq.Remarks,
                ProgramInput = null,
                ExpectedOutput = null,
                OriginalCreatedAt = DateTime.UtcNow,
                OriginalUpdatedAt = null,
                ImportedAt = DateTime.UtcNow,
                OperationPoints = []
            }).ToList()
        };
    }

    /// <summary>
    /// 按模块和科目组织题目，构建正确的层次结构
    /// </summary>
    private void OrganizeQuestionsIntoModulesAndSubjects(
        MockExamComprehensiveTrainingDto dto,
        List<ExtractedQuestionInfo> extractedQuestions,
        List<Models.ImportedComprehensiveTraining.ImportedComprehensiveTraining> comprehensiveTrainings)
    {
        try
        {
            // 参数空值检查
            if (extractedQuestions == null)
            {
                _logger.LogWarning("extractedQuestions参数为null，无法组织题目结构");
                dto.Questions = null;
                dto.Modules = [];
                dto.Subjects = [];
                return;
            }

            if (comprehensiveTrainings == null)
            {
                _logger.LogWarning("comprehensiveTrainings参数为null，使用默认配置");
                comprehensiveTrainings = [];
            }

            _logger.LogInformation("开始组织题目结构，共 {QuestionCount} 道题目", extractedQuestions.Count);

            // 详细记录题目的ModuleId信息
            foreach (ExtractedQuestionInfo question in extractedQuestions)
            {
                _logger.LogInformation("题目 {QuestionId}: ModuleId={ModuleId}, SubjectId={SubjectId}, Title={Title}",
                    question.OriginalQuestionId, question.ModuleId, question.SubjectId, question.Title);
            }

            // 收集所有可用的模块和科目信息
            List<ImportedComprehensiveTrainingModule> allModules = [];
            List<ImportedComprehensiveTrainingSubject> allSubjects = [];

            foreach (Models.ImportedComprehensiveTraining.ImportedComprehensiveTraining training in comprehensiveTrainings)
            {
                if (training == null)
                {
                    _logger.LogWarning("发现null的综合训练对象，跳过");
                    continue;
                }

                // 安全地添加模块和科目，处理可能的null集合
                if (training.Modules != null)
                {
                    allModules.AddRange(training.Modules);
                }

                if (training.Subjects != null)
                {
                    allSubjects.AddRange(training.Subjects);
                }

                _logger.LogInformation("综合训练 {TrainingName} 包含 {ModuleCount} 个模块，{SubjectCount} 个科目",
                    training.Name ?? "未知", training.Modules?.Count ?? 0, training.Subjects?.Count ?? 0);

                // 详细记录模块信息
                if (training.Modules != null)
                {
                    foreach (ImportedComprehensiveTrainingModule module in training.Modules)
                    {
                        if (module != null)
                        {
                            _logger.LogInformation("模块 {ModuleId}: {ModuleName} ({ModuleType})",
                                module.Id, module.Name ?? "未知", module.Type ?? "未知");
                        }
                    }
                }
            }

            _logger.LogInformation("总共找到 {ModuleCount} 个模块，{SubjectCount} 个科目",
                allModules.Count, allSubjects.Count);

            // 按模块ID分组题目并创建模块结构
            dto.Modules = CreateModuleStructure(extractedQuestions, allModules);

            // 按科目ID分组题目并创建科目结构
            dto.Subjects = CreateSubjectStructure(extractedQuestions, allSubjects);

            // 智能处理根级别Questions：在模块化场景下完全移除，避免数据重复
            if (dto.Modules.Any() || dto.Subjects.Any())
            {
                // 如果有模块或科目结构，检查是否有未分组的题目
                List<MockExamQuestionDto> ungroupedQuestions = GetUngroupedQuestions(extractedQuestions, dto.Modules, dto.Subjects);

                if (ungroupedQuestions.Any())
                {
                    // 如果有未分组的题目，保留在根级别
                    dto.Questions = ungroupedQuestions;
                    _logger.LogInformation("模块化结构已建立，根级别Questions包含未分组题目：{UngroupedCount} 道",
                        dto.Questions.Count);
                }
                else
                {
                    // 如果所有题目都已分组，设置为null以完全隐藏该字段
                    dto.Questions = null;
                    _logger.LogInformation("模块化结构已建立，所有题目已分组，根级别Questions字段已隐藏");
                }
            }
            else
            {
                // 如果没有模块或科目结构，保留所有题目在根级别（向后兼容）
                dto.Questions = MapQuestionsToDto(extractedQuestions);

                _logger.LogInformation("未找到模块化结构，所有题目保留在根级别：{TotalCount} 道",
                    dto.Questions?.Count ?? 0);
            }

            _logger.LogInformation("成功组织题目结构：{ModuleCount} 个模块，{SubjectCount} 个科目，{QuestionCount} 道题目",
                dto.Modules.Count, dto.Subjects.Count, dto.Questions?.Count ?? 0);

            // 详细日志记录模块信息
            foreach (MockExamModuleDto module in dto.Modules)
            {
                _logger.LogInformation("模块：{ModuleName} (类型: {ModuleType})，包含 {QuestionCount} 道题目，总分 {Score}",
                    module.Name, module.Type, module.Questions.Count, module.Score);
            }

            // 详细日志记录科目信息
            foreach (MockExamSubjectDto subject in dto.Subjects)
            {
                _logger.LogInformation("科目：{SubjectName} (类型: {SubjectType})，包含 {QuestionCount} 道题目，总分 {Score}",
                    subject.SubjectName, subject.SubjectType, subject.QuestionCount, subject.Score);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "组织题目结构失败");
            // 如果组织失败，至少保证有题目列表
            dto.Questions = MapQuestionsToDto(extractedQuestions);
            dto.Modules = [];
            dto.Subjects = [];
        }
    }

    /// <summary>
    /// 获取模块显示名称
    /// </summary>
    private static string GetModuleDisplayName(string moduleType)
    {
        return moduleType?.ToLower() switch
        {
            "csharp" or "c#" => "C#编程",
            "ppt" or "powerpoint" => "PowerPoint",
            "word" => "Word",
            "excel" => "Excel",
            "windows" => "Windows操作系统",
            "网络" or "network" => "计算机网络",
            "数据库" or "database" => "数据库",
            _ => moduleType ?? "未知模块"
        };
    }

    /// <summary>
    /// 将ExtractedQuestionInfo列表映射为MockExamQuestionDto列表
    /// </summary>
    private static List<MockExamQuestionDto> MapQuestionsToDto(List<ExtractedQuestionInfo> questions)
    {
        return questions.Select(eq => new MockExamQuestionDto
        {
            Id = eq.OriginalQuestionId,
            OriginalQuestionId = eq.OriginalQuestionId.ToString(),
            ComprehensiveTrainingId = eq.ComprehensiveTrainingId,
            SubjectId = eq.SubjectId,
            ModuleId = eq.ModuleId,
            Title = eq.Title,
            Content = eq.Content,
            Score = eq.Score,
            SortOrder = eq.SortOrder,
            IsRequired = true,
            IsEnabled = true,
            QuestionConfig = eq.QuestionConfig,
            AnswerValidationRules = eq.AnswerValidationRules,
            StandardAnswer = null,
            ScoringRules = null,
            Tags = eq.Tags,
            Remarks = eq.Remarks,
            ProgramInput = null,
            ExpectedOutput = null,
            OriginalCreatedAt = DateTime.UtcNow,
            OriginalUpdatedAt = null,
            ImportedAt = DateTime.UtcNow,
            OperationPoints = eq.OperationPoints?.Select(op => new MockExamOperationPointDto
            {
                Id = op.Id,
                OriginalOperationPointId = op.Id.ToString(),
                QuestionId = eq.OriginalQuestionId,
                Name = op.Name,
                Description = op.Description,
                ModuleType = op.ModuleType,
                Score = op.Score,
                Order = op.Order,
                IsEnabled = true,
                CreatedTime = DateTime.UtcNow.ToString(),
                ImportedAt = DateTime.UtcNow,
                Parameters = op.Parameters?.Select(p => new MockExamParameterDto
                {
                    Id = p.Id,
                    OperationPointId = op.Id,
                    Name = p.Name,
                    DisplayName = p.Name,
                    Description = p.Description,
                    Type = p.ParameterType ?? "string",
                    Value = !string.IsNullOrWhiteSpace(p.Value) ? p.Value : (p.DefaultValue ?? ""),
                    DefaultValue = p.DefaultValue ?? "",
                    IsRequired = false,
                    Order = 0,
                    EnumOptions = null,
                    ValidationRule = null,
                    ValidationErrorMessage = null,
                    MinValue = !string.IsNullOrEmpty(p.MinValue) && double.TryParse(p.MinValue, out double min) ? min : null,
                    MaxValue = !string.IsNullOrEmpty(p.MaxValue) && double.TryParse(p.MaxValue, out double max) ? max : null,
                    IsEnabled = true,
                    ImportedAt = DateTime.UtcNow
                }).ToList() ?? []
            }).ToList() ?? []
        }).ToList();
    }

    /// <summary>
    /// 创建模块结构
    /// </summary>
    private List<MockExamModuleDto> CreateModuleStructure(
        List<ExtractedQuestionInfo> extractedQuestions,
        List<ImportedComprehensiveTrainingModule> allModules)
    {
        List<MockExamModuleDto> modules = [];

        _logger.LogInformation("CreateModuleStructure开始：题目总数 {QuestionCount}，可用模块数 {ModuleCount}",
            extractedQuestions.Count, allModules.Count);

        // 检查题目的ModuleId分布
        var questionsWithModuleId = extractedQuestions.Where(q => q.ModuleId.HasValue).ToList();
        var questionsWithoutModuleId = extractedQuestions.Where(q => !q.ModuleId.HasValue).ToList();

        _logger.LogInformation("题目ModuleId分布：有ModuleId的题目 {WithModuleId} 道，无ModuleId的题目 {WithoutModuleId} 道",
            questionsWithModuleId.Count, questionsWithoutModuleId.Count);

        // 按模块ID分组题目
        var moduleQuestionGroups = extractedQuestions
            .Where(q => q.ModuleId.HasValue)
            .GroupBy(q => q.ModuleId!.Value)
            .ToList();

        _logger.LogInformation("找到 {GroupCount} 个模块分组", moduleQuestionGroups.Count);

        // 记录每个分组的详细信息
        foreach (var group in moduleQuestionGroups)
        {
            _logger.LogInformation("模块分组 {ModuleId}：包含 {QuestionCount} 道题目",
                group.Key, group.Count());
        }

        // 如果没有找到模块分组，但有题目，尝试强制创建默认模块
        if (moduleQuestionGroups.Count == 0 && extractedQuestions.Count > 0)
        {
            _logger.LogWarning("没有找到模块分组，但有 {QuestionCount} 道题目，尝试强制创建默认模块", extractedQuestions.Count);

            // 检查是否所有题目都没有ModuleId（重用之前的变量）
            if (questionsWithoutModuleId.Count == extractedQuestions.Count)
            {
                _logger.LogInformation("所有题目都没有ModuleId，创建默认PowerPoint模块");

                // 创建默认的PowerPoint模块
                MockExamModuleDto defaultModule = new()
                {
                    Id = 3, // 使用默认的PowerPoint模块ID
                    OriginalModuleId = "Default_PPT_Module",
                    Name = "PowerPoint演示文稿",
                    Type = "ppt",
                    Description = "PowerPoint演示文稿制作与编辑",
                    Score = extractedQuestions.Sum(q => q.Score),
                    Order = 1,
                    IsEnabled = true,
                    ImportedAt = DateTime.UtcNow,
                    Questions = MapQuestionsToDto(extractedQuestions)
                };

                modules.Add(defaultModule);
                _logger.LogInformation("已创建默认PowerPoint模块，包含 {QuestionCount} 道题目", extractedQuestions.Count);

                return modules;
            }
        }

        foreach (var group in moduleQuestionGroups)
        {
            int moduleId = group.Key;
            List<ExtractedQuestionInfo> moduleQuestions = [.. group];

            // 查找对应的模块信息
            ImportedComprehensiveTrainingModule? originalModule = allModules.FirstOrDefault(m => m.Id == moduleId);

            if (originalModule != null)
            {
                _logger.LogInformation("找到模块 {ModuleId}: {ModuleName} ({ModuleType})，包含 {QuestionCount} 道题目",
                    moduleId, originalModule.Name, originalModule.Type, moduleQuestions.Count);

                MockExamModuleDto moduleDto = new()
                {
                    Id = originalModule.Id,
                    OriginalModuleId = originalModule.OriginalModuleId,
                    Name = originalModule.Name,
                    Type = originalModule.Type,
                    Description = originalModule.Description,
                    Score = moduleQuestions.Sum(q => q.Score),
                    Order = originalModule.Order,
                    IsEnabled = originalModule.IsEnabled,
                    ImportedAt = originalModule.ImportedAt,
                    Questions = MapQuestionsToDto(moduleQuestions)
                };

                modules.Add(moduleDto);
            }
            else
            {
                _logger.LogWarning("未找到模块ID {ModuleId} 的信息，使用默认配置", moduleId);

                // 如果找不到原始模块信息，创建默认模块
                MockExamModuleDto defaultModuleDto = new()
                {
                    Id = moduleId,
                    OriginalModuleId = $"Module_{moduleId}",
                    Name = GetModuleDisplayName(GetDefaultModuleType(moduleId)),
                    Type = GetDefaultModuleType(moduleId),
                    Description = $"{GetModuleDisplayName(GetDefaultModuleType(moduleId))}模块考试",
                    Score = moduleQuestions.Sum(q => q.Score),
                    Order = moduleId,
                    IsEnabled = true,
                    ImportedAt = DateTime.UtcNow,
                    Questions = MapQuestionsToDto(moduleQuestions)
                };

                modules.Add(defaultModuleDto);
            }
        }

        return [.. modules.OrderBy(m => m.Order)];
    }

    /// <summary>
    /// 创建科目结构
    /// </summary>
    private List<MockExamSubjectDto> CreateSubjectStructure(
        List<ExtractedQuestionInfo> extractedQuestions,
        List<ImportedComprehensiveTrainingSubject> allSubjects)
    {
        List<MockExamSubjectDto> subjects = [];

        // 按科目ID分组题目
        var subjectQuestionGroups = extractedQuestions
            .Where(q => q.SubjectId.HasValue)
            .GroupBy(q => q.SubjectId!.Value)
            .ToList();

        _logger.LogInformation("找到 {GroupCount} 个科目分组", subjectQuestionGroups.Count);

        foreach (var group in subjectQuestionGroups)
        {
            int subjectId = group.Key;
            List<ExtractedQuestionInfo> subjectQuestions = [.. group];

            // 查找对应的科目信息
            ImportedComprehensiveTrainingSubject? originalSubject = allSubjects.FirstOrDefault(s => s.Id == subjectId);

            if (originalSubject != null)
            {
                _logger.LogInformation("找到科目 {SubjectId}: {SubjectName} ({SubjectType})，包含 {QuestionCount} 道题目",
                    subjectId, originalSubject.SubjectName, originalSubject.SubjectType, subjectQuestions.Count);

                MockExamSubjectDto subjectDto = new()
                {
                    Id = originalSubject.Id,
                    OriginalSubjectId = originalSubject.OriginalSubjectId.ToString(),
                    SubjectType = originalSubject.SubjectType,
                    SubjectName = originalSubject.SubjectName,
                    Description = originalSubject.Description,
                    Score = subjectQuestions.Sum(q => q.Score),
                    DurationMinutes = originalSubject.DurationMinutes,
                    SortOrder = originalSubject.SortOrder,
                    IsRequired = originalSubject.IsRequired,
                    IsEnabled = originalSubject.IsEnabled,
                    MinScore = originalSubject.MinScore ?? 0,
                    Weight = originalSubject.Weight,
                    SubjectConfig = originalSubject.SubjectConfig,
                    QuestionCount = subjectQuestions.Count,
                    ImportedAt = originalSubject.ImportedAt,
                    Questions = MapQuestionsToDto(subjectQuestions)
                };

                subjects.Add(subjectDto);
            }
            else
            {
                _logger.LogWarning("未找到科目ID {SubjectId} 的信息", subjectId);
            }
        }

        return [.. subjects.OrderBy(s => s.SortOrder)];
    }

    /// <summary>
    /// 获取未分组的题目（避免数据重复）
    /// </summary>
    private List<MockExamQuestionDto> GetUngroupedQuestions(
        List<ExtractedQuestionInfo> extractedQuestions,
        List<MockExamModuleDto> modules,
        List<MockExamSubjectDto> subjects)
    {
        // 收集所有已分组的题目ID
        HashSet<int> groupedQuestionIds = [];

        // 从模块中收集已分组的题目ID
        foreach (MockExamModuleDto module in modules)
        {
            foreach (MockExamQuestionDto question in module.Questions)
            {
                groupedQuestionIds.Add(question.Id);
            }
        }

        // 从科目中收集已分组的题目ID
        foreach (MockExamSubjectDto subject in subjects)
        {
            foreach (MockExamQuestionDto question in subject.Questions)
            {
                groupedQuestionIds.Add(question.Id);
            }
        }

        // 找出未分组的题目
        List<ExtractedQuestionInfo> ungroupedQuestions = [.. extractedQuestions.Where(q => !groupedQuestionIds.Contains(q.OriginalQuestionId))];

        _logger.LogInformation("题目分组统计：总题目 {TotalCount} 道，已分组 {GroupedCount} 道，未分组 {UngroupedCount} 道",
            extractedQuestions.Count, groupedQuestionIds.Count, ungroupedQuestions.Count);

        return MapQuestionsToDto(ungroupedQuestions);
    }

    /// <summary>
    /// 根据模块ID获取默认模块类型
    /// </summary>
    private static string GetDefaultModuleType(int moduleId)
    {
        return moduleId switch
        {
            3 => "ppt",        // 根据响应数据，moduleId=3是PPT模块
            1 => "word",       // Word模块
            2 => "excel",      // Excel模块
            4 => "csharp",     // C#模块
            5 => "windows",    // Windows模块
            _ => "unknown"
        };
    }

    /// <summary>
    /// 根据模块ID获取模块类型
    /// </summary>
    private static string GetModuleTypeById(int moduleId, List<Models.ImportedComprehensiveTraining.ImportedComprehensiveTraining> comprehensiveTrainings)
    {
        foreach (Models.ImportedComprehensiveTraining.ImportedComprehensiveTraining training in comprehensiveTrainings)
        {
            ImportedComprehensiveTrainingModule? module = training.Modules.FirstOrDefault(m => m.Id == moduleId);
            if (module != null)
            {
                return module.Type;
            }
        }

        // 如果找不到，返回基于ID的默认类型
        return moduleId switch
        {
            3 => "ppt",        // 根据响应数据，moduleId=3是PPT模块
            1 => "word",       // 假设的Word模块
            2 => "excel",      // 假设的Excel模块
            4 => "csharp",     // 假设的C#模块
            5 => "windows",    // 假设的Windows模块
            _ => "unknown"
        };
    }
}
