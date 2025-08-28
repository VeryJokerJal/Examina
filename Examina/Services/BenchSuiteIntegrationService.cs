using System.IO;
using System.Reflection;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services;
using Examina.Models;
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

    public BenchSuiteIntegrationService(ILogger<BenchSuiteIntegrationService> logger, IAILogicalScoringService? aiScoringService = null)
    {
        _logger = logger;
        _aiScoringService = aiScoringService;

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

                ScoringResult moduleResult = await ScoreModuleAsync(moduleType, moduleFilePaths, examType, examId);
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
    private async Task<ScoringResult> ScoreModuleAsync(ModuleType moduleType, List<string> filePaths, ExamType examType, int examId)
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

            // 检查是否有文件需要评分
            if (filePaths == null || filePaths.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"没有找到 {moduleType} 类型的文件";
                result.EndTime = DateTime.Now;
                return result;
            }

            // 创建简化的考试模型用于评分
            ExamModel examModel = CreateSimplifiedExamModel(moduleType, examType, examId);

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
    [Obsolete]
    private ExamModel CreateSimplifiedExamModel(ModuleType moduleType, ExamType examType, int examId)
    {
        // 创建简化的考试模型
        ExamModel examModel = new()
        {
            Id = examId.ToString(),
            Name = $"考试_{examId}",
            Description = $"{moduleType}考试",
            Modules = []
        };

        // 创建对应的模块
        ExamModuleModel module = new()
        {
            Id = $"Module_{moduleType}",
            Name = moduleType.ToString(),
            Type = moduleType,
            Questions = []
        };

        // 创建一个简化的题目
        QuestionModel question = new()
        {
            Id = $"Question_{moduleType}_1",
            Title = $"{moduleType}操作题",
            Content = $"完成{moduleType}相关操作",
            Score = 100, // 默认总分100
            OperationPoints = []
        };

        // 添加一个基本的操作点
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
        examModel.Modules.Add(module);

        return examModel;
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
