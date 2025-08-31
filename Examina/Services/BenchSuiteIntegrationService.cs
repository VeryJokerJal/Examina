using System.IO;
using System.Reflection;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services;
using Examina.Models;
using Examina.Models.Exam;
using Examina.Models.MockExam;
using Examina.Models.SpecializedTraining;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// BenchSuite评分系统集成服务实现
/// </summary>
public class BenchSuiteIntegrationService : IBenchSuiteIntegrationService
{
    private readonly ILogger<BenchSuiteIntegrationService> _logger;
    private readonly IBenchSuiteDirectoryService _directoryService;
    private readonly Dictionary<ModuleType, string> _directoryMapping;
    private readonly Dictionary<ModuleType, IScoringService> _scoringServices;
    private readonly IAILogicalScoringService? _aiScoringService;
    private readonly IStudentExamService? _studentExamService;
    private readonly IStudentMockExamService? _studentMockExamService;
    private readonly IStudentComprehensiveTrainingService? _studentComprehensiveTrainingService;
    private readonly IStudentSpecializedTrainingService? _studentSpecializedTrainingService;

    public BenchSuiteIntegrationService(
        ILogger<BenchSuiteIntegrationService> logger,
        IBenchSuiteDirectoryService directoryService,
        IAILogicalScoringService? aiScoringService = null,
        IStudentExamService? studentExamService = null,
        IStudentMockExamService? studentMockExamService = null,
        IStudentComprehensiveTrainingService? studentComprehensiveTrainingService = null,
        IStudentSpecializedTrainingService? studentSpecializedTrainingService = null)
    {
        // 添加调试信息来验证依赖注入
        System.Diagnostics.Debug.WriteLine($"[BenchSuiteIntegrationService] 构造函数被调用");
        System.Diagnostics.Debug.WriteLine($"[BenchSuiteIntegrationService] logger: {logger?.GetType().Name ?? "NULL"}");
        System.Diagnostics.Debug.WriteLine($"[BenchSuiteIntegrationService] aiScoringService: {aiScoringService?.GetType().Name ?? "NULL"}");
        System.Diagnostics.Debug.WriteLine($"[BenchSuiteIntegrationService] studentExamService: {studentExamService?.GetType().Name ?? "NULL"}");
        System.Diagnostics.Debug.WriteLine($"[BenchSuiteIntegrationService] studentMockExamService: {studentMockExamService?.GetType().Name ?? "NULL"}");
        System.Diagnostics.Debug.WriteLine($"[BenchSuiteIntegrationService] studentComprehensiveTrainingService: {studentComprehensiveTrainingService?.GetType().Name ?? "NULL"}");
        System.Diagnostics.Debug.WriteLine($"[BenchSuiteIntegrationService] studentSpecializedTrainingService: {studentSpecializedTrainingService?.GetType().Name ?? "NULL"}");

        _logger = logger;
        _directoryService = directoryService;
        _aiScoringService = aiScoringService;
        _studentExamService = studentExamService;
        _studentMockExamService = studentMockExamService;
        _studentComprehensiveTrainingService = studentComprehensiveTrainingService;
        _studentSpecializedTrainingService = studentSpecializedTrainingService;

        _directoryMapping = new Dictionary<ModuleType, string>
        {
            { ModuleType.CSharp, "CSharp" },
            { ModuleType.PowerPoint, "PPT" },
            { ModuleType.Word, "WORD" },
            { ModuleType.Excel, "EXCEL" },
            { ModuleType.Windows, "Windows" }
        };

        // 初始化真实的BenchSuite评分服务，使用OpenXML SDK实现，C#服务支持AI功能
        _scoringServices = new Dictionary<ModuleType, IScoringService>
        {
            { ModuleType.Word, new BenchSuite.Services.OpenXml.WordOpenXmlScoringService() },
            { ModuleType.Excel, new BenchSuite.Services.OpenXml.ExcelOpenXmlScoringService() },
            { ModuleType.PowerPoint, new BenchSuite.Services.OpenXml.PowerPointOpenXmlScoringService() },
            { ModuleType.Windows, new WindowsScoringService() },
            { ModuleType.CSharp, new CSharpScoringService(_aiScoringService) }
        };

        if (_aiScoringService != null)
        {
            _logger.LogInformation("BenchSuite集成服务已启用AI逻辑性判分功能");
        }
        else
        {
            _logger.LogInformation("BenchSuite集成服务使用传统判分模式（未启用AI功能）");
        }
    }

    /// <summary>
    /// 对考试文件进行评分
    /// </summary>
    public async Task<Dictionary<ModuleType, ScoringResult>> ScoreExamAsync(ExamType examType, int examId, int studentUserId, Dictionary<ModuleType, List<string>> filePaths)
    {
        Dictionary<ModuleType, ScoringResult> results = [];

        try
        {
            _logger.LogInformation("开始BenchSuite评分，考试ID: {ExamId}, 考试类型: {ExamType}, 学生ID: {StudentId}",
                examId, examType, studentUserId);

            // 验证考试目录结构
            bool validationResult = await ValidateExamDirectoryStructureAsync(examType, examId);
            if (!validationResult)
            {
                _logger.LogWarning("考试目录结构验证失败，但继续进行评分");
            }

            // 检查BenchSuite服务是否可用
            bool serviceAvailable = await IsServiceAvailableAsync();
            if (!serviceAvailable)
            {
                _logger.LogError("BenchSuite服务不可用");
                return results;
            }

            // 按模块类型进行评分
            foreach (KeyValuePair<ModuleType, List<string>> moduleGroup in filePaths)
            {
                ModuleType moduleType = moduleGroup.Key;
                List<string> moduleFilePaths = moduleGroup.Value;

                _logger.LogInformation("开始评分模块类型: {ModuleType}, 文件数量: {FileCount}",
                    moduleType, moduleFilePaths.Count);

                ScoringResult moduleResult = await ScoreModuleAsync(moduleType, moduleFilePaths, examType, examId, studentUserId);
                results[moduleType] = moduleResult;

                _logger.LogInformation("模块 {ModuleType} 评分完成，总分: {TotalScore}, 得分: {AchievedScore}",
                    moduleType, moduleResult.TotalScore, moduleResult.AchievedScore);
            }

            _logger.LogInformation("BenchSuite评分完成，共评分 {ModuleCount} 个模块", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BenchSuite评分过程中发生异常");
        }

        return results;
    }

    /// <summary>
    /// 检查BenchSuite服务是否可用
    /// </summary>
    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            // 检查BenchSuite程序集是否可用
            Assembly? benchSuiteAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name?.Contains("BenchSuite") == true);

