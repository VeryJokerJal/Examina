using System.ComponentModel;
using System.IO;
using System.Reflection;
using Examina.Models.BenchSuite;
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
    private readonly Dictionary<BenchSuiteFileType, string> _directoryMapping;
    private readonly Dictionary<BenchSuiteFileType, IScoringService> _scoringServices;

    public BenchSuiteIntegrationService(ILogger<BenchSuiteIntegrationService> logger)
    {
        _logger = logger;
        _directoryMapping = new Dictionary<BenchSuiteFileType, string>
        {
            { BenchSuiteFileType.CSharp, "CSharp" },
            { BenchSuiteFileType.PowerPoint, "PPT" },
            { BenchSuiteFileType.Word, "WORD" },
            { BenchSuiteFileType.Excel, "EXCEL" },
            { BenchSuiteFileType.Windows, "WINDOWS" }
        };

        // 初始化真实的BenchSuite评分服务
        _scoringServices = new Dictionary<BenchSuiteFileType, IScoringService>
        {
            { BenchSuiteFileType.Word, new WordScoringService() },
            { BenchSuiteFileType.Excel, new ExcelScoringService() },
            { BenchSuiteFileType.PowerPoint, new PowerPointScoringService() },
            { BenchSuiteFileType.Windows, new WindowsScoringService() },
            { BenchSuiteFileType.CSharp, new CSharpScoringService() }
        };
    }

    /// <summary>
    /// 对考试文件进行评分
    /// </summary>
    public async Task<BenchSuiteScoringResult> ScoreExamAsync(BenchSuiteScoringRequest request)
    {
        BenchSuiteScoringResult result = new()
        {
            StartTime = DateTime.Now
        };

        try
        {
            _logger.LogInformation("开始BenchSuite评分，考试ID: {ExamId}, 考试类型: {ExamType}, 学生ID: {StudentId}",
                request.ExamId, request.ExamType, request.StudentUserId);

            // 验证考试目录结构
            BenchSuiteDirectoryValidationResult validationResult = await ValidateExamDirectoryStructureAsync(request.ExamType, request.ExamId);
            if (!validationResult.IsValid)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"考试目录结构验证失败: {validationResult.ErrorMessage}";
                return result;
            }

            // 检查BenchSuite服务是否可用
            bool serviceAvailable = await IsServiceAvailableAsync();
            if (!serviceAvailable)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "BenchSuite服务不可用";
                return result;
            }

            // 按文件类型进行评分
            foreach (KeyValuePair<BenchSuiteFileType, List<string>> fileTypeGroup in request.FilePaths)
            {
                BenchSuiteFileType fileType = fileTypeGroup.Key;
                List<string> filePaths = fileTypeGroup.Value;

                _logger.LogInformation("开始评分文件类型: {FileType}, 文件数量: {FileCount}",
                    GetFileTypeDescription(fileType), filePaths.Count);

                FileTypeScoringResult fileTypeResult = await ScoreFileTypeAsync(fileType, filePaths, request);
                result.FileTypeResults[fileType] = fileTypeResult;

                result.TotalScore += fileTypeResult.TotalScore;
                result.AchievedScore += fileTypeResult.AchievedScore;
            }

            result.IsSuccess = true;
            _logger.LogInformation("BenchSuite评分完成，总分: {TotalScore}, 得分: {AchievedScore}, 得分率: {ScoreRate:P2}",
                result.TotalScore, result.AchievedScore, result.ScoreRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BenchSuite评分过程中发生异常");
            result.IsSuccess = false;
            result.ErrorMessage = $"评分过程中发生异常: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
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
    /// 获取支持的文件类型
    /// </summary>
    public IEnumerable<BenchSuiteFileType> GetSupportedFileTypes()
    {
        return Enum.GetValues<BenchSuiteFileType>();
    }

    /// <summary>
    /// 验证文件目录结构
    /// </summary>
    public async Task<BenchSuiteDirectoryValidationResult> ValidateDirectoryStructureAsync(string basePath)
    {
        BenchSuiteDirectoryValidationResult result = new();

        try
        {
            _logger.LogInformation("验证BenchSuite目录结构，基础路径: {BasePath}", basePath);

            // 检查基础目录是否存在
            if (!System.IO.Directory.Exists(basePath))
            {
                result.IsValid = false;
                result.ErrorMessage = $"基础目录不存在: {basePath}";
                result.MissingDirectories.Add(basePath);
                return result;
            }

            // 检查各子目录是否存在
            List<string> missingDirectories = new();
            foreach (KeyValuePair<BenchSuiteFileType, string> mapping in _directoryMapping)
            {
                string directoryPath = System.IO.Path.Combine(basePath, mapping.Value);
                if (!System.IO.Directory.Exists(directoryPath))
                {
                    missingDirectories.Add(directoryPath);
                    _logger.LogWarning("缺失目录: {DirectoryPath}", directoryPath);
                }
            }

            if (missingDirectories.Count > 0)
            {
                // 尝试创建缺失的目录
                foreach (string missingDir in missingDirectories)
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(missingDir);
                        _logger.LogInformation("成功创建目录: {DirectoryPath}", missingDir);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "创建目录失败: {DirectoryPath}", missingDir);
                        result.MissingDirectories.Add(missingDir);
                    }
                }
            }

            result.IsValid = result.MissingDirectories.Count == 0;
            result.Details = result.IsValid ? "目录结构验证通过" : $"缺失 {result.MissingDirectories.Count} 个目录";

            _logger.LogInformation("目录结构验证完成，结果: {IsValid}", result.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证目录结构时发生异常");
            result.IsValid = false;
            result.ErrorMessage = $"验证目录结构时发生异常: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 验证考试目录结构
    /// </summary>
    public async Task<BenchSuiteDirectoryValidationResult> ValidateExamDirectoryStructureAsync(ExamType examType, int examId)
    {
        BenchSuiteDirectoryValidationResult result = new();

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
                result.IsValid = false;
                result.ErrorMessage = $"基础目录不存在: {basePath}";
                result.MissingDirectories.Add(basePath);
                return result;
            }

            // 检查考试类型目录是否存在
            if (!System.IO.Directory.Exists(examTypePath))
            {
                result.IsValid = false;
                result.ErrorMessage = $"考试类型目录不存在: {examTypePath}";
                result.MissingDirectories.Add(examTypePath);
                return result;
            }

            // 检查考试ID目录是否存在
            if (!System.IO.Directory.Exists(examIdPath))
            {
                result.IsValid = false;
                result.ErrorMessage = $"考试ID目录不存在: {examIdPath}";
                result.MissingDirectories.Add(examIdPath);
                return result;
            }

            // 检查各科目目录是否存在
            List<string> missingDirectories = new();
            foreach (KeyValuePair<BenchSuiteFileType, string> mapping in _directoryMapping)
            {
                string subjectPath = System.IO.Path.Combine(examIdPath, mapping.Value);
                if (!System.IO.Directory.Exists(subjectPath))
                {
                    missingDirectories.Add(subjectPath);
                    _logger.LogWarning("缺失科目目录: {SubjectPath}", subjectPath);
                }
            }

            if (missingDirectories.Count > 0)
            {
                result.IsValid = false;
                result.ErrorMessage = $"缺失 {missingDirectories.Count} 个科目目录";
                result.MissingDirectories.AddRange(missingDirectories);
                return result;
            }

            result.IsValid = true;
            result.Details = "考试目录结构验证通过";

            _logger.LogInformation("考试目录结构验证完成，结果: {IsValid}", result.IsValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证考试目录结构时发生异常");
            result.IsValid = false;
            result.ErrorMessage = $"验证考试目录结构时发生异常: {ex.Message}";
        }

        return result;
    }

    #region 私有方法

    /// <summary>
    /// 对指定文件类型进行评分
    /// </summary>
    private async Task<FileTypeScoringResult> ScoreFileTypeAsync(BenchSuiteFileType fileType, List<string> filePaths, BenchSuiteScoringRequest request)
    {
        FileTypeScoringResult result = new()
        {
            FileType = fileType
        };

        try
        {
            // 检查是否有对应的评分服务
            if (!_scoringServices.TryGetValue(fileType, out IScoringService? scoringService))
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"不支持的文件类型: {GetFileTypeDescription(fileType)}";
                return result;
            }

            // 检查是否有文件需要评分
            if (filePaths == null || filePaths.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"没有找到 {GetFileTypeDescription(fileType)} 类型的文件";
                return result;
            }

            // 创建简化的考试模型用于评分
            ExamModel examModel = CreateSimplifiedExamModel(fileType, request);

            decimal totalScore = 0;
            decimal achievedScore = 0;
            List<string> details = new();

            // 对每个文件进行评分
            foreach (string filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    details.Add($"文件不存在: {filePath}");
                    continue;
                }

                try
                {
                    // 调用真实的BenchSuite评分服务
                    ScoringResult fileResult = await scoringService.ScoreFileAsync(filePath, examModel);

                    totalScore += fileResult.TotalScore;
                    achievedScore += fileResult.AchievedScore;

                    details.Add($"文件 {Path.GetFileName(filePath)}: {fileResult.AchievedScore}/{fileResult.TotalScore}");

                    if (!fileResult.IsSuccess)
                    {
                        details.Add($"评分警告: {fileResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    details.Add($"文件 {Path.GetFileName(filePath)} 评分失败: {ex.Message}");
                    _logger.LogWarning(ex, "文件评分失败: {FilePath}", filePath);
                }
            }

            result.TotalScore = totalScore;
            result.AchievedScore = achievedScore;
            result.IsSuccess = true;
            result.Details = string.Join("; ", details);

            _logger.LogInformation("文件类型 {FileType} 评分完成，得分: {AchievedScore}/{TotalScore}",
                GetFileTypeDescription(fileType), result.AchievedScore, result.TotalScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件类型 {FileType} 评分失败", GetFileTypeDescription(fileType));
            result.IsSuccess = false;
            result.ErrorMessage = $"评分失败: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 创建简化的考试模型用于评分
    /// </summary>
    private ExamModel CreateSimplifiedExamModel(BenchSuiteFileType fileType, BenchSuiteScoringRequest request)
    {
        // 创建简化的考试模型
        ExamModel examModel = new()
        {
            Id = request.ExamId.ToString(),
            Name = $"考试_{request.ExamId}",
            Description = $"{GetFileTypeDescription(fileType)}考试",
            Modules = new List<ExamModuleModel>()
        };

        // 根据文件类型创建对应的模块
        ModuleType moduleType = GetModuleTypeFromFileType(fileType);
        ExamModuleModel module = new()
        {
            Id = $"Module_{fileType}",
            Name = GetFileTypeDescription(fileType),
            Type = moduleType,
            Questions = new List<QuestionModel>()
        };

        // 创建一个简化的题目
        QuestionModel question = new()
        {
            Id = $"Question_{fileType}_1",
            Title = $"{GetFileTypeDescription(fileType)}操作题",
            Content = $"完成{GetFileTypeDescription(fileType)}相关操作",
            Score = 100, // 默认总分100
            OperationPoints = new List<OperationPointModel>()
        };

        // 添加一个基本的操作点
        OperationPointModel operationPoint = new()
        {
            Id = $"OP_{fileType}_1",
            Name = $"{GetFileTypeDescription(fileType)}基本操作",
            ModuleType = moduleType,
            Score = 100,
            IsEnabled = true,
            Parameters = new List<ConfigurationParameterModel>()
        };

        question.OperationPoints.Add(operationPoint);
        module.Questions.Add(question);
        examModel.Modules.Add(module);

        return examModel;
    }

    /// <summary>
    /// 根据文件类型获取模块类型
    /// </summary>
    private ModuleType GetModuleTypeFromFileType(BenchSuiteFileType fileType)
    {
        return fileType switch
        {
            BenchSuiteFileType.Word => ModuleType.Word,
            BenchSuiteFileType.Excel => ModuleType.Excel,
            BenchSuiteFileType.PowerPoint => ModuleType.PowerPoint,
            BenchSuiteFileType.Windows => ModuleType.Windows,
            BenchSuiteFileType.CSharp => ModuleType.CSharp,
            _ => ModuleType.Word // 默认值
        };
    }

    /// <summary>
    /// 获取文件类型描述
    /// </summary>
    private static string GetFileTypeDescription(BenchSuiteFileType fileType)
    {
        FieldInfo? field = fileType.GetType().GetField(fileType.ToString());
        DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? fileType.ToString();
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
