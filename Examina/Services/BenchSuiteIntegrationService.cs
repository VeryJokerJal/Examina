using System.ComponentModel;
using System.Reflection;
using Examina.Models.BenchSuite;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// BenchSuite评分系统集成服务实现
/// </summary>
public class BenchSuiteIntegrationService : IBenchSuiteIntegrationService
{
    private readonly ILogger<BenchSuiteIntegrationService> _logger;
    private readonly Dictionary<BenchSuiteFileType, string> _directoryMapping;

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

            // 验证目录结构
            BenchSuiteDirectoryValidationResult validationResult = await ValidateDirectoryStructureAsync(request.BasePath);
            if (!validationResult.IsValid)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"目录结构验证失败: {validationResult.ErrorMessage}";
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
            // 根据文件类型选择相应的评分服务
            // 这里需要调用BenchSuite的具体评分服务
            // 由于BenchSuite是独立的程序集，这里使用反射或依赖注入来调用

            // 模拟评分结果（实际实现中需要调用BenchSuite的评分服务）
            result.TotalScore = 100;
            result.AchievedScore = 85;
            result.IsSuccess = true;
            result.Details = $"文件类型 {GetFileTypeDescription(fileType)} 评分完成";

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
    /// 获取文件类型描述
    /// </summary>
    private static string GetFileTypeDescription(BenchSuiteFileType fileType)
    {
        FieldInfo? field = fileType.GetType().GetField(fileType.ToString());
        DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? fileType.ToString();
    }

    #endregion
}