            if (benchSuiteAssembly == null)
            {
                // 尝试加载BenchSuite程序集
                string benchSuitePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BenchSuite.dll");
                if (File.Exists(benchSuitePath))
                {
                    _ = Assembly.LoadFrom(benchSuitePath);
                    _logger.LogInformation("成功加载BenchSuite程序集");
                    return true;
                }
                else
                {
                    _logger.LogWarning("BenchSuite程序集不存在: {Path}", benchSuitePath);
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查BenchSuite服务可用性时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 获取支持的模块类型
    /// </summary>
    public IEnumerable<ModuleType> GetSupportedModuleTypes()
    {
        return _scoringServices.Keys;
    }

    /// <summary>
    /// 验证文件目录结构
    /// </summary>
    public async Task<bool> ValidateDirectoryStructureAsync(string basePath)
    {
        try
        {
            _logger.LogInformation("验证BenchSuite目录结构，基础路径: {BasePath}", basePath);

            // 检查基础目录是否存在
            if (!Directory.Exists(basePath))
            {
                _logger.LogWarning("基础目录不存在: {BasePath}", basePath);
                return false;
            }

            // 检查各子目录是否存在
            foreach (KeyValuePair<ModuleType, string> mapping in _directoryMapping)
            {
                string directoryPath = Path.Combine(basePath, mapping.Value);
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogWarning("缺失目录: {DirectoryPath}", directoryPath);
                    // 尝试创建缺失的目录
                    try
                    {
                        _ = Directory.CreateDirectory(directoryPath);
                        _logger.LogInformation("成功创建目录: {DirectoryPath}", directoryPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "创建目录失败: {DirectoryPath}", directoryPath);
                        return false;
                    }
                }
            }

            _logger.LogInformation("目录结构验证通过");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证目录结构时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 验证目录结构（简化版本）
    /// </summary>
    public async Task<bool> ValidateExamDirectoryStructureAsync(ExamType examType, int examId)
    {
        try
        {
            _logger.LogInformation("验证目录结构");

            // 直接验证基础目录结构
            return await ValidateDirectoryStructureAsync(_directoryService.GetBasePath());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证考试目录结构时发生异常");
            return false;
        }
    }

    #region 私有方法

    /// <summary>
    /// 对指定模块类型进行评分
    /// </summary>
    private async Task<ScoringResult> ScoreModuleAsync(ModuleType moduleType, List<string> filePaths, ExamType examType, int examId, int studentUserId)
    {
        ScoringResult result = new()
        {
            StartTime = DateTime.Now
        };

        try
        {
            // 检查是否有对应的评分服务
            if (!_scoringServices.TryGetValue(moduleType, out IScoringService? scoringService))
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"不支持的模块类型: {moduleType}";
                result.EndTime = DateTime.Now;
                return result;
            }

            // Windows 模块允许在无文件的情况下进行评分
            if (moduleType == ModuleType.Windows)
            {
                // 使用简化的模块目录路径
                string windowsModulePath = _directoryService.GetDirectoryPath(ModuleType.Windows);

                ExamModel examModelToUse = await CreateSimplifiedExamModel(moduleType, examType, examId, studentUserId);

                // 为Windows评分服务设置模块路径
                if (scoringService is WindowsScoringService windowsService)
                {
                    windowsService.SetBasePath(windowsModulePath);
                }

                // 调用评分（Windows不依赖具体文件路径）
                result = await scoringService.ScoreFileAsync(string.Empty, examModelToUse);
            }
            else
            {
                // 非Windows模块：需要文件
                if (filePaths == null || filePaths.Count == 0)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"没有找到 {moduleType} 类型的文件";
                    result.EndTime = DateTime.Now;
                    return result;
                }

                // 创建简化的考试模型用于评分
                ExamModel examModel = await CreateSimplifiedExamModel(moduleType, examType, examId, studentUserId);

                // 如果只有一个文件，直接评分
                if (filePaths.Count == 1)
                {
                    string filePath = filePaths[0];
                    if (!File.Exists(filePath))
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = $"文件不存在: {filePath}";
                        result.EndTime = DateTime.Now;
                        return result;
                    }

                    // 调用真实的BenchSuite评分服务
                    result = await scoringService.ScoreFileAsync(filePath, examModel);
                }
                else
                {
                    // 多个文件时，合并结果
                    double totalScore = 0;
                    double achievedScore = 0;
                    List<KnowledgePointResult> allKnowledgePoints = [];

                    foreach (string filePath in filePaths)
                    {
                        if (!File.Exists(filePath))
                        {
                            _logger.LogWarning("文件不存在: {FilePath}", filePath);
                            continue;
                        }

                        try
                        {
                            // 调用真实的BenchSuite评分服务
                            ScoringResult fileResult = await scoringService.ScoreFileAsync(filePath, examModel);

                            totalScore += fileResult.TotalScore;
                            achievedScore += fileResult.AchievedScore;
                            allKnowledgePoints.AddRange(fileResult.KnowledgePointResults);

                            if (!fileResult.IsSuccess)
                            {
                                _logger.LogWarning("文件评分警告: {FilePath}, {ErrorMessage}", filePath, fileResult.ErrorMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "文件评分失败: {FilePath}", filePath);
                        }
                    }

                    result.TotalScore = totalScore;
                    result.AchievedScore = achievedScore;
                    result.KnowledgePointResults = allKnowledgePoints;
                    result.IsSuccess = true;
                }
            }

            _logger.LogInformation("模块类型 {ModuleType} 评分完成，得分: {AchievedScore}/{TotalScore}",
                moduleType, result.AchievedScore, result.TotalScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "模块类型 {ModuleType} 评分失败", moduleType);
            result.IsSuccess = false;
            result.ErrorMessage = $"评分失败: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 创建简化的考试模型用于评分
    /// </summary>
    private async Task<ExamModel> CreateSimplifiedExamModel(ModuleType moduleType, ExamType examType, int examId, int studentUserId)
    {
        _logger.LogInformation("开始获取考试数据，考试类型: {ExamType}, 模块类型: {ModuleType}, 考试ID: {ExamId}, 学生ID: {StudentUserId}",
            examType, moduleType, examId, studentUserId);

        try
        {
            // 根据考试类型调用不同的API端点
            object? examData = await GetExamDataByTypeAsync(examType, examId, studentUserId);

            if (examData != null)
            {
                _logger.LogInformation("成功从API获取考试数据，考试类型: {ExamType}, 考试ID: {ExamId}", examType, examId);

                try
                {
                    return MapExamDataToExamModel(examData, examType, moduleType);
                }
                catch (Exception mappingEx)
                {
                    _logger.LogError(mappingEx, "数据映射失败，考试类型: {ExamType}, 考试ID: {ExamId}，使用降级数据", examType, examId);
                }
            }
            else
            {
                _logger.LogWarning("API返回空数据，考试类型: {ExamType}, 考试ID: {ExamId}, 学生ID: {StudentUserId}，可能是权限问题或考试不存在",
                    examType, examId, studentUserId);
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "网络请求失败，考试类型: {ExamType}, 考试ID: {ExamId}, 学生ID: {StudentUserId}，使用降级数据",
                examType, examId, studentUserId);
        }
        catch (TaskCanceledException timeoutEx)
        {
            _logger.LogError(timeoutEx, "API请求超时，考试类型: {ExamType}, 考试ID: {ExamId}, 学生ID: {StudentUserId}，使用降级数据",
                examType, examId, studentUserId);
        }
        catch (UnauthorizedAccessException authEx)
        {
            _logger.LogError(authEx, "API访问权限不足，考试类型: {ExamType}, 考试ID: {ExamId}, 学生ID: {StudentUserId}，使用降级数据",
                examType, examId, studentUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从API获取考试数据时发生未知错误，考试类型: {ExamType}, 考试ID: {ExamId}, 学生ID: {StudentUserId}，使用降级数据",
                examType, examId, studentUserId);
        }

        // 降级机制：使用原有的模拟数据逻辑
        _logger.LogInformation("使用降级数据，考试类型: {ExamType}, 模块类型: {ModuleType}, 考试ID: {ExamId}", examType, moduleType, examId);
        return CreateFallbackExamModel(moduleType, examType, examId);
    }

    /// <summary>
    /// 根据考试类型获取考试数据
    /// </summary>
    private async Task<object?> GetExamDataByTypeAsync(ExamType examType, int examId, int studentUserId)
    {
        return examType switch
        {
            ExamType.MockExam => await GetMockExamDataAsync(examId, studentUserId),
            ExamType.ComprehensiveTraining => await GetComprehensiveTrainingDataAsync(examId, studentUserId),
            ExamType.FormalExam => await GetFormalExamDataAsync(examId, studentUserId),
            ExamType.Practice => await GetFormalExamDataAsync(examId, studentUserId), // Practice使用相同的API
            ExamType.SpecialPractice => await GetSpecialPracticeDataAsync(examId, studentUserId),
            ExamType.SpecializedTraining => await GetSpecializedTrainingDataAsync(examId, studentUserId),
            _ => null
        };
    }

    /// <summary>
    /// 获取模拟考试数据
    /// </summary>
    private async Task<object?> GetMockExamDataAsync(int mockExamId, int studentUserId)
    {
        if (_studentMockExamService == null)
        {
            _logger.LogWarning("StudentMockExamService未注入，无法获取模拟考试数据");
            return null;
        }

        try
        {
            return await _studentMockExamService.GetMockExamDetailsAsync(mockExamId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模拟考试数据失败，模拟考试ID: {MockExamId}", mockExamId);
            return null;
        }
    }

    /// <summary>
    /// 获取综合实训数据
    /// </summary>
    private async Task<object?> GetComprehensiveTrainingDataAsync(int trainingId, int studentUserId)
    {
        if (_studentComprehensiveTrainingService == null)
        {
            _logger.LogWarning("StudentComprehensiveTrainingService未注入，无法获取综合实训数据");
            return null;
        }

        try
        {
            return await _studentComprehensiveTrainingService.GetTrainingDetailsAsync(trainingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取综合实训数据失败，训练ID: {TrainingId}", trainingId);
            return null;
        }
    }

    /// <summary>
    /// 获取正式考试数据
    /// </summary>
    private async Task<object?> GetFormalExamDataAsync(int examId, int studentUserId)
    {
        if (_studentExamService == null)
        {
            _logger.LogWarning("StudentExamService未注入，无法获取正式考试数据");
            return null;
        }

        try
        {
            return await _studentExamService.GetExamDetailsAsync(examId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取正式考试数据失败，考试ID: {ExamId}", examId);
            return null;
        }
    }

    /// <summary>
    /// 获取专项练习数据
    /// </summary>
    private async Task<object?> GetSpecialPracticeDataAsync(int practiceId, int studentUserId)
    {
        // 专项练习目前使用专项训练的数据结构，因为它们在系统中是统一管理的
        // 这里直接调用专项训练的获取方法
        return await GetSpecializedTrainingDataAsync(practiceId, studentUserId);
    }

    /// <summary>
    /// 获取专项训练数据
    /// </summary>
    private async Task<object?> GetSpecializedTrainingDataAsync(int trainingId, int studentUserId)
    {
        if (_studentSpecializedTrainingService == null)
        {
            _logger.LogWarning("StudentSpecializedTrainingService未注入，无法获取专项训练数据");
            return null;
        }

        try
        {
            return await _studentSpecializedTrainingService.GetTrainingDetailsAsync(trainingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取专项训练数据失败，训练ID: {TrainingId}", trainingId);
            return null;
        }
    }

    /// <summary>
    /// 根据考试类型映射考试数据到ExamModel
    /// </summary>
    private ExamModel MapExamDataToExamModel(object examData, ExamType examType, ModuleType targetModuleType)
    {
        return examType switch
        {
            ExamType.MockExam => MapMockExamToExamModel(examData, targetModuleType),
            ExamType.ComprehensiveTraining => MapComprehensiveTrainingToExamModel(examData, targetModuleType),
            ExamType.FormalExam or ExamType.Practice => MapStudentExamDtoToExamModel((StudentExamDto)examData, targetModuleType),
            ExamType.SpecialPractice or ExamType.SpecializedTraining => MapSpecializedTrainingToExamModel(examData, targetModuleType),
            _ => throw new NotSupportedException($"不支持的考试类型: {examType}")
        };
    }

    /// <summary>
    /// 映射模拟考试数据到ExamModel
    /// </summary>
    private ExamModel MapMockExamToExamModel(object mockExamData, ModuleType targetModuleType)
    {
        if (mockExamData is not StudentMockExamDto mockExamDto)
        {
            throw new ArgumentException("模拟考试数据类型不匹配", nameof(mockExamData));
        }

        _logger.LogInformation("映射模拟考试数据到ExamModel，考试名称: {ExamName}, 目标模块类型: {ModuleType}",
            mockExamDto.Name, targetModuleType);

        ExamModel examModel = new()
        {
            Id = mockExamDto.Id.ToString(),
            Name = string.IsNullOrWhiteSpace(mockExamDto.Name) ? $"模拟考试_{mockExamDto.Id}" : mockExamDto.Name,
            Description = mockExamDto.Description ?? string.Empty,
            TotalScore = mockExamDto.TotalScore > 0 ? mockExamDto.TotalScore : 100,
            DurationMinutes = mockExamDto.DurationMinutes > 0 ? mockExamDto.DurationMinutes : 120,
            Modules = []
        };

        // 模拟考试没有模块概念，需要根据题目的操作点创建虚拟模块
        // 按目标模块类型过滤相关题目
        _logger.LogDebug("开始过滤模拟考试题目，目标模块类型: {TargetModuleType}, 总题目数: {TotalQuestions}",
            targetModuleType, mockExamDto.Questions.Count);

        // 记录每个题目的详细信息，用于调试
        foreach (StudentMockExamQuestionDto question in mockExamDto.Questions)
        {
            string operationPointTypes = string.Join(", ", question.OperationPoints.Select(op => $"'{op.ModuleType}'"));
            _logger.LogDebug("题目 {QuestionId} '{Title}' 的操作点模块类型: [{OperationPointTypes}], 操作点数量: {Count}",
                question.OriginalQuestionId, question.Title, operationPointTypes, question.OperationPoints.Count);
        }

        // 使用增强的过滤逻辑，支持 C# 题目的特殊处理
        IEnumerable<StudentMockExamQuestionDto> relevantQuestions = mockExamDto.Questions
            .Where(q => IsQuestionRelevantForModule(q, targetModuleType));

        _logger.LogDebug("过滤后的相关题目数量: {RelevantQuestionsCount}", relevantQuestions.Count());

        if (relevantQuestions.Any())
        {
            _logger.LogInformation("找到 {Count} 个匹配 {ModuleType} 模块类型的题目，开始创建模块",
                relevantQuestions.Count(), targetModuleType);

            ExamModuleModel module = new()
            {
                Id = $"MockExam_Module_{targetModuleType}",
                Name = $"{targetModuleType}模块",
                Type = targetModuleType,
                Description = $"模拟考试 - {targetModuleType}模块",
                Score = relevantQuestions.Sum(q => q.Score),
                Order = 1,
                Questions = []
            };

            foreach (StudentMockExamQuestionDto questionDto in relevantQuestions.OrderBy(q => q.SortOrder))
            {
                QuestionModel question = new()
                {
                    Id = questionDto.OriginalQuestionId.ToString(),
                    Title = string.IsNullOrWhiteSpace(questionDto.Title) ? $"题目_{questionDto.OriginalQuestionId}" : questionDto.Title,
                    Content = questionDto.Content ?? string.Empty,
#pragma warning disable CS0618 // 类型或成员已过时
                    Score = CalculateCSharpQuestionScore(questionDto, targetModuleType),
#pragma warning restore CS0618 // 类型或成员已过时
                    Order = questionDto.SortOrder,
                    OperationPoints = [],
                    // 添加C#特有字段
                    ProgramInput = questionDto.ProgramInput,
                    ExpectedOutput = questionDto.ExpectedOutput,
                    CSharpQuestionType = GetCSharpQuestionTypeString(questionDto),
                    CSharpDirectScore = GetCSharpDirectScore(questionDto),
                    CodeBlanks = GetCodeBlanks(questionDto),
                    TemplateCode = questionDto.TemplateCode,
                    CodeFilePath = ConvertToAbsolutePath(questionDto.CodeFilePath, _directoryService),
                    DocumentFilePath = ConvertToAbsolutePath(questionDto.DocumentFilePath, _directoryService),
                    // 添加其他重要字段
                    QuestionConfig = questionDto.QuestionConfig,
                    AnswerValidationRules = questionDto.AnswerValidationRules,
                    Tags = questionDto.Tags
                };

                // 只添加匹配目标模块类型的操作点
                IEnumerable<StudentMockExamOperationPointDto> matchingOperationPoints = questionDto.OperationPoints
                    .Where(op => IsModuleTypeMatch(op.ModuleType, targetModuleType))
                    .OrderBy(op => op.Order);

                _logger.LogDebug("题目 {QuestionId} 中匹配的操作点数量: {MatchingCount}/{TotalCount}",
                    questionDto.OriginalQuestionId, matchingOperationPoints.Count(), questionDto.OperationPoints.Count);

                foreach (StudentMockExamOperationPointDto opDto in matchingOperationPoints)
                {
                    ModuleType parsedModuleType = ParseModuleType(opDto.ModuleType);
                    _logger.LogDebug("映射操作点 {OperationPointId}: '{OriginalType}' → {ParsedType}",
                        opDto.Id, opDto.ModuleType, parsedModuleType);

                    OperationPointModel operationPoint = new()
                    {
                        Id = opDto.Id.ToString(),
                        Name = string.IsNullOrWhiteSpace(opDto.Name) ? $"操作点_{opDto.Id}" : opDto.Name,
                        Description = opDto.Description ?? string.Empty,
                        ModuleType = parsedModuleType,
                        Score = opDto.Score,
                        Order = opDto.Order,
                        IsEnabled = true,
                        Parameters = MapMockExamParametersToConfigurationParameters(opDto.Parameters)
                    };

                    question.OperationPoints.Add(operationPoint);
                }

                // 检查是否应该添加题目（扩展逻辑以支持所有模块类型）
                bool shouldAddQuestion = question.OperationPoints.Count > 0 ||
                                       (targetModuleType == ModuleType.CSharp && IsCSharpQuestion(questionDto)) ||
                                       ShouldCreateDefaultOperationPointForMockExam(questionDto, targetModuleType);

                if (shouldAddQuestion)
                {
                    // 如果没有操作点，为所有模块类型创建默认操作点
                    if (question.OperationPoints.Count == 0)
                    {
                        OperationPointModel defaultOperationPoint = CreateDefaultOperationPointForMockExam(questionDto, targetModuleType);
                        question.OperationPoints.Add(defaultOperationPoint);

                        _logger.LogDebug("为模拟考试题目 {QuestionId} 创建默认操作点，模块类型: {ModuleType}",
                            questionDto.OriginalQuestionId, targetModuleType);
                    }

                    module.Questions.Add(question);
                }
                else
                {
                    _logger.LogDebug("模拟考试题目 {QuestionId} 没有匹配的操作点且不符合默认操作点创建条件，跳过", questionDto.OriginalQuestionId);
                }
            }

            if (module.Questions.Count > 0)
            {
                examModel.Modules.Add(module);
                _logger.LogInformation("成功创建 {ModuleType} 模块，包含 {QuestionCount} 个题目，总分 {TotalScore}",
                    targetModuleType, module.Questions.Count, module.Score);
            }
            else
            {
                _logger.LogWarning("模块 {ModuleType} 中没有有效题目（所有题目的操作点都被过滤掉了）", targetModuleType);
            }
        }

        // 如果没有找到相关模块，创建一个基本模块
        if (examModel.Modules.Count == 0)
        {
            _logger.LogWarning("模拟考试中未找到模块类型 {ModuleType} 的数据，创建基本模块。可能的原因：1) 数据中没有该模块类型的题目 2) 模块类型字符串不匹配",
                targetModuleType);
            examModel.Modules.Add(CreateBasicModule(targetModuleType));
        }

        return examModel;
    }

    /// <summary>
    /// 映射综合实训数据到ExamModel
    /// </summary>
    private ExamModel MapComprehensiveTrainingToExamModel(object trainingData, ModuleType targetModuleType)
    {
        if (trainingData is not StudentComprehensiveTrainingDto trainingDto)
        {
            throw new ArgumentException("综合实训数据类型不匹配", nameof(trainingData));
        }

        _logger.LogInformation("映射综合实训数据到ExamModel，训练名称: {TrainingName}, 目标模块类型: {ModuleType}",
            trainingDto.Name, targetModuleType);

        ExamModel examModel = new()
        {
            Id = trainingDto.Id.ToString(),
            Name = string.IsNullOrWhiteSpace(trainingDto.Name) ? $"综合实训_{trainingDto.Id}" : trainingDto.Name,
            Description = trainingDto.Description ?? string.Empty,
            TotalScore = trainingDto.CalculatedTotalScore > 0 ? trainingDto.CalculatedTotalScore : trainingDto.TotalScore,
            DurationMinutes = trainingDto.DurationMinutes > 0 ? trainingDto.DurationMinutes : 120,
            Modules = []
        };

        // 首先从模块列表中查找匹配的模块
        _logger.LogDebug("开始过滤综合实训模块，目标模块类型: {TargetModuleType}, 总模块数: {TotalModules}",
            targetModuleType, trainingDto.Modules.Count);

        IEnumerable<StudentComprehensiveTrainingModuleDto> relevantModules = trainingDto.Modules
            .Where(m => IsModuleTypeMatch(m.Type, targetModuleType));

        _logger.LogDebug("过滤后的相关模块数量: {RelevantModulesCount}", relevantModules.Count());

        foreach (StudentComprehensiveTrainingModuleDto moduleDto in relevantModules.OrderBy(m => m.Order))
        {
            ExamModuleModel module = new()
            {
                Id = moduleDto.Id.ToString(),
                Name = string.IsNullOrWhiteSpace(moduleDto.Name) ? $"{targetModuleType}模块" : moduleDto.Name,
                Type = ParseModuleType(moduleDto.Type),
                Description = moduleDto.Description ?? string.Empty,
                Score = moduleDto.Score,
                Order = moduleDto.Order,
                Questions = []
            };

            foreach (StudentComprehensiveTrainingQuestionDto questionDto in moduleDto.Questions.OrderBy(q => q.SortOrder))
            {
                QuestionModel question = MapComprehensiveTrainingQuestionToQuestionModel(questionDto, targetModuleType, _directoryService);

                // 对于C#题目，即使没有操作点也要添加
                bool shouldAddQuestion = question.OperationPoints.Count > 0 ||
                                       (targetModuleType == ModuleType.CSharp && IsCSharpQuestion(questionDto));

                if (shouldAddQuestion)
                {
                    // 如果是C#题目但没有操作点，创建一个默认操作点
                    if (targetModuleType == ModuleType.CSharp && question.OperationPoints.Count == 0)
                    {
                        OperationPointModel defaultOperationPoint = new()
                        {
                            Id = $"default_op_{questionDto.Id}",
                            Name = "C#编程操作",
                            Description = "C#编程题目操作点",
                            ModuleType = ModuleType.CSharp,
                            Score = questionDto.Score,
                            Order = 1,
                            IsEnabled = true,
                            Parameters = []
                        };

                        question.OperationPoints.Add(defaultOperationPoint);
                    }

                    module.Questions.Add(question);
                }
            }

            if (module.Questions.Count > 0)
            {
                examModel.Modules.Add(module);
            }
        }

        // 如果模块列表中没有找到，再从科目列表中查找
        if (examModel.Modules.Count == 0)
        {
            foreach (StudentComprehensiveTrainingSubjectDto subjectDto in trainingDto.Subjects.OrderBy(s => s.SortOrder))
            {
                // 检查科目中是否有匹配目标模块类型的题目
                IEnumerable<StudentComprehensiveTrainingQuestionDto> relevantQuestions = subjectDto.Questions
                    .Where(q => q.OperationPoints.Any(op => IsModuleTypeMatch(op.ModuleType, targetModuleType)));

                if (relevantQuestions.Any())
                {
                    ExamModuleModel module = new()
                    {
                        Id = $"Subject_{subjectDto.Id}_{targetModuleType}",
                        Name = $"{subjectDto.SubjectName} - {targetModuleType}",
                        Type = targetModuleType,
                        Description = subjectDto.Description ?? $"{subjectDto.SubjectName}科目 - {targetModuleType}模块",
                        Score = relevantQuestions.Sum(q => q.Score),
                        Order = subjectDto.SortOrder,
                        Questions = []
                    };

                    foreach (StudentComprehensiveTrainingQuestionDto questionDto in relevantQuestions.OrderBy(q => q.SortOrder))
                    {
                        QuestionModel question = MapComprehensiveTrainingQuestionToQuestionModel(questionDto, targetModuleType, _directoryService);
                        if (question.OperationPoints.Count > 0)
                        {
                            module.Questions.Add(question);
                        }
                    }

                    if (module.Questions.Count > 0)
                    {
                        examModel.Modules.Add(module);
                    }
                }
            }
        }

        // 如果仍然没有找到相关模块，创建一个基本模块
        if (examModel.Modules.Count == 0)
        {
            _logger.LogWarning("综合实训中未找到模块类型 {ModuleType} 的数据，创建基本模块", targetModuleType);
            examModel.Modules.Add(CreateBasicModule(targetModuleType));
        }

        return examModel;
    }

    /// <summary>
    /// 映射专项训练数据到ExamModel
    /// </summary>
    private ExamModel MapSpecializedTrainingToExamModel(object examData, ModuleType targetModuleType)
    {
        if (examData is not StudentSpecializedTrainingDto trainingDto)
        {
            _logger.LogError("专项训练数据类型不匹配，期望 StudentSpecializedTrainingDto，实际: {ActualType}", examData?.GetType().Name ?? "null");
            return CreateFallbackExamModel(targetModuleType, ExamType.SpecializedTraining, 0);
        }

        _logger.LogInformation("映射专项训练数据到ExamModel，训练ID: {TrainingId}, 目标模块类型: {ModuleType}", trainingDto.Id, targetModuleType);

        ExamModel examModel = new()
        {
            Id = trainingDto.Id.ToString(),
            Name = string.IsNullOrWhiteSpace(trainingDto.Name) ? $"专项训练_{trainingDto.Id}" : trainingDto.Name,
            Description = trainingDto.Description ?? string.Empty,
            TotalScore = trainingDto.TotalScore,
            DurationMinutes = trainingDto.Duration,
            Modules = []
        };

        // 专项训练通常只包含一种模块类型，检查是否匹配目标模块类型
        ModuleType trainingModuleType = ParseModuleType(trainingDto.ModuleType);
        if (trainingModuleType != targetModuleType)
        {
            _logger.LogWarning("专项训练模块类型 {TrainingModuleType} 与目标模块类型 {TargetModuleType} 不匹配",
                trainingModuleType, targetModuleType);
            return CreateFallbackExamModel(targetModuleType, ExamType.SpecializedTraining, trainingDto.Id);
        }

        // 处理模块数据
        if (trainingDto.Modules.Count > 0)
        {
            foreach (StudentSpecializedTrainingModuleDto moduleDto in trainingDto.Modules
                .Where(m => string.Equals(m.Type, targetModuleType.ToString(), StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Order))
            {
                ExamModuleModel module = new()
                {
                    Id = moduleDto.Id.ToString(),
                    Name = string.IsNullOrWhiteSpace(moduleDto.Name) ? $"{targetModuleType}模块" : moduleDto.Name,
                    Type = ParseModuleType(moduleDto.Type),
                    Description = moduleDto.Description ?? string.Empty,
                    Score = moduleDto.Score,
                    Order = moduleDto.Order,
                    Questions = []
                };

                foreach (StudentSpecializedTrainingQuestionDto questionDto in moduleDto.Questions.OrderBy(q => q.Order))
                {
                    QuestionModel question = MapSpecializedTrainingQuestionToQuestionModel(questionDto, targetModuleType, _directoryService);

                    // 对于C#题目，即使没有操作点也要添加
                    bool shouldAddQuestion = question.OperationPoints.Count > 0 ||
                                           (targetModuleType == ModuleType.CSharp && IsCSharpQuestion(questionDto));

                    if (shouldAddQuestion)
                    {
                        // 如果是C#题目但没有操作点，创建一个默认操作点
                        if (targetModuleType == ModuleType.CSharp && question.OperationPoints.Count == 0)
                        {
                            OperationPointModel defaultOperationPoint = new()
                            {
                                Id = $"default_op_{questionDto.Id}",
                                Name = "C#编程操作",
                                Description = "C#编程题目操作点",
                                ModuleType = ModuleType.CSharp,
                                Score = questionDto.Score,
                                Order = 1,
                                IsEnabled = true,
                                Parameters = []
                            };

                            question.OperationPoints.Add(defaultOperationPoint);
                        }

                        module.Questions.Add(question);
                    }
                }

                if (module.Questions.Count > 0)
                {
                    examModel.Modules.Add(module);
                }
            }
        }

        // 处理直接的题目数据（如果没有模块结构）
        if (examModel.Modules.Count == 0 && trainingDto.Questions.Count > 0)
        {
            ExamModuleModel module = new()
            {
                Id = $"SpecializedTraining_Module_{targetModuleType}",
                Name = $"{targetModuleType}模块",
                Type = targetModuleType,
                Description = $"专项训练 - {targetModuleType}模块",
                Score = trainingDto.Questions.Sum(q => q.Score),
                Order = 1,
                Questions = []
            };

            foreach (StudentSpecializedTrainingQuestionDto questionDto in trainingDto.Questions.OrderBy(q => q.Order))
            {
                QuestionModel question = MapSpecializedTrainingQuestionToQuestionModel(questionDto, targetModuleType, _directoryService);

                // 检查是否应该添加题目（扩展逻辑以支持所有模块类型）
                bool shouldAddQuestion = question.OperationPoints.Count > 0 ||
                                       (targetModuleType == ModuleType.CSharp && IsCSharpQuestion(questionDto)) ||
                                       ShouldCreateDefaultOperationPoint(questionDto, targetModuleType);

                if (shouldAddQuestion)
                {
                    // 如果没有操作点，为所有模块类型创建默认操作点
                    if (question.OperationPoints.Count == 0)
                    {
                        OperationPointModel defaultOperationPoint = CreateDefaultOperationPoint(questionDto, targetModuleType);
                        question.OperationPoints.Add(defaultOperationPoint);

                        _logger.LogDebug("为题目 {QuestionId} 创建默认操作点，模块类型: {ModuleType}",
                            questionDto.Id, targetModuleType);
                    }

                    module.Questions.Add(question);
                }
                else
                {
                    _logger.LogDebug("题目 {QuestionId} 没有匹配的操作点且不符合默认操作点创建条件，跳过", questionDto.Id);
                }
            }

            if (module.Questions.Count > 0)
            {
                examModel.Modules.Add(module);
            }
        }

        // 如果仍然没有找到相关模块，创建一个基本模块
        if (examModel.Modules.Count == 0)
        {
            _logger.LogWarning("专项训练中未找到模块类型 {ModuleType} 的数据，创建基本模块", targetModuleType);
            examModel.Modules.Add(CreateBasicModule(targetModuleType));
        }

        return examModel;
    }

    /// <summary>
    /// 映射综合实训题目到QuestionModel
    /// </summary>
    private static QuestionModel MapComprehensiveTrainingQuestionToQuestionModel(StudentComprehensiveTrainingQuestionDto questionDto, ModuleType targetModuleType, IBenchSuiteDirectoryService? directoryService = null)
    {
        QuestionModel question = new()
        {
            Id = questionDto.Id.ToString(),
            Title = string.IsNullOrWhiteSpace(questionDto.Title) ? $"题目_{questionDto.Id}" : questionDto.Title,
            Content = questionDto.Content ?? string.Empty,
#pragma warning disable CS0618 // 类型或成员已过时
            Score = CalculateCSharpQuestionScore(questionDto, targetModuleType),
#pragma warning restore CS0618 // 类型或成员已过时
            Order = questionDto.SortOrder,
            OperationPoints = [],
            // 添加C#特有字段
            ProgramInput = questionDto.ProgramInput,
            ExpectedOutput = questionDto.ExpectedOutput,
            CSharpQuestionType = GetCSharpQuestionTypeString(questionDto),
            CSharpDirectScore = GetCSharpDirectScore(questionDto),
            CodeBlanks = GetCodeBlanks(questionDto),
            TemplateCode = questionDto.TemplateCode,
            CodeFilePath = ConvertToAbsolutePath(questionDto.CodeFilePath, directoryService),
            DocumentFilePath = ConvertToAbsolutePath(questionDto.DocumentFilePath, directoryService),
            // 添加其他重要字段
            QuestionConfig = questionDto.QuestionConfig,
            AnswerValidationRules = questionDto.AnswerValidationRules,
            Tags = questionDto.Tags
        };

        // 只添加匹配目标模块类型的操作点
        foreach (StudentComprehensiveTrainingOperationPointDto opDto in questionDto.OperationPoints
            .Where(op => IsModuleTypeMatch(op.ModuleType, targetModuleType))
            .OrderBy(op => op.Order))
        {
            OperationPointModel operationPoint = new()
            {
                Id = opDto.Id.ToString(),
                Name = string.IsNullOrWhiteSpace(opDto.Name) ? $"操作点_{opDto.Id}" : opDto.Name,
                Description = opDto.Description ?? string.Empty,
                ModuleType = ParseModuleType(opDto.ModuleType),
                Score = opDto.Score,
                Order = opDto.Order,
                IsEnabled = true,
                Parameters = MapComprehensiveTrainingParametersToConfigurationParameters(opDto.Parameters)
            };

            question.OperationPoints.Add(operationPoint);
        }

        return question;
    }

    /// <summary>
    /// 映射专项训练题目到QuestionModel
    /// </summary>
    private static QuestionModel MapSpecializedTrainingQuestionToQuestionModel(StudentSpecializedTrainingQuestionDto questionDto, ModuleType targetModuleType, IBenchSuiteDirectoryService? directoryService = null)
    {
        QuestionModel question = new()
        {
            Id = questionDto.Id.ToString(),
            Title = string.IsNullOrWhiteSpace(questionDto.Title) ? $"题目_{questionDto.Id}" : questionDto.Title,
            Content = questionDto.Content ?? string.Empty,
#pragma warning disable CS0618 // 类型或成员已过时
            Score = (double)CalculateCSharpQuestionScore(questionDto, targetModuleType),
#pragma warning restore CS0618 // 类型或成员已过时
            Order = questionDto.Order,
            OperationPoints = [],
            // 添加C#特有字段
            ProgramInput = GetProgramInput(questionDto),
            ExpectedOutput = GetExpectedOutput(questionDto),
            CSharpQuestionType = GetCSharpQuestionTypeString(questionDto),
            CSharpDirectScore = GetCSharpDirectScore(questionDto),
            CodeBlanks = GetCodeBlanks(questionDto),
            TemplateCode = questionDto.TemplateCode,
            CodeFilePath = ConvertToAbsolutePath(questionDto.CodeFilePath, directoryService),
            DocumentFilePath = ConvertToAbsolutePath(questionDto.DocumentFilePath, directoryService),
            // 添加其他重要字段
            QuestionConfig = questionDto.QuestionConfig,
            AnswerValidationRules = questionDto.AnswerValidationRules,
            Tags = questionDto.Tags
        };

        // 记录操作点过滤前的状态
        System.Diagnostics.Debug.WriteLine($"[MapSpecializedTrainingQuestion] 题目 {questionDto.Id} 原始操作点数量: {questionDto.OperationPoints.Count}");
        foreach (StudentSpecializedTrainingOperationPointDto op in questionDto.OperationPoints)
        {
            System.Diagnostics.Debug.WriteLine($"[MapSpecializedTrainingQuestion] 原始操作点: ID={op.Id}, Name='{op.Name}', ModuleType='{op.ModuleType}'");
        }

        // 只添加匹配目标模块类型的操作点
        IEnumerable<StudentSpecializedTrainingOperationPointDto> matchingOperationPoints = questionDto.OperationPoints
            .Where(op => IsModuleTypeMatch(op.ModuleType, targetModuleType))
            .OrderBy(op => op.Order);

        System.Diagnostics.Debug.WriteLine($"[MapSpecializedTrainingQuestion] 题目 {questionDto.Id} 匹配的操作点数量: {matchingOperationPoints.Count()}/{questionDto.OperationPoints.Count}");

        foreach (StudentSpecializedTrainingOperationPointDto opDto in matchingOperationPoints)
        {
            try
            {
                OperationPointModel operationPoint = new()
                {
                    Id = opDto.Id.ToString(),
                    Name = string.IsNullOrWhiteSpace(opDto.Name) ? $"操作点_{opDto.Id}" : opDto.Name,
                    Description = opDto.Description ?? string.Empty,
                    ModuleType = ParseModuleType(opDto.ModuleType),
                    Score = opDto.Score,
                    Order = opDto.Order,
                    IsEnabled = true,
                    Parameters = MapSpecializedTrainingParametersToConfigurationParameters(opDto.Parameters)
                };

                question.OperationPoints.Add(operationPoint);

                System.Diagnostics.Debug.WriteLine($"[MapSpecializedTrainingQuestion] 成功映射操作点: ID={opDto.Id}, Name='{operationPoint.Name}', ModuleType={operationPoint.ModuleType}, 参数数量={operationPoint.Parameters.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapSpecializedTrainingQuestion] 映射操作点失败: ID={opDto.Id}, Error={ex.Message}");
                // 继续处理其他操作点
            }
        }

        return question;
    }

    /// <summary>
    /// 映射模拟考试参数到配置参数（增强版，包含验证和错误处理）
    /// </summary>
    private static List<ConfigurationParameterModel> MapMockExamParametersToConfigurationParameters(IEnumerable<StudentMockExamParameterDto> parameters)
    {
        List<ConfigurationParameterModel> configParams = [];

        if (parameters == null)
        {
            System.Diagnostics.Debug.WriteLine("[MapMockExamParameters] 参数集合为null，返回空列表");
            return configParams;
        }

        int order = 1;
        foreach (StudentMockExamParameterDto paramDto in parameters)
        {
            try
            {
                if (paramDto == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MapMockExamParameters] 跳过null参数，顺序: {order}");
                    continue;
                }

                ConfigurationParameterModel configParam = new()
                {
                    Id = paramDto.Id.ToString(),
                    Name = !string.IsNullOrWhiteSpace(paramDto.Name) ? paramDto.Name : $"参数_{paramDto.Id}",
                    Value = !string.IsNullOrWhiteSpace(paramDto.Value) ? paramDto.Value : (paramDto.DefaultValue ?? string.Empty),
                    Type = ParseParameterType(paramDto.ParameterType),
                    Description = paramDto.Description ?? string.Empty,
                    IsRequired = true,
                    Order = order++
                };

                configParams.Add(configParam);

                System.Diagnostics.Debug.WriteLine($"[MapMockExamParameters] 成功映射参数: ID={paramDto.Id}, Name='{configParam.Name}', Type={configParam.Type}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapMockExamParameters] 映射参数失败: ID={paramDto?.Id}, Error={ex.Message}");
                // 继续处理其他参数，不因单个参数失败而中断
            }
        }

        System.Diagnostics.Debug.WriteLine($"[MapMockExamParameters] 完成参数映射，成功映射 {configParams.Count} 个参数");
        return configParams;
    }

    /// <summary>
    /// 映射专项训练参数到配置参数（增强版，包含验证和错误处理）
    /// </summary>
    private static List<ConfigurationParameterModel> MapSpecializedTrainingParametersToConfigurationParameters(IEnumerable<StudentSpecializedTrainingParameterDto> parameters)
    {
        List<ConfigurationParameterModel> configParams = [];

        if (parameters == null)
        {
            System.Diagnostics.Debug.WriteLine("[MapSpecializedTrainingParameters] 参数集合为null，返回空列表");
            return configParams;
        }

        int order = 1;
        foreach (StudentSpecializedTrainingParameterDto paramDto in parameters)
        {
            try
            {
                if (paramDto == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MapSpecializedTrainingParameters] 跳过null参数，顺序: {order}");
                    continue;
                }

                ConfigurationParameterModel configParam = new()
                {
                    Id = paramDto.Id.ToString(),
                    Name = !string.IsNullOrWhiteSpace(paramDto.Name) ? paramDto.Name : $"参数_{paramDto.Id}",
                    Value = !string.IsNullOrWhiteSpace(paramDto.Value) ? paramDto.Value : (paramDto.DefaultValue ?? string.Empty),
                    Type = ParseParameterType(paramDto.ParameterType),
                    Description = paramDto.Description ?? string.Empty,
                    IsRequired = true,
                    Order = order++
                };

                configParams.Add(configParam);

                System.Diagnostics.Debug.WriteLine($"[MapSpecializedTrainingParameters] 成功映射参数: ID={paramDto.Id}, Name='{configParam.Name}', Type={configParam.Type}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MapSpecializedTrainingParameters] 映射参数失败: ID={paramDto?.Id}, Error={ex.Message}");
                // 继续处理其他参数，不因单个参数失败而中断
            }
        }

        System.Diagnostics.Debug.WriteLine($"[MapSpecializedTrainingParameters] 完成参数映射，成功映射 {configParams.Count} 个参数");
        return configParams;
    }

    /// <summary>
    /// 映射综合实训参数到配置参数
    /// </summary>
    private static List<ConfigurationParameterModel> MapComprehensiveTrainingParametersToConfigurationParameters(IEnumerable<StudentComprehensiveTrainingParameterDto> parameters)
    {
        List<ConfigurationParameterModel> configParams = [];

        int order = 1;
        foreach (StudentComprehensiveTrainingParameterDto paramDto in parameters)
        {
            ConfigurationParameterModel configParam = new()
            {
                Id = paramDto.Id.ToString(),
                Name = paramDto.Name ?? string.Empty,
                Value = paramDto.DefaultValue ?? string.Empty,
                Type = ParseParameterType(paramDto.ParameterType),
                Description = paramDto.Description ?? string.Empty,
                IsRequired = true,
                Order = order++
            };

            configParams.Add(configParam);
        }

        return configParams;
    }

    /// <summary>
    /// 计算C#题目的分数（遵循ExamLab的评分逻辑）
    /// </summary>
    private static double CalculateCSharpQuestionScore(StudentMockExamQuestionDto questionDto, ModuleType targetModuleType)
    {
        // 如果不是C#模块，使用原始分数
        if (targetModuleType != ModuleType.CSharp)
        {
            return questionDto.Score;
        }

        // 对于C#模块，尝试按照ExamLab的逻辑计算分数
        // 1. 首先检查是否有操作点
        if (questionDto.OperationPoints.Any(op => IsModuleTypeMatch(op.ModuleType, ModuleType.CSharp)))
        {
            return questionDto.OperationPoints
                .Where(op => IsModuleTypeMatch(op.ModuleType, ModuleType.CSharp))
                .Sum(op => op.Score);
        }

        // 2. 如果没有C#操作点，检查C#特有字段
        // 注意：StudentMockExamQuestionDto 可能没有 CSharpQuestionType 和 CSharpDirectScore 字段
        // 在这种情况下，我们使用原始分数作为降级
        return questionDto.Score;
    }

    /// <summary>
    /// 获取C#题目类型字符串
    /// </summary>
    private static string? GetCSharpQuestionTypeString(StudentMockExamQuestionDto questionDto)
    {
        // 检查题目内容，尝试推断C#题目类型
        string titleLower = questionDto.Title?.ToLowerInvariant() ?? "";
        string contentLower = questionDto.Content?.ToLowerInvariant() ?? "";

        if (titleLower.Contains("代码补全") || contentLower.Contains("填空") || contentLower.Contains("补全"))
        {
            return "CodeCompletion";
        }
        else if (titleLower.Contains("调试") || titleLower.Contains("纠错") || contentLower.Contains("错误"))
        {
            return "Debugging";
        }
        else if (titleLower.Contains("编写") || titleLower.Contains("实现") || contentLower.Contains("实现"))
        {
            return "Implementation";
        }

        // 默认为代码补全
        return "CodeCompletion";
    }

    /// <summary>
    /// 获取C#题目直接分数
    /// </summary>
    private static double? GetCSharpDirectScore(StudentMockExamQuestionDto questionDto)
    {
        // StudentMockExamQuestionDto 可能没有 CSharpDirectScore 字段
        // 使用题目的总分作为直接分数
        return questionDto.Score;
    }

    /// <summary>
    /// 获取代码填空处集合
    /// </summary>
    private static List<CodeBlankModel>? GetCodeBlanks(StudentMockExamQuestionDto questionDto)
    {
        // 优先从CodeBlanks字段直接读取
        if (!string.IsNullOrWhiteSpace(questionDto.CodeBlanks))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(questionDto.CodeBlanks);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return ParseCodeBlanksFromJson(doc.RootElement);
                }
            }
            catch (JsonException)
            {
                // JSON解析失败，继续尝试其他方法
            }
        }

        // 备用方案：从QuestionConfig中解析CodeBlanks信息
        if (!string.IsNullOrWhiteSpace(questionDto.QuestionConfig))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(questionDto.QuestionConfig);
                if (doc.RootElement.TryGetProperty("codeBlanks", out JsonElement codeBlanksElement))
                {
                    return ParseCodeBlanksFromJson(codeBlanksElement);
                }
            }
            catch (JsonException)
            {
                // JSON解析失败
            }
        }

        // 如果没有找到CodeBlanks，返回null
        return null;
    }

    // 为综合实训题目提供重载方法
    /// <summary>
    /// 计算C#题目的分数（综合实训版本）
    /// </summary>
    private static double CalculateCSharpQuestionScore(StudentComprehensiveTrainingQuestionDto questionDto, ModuleType targetModuleType)
    {
        // 如果不是C#模块，使用原始分数
        if (targetModuleType != ModuleType.CSharp)
        {
            return questionDto.Score;
        }

        // 对于C#模块，尝试按照ExamLab的逻辑计算分数
        return questionDto.OperationPoints.Any(op => IsModuleTypeMatch(op.ModuleType, ModuleType.CSharp))
            ? questionDto.OperationPoints
                .Where(op => IsModuleTypeMatch(op.ModuleType, ModuleType.CSharp))
                .Sum(op => op.Score)
            : questionDto.Score;
    }

    /// <summary>
    /// 获取C#题目类型字符串（综合实训版本）
    /// </summary>
    private static string? GetCSharpQuestionTypeString(StudentComprehensiveTrainingQuestionDto questionDto)
    {
        string titleLower = questionDto.Title?.ToLowerInvariant() ?? "";
        string contentLower = questionDto.Content?.ToLowerInvariant() ?? "";

        if (titleLower.Contains("代码补全") || contentLower.Contains("填空") || contentLower.Contains("补全"))
        {
            return "CodeCompletion";
        }
        else if (titleLower.Contains("调试") || titleLower.Contains("纠错") || contentLower.Contains("错误"))
        {
            return "Debugging";
        }
        else if (titleLower.Contains("编写") || titleLower.Contains("实现") || contentLower.Contains("实现"))
        {
            return "Implementation";
        }

        return "CodeCompletion";
    }

    /// <summary>
    /// 获取C#题目直接分数（综合实训版本）
    /// </summary>
    private static double? GetCSharpDirectScore(StudentComprehensiveTrainingQuestionDto questionDto)
    {
        return questionDto.Score;
    }

    /// <summary>
    /// 获取代码填空处集合（综合实训版本）
    /// </summary>
    private static List<CodeBlankModel>? GetCodeBlanks(StudentComprehensiveTrainingQuestionDto questionDto)
    {
        // 优先从CodeBlanks字段直接读取
        if (!string.IsNullOrWhiteSpace(questionDto.CodeBlanks))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(questionDto.CodeBlanks);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return ParseCodeBlanksFromJson(doc.RootElement);
                }
            }
            catch (JsonException)
            {
                // JSON解析失败，继续尝试其他方法
            }
        }

        // 备用方案：从QuestionConfig中解析CodeBlanks信息
        if (!string.IsNullOrWhiteSpace(questionDto.QuestionConfig))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(questionDto.QuestionConfig);
                if (doc.RootElement.TryGetProperty("codeBlanks", out JsonElement codeBlanksElement))
                {
                    return ParseCodeBlanksFromJson(codeBlanksElement);
                }
            }
            catch (JsonException)
            {
                // JSON解析失败
            }
        }

        return null;
    }

    /// <summary>
    /// 检查题目是否为C#编程题目（综合实训版本）
    /// </summary>
    private static bool IsCSharpQuestion(StudentComprehensiveTrainingQuestionDto question)
    {
        // 检查题目标题和内容中的C#关键词
        string titleLower = question.Title?.ToLowerInvariant() ?? "";
        string contentLower = question.Content?.ToLowerInvariant() ?? "";

        string[] csharpKeywords = {
            "c#", "csharp", "编程", "程序设计", "代码", "class", "namespace",
            "using", "public", "private", "static", "void", "int", "string",
            "console", "writeline", "main", "method", "变量", "函数", "方法"
        };

        bool hasKeywords = csharpKeywords.Any(keyword =>
            titleLower.Contains(keyword) || contentLower.Contains(keyword));

        // 检查是否有C#特有的字段
        bool hasCSharpFields = !string.IsNullOrEmpty(question.ProgramInput) ||
                              !string.IsNullOrEmpty(question.ExpectedOutput);

        return hasKeywords || hasCSharpFields;
    }

    // 为专项训练题目提供重载方法
    /// <summary>
    /// 计算C#题目的分数（专项训练版本）
    /// </summary>
    private static decimal CalculateCSharpQuestionScore(StudentSpecializedTrainingQuestionDto questionDto, ModuleType targetModuleType)
    {
        // 如果不是C#模块，使用原始分数
        if (targetModuleType != ModuleType.CSharp)
        {
            return (decimal)questionDto.Score;
        }

        // 对于C#模块，尝试按照ExamLab的逻辑计算分数
        return questionDto.OperationPoints.Any(op => IsModuleTypeMatch(op.ModuleType, ModuleType.CSharp))
            ? (decimal)questionDto.OperationPoints
                .Where(op => IsModuleTypeMatch(op.ModuleType, ModuleType.CSharp))
                .Sum(op => op.Score)
            : (decimal)questionDto.Score;
    }

    /// <summary>
    /// 获取C#题目类型字符串（专项训练版本）
    /// </summary>
    private static string? GetCSharpQuestionTypeString(StudentSpecializedTrainingQuestionDto questionDto)
    {
        string titleLower = questionDto.Title?.ToLowerInvariant() ?? "";
        string contentLower = questionDto.Content?.ToLowerInvariant() ?? "";

        if (titleLower.Contains("代码补全") || contentLower.Contains("填空") || contentLower.Contains("补全"))
        {
            return "CodeCompletion";
        }
        else if (titleLower.Contains("调试") || titleLower.Contains("纠错") || contentLower.Contains("错误"))
        {
            return "Debugging";
        }
        else if (titleLower.Contains("编写") || titleLower.Contains("实现") || contentLower.Contains("实现"))
        {
            return "Implementation";
        }

        return "CodeCompletion";
    }

    /// <summary>
    /// 获取C#题目直接分数（专项训练版本）
    /// </summary>
    private static double? GetCSharpDirectScore(StudentSpecializedTrainingQuestionDto questionDto)
    {
        return questionDto.Score;
    }

    /// <summary>
    /// 获取代码填空处集合（专项训练版本）
    /// </summary>
    private static List<CodeBlankModel>? GetCodeBlanks(StudentSpecializedTrainingQuestionDto questionDto)
    {
        // 优先从CodeBlanks字段直接读取
        if (!string.IsNullOrWhiteSpace(questionDto.CodeBlanks))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(questionDto.CodeBlanks);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return ParseCodeBlanksFromJson(doc.RootElement);
                }
            }
            catch (JsonException)
            {
                // JSON解析失败，继续尝试其他方法
            }
        }

        // 备用方案：从QuestionConfig中解析CodeBlanks信息
        if (!string.IsNullOrWhiteSpace(questionDto.QuestionConfig))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(questionDto.QuestionConfig);
                if (doc.RootElement.TryGetProperty("codeBlanks", out JsonElement codeBlanksElement))
                {
                    return ParseCodeBlanksFromJson(codeBlanksElement);
                }
            }
            catch (JsonException)
            {
                // JSON解析失败
            }
        }

        return null;
    }

    /// <summary>
    /// 获取程序输入（专项训练版本）
    /// </summary>
    private static string? GetProgramInput(StudentSpecializedTrainingQuestionDto questionDto)
    {
        return questionDto.ProgramInput;
    }

    /// <summary>
    /// 获取预期输出（专项训练版本）
    /// </summary>
    private static string? GetExpectedOutput(StudentSpecializedTrainingQuestionDto questionDto)
    {
        return questionDto.ExpectedOutput;
    }

    /// <summary>
    /// 检查题目是否为C#编程题目（专项训练版本）
    /// </summary>
    private static bool IsCSharpQuestion(StudentSpecializedTrainingQuestionDto question)
    {
        // 检查题目标题和内容中的C#关键词
        string titleLower = question.Title?.ToLowerInvariant() ?? "";
        string contentLower = question.Content?.ToLowerInvariant() ?? "";

        string[] csharpKeywords = [
            "c#", "csharp", "编程", "程序设计", "代码", "class", "namespace",
            "using", "public", "private", "static", "void", "int", "string",
            "console", "writeline", "main", "method", "变量", "函数", "方法"
        ];

        return csharpKeywords.Any(keyword =>
            titleLower.Contains(keyword) || contentLower.Contains(keyword));
    }

    /// <summary>
    /// 解析参数类型字符串为ParameterType枚举（增强版，支持更多类型和详细日志）
    /// </summary>
    private static BenchSuite.Models.ParameterType ParseParameterType(string? parameterTypeString)
    {
        if (string.IsNullOrWhiteSpace(parameterTypeString))
        {
            System.Diagnostics.Debug.WriteLine("[ParseParameterType] 参数类型字符串为空，返回默认值 Text");
            return ParameterType.Text;
        }

        string originalInput = parameterTypeString;
        string normalized = parameterTypeString.ToLowerInvariant().Trim();

        // 首先尝试直接解析枚举
        if (Enum.TryParse<BenchSuite.Models.ParameterType>(parameterTypeString, true, out BenchSuite.Models.ParameterType directResult))
        {
            System.Diagnostics.Debug.WriteLine($"[ParseParameterType] 直接解析成功: '{originalInput}' → {directResult}");
            return directResult;
        }

        BenchSuite.Models.ParameterType result = normalized switch
        {
            // 文本类型变体
            "string" or "text" or "str" or "字符串" or "文本" => BenchSuite.Models.ParameterType.Text,

            // 数字类型变体
            "int" or "integer" or "number" or "numeric" or "数字" or "整数" or "double" or "decimal" or "float" => BenchSuite.Models.ParameterType.Number,

            // 布尔类型变体
            "bool" or "boolean" or "布尔" or "真假" or "是否" => BenchSuite.Models.ParameterType.Boolean,

            // 枚举类型变体
            "enum" or "enumeration" or "select" or "选择" or "枚举" => BenchSuite.Models.ParameterType.Enum,

            // 颜色类型变体
            "color" or "colour" or "颜色" => BenchSuite.Models.ParameterType.Color,

            // 文件类型变体
            "file" or "filepath" or "文件" or "文件路径" => BenchSuite.Models.ParameterType.File,

            // 文件夹类型变体
            "folder" or "directory" or "dir" or "folderpath" or "文件夹" or "目录" or "文件夹路径" or "目录路径" => BenchSuite.Models.ParameterType.Folder,

            // 路径类型变体（通用路径）
            "path" or "路径" => BenchSuite.Models.ParameterType.Path,

            // 多选类型变体
            "multiplechoice" or "multiple_choice" or "multichoice" or "多选" or "多项选择" => BenchSuite.Models.ParameterType.MultipleChoice,

            // 日期类型变体
            "date" or "datetime" or "time" or "日期" or "时间" or "日期时间" => BenchSuite.Models.ParameterType.Date,

            _ => BenchSuite.Models.ParameterType.Text // 默认值
        };

        if (result != BenchSuite.Models.ParameterType.Text || normalized.Contains("text") || normalized.Contains("string"))
        {
            System.Diagnostics.Debug.WriteLine($"[ParseParameterType] 变体解析成功: '{originalInput}' (normalized: '{normalized}') → {result}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[ParseParameterType] 无法识别的参数类型: '{originalInput}' (normalized: '{normalized}')，返回默认值 Text");
        }

        return result;
    }

    /// <summary>
    /// 创建基础考试模型
    /// </summary>
    private static ExamModel CreateBasicExamModel(string examName, ModuleType targetModuleType)
    {
        ExamModel examModel = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = examName,
            Description = $"{examName} - {targetModuleType}模块",
            TotalScore = 100,
            DurationMinutes = 120,
            Modules = []
        };

        examModel.Modules.Add(CreateBasicModule(targetModuleType));
        return examModel;
    }

    /// <summary>
    /// 将StudentExamDto映射为ExamModel
    /// </summary>
    private ExamModel MapStudentExamDtoToExamModel(StudentExamDto examDto, ModuleType targetModuleType)
    {
        if (examDto == null)
        {
            throw new ArgumentNullException(nameof(examDto), "考试数据不能为空");
        }

        ExamModel examModel = new()
        {
            Id = examDto.Id.ToString(),
            Name = string.IsNullOrWhiteSpace(examDto.Name) ? $"考试_{examDto.Id}" : examDto.Name,
            Description = examDto.Description ?? string.Empty,
            TotalScore = examDto.TotalScore > 0 ? examDto.TotalScore : 100,
            DurationMinutes = examDto.DurationMinutes > 0 ? examDto.DurationMinutes : 120,
            Modules = []
        };

        // 根据目标模块类型过滤相关模块
        IEnumerable<StudentModuleDto> relevantModules = examDto.Modules
            .Where(m => string.Equals(m.Type, targetModuleType.ToString(), StringComparison.OrdinalIgnoreCase));

        foreach (StudentModuleDto moduleDto in relevantModules)
        {
            ExamModuleModel module = new()
            {
                Id = moduleDto.Id.ToString(),
                Name = moduleDto.Name,
                Type = ParseModuleType(moduleDto.Type),
                Description = moduleDto.Description,
                Score = moduleDto.Score,
                Order = moduleDto.Order,
                Questions = []
            };

            foreach (StudentQuestionDto questionDto in moduleDto.Questions)
            {
                QuestionModel question = new()
                {
                    Id = questionDto.Id.ToString(),
                    Title = questionDto.Title,
                    Content = questionDto.Content,
#pragma warning disable CS0618 // 类型或成员已过时
                    Score = questionDto.Score,
#pragma warning restore CS0618 // 类型或成员已过时
                    Order = questionDto.SortOrder,
                    OperationPoints = []
                };

                foreach (StudentOperationPointDto opDto in questionDto.OperationPoints)
                {
                    OperationPointModel operationPoint = new()
                    {
                        Id = opDto.Id.ToString(),
                        Name = opDto.Name,
                        Description = opDto.Description,
                        ModuleType = ParseModuleType(opDto.ModuleType),
                        Score = opDto.Score,
                        Order = opDto.Order,
                        IsEnabled = true,
                        Parameters = []
                    };

                    question.OperationPoints.Add(operationPoint);
                }

                module.Questions.Add(question);
            }

            examModel.Modules.Add(module);
        }

        // 如果没有找到相关模块，创建一个基本模块
        if (examModel.Modules.Count == 0)
        {
            _logger.LogWarning("未找到模块类型 {ModuleType} 的数据，创建基本模块", targetModuleType);
            examModel.Modules.Add(CreateBasicModule(targetModuleType));
        }

        return examModel;
    }

    /// <summary>
    /// 检查题目是否与目标模块类型相关（增强版，支持C#题目的特殊处理）
    /// </summary>
    private static bool IsQuestionRelevantForModule(StudentMockExamQuestionDto question, ModuleType targetModuleType)
    {
        // 1. 检查操作点中是否有匹配的模块类型
        bool hasMatchingOperationPoint = question.OperationPoints.Any(op => IsModuleTypeMatch(op.ModuleType, targetModuleType));

        if (hasMatchingOperationPoint)
        {
            return true;
        }

        // 2. 对于C#模块，进行特殊检查
        if (targetModuleType == ModuleType.CSharp)
        {
            // 检查题目是否包含C#相关的特征
            return IsCSharpQuestion(question);
        }

        return false;
    }

    /// <summary>
    /// 检查题目是否为C#编程题目（基于题目内容和特征）
    /// </summary>
    private static bool IsCSharpQuestion(StudentMockExamQuestionDto question)
    {
        // 检查题目标题和内容中的C#关键词
        string titleLower = question.Title?.ToLowerInvariant() ?? "";
        string contentLower = question.Content?.ToLowerInvariant() ?? "";

        string[] csharpKeywords = {
            "c#", "csharp", "编程", "程序设计", "代码", "class", "namespace",
            "using", "public", "private", "static", "void", "int", "string",
            "console", "writeline", "main", "method", "变量", "函数", "方法"
        };

        bool hasKeywords = csharpKeywords.Any(keyword =>
            titleLower.Contains(keyword) || contentLower.Contains(keyword));

        // 检查是否有C#特有的字段
        bool hasCSharpFields = !string.IsNullOrEmpty(question.ProgramInput) ||
                              !string.IsNullOrEmpty(question.ExpectedOutput);

        return hasKeywords || hasCSharpFields;
    }

    /// <summary>
    /// 检查模块类型字符串是否与目标模块类型匹配（增强版，包含详细日志）
    /// </summary>
    private static bool IsModuleTypeMatch(string moduleTypeString, ModuleType targetModuleType)
    {
        if (string.IsNullOrWhiteSpace(moduleTypeString))
        {
            System.Diagnostics.Debug.WriteLine($"[IsModuleTypeMatch] 模块类型字符串为空，目标类型: {targetModuleType}，返回 false");
            return false;
        }

        // 首先尝试解析为ModuleType，然后比较
        ModuleType parsedType = ParseModuleType(moduleTypeString);
        bool isMatch = parsedType == targetModuleType;

        System.Diagnostics.Debug.WriteLine($"[IsModuleTypeMatch] 模块类型匹配检查: '{moduleTypeString}' → {parsedType} vs {targetModuleType} = {isMatch}");

        return isMatch;
    }

    /// <summary>
    /// 解析模块类型字符串为枚举（增强版，支持更多变体和详细日志）
    /// </summary>
    private static ModuleType ParseModuleType(string moduleTypeString)
    {
        if (string.IsNullOrWhiteSpace(moduleTypeString))
        {
            System.Diagnostics.Debug.WriteLine("[ParseModuleType] 输入为空，返回默认值 Windows");
            return ModuleType.Windows; // 默认值
        }

        string originalInput = moduleTypeString;

        // 首先尝试直接解析
        if (Enum.TryParse<ModuleType>(moduleTypeString, true, out ModuleType result))
        {
            System.Diagnostics.Debug.WriteLine($"[ParseModuleType] 直接解析成功: '{originalInput}' → {result}");
            return result;
        }

        // 处理各种别名和变体
        string normalized = moduleTypeString.Trim().ToLowerInvariant();
        ModuleType parsedType = normalized switch
        {
            // PowerPoint 变体
            "ppt" or "powerpoint" or "power-point" or "power_point" or "pptx" or "演示文稿" or "幻灯片" => ModuleType.PowerPoint,

            // Word 变体
            "word" or "msword" or "ms-word" or "microsoft-word" or "doc" or "docx" or "文档" or "文字处理" => ModuleType.Word,

            // Excel 变体
            "excel" or "msexcel" or "ms-excel" or "microsoft-excel" or "xls" or "xlsx" or "电子表格" or "表格" => ModuleType.Excel,

            // Windows 变体
            "windows" or "win" or "os" or "操作系统" or "系统操作" or "文件管理" or "system" => ModuleType.Windows,

            // C# 变体（重点增强）
            "csharp" or "c#" or "c-sharp" or "c_sharp" or "cs" or "dotnet" or ".net" or "编程" or "程序设计" or "代码" or "programming" or "code" => ModuleType.CSharp,

            _ => ModuleType.Windows // 默认值
        };

        if (parsedType != ModuleType.Windows || normalized.Contains("windows") || normalized.Contains("win") || normalized.Contains("操作系统"))
        {
            System.Diagnostics.Debug.WriteLine($"[ParseModuleType] 变体解析成功: '{originalInput}' (normalized: '{normalized}') → {parsedType}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[ParseModuleType] 无法识别的模块类型: '{originalInput}' (normalized: '{normalized}')，返回默认值 Windows");
        }

        return parsedType;
    }

    /// <summary>
    /// 判断是否应该为题目创建默认操作点
    /// </summary>
    private static bool ShouldCreateDefaultOperationPoint(StudentSpecializedTrainingQuestionDto questionDto, ModuleType targetModuleType)
    {
        // C#题目的特殊处理
        if (targetModuleType == ModuleType.CSharp && IsCSharpQuestion(questionDto))
        {
            return true;
        }

        // 对于其他模块类型，如果题目有相关内容，也创建默认操作点
        return targetModuleType switch
        {
            ModuleType.Word => !string.IsNullOrWhiteSpace(questionDto.DocumentFilePath) ||
                              questionDto.Content.Contains("Word") || questionDto.Content.Contains("文档"),
            ModuleType.Excel => !string.IsNullOrWhiteSpace(questionDto.DocumentFilePath) ||
                               questionDto.Content.Contains("Excel") || questionDto.Content.Contains("表格"),
            ModuleType.PowerPoint => !string.IsNullOrWhiteSpace(questionDto.DocumentFilePath) ||
                                    questionDto.Content.Contains("PowerPoint") || questionDto.Content.Contains("演示"),
            ModuleType.Windows => questionDto.Content.Contains("文件") || questionDto.Content.Contains("系统") ||
                                 questionDto.Content.Contains("操作"),
            _ => false
        };
    }

    /// <summary>
    /// 为题目创建默认操作点
    /// </summary>
    private static OperationPointModel CreateDefaultOperationPoint(StudentSpecializedTrainingQuestionDto questionDto, ModuleType targetModuleType)
    {
        string operationName = targetModuleType switch
        {
            ModuleType.CSharp => "C#编程操作",
            ModuleType.Word => "Word文档操作",
            ModuleType.Excel => "Excel表格操作",
            ModuleType.PowerPoint => "PowerPoint演示操作",
            ModuleType.Windows => "Windows系统操作",
            _ => $"{targetModuleType}操作"
        };

        string operationDescription = targetModuleType switch
        {
            ModuleType.CSharp => "C#编程题目默认操作点",
            ModuleType.Word => "Word文档处理默认操作点",
            ModuleType.Excel => "Excel表格处理默认操作点",
            ModuleType.PowerPoint => "PowerPoint演示制作默认操作点",
            ModuleType.Windows => "Windows系统操作默认操作点",
            _ => $"{targetModuleType}模块默认操作点"
        };

        return new OperationPointModel
        {
            Id = $"default_op_{questionDto.Id}_{targetModuleType}",
            Name = operationName,
            Description = operationDescription,
            ModuleType = targetModuleType,
            Score = questionDto.Score,
            Order = 1,
            IsEnabled = true,
            Parameters = []
        };
    }

    /// <summary>
    /// 判断是否应该为模拟考试题目创建默认操作点
    /// </summary>
    private static bool ShouldCreateDefaultOperationPointForMockExam(StudentMockExamQuestionDto questionDto, ModuleType targetModuleType)
    {
        // C#题目的特殊处理
        if (targetModuleType == ModuleType.CSharp && IsCSharpQuestion(questionDto))
        {
            return true;
        }

        // 对于其他模块类型，如果题目有相关内容，也创建默认操作点
        return targetModuleType switch
        {
            ModuleType.Word => !string.IsNullOrWhiteSpace(questionDto.DocumentFilePath) ||
                              questionDto.Content.Contains("Word") || questionDto.Content.Contains("文档"),
            ModuleType.Excel => !string.IsNullOrWhiteSpace(questionDto.DocumentFilePath) ||
                               questionDto.Content.Contains("Excel") || questionDto.Content.Contains("表格"),
            ModuleType.PowerPoint => !string.IsNullOrWhiteSpace(questionDto.DocumentFilePath) ||
                                    questionDto.Content.Contains("PowerPoint") || questionDto.Content.Contains("演示"),
            ModuleType.Windows => questionDto.Content.Contains("文件") || questionDto.Content.Contains("系统") ||
                                 questionDto.Content.Contains("操作"),
            _ => false
        };
    }

    /// <summary>
    /// 为模拟考试题目创建默认操作点
    /// </summary>
    private static OperationPointModel CreateDefaultOperationPointForMockExam(StudentMockExamQuestionDto questionDto, ModuleType targetModuleType)
    {
        string operationName = targetModuleType switch
        {
            ModuleType.CSharp => "C#编程操作",
            ModuleType.Word => "Word文档操作",
            ModuleType.Excel => "Excel表格操作",
            ModuleType.PowerPoint => "PowerPoint演示操作",
            ModuleType.Windows => "Windows系统操作",
            _ => $"{targetModuleType}操作"
        };

        string operationDescription = targetModuleType switch
        {
            ModuleType.CSharp => "C#编程题目默认操作点",
            ModuleType.Word => "Word文档处理默认操作点",
            ModuleType.Excel => "Excel表格处理默认操作点",
            ModuleType.PowerPoint => "PowerPoint演示制作默认操作点",
            ModuleType.Windows => "Windows系统操作默认操作点",
            _ => $"{targetModuleType}模块默认操作点"
        };

        return new OperationPointModel
        {
            Id = $"default_op_{questionDto.OriginalQuestionId}_{targetModuleType}",
            Name = operationName,
            Description = operationDescription,
            ModuleType = targetModuleType,
            Score = questionDto.Score,
            Order = 1,
            IsEnabled = true,
            Parameters = []
        };
    }

    /// <summary>
    /// 创建降级考试模型（原有的模拟数据逻辑）
    /// </summary>
    private ExamModel CreateFallbackExamModel(ModuleType moduleType, ExamType examType, int examId)
    {
        ExamModel examModel = new()
        {
            Id = examId.ToString(),
            Name = $"考试_{examId}",
            Description = $"{moduleType}考试",
            Modules = []
        };

        examModel.Modules.Add(CreateBasicModule(moduleType));
        return examModel;
    }

    /// <summary>
    /// 创建基本模块
    /// </summary>
    private static ExamModuleModel CreateBasicModule(ModuleType moduleType)
    {
        ExamModuleModel module = new()
        {
            Id = $"Module_{moduleType}",
            Name = moduleType.ToString(),
            Type = moduleType,
            Questions = []
        };

        QuestionModel question = new()
        {
            Id = $"Question_{moduleType}_1",
            Title = $"{moduleType}操作题",
            Content = $"完成{moduleType}相关操作",
#pragma warning disable CS0618 // 类型或成员已过时
            Score = 100,
#pragma warning restore CS0618 // 类型或成员已过时
            OperationPoints = []
        };

        OperationPointModel operationPoint = new()
        {
            Id = $"OP_{moduleType}_1",
            Name = $"{moduleType}基本操作",
            ModuleType = moduleType,
            Score = 100,
            IsEnabled = true,
            Parameters = []
        };

        question.OperationPoints.Add(operationPoint);
        module.Questions.Add(question);

        return module;
    }

    /// <summary>
    /// 获取模块类型描述
    /// </summary>
    private static string GetModuleTypeDescription(ModuleType moduleType)
    {
        return moduleType switch
        {
            ModuleType.Word => "Word文档",
            ModuleType.Excel => "Excel表格",
            ModuleType.PowerPoint => "PowerPoint演示文稿",
            ModuleType.Windows => "Windows操作",
            ModuleType.CSharp => "C#编程",
            _ => moduleType.ToString()
        };
    }



    #endregion

    #region JSON解析辅助方法

    /// <summary>
    /// 从JsonElement中获取字符串属性
    /// </summary>
    private static string? GetJsonStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    /// <summary>
    /// 从JsonElement中获取双精度浮点数属性
    /// </summary>
    private static double? GetJsonDoubleProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property)
            ? property.ValueKind switch
            {
                JsonValueKind.Number => property.GetDouble(),
                JsonValueKind.String when double.TryParse(property.GetString(), out double value) => value,
                _ => null
            }
            : null;
    }

    /// <summary>
    /// 从JsonElement中获取整数属性
    /// </summary>
    private static int? GetJsonIntProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property)
            ? property.ValueKind switch
            {
                JsonValueKind.Number => property.GetInt32(),
                JsonValueKind.String when int.TryParse(property.GetString(), out int value) => value,
                _ => null
            }
            : null;
    }

    /// <summary>
    /// 从JsonElement中获取数值属性（支持整数和小数）
    /// </summary>
    private static double? GetJsonNumberProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property)
            ? property.ValueKind switch
            {
                JsonValueKind.Number => property.GetDouble(),
                JsonValueKind.String when double.TryParse(property.GetString(), out double value) => value,
                _ => null
            }
            : null;
    }

    /// <summary>
    /// 从JsonElement中获取布尔属性
    /// </summary>
    private static bool? GetJsonBoolProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property)
            ? property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(property.GetString(), out bool value) => value,
                _ => null
            }
            : null;
    }

    /// <summary>
    /// 从JsonElement中解析CodeBlanks数组
    /// </summary>
    private static List<CodeBlankModel>? ParseCodeBlanksFromJson(JsonElement codeBlanksElement)
    {
        if (codeBlanksElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        List<CodeBlankModel> codeBlanks = [];

        foreach (JsonElement blankElement in codeBlanksElement.EnumerateArray())
        {
            CodeBlankModel codeBlank = new()
            {
                Id = GetJsonStringProperty(blankElement, "id") ?? Guid.NewGuid().ToString(),
                Name = GetJsonStringProperty(blankElement, "name") ?? "填空",
                Description = GetJsonStringProperty(blankElement, "description") ?? "",
                Score = GetJsonDoubleProperty(blankElement, "score") ?? 1.0,
                Order = GetJsonIntProperty(blankElement, "order") ?? 1,
                IsEnabled = GetJsonBoolProperty(blankElement, "isEnabled") ?? true,
                StandardAnswer = GetJsonStringProperty(blankElement, "standardAnswer"),
                CreatedTime = GetJsonStringProperty(blankElement, "createdTime") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            codeBlanks.Add(codeBlank);
        }

        return codeBlanks.Count > 0 ? codeBlanks : null;
    }

    #endregion

    #region 路径处理辅助方法

    /// <summary>
    /// 将相对路径转换为绝对路径
    /// </summary>
    /// <param name="filePath">文件路径（可能是相对路径或绝对路径）</param>
    /// <param name="directoryService">目录服务</param>
    /// <returns>绝对路径，如果输入为空则返回空</returns>
    private static string? ConvertToAbsolutePath(string? filePath, IBenchSuiteDirectoryService? directoryService = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return filePath;
        }

        // 如果已经是绝对路径，直接返回
        if (Path.IsPathRooted(filePath))
        {
            return filePath;
        }

        // 如果是相对路径，与基础路径拼接
        if (directoryService != null)
        {
            string basePath = directoryService.GetBasePath();
            string absolutePath = Path.Combine(basePath, filePath);

            System.Diagnostics.Debug.WriteLine($"路径转换: 相对路径='{filePath}', 基础路径='{basePath}', 绝对路径='{absolutePath}'");

            return absolutePath;
        }

        // 如果没有directoryService，返回原路径
        return filePath;
    }

    #endregion
}
