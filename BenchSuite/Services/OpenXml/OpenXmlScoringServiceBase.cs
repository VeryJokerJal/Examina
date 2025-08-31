using BenchSuite.Interfaces;
using BenchSuite.Models;
using DocumentFormat.OpenXml.Packaging;

namespace BenchSuite.Services.OpenXml;

/// <summary>
/// OpenXML评分服务基类
/// </summary>
public abstract class OpenXmlScoringServiceBase : IScoringService
{
    protected readonly ScoringConfiguration _defaultConfiguration;
    protected abstract string[] SupportedExtensions { get; }

    protected OpenXmlScoringServiceBase()
    {
        _defaultConfiguration = new ScoringConfiguration();
    }

    /// <summary>
    /// 对文件进行打分（异步版本）
    /// </summary>
    public async Task<ScoringResult> ScoreFileAsync(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        return await Task.Run(() => ScoreFile(filePath, examModel, configuration));
    }

    /// <summary>
    /// 对单个题目进行评分（异步版本）
    /// </summary>
    public async Task<ScoringResult> ScoreQuestionAsync(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        return await Task.Run(() => ScoreQuestion(filePath, question, configuration));
    }

    /// <summary>
    /// 对文件进行打分（同步版本）
    /// </summary>
    public abstract ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null);

    /// <summary>
    /// 对单个题目进行评分（同步版本）
    /// </summary>
    protected abstract ScoringResult ScoreQuestion(string filePath, QuestionModel question, ScoringConfiguration? configuration = null);

    /// <summary>
    /// 验证文件是否可以被处理
    /// </summary>
    public bool CanProcessFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    /// <summary>
    /// 获取支持的文件扩展名
    /// </summary>
    public IEnumerable<string> GetSupportedExtensions()
    {
        return SupportedExtensions;
    }

    /// <summary>
    /// 验证文档是否有效
    /// </summary>
    protected virtual bool ValidateDocument(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            if (!CanProcessFile(filePath))
            {
                return false;
            }

            // 尝试打开文档验证格式
            return ValidateDocumentFormat(filePath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证文档格式（由子类实现）
    /// </summary>
    protected abstract bool ValidateDocumentFormat(string filePath);

    /// <summary>
    /// 处理异常并更新评分结果
    /// </summary>
    protected virtual void HandleException(Exception ex, ScoringResult result)
    {
        result.IsSuccess = false;
        result.Details = ex switch
        {
            FileNotFoundException => $"文件不存在: {ex.Message}",
            UnauthorizedAccessException => $"文件访问被拒绝: {ex.Message}",
            OpenXmlPackageException => $"文档格式错误: {ex.Message}",
            InvalidOperationException => $"操作无效: {ex.Message}",
            _ => $"处理文档时发生错误: {ex.Message}"
        };
    }

    /// <summary>
    /// 创建基础评分结果
    /// </summary>
    protected ScoringResult CreateBaseScoringResult(string? questionId = null, string? questionTitle = null)
    {
        return new ScoringResult
        {
            QuestionId = questionId,
            QuestionTitle = questionTitle,
            StartTime = DateTime.Now,
            IsSuccess = false,
            KnowledgePointResults = []
        };
    }

    /// <summary>
    /// 完成评分结果
    /// </summary>
    protected void FinalizeScoringResult(ScoringResult result, List<OperationPointModel> operationPoints)
    {
        result.EndTime = DateTime.Now;
        result.TotalScore = operationPoints.Sum(op => op.Score);
        result.AchievedScore = result.KnowledgePointResults.Sum(kpr => kpr.AchievedScore);

        if (!result.IsSuccess && string.IsNullOrEmpty(result.Details))
        {
            result.IsSuccess = true;
        }
    }

    /// <summary>
    /// 映射操作点名称到知识点类型
    /// </summary>
    protected virtual string MapOperationPointNameToKnowledgeType(string operationPointName)
    {
        // 基础映射逻辑，子类可以重写
        return operationPointName switch
        {
            // 通用映射
            var name when name.Contains("Font") => "SetFont",
            var name when name.Contains("Color") => "SetColor",
            var name when name.Contains("Size") => "SetSize",
            var name when name.Contains("Insert") => "Insert",
            var name when name.Contains("Delete") => "Delete",
            _ => operationPointName
        };
    }

    /// <summary>
    /// 创建知识点检测结果
    /// </summary>
    protected KnowledgePointResult CreateKnowledgePointResult(OperationPointModel operationPoint, string knowledgePointType)
    {
        return new KnowledgePointResult
        {
            KnowledgePointId = operationPoint.Id,
            OperationPointId = operationPoint.Id,
            KnowledgePointName = operationPoint.Name,
            KnowledgePointType = knowledgePointType,
            TotalScore = operationPoint.Score,
            AchievedScore = 0,
            IsCorrect = false,
            Parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value)
        };
    }

    /// <summary>
    /// 设置知识点检测成功
    /// </summary>
    protected void SetKnowledgePointSuccess(KnowledgePointResult result, string? details = null)
    {
        result.IsCorrect = true;
        result.AchievedScore = result.TotalScore;
        if (!string.IsNullOrEmpty(details))
        {
            result.Details = details;
        }
    }

    /// <summary>
    /// 设置知识点检测失败
    /// </summary>
    protected void SetKnowledgePointFailure(KnowledgePointResult result, string errorMessage, string? details = null)
    {
        result.IsCorrect = false;
        result.AchievedScore = 0;
        result.ErrorMessage = errorMessage;

        // 确保Details总是被设置，如果没有提供details参数，则使用errorMessage作为Details
        if (!string.IsNullOrEmpty(details))
        {
            result.Details = details;
        }
        else
        {
            // 统一使用Details属性，同时保持ErrorMessage的兼容性
            result.Details = errorMessage;
        }
    }

    /// <summary>
    /// 安全获取参数值
    /// </summary>
    protected bool TryGetParameter(Dictionary<string, string> parameters, string key, out string value)
    {
        return parameters.TryGetValue(key, out value!) && !string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// 安全获取整数参数
    /// </summary>
    protected bool TryGetIntParameter(Dictionary<string, string> parameters, string key, out int value)
    {
        value = 0;
        return TryGetParameter(parameters, key, out string strValue) && int.TryParse(strValue, out value);
    }

    /// <summary>
    /// 安全获取浮点数参数
    /// </summary>
    protected bool TryGetFloatParameter(Dictionary<string, string> parameters, string key, out float value)
    {
        value = 0f;
        return TryGetParameter(parameters, key, out string strValue) && float.TryParse(strValue, out value);
    }

    /// <summary>
    /// 安全获取双精度参数
    /// </summary>
    protected bool TryGetDoubleParameter(Dictionary<string, string> parameters, string key, out double value)
    {
        value = 0d;
        return TryGetParameter(parameters, key, out string strValue) && double.TryParse(strValue, out value);
    }

    /// <summary>
    /// 安全获取Number类型参数（支持整数和小数）
    /// </summary>
    protected bool TryGetNumberParameter(Dictionary<string, string> parameters, string key, out double value)
    {
        value = 0d;
        return TryGetParameter(parameters, key, out string strValue) && double.TryParse(strValue, out value);
    }

    /// <summary>
    /// 标准化文本比较
    /// </summary>
    protected bool TextEquals(string? text1, string? text2, bool ignoreCase = true)
    {
        if (text1 == null && text2 == null) return true;
        if (text1 == null || text2 == null) return false;
        
        return string.Equals(text1.Trim(), text2.Trim(), 
            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    /// <summary>
    /// 标准化文本包含检查
    /// </summary>
    protected bool TextContains(string? text, string? searchText, bool ignoreCase = true)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchText)) return false;
        
        return text.Contains(searchText, 
            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
