using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Examina.Models;
using Examina.Models.Exam;
using Examina.Configuration;
using Microsoft.Extensions.Logging;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services;

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

    public BenchSuiteIntegrationService(
        ILogger<BenchSuiteIntegrationService> logger,
        IAILogicalScoringService? aiScoringService = null,
        IStudentExamService? studentExamService = null,
        IStudentMockExamService? studentMockExamService = null,
        IStudentComprehensiveTrainingService? studentComprehensiveTrainingService = null)
    {
        _logger = logger;
        _aiScoringService = aiScoringService;
        _studentExamService = studentExamService;
        _studentMockExamService = studentMockExamService;
        _studentComprehensiveTrainingService = studentComprehensiveTrainingService;

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
        Dictionary<ModuleType, ScoringResult> results = new();

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
                    Assembly.LoadFrom(benchSuitePath);
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
                        System.IO.Directory.CreateDirectory(directoryPath);
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
                    decimal totalScore = 0;
                    decimal achievedScore = 0;
                    List<KnowledgePointResult> allKnowledgePoints = new();

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
            ExamType.SpecialPractice => await GetFormalExamDataAsync(examId, studentUserId), // 暂时使用相同的API
            ExamType.SpecializedTraining => await GetFormalExamDataAsync(examId, studentUserId), // 暂时使用相同的API
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
    /// 根据考试类型映射考试数据到ExamModel
    /// </summary>
    private ExamModel MapExamDataToExamModel(object examData, ExamType examType, ModuleType targetModuleType)
    {
        return examType switch
        {
            ExamType.MockExam => MapMockExamToExamModel(examData, targetModuleType),
            ExamType.ComprehensiveTraining => MapComprehensiveTrainingToExamModel(examData, targetModuleType),
            ExamType.FormalExam or ExamType.Practice or ExamType.SpecialPractice or ExamType.SpecializedTraining
                => MapStudentExamDtoToExamModel((StudentExamDto)examData, targetModuleType),
            _ => throw new NotSupportedException($"不支持的考试类型: {examType}")
        };
    }

    /// <summary>
    /// 映射模拟考试数据到ExamModel
    /// </summary>
    private ExamModel MapMockExamToExamModel(object mockExamData, ModuleType targetModuleType)
    {
        // 这里需要根据实际的模拟考试DTO结构进行映射
        // 暂时使用基础映射，后续可以根据具体需求完善
        _logger.LogInformation("映射模拟考试数据到ExamModel，目标模块类型: {ModuleType}", targetModuleType);

        // TODO: 实现具体的模拟考试数据映射逻辑
        // 目前返回基础模型，避免编译错误
        return CreateBasicExamModel("模拟考试", targetModuleType);
    }

    /// <summary>
    /// 映射综合实训数据到ExamModel
    /// </summary>
    private ExamModel MapComprehensiveTrainingToExamModel(object trainingData, ModuleType targetModuleType)
    {
        // 这里需要根据实际的综合实训DTO结构进行映射
        // 暂时使用基础映射，后续可以根据具体需求完善
        _logger.LogInformation("映射综合实训数据到ExamModel，目标模块类型: {ModuleType}", targetModuleType);

        // TODO: 实现具体的综合实训数据映射逻辑
        // 目前返回基础模型，避免编译错误
        return CreateBasicExamModel("综合实训", targetModuleType);
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
            TotalScore = 100m,
            DurationMinutes = 120,
            Modules = new List<ExamModuleModel>()
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
            TotalScore = examDto.TotalScore > 0 ? (decimal)examDto.TotalScore : 100m,
            DurationMinutes = examDto.DurationMinutes > 0 ? examDto.DurationMinutes : 120,
            Modules = new List<ExamModuleModel>()
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
                Score = (decimal)moduleDto.Score,
                Order = moduleDto.Order,
                Questions = new List<QuestionModel>()
            };

            foreach (StudentQuestionDto questionDto in moduleDto.Questions)
            {
                QuestionModel question = new()
                {
                    Id = questionDto.Id.ToString(),
                    Title = questionDto.Title,
                    Content = questionDto.Content,
                    Score = (decimal)questionDto.Score,
                    Order = questionDto.SortOrder,
                    OperationPoints = new List<OperationPointModel>()
                };

                foreach (StudentOperationPointDto opDto in questionDto.OperationPoints)
                {
                    OperationPointModel operationPoint = new()
                    {
                        Id = opDto.Id.ToString(),
                        Name = opDto.Name,
                        Description = opDto.Description,
                        ModuleType = ParseModuleType(opDto.ModuleType),
                        Score = (decimal)opDto.Score,
                        Order = opDto.Order,
                        IsEnabled = true,
                        Parameters = new List<ConfigurationParameterModel>()
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
    /// 解析模块类型字符串为枚举
    /// </summary>
    private static ModuleType ParseModuleType(string moduleTypeString)
    {
        if (Enum.TryParse<ModuleType>(moduleTypeString, true, out ModuleType result))
        {
            return result;
        }

        // 处理一些常见的别名
        return moduleTypeString.ToLowerInvariant() switch
        {
            "ppt" or "powerpoint" => ModuleType.PowerPoint,
            "word" => ModuleType.Word,
            "excel" => ModuleType.Excel,
            "windows" => ModuleType.Windows,
            "csharp" or "c#" => ModuleType.CSharp,
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
            Modules = new List<ExamModuleModel>()
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
            Questions = new List<QuestionModel>()
        };

        QuestionModel question = new()
        {
            Id = $"Question_{moduleType}_1",
            Title = $"{moduleType}操作题",
            Content = $"完成{moduleType}相关操作",
            Score = 100,
            OperationPoints = new List<OperationPointModel>()
        };

        OperationPointModel operationPoint = new()
        {
            Id = $"OP_{moduleType}_1",
            Name = $"{moduleType}基本操作",
            ModuleType = moduleType,
            Score = 100,
            IsEnabled = true,
            Parameters = new List<ConfigurationParameterModel>()
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
