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
    private readonly Dictionary<ModuleType, string> _directoryMapping;
    private readonly Dictionary<ModuleType, IScoringService> _scoringServices;
    private readonly IAILogicalScoringService? _aiScoringService;
    private readonly IStudentExamService? _studentExamService;
    private readonly IStudentMockExamService? _studentMockExamService;
    private readonly IStudentComprehensiveTrainingService? _studentComprehensiveTrainingService;
    private readonly IStudentSpecializedTrainingService? _studentSpecializedTrainingService;

    public BenchSuiteIntegrationService(
        ILogger<BenchSuiteIntegrationService> logger,
        IAILogicalScoringService? aiScoringService = null,
        IStudentExamService? studentExamService = null,
        IStudentMockExamService? studentMockExamService = null,
        IStudentComprehensiveTrainingService? studentComprehensiveTrainingService = null,
        IStudentSpecializedTrainingService? studentSpecializedTrainingService = null)
    {
        _logger = logger;
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
            { ModuleType.Windows, "WINDOWS" }
        };

        // 初始化真实的BenchSuite评分服务，C#服务支持AI功能
        _scoringServices = new Dictionary<ModuleType, IScoringService>
        {
            { ModuleType.Word, new WordScoringService() },
            { ModuleType.Excel, new ExcelScoringService() },
            { ModuleType.PowerPoint, new PowerPointScoringService() },
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
                string benchSuitePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BenchSuite.dll");
                if (System.IO.File.Exists(benchSuitePath))
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
            if (!System.IO.Directory.Exists(basePath))
            {
                _logger.LogWarning("基础目录不存在: {BasePath}", basePath);
                return false;
            }

            // 检查各子目录是否存在
            foreach (KeyValuePair<ModuleType, string> mapping in _directoryMapping)
            {
                string directoryPath = System.IO.Path.Combine(basePath, mapping.Value);
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    _logger.LogWarning("缺失目录: {DirectoryPath}", directoryPath);
                    // 尝试创建缺失的目录
                    try
                    {
                        _ = System.IO.Directory.CreateDirectory(directoryPath);
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
    /// 验证考试目录结构
    /// </summary>
    public async Task<bool> ValidateExamDirectoryStructureAsync(ExamType examType, int examId)
    {
        try
        {
            _logger.LogInformation("验证考试目录结构，考试类型: {ExamType}, 考试ID: {ExamId}", examType, examId);

            string basePath = @"C:\河北对口计算机\";
            string examTypeFolder = GetExamTypeFolder(examType);
            string examTypePath = System.IO.Path.Combine(basePath, examTypeFolder);
            string examIdPath = System.IO.Path.Combine(examTypePath, examId.ToString());

            // 检查基础目录是否存在
            if (!System.IO.Directory.Exists(basePath))
            {
                _logger.LogWarning("基础目录不存在: {BasePath}", basePath);
                return false;
            }

            // 检查考试类型目录是否存在
            if (!System.IO.Directory.Exists(examTypePath))
            {
                _logger.LogWarning("考试类型目录不存在: {ExamTypePath}", examTypePath);
                return false;
            }

            // 检查考试ID目录是否存在
            if (!System.IO.Directory.Exists(examIdPath))
            {
                _logger.LogWarning("考试ID目录不存在: {ExamIdPath}", examIdPath);
                return false;
            }

            _logger.LogInformation("考试目录结构验证通过");
            return true;
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
                string basePath = @"C:\河北对口计算机\";
                string examTypeFolder = GetExamTypeFolder(examType);
                string examRootPath = System.IO.Path.Combine(basePath, examTypeFolder, examId.ToString());

                ExamModel examModelToUse = await CreateSimplifiedExamModel(moduleType, examType, examId, studentUserId);

                // 为Windows评分服务设置基础路径
                if (scoringService is WindowsScoringService windowsService)
                {
                    windowsService.SetBasePath(examRootPath);
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
            TotalScore = (double)(mockExamDto.TotalScore > 0 ? mockExamDto.TotalScore : 100m),
            DurationMinutes = mockExamDto.DurationMinutes > 0 ? mockExamDto.DurationMinutes : 120,
            Modules = []
        };

        // 模拟考试没有模块概念，需要根据题目的操作点创建虚拟模块
        // 按目标模块类型过滤相关题目
        _logger.LogDebug("开始过滤模拟考试题目，目标模块类型: {TargetModuleType}, 总题目数: {TotalQuestions}",
            targetModuleType, mockExamDto.Questions.Count);

        IEnumerable<StudentMockExamQuestionDto> relevantQuestions = mockExamDto.Questions
            .Where(q => q.OperationPoints.Any(op => IsModuleTypeMatch(op.ModuleType, targetModuleType)));

        _logger.LogDebug("过滤后的相关题目数量: {RelevantQuestionsCount}", relevantQuestions.Count());

        // 记录每个题目的操作点模块类型，用于调试
        foreach (StudentMockExamQuestionDto question in mockExamDto.Questions)
        {
            string operationPointTypes = string.Join(", ", question.OperationPoints.Select(op => $"'{op.ModuleType}'"));
            _logger.LogDebug("题目 {QuestionId} 的操作点模块类型: [{OperationPointTypes}]",
                question.OriginalQuestionId, operationPointTypes);
        }

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
                    Score = questionDto.Score,
#pragma warning restore CS0618 // 类型或成员已过时
                    Order = questionDto.SortOrder,
                    OperationPoints = []
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

                if (question.OperationPoints.Count > 0)
                {
                    module.Questions.Add(question);
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
                QuestionModel question = MapComprehensiveTrainingQuestionToQuestionModel(questionDto, targetModuleType);
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
                        QuestionModel question = MapComprehensiveTrainingQuestionToQuestionModel(questionDto, targetModuleType);
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
                    QuestionModel question = MapSpecializedTrainingQuestionToQuestionModel(questionDto, targetModuleType);
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
                QuestionModel question = MapSpecializedTrainingQuestionToQuestionModel(questionDto, targetModuleType);
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
    private static QuestionModel MapComprehensiveTrainingQuestionToQuestionModel(StudentComprehensiveTrainingQuestionDto questionDto, ModuleType targetModuleType)
    {
        QuestionModel question = new()
        {
            Id = questionDto.Id.ToString(),
            Title = string.IsNullOrWhiteSpace(questionDto.Title) ? $"题目_{questionDto.Id}" : questionDto.Title,
            Content = questionDto.Content ?? string.Empty,
#pragma warning disable CS0618 // 类型或成员已过时
            Score = questionDto.Score,
#pragma warning restore CS0618 // 类型或成员已过时
            Order = questionDto.SortOrder,
            OperationPoints = []
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
    private static QuestionModel MapSpecializedTrainingQuestionToQuestionModel(StudentSpecializedTrainingQuestionDto questionDto, ModuleType targetModuleType)
    {
        QuestionModel question = new()
        {
            Id = questionDto.Id.ToString(),
            Title = string.IsNullOrWhiteSpace(questionDto.Title) ? $"题目_{questionDto.Id}" : questionDto.Title,
            Content = questionDto.Content ?? string.Empty,
#pragma warning disable CS0618 // 类型或成员已过时
            Score = questionDto.Score,
#pragma warning restore CS0618 // 类型或成员已过时
            Order = questionDto.Order,
            OperationPoints = []
        };

        // 只添加匹配目标模块类型的操作点
        foreach (StudentSpecializedTrainingOperationPointDto opDto in questionDto.OperationPoints
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
                Parameters = MapSpecializedTrainingParametersToConfigurationParameters(opDto.Parameters)
            };

            question.OperationPoints.Add(operationPoint);
        }

        return question;
    }

    /// <summary>
    /// 映射模拟考试参数到配置参数
    /// </summary>
    private static List<ConfigurationParameterModel> MapMockExamParametersToConfigurationParameters(IEnumerable<StudentMockExamParameterDto> parameters)
    {
        List<ConfigurationParameterModel> configParams = [];

        int order = 1;
        foreach (StudentMockExamParameterDto paramDto in parameters)
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
    /// 映射专项训练参数到配置参数
    /// </summary>
    private static List<ConfigurationParameterModel> MapSpecializedTrainingParametersToConfigurationParameters(IEnumerable<StudentSpecializedTrainingParameterDto> parameters)
    {
        List<ConfigurationParameterModel> configParams = [];

        int order = 1;
        foreach (StudentSpecializedTrainingParameterDto paramDto in parameters)
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
    /// 解析参数类型字符串为ParameterType枚举
    /// </summary>
    private static BenchSuite.Models.ParameterType ParseParameterType(string? parameterTypeString)
    {
        return string.IsNullOrWhiteSpace(parameterTypeString)
            ? ParameterType.Text
            : parameterTypeString.ToLowerInvariant() switch
            {
                "string" or "text" => BenchSuite.Models.ParameterType.Text,
                "int" or "integer" or "number" => BenchSuite.Models.ParameterType.Number,
                "bool" or "boolean" => BenchSuite.Models.ParameterType.Boolean,
                "enum" or "enumeration" => BenchSuite.Models.ParameterType.Enum,
                "color" => BenchSuite.Models.ParameterType.Color,
                "file" or "filepath" => BenchSuite.Models.ParameterType.File,
                "multiplechoice" or "multiple_choice" => BenchSuite.Models.ParameterType.MultipleChoice,
                "date" or "datetime" => BenchSuite.Models.ParameterType.Date,
                _ => BenchSuite.Models.ParameterType.Text // 默认值
            };
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
            TotalScore = (double)100m,
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
            TotalScore = (double)(examDto.TotalScore > 0 ? examDto.TotalScore : 100m),
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
    /// 检查模块类型字符串是否与目标模块类型匹配
    /// </summary>
    private static bool IsModuleTypeMatch(string moduleTypeString, ModuleType targetModuleType)
    {
        if (string.IsNullOrWhiteSpace(moduleTypeString))
        {
            return false;
        }

        // 首先尝试解析为ModuleType，然后比较
        ModuleType parsedType = ParseModuleType(moduleTypeString);
        return parsedType == targetModuleType;
    }

    /// <summary>
    /// 解析模块类型字符串为枚举（增强版，支持更多C#变体）
    /// </summary>
    private static ModuleType ParseModuleType(string moduleTypeString)
    {
        if (string.IsNullOrWhiteSpace(moduleTypeString))
        {
            return ModuleType.Windows; // 默认值
        }

        // 首先尝试直接解析
        if (Enum.TryParse<ModuleType>(moduleTypeString, true, out ModuleType result))
        {
            return result;
        }

        // 处理各种别名和变体
        string normalized = moduleTypeString.Trim().ToLowerInvariant();
        return normalized switch
        {
            // PowerPoint 变体
            "ppt" or "powerpoint" or "power-point" or "power_point" => ModuleType.PowerPoint,

            // Word 变体
            "word" or "msword" or "ms-word" or "microsoft-word" => ModuleType.Word,

            // Excel 变体
            "excel" or "msexcel" or "ms-excel" or "microsoft-excel" => ModuleType.Excel,

            // Windows 变体
            "windows" or "win" or "os" or "操作系统" => ModuleType.Windows,

            // C# 变体（重点增强）
            "csharp" or "c#" or "c-sharp" or "c_sharp" or "cs" or "dotnet" or ".net" or "编程" or "程序设计" => ModuleType.CSharp,

            _ => ModuleType.Windows // 默认值
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

    /// <summary>
    /// 获取考试类型对应的文件夹名称
    /// </summary>
    private static string GetExamTypeFolder(ExamType examType)
    {
        return examType switch
        {
            ExamType.MockExam => "MockExams",
            ExamType.FormalExam => "OnlineExams",
            ExamType.ComprehensiveTraining => "ComprehensiveTraining",
            ExamType.SpecializedTraining => "SpecializedTraining",
            ExamType.Practice => "Practice",
            ExamType.SpecialPractice => "SpecialPractice",
            _ => "Unknown"
        };
    }

    #endregion
}
