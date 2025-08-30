using System.IO;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using Microsoft.Extensions.Logging;

namespace Examina.Services;

/// <summary>
/// OpenXML评分服务管理器
/// 根据文件类型自动选择合适的评分服务
/// </summary>
public class OpenXmlScoringManager
{
    private readonly IWordScoringService _wordScoringService;
    private readonly IPowerPointScoringService _powerPointScoringService;
    private readonly IExcelScoringService _excelScoringService;
    private readonly ILogger<OpenXmlScoringManager> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OpenXmlScoringManager(
        IWordScoringService wordScoringService,
        IPowerPointScoringService powerPointScoringService,
        IExcelScoringService excelScoringService,
        ILogger<OpenXmlScoringManager> logger)
    {
        _wordScoringService = wordScoringService;
        _powerPointScoringService = powerPointScoringService;
        _excelScoringService = excelScoringService;
        _logger = logger;
    }

    /// <summary>
    /// 根据文件路径获取合适的评分服务
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>评分服务</returns>
    public IScoringService GetScoringService(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".docx" or ".doc" => _wordScoringService,
            ".pptx" or ".ppt" => _powerPointScoringService,
            ".xlsx" or ".xls" => _excelScoringService,
            _ => throw new NotSupportedException($"不支持的文件类型: {extension}")
        };
    }

    /// <summary>
    /// 检查文件是否支持OpenXML评分
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>是否支持</returns>
    public bool IsFileSupported(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".docx" or ".doc" or ".pptx" or ".ppt" or ".xlsx" or ".xls";
    }

    /// <summary>
    /// 对文件进行评分
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="examModel">试卷模型</param>
    /// <param name="configuration">评分配置</param>
    /// <returns>评分结果</returns>
    public async Task<ScoringResult> ScoreFileAsync(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        try
        {
            _logger.LogInformation("开始对文件进行OpenXML评分: {FilePath}", filePath);

            if (!IsFileSupported(filePath))
            {
                throw new NotSupportedException($"不支持的文件类型: {Path.GetExtension(filePath)}");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: {filePath}");
            }

            IScoringService scoringService = GetScoringService(filePath);
            ScoringResult result = await scoringService.ScoreFileAsync(filePath, examModel, configuration);

            _logger.LogInformation("文件评分完成: {FilePath}, 总分: {TotalScore}, 得分: {AchievedScore}",
                filePath, result.TotalScore, result.AchievedScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件评分失败: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// 对单个知识点进行检测
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="knowledgePointType">知识点类型</param>
    /// <param name="parameters">检测参数</param>
    /// <returns>知识点检测结果</returns>
    public async Task<KnowledgePointResult> DetectKnowledgePointAsync(string filePath, string knowledgePointType, Dictionary<string, string> parameters)
    {
        try
        {
            _logger.LogInformation("开始检测知识点: {FilePath}, 类型: {KnowledgePointType}", filePath, knowledgePointType);

            if (!IsFileSupported(filePath))
            {
                throw new NotSupportedException($"不支持的文件类型: {Path.GetExtension(filePath)}");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: {filePath}");
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            KnowledgePointResult result = extension switch
            {
                ".docx" or ".doc" => await _wordScoringService.DetectKnowledgePointAsync(filePath, knowledgePointType, parameters),
                ".pptx" or ".ppt" => await _powerPointScoringService.DetectKnowledgePointAsync(filePath, knowledgePointType, parameters),
                ".xlsx" or ".xls" => await _excelScoringService.DetectKnowledgePointAsync(filePath, knowledgePointType, parameters),
                _ => throw new NotSupportedException($"不支持的文件类型: {extension}")
            };

            _logger.LogInformation("知识点检测完成: {FilePath}, 类型: {KnowledgePointType}, 结果: {IsCorrect}",
                filePath, knowledgePointType, result.IsCorrect);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "知识点检测失败: {FilePath}, 类型: {KnowledgePointType}", filePath, knowledgePointType);
            throw;
        }
    }

    /// <summary>
    /// 批量检测知识点
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="knowledgePoints">要检测的知识点列表</param>
    /// <returns>知识点检测结果列表</returns>
    public async Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(string filePath, List<OperationPointModel> knowledgePoints)
    {
        try
        {
            _logger.LogInformation("开始批量检测知识点: {FilePath}, 数量: {Count}", filePath, knowledgePoints.Count);

            if (!IsFileSupported(filePath))
            {
                throw new NotSupportedException($"不支持的文件类型: {Path.GetExtension(filePath)}");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"文件不存在: {filePath}");
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            List<KnowledgePointResult> results = extension switch
            {
                ".docx" or ".doc" => await _wordScoringService.DetectKnowledgePointsAsync(filePath, knowledgePoints),
                ".pptx" or ".ppt" => await _powerPointScoringService.DetectKnowledgePointsAsync(filePath, knowledgePoints),
                ".xlsx" or ".xls" => await _excelScoringService.DetectKnowledgePointsAsync(filePath, knowledgePoints),
                _ => throw new NotSupportedException($"不支持的文件类型: {extension}")
            };

            int correctCount = results.Count(r => r.IsCorrect);
            _logger.LogInformation("批量知识点检测完成: {FilePath}, 总数: {Total}, 正确: {Correct}",
                filePath, results.Count, correctCount);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量知识点检测失败: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    /// <returns>支持的文件扩展名列表</returns>
    public IEnumerable<string> GetSupportedExtensions()
    {
        return new[] { ".docx", ".doc", ".pptx", ".ppt", ".xlsx", ".xls" };
    }

    /// <summary>
    /// 获取文件类型描述
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>文件类型描述</returns>
    public string GetFileTypeDescription(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return "未知";
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".docx" or ".doc" => "Word文档",
            ".pptx" or ".ppt" => "PowerPoint演示文稿",
            ".xlsx" or ".xls" => "Excel工作簿",
            _ => "不支持的文件类型"
        };
    }
}
