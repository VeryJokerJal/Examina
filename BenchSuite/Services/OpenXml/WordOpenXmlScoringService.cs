using BenchSuite.Interfaces;
using BenchSuite.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BenchSuite.Services.OpenXml;

/// <summary>
/// Word OpenXML评分服务实现
/// </summary>
public class WordOpenXmlScoringService : OpenXmlScoringServiceBase, IWordScoringService
{
    protected override string[] SupportedExtensions => [".docx"];

    /// <summary>
    /// 验证Word文档格式
    /// </summary>
    protected override bool ValidateDocumentFormat(string filePath)
    {
        try
        {
            using WordprocessingDocument document = WordprocessingDocument.Open(filePath, false);
            return document.MainDocumentPart != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 对Word文件进行打分（同步版本）
    /// </summary>
    public override ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        ScoringConfiguration config = configuration ?? _defaultConfiguration;
        ScoringResult result = CreateBaseScoringResult();

        try
        {
            if (!ValidateDocument(filePath))
            {
                result.Details = $"无效的Word文档: {filePath}";
                return result;
            }

            // 获取Word模块
            ExamModuleModel? wordModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.Word);
            if (wordModule == null)
            {
                result.Details = "试卷中未找到Word模块，跳过Word评分";
                result.IsSuccess = true;
                result.TotalScore = 0;
                result.AchievedScore = 0;
                result.KnowledgePointResults = [];
                return result;
            }

            // 收集所有Word相关的操作点并记录题目关联关系
            List<OperationPointModel> allOperationPoints = [];
            Dictionary<string, string> operationPointToQuestionMap = [];

            foreach (QuestionModel question in wordModule.Questions)
            {
                // 只处理Word相关且启用的操作点
                List<OperationPointModel> wordOperationPoints = question.OperationPoints.Where(op => op.ModuleType == ModuleType.Word && op.IsEnabled).ToList();

                System.Diagnostics.Debug.WriteLine($"[WordOpenXmlScoringService] 题目 '{question.Title}' (ID: {question.Id}) 包含 {wordOperationPoints.Count} 个Word操作点");

                foreach (OperationPointModel operationPoint in wordOperationPoints)
                {
                    allOperationPoints.Add(operationPoint);
                    operationPointToQuestionMap[operationPoint.Id] = question.Id;
                    System.Diagnostics.Debug.WriteLine($"[WordOpenXmlScoringService] 添加操作点: {operationPoint.Name} (ID: {operationPoint.Id}) -> 题目: {question.Id}");
                }
            }

            if (allOperationPoints.Count == 0)
            {
                result.Details = "Word模块中未找到启用的Word操作点";
                System.Diagnostics.Debug.WriteLine($"[WordOpenXmlScoringService] 警告: Word模块包含 {wordModule.Questions.Count} 个题目，但没有找到启用的Word操作点");
                return result;
            }

            System.Diagnostics.Debug.WriteLine($"[WordOpenXmlScoringService] 总共收集到 {allOperationPoints.Count} 个Word操作点，来自 {wordModule.Questions.Count} 个题目");

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, allOperationPoints).Result;

            // 为每个知识点结果设置题目关联信息
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                if (operationPointToQuestionMap.TryGetValue(kpResult.KnowledgePointId, out string? questionId))
                {
                    kpResult.QuestionId = questionId;

                    // 查找题目标题用于调试信息（KnowledgePointResult模型中没有QuestionTitle属性）
                    QuestionModel? question = wordModule.Questions.FirstOrDefault(q => q.Id == questionId);
                    if (question != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WordOpenXmlScoringService] 知识点 '{kpResult.KnowledgePointName}' 关联到题目 '{question.Title}' (ID: {questionId})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[WordOpenXmlScoringService] 警告: 无法找到ID为 {questionId} 的题目");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[WordOpenXmlScoringService] 警告: 知识点 '{kpResult.KnowledgePointName}' (ID: {kpResult.KnowledgePointId}) 没有找到对应的题目映射");
                }
            }

            FinalizeScoringResult(result, allOperationPoints);

            System.Diagnostics.Debug.WriteLine($"[WordOpenXmlScoringService] 评分完成: 总分 {result.TotalScore}, 获得分数 {result.AchievedScore}, 成功率 {(result.TotalScore > 0 ? (result.AchievedScore / result.TotalScore * 100) : 0):F1}%");
        }
        catch (Exception ex)
        {
            HandleException(ex, result);
        }

        return result;
    }

    /// <summary>
    /// 对单个题目进行评分（同步版本）
    /// </summary>
    protected override ScoringResult ScoreQuestion(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        ScoringResult result = CreateBaseScoringResult(question.Id, question.Title);

        try
        {
            if (!ValidateDocument(filePath))
            {
                result.Details = $"无效的Word文档: {filePath}";
                return result;
            }

            // 获取题目的操作点（只处理Word相关的操作点）
            List<OperationPointModel> wordOperationPoints = [.. question.OperationPoints.Where(op => op.ModuleType == ModuleType.Word && op.IsEnabled)];

            if (wordOperationPoints.Count == 0)
            {
                result.Details = "题目没有包含任何Word操作点";
                return result;
            }

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, wordOperationPoints).Result;

            // 为每个知识点结果设置题目ID
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                kpResult.QuestionId = question.Id;
            }

            FinalizeScoringResult(result, wordOperationPoints);
        }
        catch (Exception ex)
        {
            HandleException(ex, result);
        }

        return result;
    }

    /// <summary>
    /// 检测Word中的特定知识点
    /// </summary>
    public async Task<KnowledgePointResult> DetectKnowledgePointAsync(string filePath, string knowledgePointType, Dictionary<string, string> parameters)
    {
        return await Task.Run(() =>
        {
            KnowledgePointResult result = new()
            {
                KnowledgePointType = knowledgePointType,
                Parameters = parameters
            };

            try
            {
                if (!ValidateDocument(filePath))
                {
                    result.Details = "无效的Word文档";
                    return result;
                }

                using WordprocessingDocument document = WordprocessingDocument.Open(filePath, false);
                result = DetectSpecificKnowledgePoint(document, knowledgePointType, parameters);
            }
            catch (Exception ex)
            {
                result.Details = $"检测知识点时发生错误: {ex.Message}";
                result.IsCorrect = false;
            }

            return result;
        });
    }

    /// <summary>
    /// 批量检测Word中的知识点
    /// </summary>
    public async Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(string filePath, List<OperationPointModel> knowledgePoints)
    {
        return await Task.Run(() =>
        {
            List<KnowledgePointResult> results = [];

            try
            {
                if (!ValidateDocument(filePath))
                {
                    // 为所有知识点返回错误结果
                    foreach (OperationPointModel operationPoint in knowledgePoints)
                    {
                        KnowledgePointResult errorResult = CreateKnowledgePointResult(operationPoint, operationPoint.WordKnowledgeType ?? string.Empty);
                        SetKnowledgePointFailure(errorResult, "无效的Word文档");
                        results.Add(errorResult);
                    }
                    return results;
                }

                using WordprocessingDocument document = WordprocessingDocument.Open(filePath, false);

                // 逐个检测知识点
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    try
                    {
                        Dictionary<string, string> parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);
                        string knowledgePointType = MapOperationPointNameToKnowledgeType(operationPoint.Name);

                        KnowledgePointResult result = DetectSpecificKnowledgePoint(document, knowledgePointType, parameters);

                        result.KnowledgePointId = operationPoint.Id;
                        result.OperationPointId = operationPoint.Id;
                        result.KnowledgePointName = operationPoint.Name;
                        result.TotalScore = operationPoint.Score;

                        // 重新计算得分，确保使用正确的TotalScore
                        if (result.IsCorrect && result.AchievedScore == 0)
                        {
                            result.AchievedScore = result.TotalScore;
                        }

                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        KnowledgePointResult errorResult = CreateKnowledgePointResult(operationPoint, operationPoint.WordKnowledgeType ?? string.Empty);
                        SetKnowledgePointFailure(errorResult, $"检测失败: {ex.Message}");
                        results.Add(errorResult);
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果无法打开文件，为所有知识点返回错误结果
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    KnowledgePointResult errorResult = CreateKnowledgePointResult(operationPoint, operationPoint.WordKnowledgeType ?? string.Empty);
                    SetKnowledgePointFailure(errorResult, $"无法打开Word文件: {ex.Message}");
                    results.Add(errorResult);
                }
            }

            return results;
        });
    }

    /// <summary>
    /// 设置知识点检测失败并包含完整的Details信息
    /// </summary>
    private void SetKnowledgePointFailureWithDetails(KnowledgePointResult result, string errorMessage,
        string operationType, Dictionary<string, string> parameters, string expectedValue = "", string actualValue = "")
    {
        // 构建段落描述
        string paragraphDesc = "未知段落";
        if (TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber))
        {
            paragraphDesc = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
        }

        // 构建详细的Details信息
        string details;
        if (!string.IsNullOrEmpty(expectedValue) && !string.IsNullOrEmpty(actualValue))
        {
            details = $"{paragraphDesc} {operationType}: 期望 {expectedValue}, 实际 {actualValue}";
        }
        else
        {
            details = !string.IsNullOrEmpty(expectedValue)
                ? $"{paragraphDesc} {operationType}: 期望 {expectedValue}, 检测失败 - {errorMessage}"
                : $"{paragraphDesc} {operationType}检测失败: {errorMessage}";
        }

        SetKnowledgePointFailure(result, errorMessage, details);
    }

    /// <summary>
    /// 设置通用知识点检测失败并包含完整的Details信息（用于非段落相关的检测）
    /// </summary>
    private void SetGeneralKnowledgePointFailureWithDetails(KnowledgePointResult result, string errorMessage,
        string operationType, Dictionary<string, string> parameters, string expectedValue = "", string actualValue = "")
    {
        // 构建详细的Details信息
        string details;
        if (!string.IsNullOrEmpty(expectedValue) && !string.IsNullOrEmpty(actualValue))
        {
            details = $"{operationType}: 期望 {expectedValue}, 实际 {actualValue}";
        }
        else
        {
            details = !string.IsNullOrEmpty(expectedValue)
                ? $"{operationType}: 期望 {expectedValue}, 检测失败 - {errorMessage}"
                : $"{operationType}检测失败: {errorMessage}";
        }

        SetKnowledgePointFailure(result, errorMessage, details);
    }

    /// <summary>
    /// 映射Word操作点名称到知识点类型
    /// </summary>
    protected override string MapOperationPointNameToKnowledgeType(string operationPointName)
    {
        // 创建完整的中文操作点名称到英文知识点类型的映射
        Dictionary<string, string> nameToTypeMapping = new()
        {
            // 第一类：段落操作（14个）
            { "设置段落的字体", "SetParagraphFont" },
            { "设置段落的字号", "SetParagraphFontSize" },
            { "设置段落的字形", "SetParagraphFontStyle" },
            { "设置段落字间距", "SetParagraphCharacterSpacing" },
            { "设置段落文字的颜色", "SetParagraphTextColor" },
            { "设置段落对齐方式", "SetParagraphAlignment" },
            { "设置段落缩进", "SetParagraphIndentation" },
            { "设置行间距", "SetParagraphLineSpacing" },
            { "首字下沉", "SetParagraphDropCap" },
            { "设置段落间距", "SetParagraphSpacing" },
            { "设置段落边框颜色", "SetParagraphBorderColor" },
            { "设置段落边框样式", "SetParagraphBorderStyle" },
            { "设置段落边框宽度", "SetParagraphBorderWidth" },
            { "设置段落底纹", "SetParagraphShading" },

            // 第二类：页面设置（15个）
            { "设置纸张大小", "SetPaperSize" },
            { "设置页边距", "SetPageMargins" },
            { "设置页眉中的文字", "SetHeaderText" },
            { "设置页眉中文字的字体", "SetHeaderFont" },
            { "设置页眉中文字的字号", "SetHeaderFontSize" },
            { "设置页眉中文字的对齐方式", "SetHeaderAlignment" },
            { "设置页脚中的文字", "SetFooterText" },
            { "设置页脚中文字的字体", "SetFooterFont" },
            { "设置页脚中文字的字号", "SetFooterFontSize" },
            { "设置页脚中文字的对齐方式", "SetFooterAlignment" },
            { "设置页码", "SetPageNumber" },
            { "设置页面背景", "SetPageBackground" },
            { "设置页面边框颜色", "SetPageBorderColor" },
            { "设置页面边框样式", "SetPageBorderStyle" },
            { "设置页面边框宽度", "SetPageBorderWidth" },

            // 第三类：水印设置（4个）
            { "设置水印文字", "SetWatermarkText" },
            { "设置水印文字的字体", "SetWatermarkFont" },
            { "设置水印文字的字号", "SetWatermarkFontSize" },
            { "设置水印文字水平或倾斜方式", "SetWatermarkOrientation" },

            // 第四类：项目符号与编号（1个）
            { "设置项目编号", "SetBulletNumbering" },

            // 第五类：表格操作（10个）
            { "设置表格的行数和列数", "SetTableRowsColumns" },
            { "设置表格底纹", "SetTableShading" },
            { "设置表格行高", "SetTableRowHeight" },
            { "设置表格列宽", "SetTableColumnWidth" },
            { "设置表格单元格内容", "SetTableCellContent" },
            { "设置表格单元格对齐方式", "SetTableCellAlignment" },
            { "设置表格对齐方式", "SetTableAlignment" },
            { "合并表格单元格", "MergeTableCells" },
            { "设置表格标题内容", "SetTableHeaderContent" },
            { "设置表格标题对齐方式", "SetTableHeaderAlignment" },

            // 第六类：图形和图片设置（16个）
            { "插入自选图形类型", "InsertAutoShape" },
            { "设置自选图形大小", "SetAutoShapeSize" },
            { "设置自选图形线条颜色", "SetAutoShapeLineColor" },
            { "设置自选图形填充颜色", "SetAutoShapeFillColor" },
            { "设置自选图形文字大小", "SetAutoShapeTextSize" },
            { "设置自选图形文字颜色", "SetAutoShapeTextColor" },
            { "设置自选图形文字内容", "SetAutoShapeTextContent" },
            { "设置自选图形位置", "SetAutoShapePosition" },
            { "设置图片边框复合类型", "SetImageBorderCompoundType" },
            { "设置图片边框虚线类型", "SetImageBorderDashType" },
            { "设置图片边框宽度", "SetImageBorderWidth" },
            { "设置图片边框颜色", "SetImageBorderColor" },
            { "设置图片阴影", "SetImageShadow" },
            { "设置图片环绕方式", "SetImageWrapStyle" },
            { "设置图片位置", "SetImagePosition" },
            { "设置插入图片的高度和宽度", "SetImageSize" },

            // 第七类：文本框设置（5个）
            { "设置文本框边框颜色", "SetTextBoxBorderColor" },
            { "设置文本框中文字内容", "SetTextBoxContent" },
            { "设置文本框中文字大小", "SetTextBoxTextSize" },
            { "设置文本框位置", "SetTextBoxPosition" },
            { "设置文本框环绕方式", "SetTextBoxWrapStyle" },

            // 第八类：其他操作（2个）
            { "查找与替换", "FindAndReplace" },
            { "设置指定文字字号", "SetSpecificTextFontSize" }
        };

        // 尝试精确匹配
        if (nameToTypeMapping.TryGetValue(operationPointName, out string? exactMatch))
        {
            return exactMatch;
        }

        // 如果没有精确匹配，回退到基类处理
        return base.MapOperationPointNameToKnowledgeType(operationPointName);
    }

    /// <summary>
    /// 验证必需参数并生成调试信息
    /// </summary>
    private bool ValidateRequiredParameters(Dictionary<string, string> parameters, string knowledgePointType, string[] requiredParams, out string errorDetails)
    {
        List<string> missingParams = [];

        foreach (string param in requiredParams)
        {
            if (!TryGetParameter(parameters, param, out string _))
            {
                missingParams.Add(param);
            }
        }

        if (missingParams.Count > 0)
        {
            string missingParamsList = string.Join("', '", missingParams);
            errorDetails = $"缺少必需参数: '{missingParamsList}'";

            // 输出详细的调试信息
            System.Diagnostics.Debug.WriteLine($"[WordOpenXmlScoringService] 知识点检测失败 - {knowledgePointType}: {errorDetails}");

            return false;
        }

        errorDetails = string.Empty;
        return true;
    }

    /// <summary>
    /// 检测特定知识点
    /// </summary>
    private KnowledgePointResult DetectSpecificKnowledgePoint(WordprocessingDocument document, string knowledgePointType, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = knowledgePointType,
            Parameters = parameters
        };

        try
        {
            // 根据知识点类型进行具体检测
            switch (knowledgePointType)
            {
                // 段落操作（14个）
                case "SetParagraphFont":
                    result = DetectParagraphFont(document, parameters);
                    break;
                case "SetParagraphFontSize":
                    result = DetectParagraphFontSize(document, parameters);
                    break;
                case "SetParagraphFontStyle":
                    result = DetectParagraphFontStyle(document, parameters);
                    break;
                case "SetParagraphCharacterSpacing":
                    result = DetectParagraphCharacterSpacing(document, parameters);
                    break;
                case "SetParagraphTextColor":
                    result = DetectParagraphTextColor(document, parameters);
                    break;
                case "SetParagraphAlignment":
                    result = DetectParagraphAlignment(document, parameters);
                    break;
                case "SetParagraphIndentation":
                    result = DetectParagraphIndentation(document, parameters);
                    break;
                case "SetParagraphLineSpacing":
                    result = DetectParagraphLineSpacing(document, parameters);
                    break;
                case "SetParagraphDropCap":
                    result = DetectParagraphDropCap(document, parameters);
                    break;
                case "SetParagraphSpacing":
                    result = DetectParagraphSpacing(document, parameters);
                    break;
                case "SetParagraphBorderColor":
                    result = DetectParagraphBorderColor(document, parameters);
                    break;
                case "SetParagraphBorderStyle":
                    result = DetectParagraphBorderStyle(document, parameters);
                    break;
                case "SetParagraphBorderWidth":
                    result = DetectParagraphBorderWidth(document, parameters);
                    break;
                case "SetParagraphShading":
                    result = DetectParagraphShading(document, parameters);
                    break;

                // 页面设置（15个）
                case "SetPaperSize":
                    result = DetectPaperSize(document, parameters);
                    break;
                case "SetPageMargins":
                    result = DetectPageMargins(document, parameters);
                    break;
                case "SetHeaderText":
                    result = DetectHeaderText(document, parameters);
                    break;
                case "SetHeaderFont":
                    result = DetectHeaderFont(document, parameters);
                    break;
                case "SetHeaderFontSize":
                    result = DetectHeaderFontSize(document, parameters);
                    break;
                case "SetHeaderAlignment":
                    result = DetectHeaderAlignment(document, parameters);
                    break;
                case "SetFooterText":
                    result = DetectFooterText(document, parameters);
                    break;
                case "SetFooterFont":
                    result = DetectFooterFont(document, parameters);
                    break;
                case "SetFooterFontSize":
                    result = DetectFooterFontSize(document, parameters);
                    break;
                case "SetFooterAlignment":
                    result = DetectFooterAlignment(document, parameters);
                    break;
                case "SetPageNumber":
                    result = DetectPageNumber(document, parameters);
                    break;
                case "SetPageBackground":
                    result = DetectPageBackground(document, parameters);
                    break;
                case "SetPageBorderColor":
                    result = DetectPageBorderColor(document, parameters);
                    break;
                case "SetPageBorderStyle":
                    result = DetectPageBorderStyle(document, parameters);
                    break;
                case "SetPageBorderWidth":
                    result = DetectPageBorderWidth(document, parameters);
                    break;

                // 水印设置（4个）
                case "SetWatermarkText":
                    result = DetectWatermarkText(document, parameters);
                    break;
                case "SetWatermarkFont":
                    result = DetectWatermarkFont(document, parameters);
                    break;
                case "SetWatermarkFontSize":
                    result = DetectWatermarkFontSize(document, parameters);
                    break;
                case "SetWatermarkOrientation":
                    result = DetectWatermarkOrientation(document, parameters);
                    break;

                // 项目符号与编号（1个）
                case "SetBulletNumbering":
                    result = DetectBulletNumbering(document, parameters);
                    break;

                // 表格操作（10个）
                case "SetTableRowsColumns":
                    result = DetectTableRowsColumns(document, parameters);
                    break;
                case "SetTableShading":
                    result = DetectTableShading(document, parameters);
                    break;
                case "SetTableRowHeight":
                    result = DetectTableRowHeight(document, parameters);
                    break;
                case "SetTableColumnWidth":
                    result = DetectTableColumnWidth(document, parameters);
                    break;
                case "SetTableCellContent":
                    result = DetectTableCellContent(document, parameters);
                    break;
                case "SetTableCellAlignment":
                    result = DetectTableCellAlignment(document, parameters);
                    break;
                case "SetTableAlignment":
                    result = DetectTableAlignment(document, parameters);
                    break;
                case "MergeTableCells":
                    result = DetectMergeTableCells(document, parameters);
                    break;
                case "SetTableHeaderContent":
                    result = DetectTableHeaderContent(document, parameters);
                    break;
                case "SetTableHeaderAlignment":
                    result = DetectTableHeaderAlignment(document, parameters);
                    break;

                // 图形和图片设置（16个）
                case "InsertAutoShape":
                    result = DetectInsertAutoShape(document, parameters);
                    break;
                case "SetAutoShapeSize":
                    result = DetectAutoShapeSize(document, parameters);
                    break;
                case "SetAutoShapeLineColor":
                    result = DetectAutoShapeLineColor(document, parameters);
                    break;
                case "SetAutoShapeFillColor":
                    result = DetectAutoShapeFillColor(document, parameters);
                    break;
                case "SetAutoShapeTextSize":
                    result = DetectAutoShapeTextSize(document, parameters);
                    break;
                case "SetAutoShapeTextColor":
                    result = DetectAutoShapeTextColor(document, parameters);
                    break;
                case "SetAutoShapeTextContent":
                    result = DetectAutoShapeTextContent(document, parameters);
                    break;
                case "SetAutoShapePosition":
                    result = DetectAutoShapePosition(document, parameters);
                    break;
                case "SetImageBorderCompoundType":
                    result = DetectImageBorderCompoundType(document, parameters);
                    break;
                case "SetImageBorderDashType":
                    result = DetectImageBorderDashType(document, parameters);
                    break;
                case "SetImageBorderWidth":
                    result = DetectImageBorderWidth(document, parameters);
                    break;
                case "SetImageBorderColor":
                    result = DetectImageBorderColor(document, parameters);
                    break;
                case "SetImageShadow":
                    result = DetectImageShadow(document, parameters);
                    break;
                case "SetImageWrapStyle":
                    result = DetectImageWrapStyle(document, parameters);
                    break;
                case "SetImageSize":
                    result = DetectImageSize(document, parameters);
                    break;
                case "SetImagePosition":
                    result = DetectImagePosition(document, parameters);
                    break;

                // 文本框设置（5个）
                case "SetTextBoxBorderColor":
                    result = DetectTextBoxBorderColor(document, parameters);
                    break;
                case "SetTextBoxContent":
                    result = DetectTextBoxContent(document, parameters);
                    break;
                case "SetTextBoxTextSize":
                    result = DetectTextBoxTextSize(document, parameters);
                    break;
                case "SetTextBoxPosition":
                    result = DetectTextBoxPosition(document, parameters);
                    break;
                case "SetTextBoxWrapStyle":
                    result = DetectTextBoxWrapStyle(document, parameters);
                    break;

                // 其他操作（2个）
                case "FindAndReplace":
                    result = DetectFindAndReplace(document, parameters);
                    break;
                case "SetSpecificTextFontSize":
                    result = DetectSpecificTextFontSize(document, parameters);
                    break;

                // 段落操作方法已全部实现

                default:
                    result.Details = $"不支持的知识点类型: {knowledgePointType}";
                    result.IsCorrect = false;
                    break;
            }
        }
        catch (Exception ex)
        {
            result.Details = $"检测知识点 {knowledgePointType} 时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落字体
    /// </summary>
    private KnowledgePointResult DetectParagraphFont(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphFont",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetParameter(parameters, "FontFamily", out string expectedFont))
            {
                SetKnowledgePointFailureWithDetails(result, "缺少必要参数: ParagraphNumber 或 FontFamily",
                    "字体", parameters);
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedFont,
                GetParagraphFont,
                TextEquals, // 使用文本比较函数
                out Paragraph? matchedParagraph,
                out string? actualFont,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailureWithDetails(result, errorMessage, "字体", parameters, expectedFont);
                return result;
            }

            result.ExpectedValue = expectedFont;
            result.ActualValue = actualFont ?? string.Empty;
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 字体: 期望 {expectedFont}, 实际 {actualFont}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailureWithDetails(result, $"检测段落字体失败: {ex.Message}",
                "字体", parameters);
        }

        return result;
    }

    /// <summary>
    /// 检测段落字号
    /// </summary>
    private KnowledgePointResult DetectParagraphFontSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphFontSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetIntParameter(parameters, "FontSize", out int expectedSize))
            {
                SetKnowledgePointFailureWithDetails(result, "缺少必要参数: ParagraphNumber 或 FontSize",
                    "字号", parameters);
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedSize,
                GetParagraphFontSize,
                null, // 使用默认相等比较
                out Paragraph? matchedParagraph,
                out int actualSize,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailureWithDetails(result, errorMessage, "字号", parameters, expectedSize.ToString());
                return result;
            }

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = actualSize.ToString();
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 字号: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailureWithDetails(result, $"检测段落字号失败: {ex.Message}",
                "字号", parameters);
        }

        return result;
    }

    /// <summary>
    /// 检测段落字形
    /// </summary>
    private KnowledgePointResult DetectParagraphFontStyle(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphFontStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetParameter(parameters, "FontStyle", out string expectedStyle))
            {
                SetKnowledgePointFailureWithDetails(result, "缺少必要参数: ParagraphNumber 或 FontStyle",
                    "字形", parameters);
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            // 添加调试输出
            System.Diagnostics.Debug.WriteLine($"[段落字形检测] 期望字形: {expectedStyle}, 段落索引: {paragraphNumber}");
            if (paragraphNumber == -1)
            {
                DebugParagraphFormats(document);
            }

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedStyle,
                GetParagraphFontStyle,
                TextEquals, // 使用文本比较函数
                out Paragraph? matchedParagraph,
                out string? actualStyle,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle ?? string.Empty;
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 字形: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落字形失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落字间距
    /// </summary>
    private KnowledgePointResult DetectParagraphCharacterSpacing(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphCharacterSpacing",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetFloatParameter(parameters, "CharacterSpacing", out float expectedSpacing))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumber 或 CharacterSpacing");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            // 创建浮点数比较函数
            static bool FloatEquals(float actual, float expected)
            {
                return Math.Abs(actual - expected) < 0.1f;
            }

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedSpacing,
                GetParagraphCharacterSpacing,
                FloatEquals, // 使用浮点数比较函数
                out Paragraph? matchedParagraph,
                out float actualSpacing,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = expectedSpacing.ToString();
            result.ActualValue = actualSpacing.ToString();
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 字间距: 期望 {expectedSpacing}, 实际 {actualSpacing}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落字间距失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落文字颜色
    /// </summary>
    private KnowledgePointResult DetectParagraphTextColor(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphTextColor",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetParameter(parameters, "TextColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumber 或 TextColor");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            // 创建颜色比较函数
            static bool ColorMatches(string actual, string expected)
            {
                return TextEquals(actual, expected) || ColorEquals(actual, expected);
            }

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedColor,
                GetParagraphTextColor,
                ColorMatches, // 使用颜色比较函数
                out Paragraph? matchedParagraph,
                out string? actualColor,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor ?? string.Empty;
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 文字颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落文字颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落缩进
    /// </summary>
    private KnowledgePointResult DetectParagraphIndentation(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphIndentation",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetIntParameter(parameters, "FirstLineIndent", out int expectedFirstLine) ||
                !TryGetIntParameter(parameters, "LeftIndent", out int expectedLeft) ||
                !TryGetIntParameter(parameters, "RightIndent", out int expectedRight))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumber, FirstLineIndent, LeftIndent 或 RightIndent");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            // 创建缩进值组合
            (int FirstLine, int Left, int Right) expectedIndentation = (expectedFirstLine, expectedLeft, expectedRight);

            // 创建缩进比较函数
            static bool IndentationEquals((int FirstLine, int Left, int Right) actual, (int FirstLine, int Left, int Right) expected)
            {
                return actual.FirstLine == expected.FirstLine && actual.Left == expected.Left && actual.Right == expected.Right;
            }

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedIndentation,
                GetParagraphIndentation,
                IndentationEquals, // 使用缩进比较函数
                out Paragraph? matchedParagraph,
                out (int FirstLine, int Left, int Right) actualIndentation,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = $"首行:{expectedFirstLine}, 左:{expectedLeft}, 右:{expectedRight}";
            result.ActualValue = $"首行:{actualIndentation.FirstLine}, 左:{actualIndentation.Left}, 右:{actualIndentation.Right}";
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 缩进: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落缩进失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落行间距
    /// </summary>
    private KnowledgePointResult DetectParagraphLineSpacing(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphLineSpacing",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetFloatParameter(parameters, "LineSpacing", out float expectedSpacing))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumber 或 LineSpacing");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            // 创建浮点数比较函数
            static bool FloatEquals(float actual, float expected)
            {
                return Math.Abs(actual - expected) < 0.1f;
            }

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedSpacing,
                GetParagraphLineSpacing,
                FloatEquals, // 使用浮点数比较函数
                out Paragraph? matchedParagraph,
                out float actualSpacing,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = expectedSpacing.ToString();
            result.ActualValue = actualSpacing.ToString();
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 行间距: 期望 {expectedSpacing}, 实际 {actualSpacing}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落行间距失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落首字下沉
    /// </summary>
    private KnowledgePointResult DetectParagraphDropCap(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphDropCap",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetParameter(parameters, "DropCapType", out string expectedType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumber 或 DropCapType");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedType,
                GetParagraphDropCap,
                TextEquals, // 使用文本比较函数
                out Paragraph? matchedParagraph,
                out string? actualType,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = expectedType;
            result.ActualValue = actualType ?? string.Empty;
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 首字下沉: 期望 {expectedType}, 实际 {actualType}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落首字下沉失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落边框颜色
    /// </summary>
    private KnowledgePointResult DetectParagraphBorderColor(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphBorderColor",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetParameter(parameters, "BorderColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumber 或 BorderColor");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            // 创建颜色比较函数
            static bool ColorMatches(string actual, string expected)
            {
                return TextEquals(actual, expected) || ColorEquals(actual, expected);
            }

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedColor,
                GetParagraphBorderColor,
                ColorMatches, // 使用颜色比较函数
                out Paragraph? matchedParagraph,
                out string? actualColor,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor ?? string.Empty;
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 边框颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落边框颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落边框样式
    /// </summary>
    private KnowledgePointResult DetectParagraphBorderStyle(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphBorderStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetParameter(parameters, "BorderStyle", out string expectedStyle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumber 或 BorderStyle");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedStyle,
                GetParagraphBorderStyle,
                TextEquals, // 使用文本比较函数
                out Paragraph? matchedParagraph,
                out string? actualStyle,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle ?? string.Empty;
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 边框样式: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落边框样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落边框宽度
    /// </summary>
    private KnowledgePointResult DetectParagraphBorderWidth(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphBorderWidth",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetFloatParameter(parameters, "BorderWidth", out float expectedWidth))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumber 或 BorderWidth");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            // 创建浮点数比较函数
            static bool FloatEquals(float actual, float expected)
            {
                return Math.Abs(actual - expected) < 0.1f;
            }

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedWidth,
                GetParagraphBorderWidth,
                FloatEquals, // 使用浮点数比较函数
                out Paragraph? matchedParagraph,
                out float actualWidth,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = expectedWidth.ToString();
            result.ActualValue = actualWidth.ToString();
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 边框宽度: 期望 {expectedWidth}, 实际 {actualWidth}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落边框宽度失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落底纹
    /// </summary>
    private KnowledgePointResult DetectParagraphShading(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphShading",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetParameter(parameters, "ShadingColor", out string expectedColor) ||
                !TryGetParameter(parameters, "ShadingPattern", out string expectedPattern))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumber, ShadingColor 或 ShadingPattern");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            // 创建底纹值组合
            (string Color, string Pattern) expectedShading = (expectedColor, expectedPattern);

            // 创建底纹比较函数
            static bool ShadingEquals((string Color, string Pattern) actual, (string Color, string Pattern) expected)
            {
                return (TextEquals(actual.Color, expected.Color) || ColorEquals(actual.Color, expected.Color)) &&
                TextEquals(actual.Pattern, expected.Pattern);
            }

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedShading,
                GetParagraphShading,
                ShadingEquals, // 使用底纹比较函数
                out Paragraph? matchedParagraph,
                out (string Color, string Pattern) actualShading,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = $"颜色:{expectedColor}, 图案:{expectedPattern}";
            result.ActualValue = $"颜色:{actualShading.Color}, 图案:{actualShading.Pattern}";
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 底纹: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落底纹失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落对齐方式
    /// </summary>
    private KnowledgePointResult DetectParagraphAlignment(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphAlignment",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetParameter(parameters, "Alignment", out string expectedAlignment))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumber 或 Alignment");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedAlignment,
                GetParagraphAlignment,
                TextEquals, // 使用文本比较函数
                out Paragraph? matchedParagraph,
                out string? actualAlignment,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = actualAlignment ?? string.Empty;
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 对齐方式: 期望 {expectedAlignment}, 实际 {actualAlignment}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落对齐方式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落间距
    /// </summary>
    private KnowledgePointResult DetectParagraphSpacing(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphSpacing",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ParagraphNumber", out int paragraphNumber) ||
                !TryGetFloatParameter(parameters, "SpaceBefore", out float expectedBefore) ||
                !TryGetFloatParameter(parameters, "SpaceAfter", out float expectedAfter))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumber, SpaceBefore 或 SpaceAfter");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            // 创建间距值组合
            (float Before, float After) expectedSpacing = (expectedBefore, expectedAfter);

            // 创建间距比较函数
            static bool SpacingEquals((float Before, float After) actual, (float Before, float After) expected)
            {
                return Math.Abs(actual.Before - expected.Before) < 0.1f && Math.Abs(actual.After - expected.After) < 0.1f;
            }

            bool isMatch = FindMatchingParagraph(
                paragraphs,
                paragraphNumber,
                expectedSpacing,
                GetParagraphSpacing,
                SpacingEquals, // 使用间距比较函数
                out Paragraph? matchedParagraph,
                out (float Before, float After) actualSpacing,
                out string errorMessage);

            if (!isMatch)
            {
                SetKnowledgePointFailure(result, errorMessage);
                return result;
            }

            result.ExpectedValue = $"前:{expectedBefore}, 后:{expectedAfter}";
            result.ActualValue = $"前:{actualSpacing.Before}, 后:{actualSpacing.After}";
            result.IsCorrect = true; // 如果找到匹配就是正确的
            result.AchievedScore = result.TotalScore;

            string paragraphDescription = paragraphNumber == -1 ? "任意段落" : $"段落 {paragraphNumber}";
            result.Details = $"{paragraphDescription} 间距: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落间距失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测纸张大小
    /// </summary>
    private KnowledgePointResult DetectPaperSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPaperSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "PaperSize", out string expectedSize))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: PaperSize");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualSize = GetDocumentPaperSize(mainPart);

            result.ExpectedValue = expectedSize;
            result.ActualValue = actualSize;
            result.IsCorrect = TextEquals(actualSize, expectedSize);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"纸张大小: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测纸张大小失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页边距
    /// </summary>
    private KnowledgePointResult DetectPageMargins(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageMargins",
            Parameters = parameters
        };

        try
        {
            if (!TryGetFloatParameter(parameters, "TopMargin", out float expectedTop) ||
                !TryGetFloatParameter(parameters, "BottomMargin", out float expectedBottom) ||
                !TryGetFloatParameter(parameters, "LeftMargin", out float expectedLeft) ||
                !TryGetFloatParameter(parameters, "RightMargin", out float expectedRight))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: TopMargin, BottomMargin, LeftMargin 或 RightMargin");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            (float Top, float Bottom, float Left, float Right) = GetDocumentMargins(mainPart);

            result.ExpectedValue = $"上:{expectedTop}, 下:{expectedBottom}, 左:{expectedLeft}, 右:{expectedRight}";
            result.ActualValue = $"上:{Top}, 下:{Bottom}, 左:{Left}, 右:{Right}";
            result.IsCorrect = Math.Abs(Top - expectedTop) < 0.1f &&
                              Math.Abs(Bottom - expectedBottom) < 0.1f &&
                              Math.Abs(Left - expectedLeft) < 0.1f &&
                              Math.Abs(Right - expectedRight) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页边距: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页边距失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页眉文字
    /// </summary>
    private KnowledgePointResult DetectHeaderText(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetHeaderText",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "HeaderText", out string expectedText))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: HeaderText");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualText = GetHeaderText(mainPart);

            result.ExpectedValue = expectedText;
            result.ActualValue = actualText;
            result.IsCorrect = TextEquals(actualText, expectedText) || actualText.Contains(expectedText);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页眉文字: 期望 {expectedText}, 实际 {actualText}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页眉文字失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页眉字体
    /// </summary>
    private KnowledgePointResult DetectHeaderFont(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetHeaderFont",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "HeaderFont", out string expectedFont))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: HeaderFont");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualFont = GetHeaderFont(mainPart);

            result.ExpectedValue = expectedFont;
            result.ActualValue = actualFont;
            result.IsCorrect = TextEquals(actualFont, expectedFont);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页眉字体: 期望 {expectedFont}, 实际 {actualFont}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页眉字体失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页眉字号
    /// </summary>
    private KnowledgePointResult DetectHeaderFontSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetHeaderFontSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "HeaderFontSize", out int expectedSize))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: HeaderFontSize");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            int actualSize = GetHeaderFontSize(mainPart);

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = actualSize.ToString();
            result.IsCorrect = actualSize == expectedSize;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页眉字号: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页眉字号失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页眉对齐方式
    /// </summary>
    private KnowledgePointResult DetectHeaderAlignment(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetHeaderAlignment",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "HeaderAlignment", out string expectedAlignment))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: HeaderAlignment");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualAlignment = GetHeaderAlignment(mainPart);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = actualAlignment;
            result.IsCorrect = TextEquals(actualAlignment, expectedAlignment);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页眉对齐方式: 期望 {expectedAlignment}, 实际 {actualAlignment}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页眉对齐方式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页脚文字
    /// </summary>
    private KnowledgePointResult DetectFooterText(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFooterText",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FooterText", out string expectedText))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FooterText");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualText = GetFooterText(mainPart);

            result.ExpectedValue = expectedText;
            result.ActualValue = actualText;
            result.IsCorrect = TextEquals(actualText, expectedText) || actualText.Contains(expectedText);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页脚文字: 期望 {expectedText}, 实际 {actualText}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页脚文字失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页脚字体
    /// </summary>
    private KnowledgePointResult DetectFooterFont(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFooterFont",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FooterFont", out string expectedFont))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FooterFont");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualFont = GetFooterFont(mainPart);

            result.ExpectedValue = expectedFont;
            result.ActualValue = actualFont;
            result.IsCorrect = TextEquals(actualFont, expectedFont);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页脚字体: 期望 {expectedFont}, 实际 {actualFont}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页脚字体失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页脚字号
    /// </summary>
    private KnowledgePointResult DetectFooterFontSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFooterFontSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "FooterFontSize", out int expectedSize))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FooterFontSize");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            int actualSize = GetFooterFontSize(mainPart);

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = actualSize.ToString();
            result.IsCorrect = actualSize == expectedSize;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页脚字号: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页脚字号失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页脚对齐方式
    /// </summary>
    private KnowledgePointResult DetectFooterAlignment(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFooterAlignment",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FooterAlignment", out string expectedAlignment))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FooterAlignment");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualAlignment = GetFooterAlignment(mainPart);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = actualAlignment;
            result.IsCorrect = TextEquals(actualAlignment, expectedAlignment);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页脚对齐方式: 期望 {expectedAlignment}, 实际 {actualAlignment}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页脚对齐方式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页码
    /// </summary>
    private KnowledgePointResult DetectPageNumber(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageNumber",
            Parameters = parameters
        };

        try
        {
            // 验证必需参数
            string[] requiredParams = ["PageNumberPosition", "PageNumberFormat"];
            if (!ValidateRequiredParameters(parameters, "SetPageNumber", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

            _ = TryGetParameter(parameters, "PageNumberPosition", out string expectedPosition);
            _ = TryGetParameter(parameters, "PageNumberFormat", out string expectedFormat);

            MainDocumentPart mainPart = document.MainDocumentPart!;
            (string Position, string Format) pageNumberInfo = GetPageNumberInfo(mainPart);

            result.ExpectedValue = $"位置:{expectedPosition}, 格式:{expectedFormat}";
            result.ActualValue = $"位置:{pageNumberInfo.Position}, 格式:{pageNumberInfo.Format}";
            result.IsCorrect = TextEquals(pageNumberInfo.Position, expectedPosition) &&
                              TextEquals(pageNumberInfo.Format, expectedFormat);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页码设置: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页码失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页面背景
    /// </summary>
    private KnowledgePointResult DetectPageBackground(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageBackground",
            Parameters = parameters
        };

        try
        {
            // 验证必需参数
            string[] requiredParams = ["BackgroundColor"];
            if (!ValidateRequiredParameters(parameters, "SetPageBackground", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

            _ = TryGetParameter(parameters, "BackgroundColor", out string expectedColor);

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualColor = GetPageBackgroundColor(mainPart);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = TextEquals(actualColor, expectedColor) || ColorEquals(actualColor, expectedColor);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页面背景颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页面背景失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页面边框颜色
    /// </summary>
    private KnowledgePointResult DetectPageBorderColor(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageBorderColor",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "BorderColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: BorderColor");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualColor = GetPageBorderColor(mainPart);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = TextEquals(actualColor, expectedColor) || ColorEquals(actualColor, expectedColor);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页面边框颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页面边框颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页面边框样式
    /// </summary>
    private KnowledgePointResult DetectPageBorderStyle(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageBorderStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "BorderStyle", out string expectedStyle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: BorderStyle");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualStyle = GetPageBorderStyle(mainPart);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle;
            result.IsCorrect = TextEquals(actualStyle, expectedStyle);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页面边框样式: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页面边框样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页面边框宽度
    /// </summary>
    private KnowledgePointResult DetectPageBorderWidth(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageBorderWidth",
            Parameters = parameters
        };

        try
        {
            if (!TryGetFloatParameter(parameters, "BorderWidth", out float expectedWidth))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: BorderWidth");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            float actualWidth = GetPageBorderWidth(mainPart);

            result.ExpectedValue = expectedWidth.ToString();
            result.ActualValue = actualWidth.ToString();
            result.IsCorrect = Math.Abs(actualWidth - expectedWidth) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页面边框宽度: 期望 {expectedWidth}, 实际 {actualWidth}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页面边框宽度失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测水印文字
    /// </summary>
    private KnowledgePointResult DetectWatermarkText(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetWatermarkText",
            Parameters = parameters
        };

        try
        {
            // 验证必需参数
            string[] requiredParams = ["WatermarkText"];
            if (!ValidateRequiredParameters(parameters, "SetWatermarkText", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

            _ = TryGetParameter(parameters, "WatermarkText", out string expectedText);

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualText = GetWatermarkText(mainPart);

            result.ExpectedValue = expectedText;
            result.ActualValue = actualText;
            result.IsCorrect = TextEquals(actualText, expectedText) || actualText.Contains(expectedText);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"水印文字: 期望 {expectedText}, 实际 {actualText}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测水印文字失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测水印字体
    /// </summary>
    private KnowledgePointResult DetectWatermarkFont(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetWatermarkFont",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "WatermarkFont", out string expectedFont))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: WatermarkFont");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualFont = GetWatermarkFont(mainPart);

            result.ExpectedValue = expectedFont;
            result.ActualValue = actualFont;
            result.IsCorrect = TextEquals(actualFont, expectedFont);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"水印字体: 期望 {expectedFont}, 实际 {actualFont}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测水印字体失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测水印字号
    /// </summary>
    private KnowledgePointResult DetectWatermarkFontSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetWatermarkFontSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "WatermarkFontSize", out int expectedSize))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: WatermarkFontSize");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            int actualSize = GetWatermarkFontSize(mainPart);

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = actualSize.ToString();
            result.IsCorrect = actualSize == expectedSize;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"水印字号: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测水印字号失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测水印方向
    /// </summary>
    private KnowledgePointResult DetectWatermarkOrientation(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetWatermarkOrientation",
            Parameters = parameters
        };

        try
        {
            if (!TryGetFloatParameter(parameters, "WatermarkAngle", out float expectedAngle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: WatermarkAngle");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            float actualAngle = GetWatermarkOrientation(mainPart);

            result.ExpectedValue = expectedAngle.ToString();
            result.ActualValue = actualAngle.ToString();
            result.IsCorrect = Math.Abs(actualAngle - expectedAngle) < 1.0f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"水印角度: 期望 {expectedAngle}°, 实际 {actualAngle}°";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测水印方向失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表格行数和列数
    /// </summary>
    private KnowledgePointResult DetectTableRowsColumns(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableRowsColumns",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "Rows", out int expectedRows) ||
                !TryGetIntParameter(parameters, "Columns", out int expectedColumns))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: Rows 或 Columns");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            (int Rows, int Columns) tableInfo = GetTableRowsColumns(mainPart);

            result.ExpectedValue = $"行:{expectedRows}, 列:{expectedColumns}";
            result.ActualValue = $"行:{tableInfo.Rows}, 列:{tableInfo.Columns}";
            result.IsCorrect = tableInfo.Rows == expectedRows && tableInfo.Columns == expectedColumns;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表格尺寸: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表格行列数失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表格底纹
    /// </summary>
    private KnowledgePointResult DetectTableShading(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableShading",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "AreaType", out string areaType) ||
                !TryGetIntParameter(parameters, "AreaNumber", out int areaNumber) ||
                !TryGetParameter(parameters, "ShadingColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: AreaType, AreaNumber 或 ShadingColor");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualColor = GetTableShading(mainPart, areaType, areaNumber);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = TextEquals(actualColor, expectedColor) || ColorEquals(actualColor, expectedColor);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表格底纹({areaType} {areaNumber}): 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表格底纹失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表格行高
    /// </summary>
    private KnowledgePointResult DetectTableRowHeight(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableRowHeight",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "StartRow", out int startRow) ||
                !TryGetIntParameter(parameters, "EndRow", out int endRow) ||
                !TryGetFloatParameter(parameters, "RowHeight", out float expectedHeight) ||
                !TryGetParameter(parameters, "HeightType", out string heightType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: StartRow, EndRow, RowHeight 或 HeightType");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            float actualHeight = GetTableRowHeight(mainPart, startRow, endRow);

            result.ExpectedValue = $"{expectedHeight} ({heightType})";
            result.ActualValue = actualHeight.ToString();
            result.IsCorrect = Math.Abs(actualHeight - expectedHeight) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表格行高(行{startRow}-{endRow}): 期望 {result.ExpectedValue}, 实际 {actualHeight}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表格行高失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表格列宽
    /// </summary>
    private KnowledgePointResult DetectTableColumnWidth(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableColumnWidth",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "StartColumn", out int startColumn) ||
                !TryGetIntParameter(parameters, "EndColumn", out int endColumn) ||
                !TryGetFloatParameter(parameters, "ColumnWidth", out float expectedWidth) ||
                !TryGetParameter(parameters, "WidthType", out string widthType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: StartColumn, EndColumn, ColumnWidth 或 WidthType");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            float actualWidth = GetTableColumnWidth(mainPart, startColumn, endColumn);

            result.ExpectedValue = $"{expectedWidth} ({widthType})";
            result.ActualValue = actualWidth.ToString();
            result.IsCorrect = Math.Abs(actualWidth - expectedWidth) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表格列宽(列{startColumn}-{endColumn}): 期望 {result.ExpectedValue}, 实际 {actualWidth}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表格列宽失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表格单元格内容
    /// </summary>
    private KnowledgePointResult DetectTableCellContent(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableCellContent",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "RowNumber", out int rowNumber) ||
                !TryGetIntParameter(parameters, "ColumnNumber", out int columnNumber) ||
                !TryGetParameter(parameters, "CellContent", out string expectedContent))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: RowNumber, ColumnNumber 或 CellContent");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualContent = GetTableCellContent(mainPart, rowNumber, columnNumber);

            result.ExpectedValue = expectedContent;
            result.ActualValue = actualContent;
            result.IsCorrect = TextEquals(actualContent, expectedContent) || actualContent.Contains(expectedContent);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格({rowNumber},{columnNumber})内容: 期望 {expectedContent}, 实际 {actualContent}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表格单元格内容失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表格单元格对齐方式
    /// </summary>
    private KnowledgePointResult DetectTableCellAlignment(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableCellAlignment",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "RowNumber", out int rowNumber) ||
                !TryGetIntParameter(parameters, "ColumnNumber", out int columnNumber) ||
                !TryGetParameter(parameters, "HorizontalAlignment", out string expectedHorizontal) ||
                !TryGetParameter(parameters, "VerticalAlignment", out string expectedVertical))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: RowNumber, ColumnNumber, HorizontalAlignment 或 VerticalAlignment");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            (string Horizontal, string Vertical) = GetTableCellAlignment(mainPart, rowNumber, columnNumber);

            result.ExpectedValue = $"水平:{expectedHorizontal}, 垂直:{expectedVertical}";
            result.ActualValue = $"水平:{Horizontal}, 垂直:{Vertical}";
            result.IsCorrect = TextEquals(Horizontal, expectedHorizontal) &&
                              TextEquals(Vertical, expectedVertical);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格({rowNumber},{columnNumber})对齐: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表格单元格对齐失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表格对齐方式
    /// </summary>
    private KnowledgePointResult DetectTableAlignment(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableAlignment",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "TableAlignment", out string expectedAlignment) ||
                !TryGetFloatParameter(parameters, "LeftIndent", out float expectedIndent))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: TableAlignment 或 LeftIndent");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            (string Alignment, float LeftIndent) = GetTableAlignment(mainPart);

            result.ExpectedValue = $"对齐:{expectedAlignment}, 缩进:{expectedIndent}";
            result.ActualValue = $"对齐:{Alignment}, 缩进:{LeftIndent}";
            result.IsCorrect = TextEquals(Alignment, expectedAlignment) &&
                              Math.Abs(LeftIndent - expectedIndent) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表格对齐: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表格对齐失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测合并单元格
    /// </summary>
    private KnowledgePointResult DetectMergeTableCells(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "MergeTableCells",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "StartRow", out int startRow) ||
                !TryGetIntParameter(parameters, "StartColumn", out int startColumn) ||
                !TryGetIntParameter(parameters, "EndRow", out int endRow) ||
                !TryGetIntParameter(parameters, "EndColumn", out int endColumn))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: StartRow, StartColumn, EndRow 或 EndColumn");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasMergedCells = CheckMergedCells(mainPart, startRow, startColumn, endRow, endColumn);

            result.ExpectedValue = "已合并";
            result.ActualValue = hasMergedCells ? "已合并" : "未合并";
            result.IsCorrect = hasMergedCells;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格合并({startRow},{startColumn})-({endRow},{endColumn}): {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测合并单元格失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表头第一个单元格的内容
    /// </summary>
    private KnowledgePointResult DetectTableHeaderContent(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableHeaderContent",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ColumnNumber", out int columnNumber) ||
                !TryGetParameter(parameters, "HeaderContent", out string expectedContent))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ColumnNumber 或 HeaderContent");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualContent = GetTableHeaderContent(mainPart, columnNumber);

            result.ExpectedValue = expectedContent;
            result.ActualValue = actualContent;
            result.IsCorrect = TextEquals(actualContent, expectedContent) || actualContent.Contains(expectedContent);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表头第{columnNumber}列内容: 期望 {expectedContent}, 实际 {actualContent}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表头内容失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表头第一个单元格的对齐方式
    /// </summary>
    private KnowledgePointResult DetectTableHeaderAlignment(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableHeaderAlignment",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "ColumnNumber", out int columnNumber) ||
                !TryGetParameter(parameters, "HorizontalAlignment", out string expectedHorizontal) ||
                !TryGetParameter(parameters, "VerticalAlignment", out string expectedVertical))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ColumnNumber, HorizontalAlignment 或 VerticalAlignment");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            (string Horizontal, string Vertical) = GetTableHeaderAlignment(mainPart, columnNumber);

            result.ExpectedValue = $"水平:{expectedHorizontal}, 垂直:{expectedVertical}";
            result.ActualValue = $"水平:{Horizontal}, 垂直:{Vertical}";
            result.IsCorrect = TextEquals(Horizontal, expectedHorizontal) &&
                              TextEquals(Vertical, expectedVertical);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表头第{columnNumber}列对齐: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表头对齐失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测项目编号
    /// </summary>
    private KnowledgePointResult DetectBulletNumbering(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetBulletNumbering",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "ParagraphNumbers", out string paragraphNumbers) ||
                !TryGetParameter(parameters, "NumberingType", out string expectedType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphNumbers 或 NumberingType");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualType = GetBulletNumberingType(mainPart, paragraphNumbers);

            result.ExpectedValue = expectedType;
            result.ActualValue = actualType;
            result.IsCorrect = TextEquals(actualType, expectedType);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"项目编号(段落{paragraphNumbers}): 期望 {expectedType}, 实际 {actualType}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测项目编号失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入自选图形类型
    /// </summary>
    private KnowledgePointResult DetectInsertAutoShape(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertAutoShape",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "ShapeType", out string expectedType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ShapeType");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualType = GetAutoShapeType(mainPart);

            result.ExpectedValue = expectedType;
            result.ActualValue = actualType;
            result.IsCorrect = TextEquals(actualType, expectedType);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"自选图形类型: 期望 {expectedType}, 实际 {actualType}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测自选图形类型失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测自选图形大小
    /// </summary>
    private KnowledgePointResult DetectAutoShapeSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAutoShapeSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetFloatParameter(parameters, "ShapeHeight", out float expectedHeight) ||
                !TryGetFloatParameter(parameters, "ShapeWidth", out float expectedWidth))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ShapeHeight 或 ShapeWidth");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            (float Height, float Width) sizeInfo = GetAutoShapeSize(mainPart);

            result.ExpectedValue = $"高:{expectedHeight}, 宽:{expectedWidth}";
            result.ActualValue = $"高:{sizeInfo.Height}, 宽:{sizeInfo.Width}";
            result.IsCorrect = Math.Abs(sizeInfo.Height - expectedHeight) < 0.1f &&
                              Math.Abs(sizeInfo.Width - expectedWidth) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"自选图形大小: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测自选图形大小失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测自选图形线条颜色
    /// </summary>
    private KnowledgePointResult DetectAutoShapeLineColor(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAutoShapeLineColor",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "LineColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: LineColor");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualColor = GetAutoShapeLineColor(mainPart);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = TextEquals(actualColor, expectedColor) || ColorEquals(actualColor, expectedColor);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"自选图形线条颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测自选图形线条颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测自选图形填充颜色
    /// </summary>
    private KnowledgePointResult DetectAutoShapeFillColor(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAutoShapeFillColor",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FillColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FillColor");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualColor = GetAutoShapeFillColor(mainPart);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = TextEquals(actualColor, expectedColor) || ColorEquals(actualColor, expectedColor);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"自选图形填充颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测自选图形填充颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测自选图形中文字大小
    /// </summary>
    private KnowledgePointResult DetectAutoShapeTextSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAutoShapeTextSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "FontSize", out int expectedSize))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FontSize");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            int actualSize = GetAutoShapeTextSize(mainPart);

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = actualSize.ToString();
            result.IsCorrect = actualSize == expectedSize;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"自选图形文字大小: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测自选图形文字大小失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测自选图形中文字颜色
    /// </summary>
    private KnowledgePointResult DetectAutoShapeTextColor(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAutoShapeTextColor",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "TextColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: TextColor");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualColor = GetAutoShapeTextColor(mainPart);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = TextEquals(actualColor, expectedColor) || ColorEquals(actualColor, expectedColor);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"自选图形文字颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测自选图形文字颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测自选图形中文字内容
    /// </summary>
    private KnowledgePointResult DetectAutoShapeTextContent(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAutoShapeTextContent",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "TextContent", out string expectedContent))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: TextContent");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualContent = GetAutoShapeTextContent(mainPart);

            result.ExpectedValue = expectedContent;
            result.ActualValue = actualContent;
            result.IsCorrect = TextEquals(actualContent, expectedContent) || actualContent.Contains(expectedContent);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"自选图形文字内容: 期望 {expectedContent}, 实际 {actualContent}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测自选图形文字内容失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测自选图形位置
    /// </summary>
    private KnowledgePointResult DetectAutoShapePosition(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAutoShapePosition",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            (bool HasPosition, string Horizontal, string Vertical) = GetAutoShapePosition(mainPart);

            result.ExpectedValue = "位置已设置";
            result.ActualValue = HasPosition ? $"水平:{Horizontal}, 垂直:{Vertical}" : "未设置位置";
            result.IsCorrect = HasPosition;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"自选图形位置: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测自选图形位置失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入图片边框复合类型
    /// </summary>
    private KnowledgePointResult DetectImageBorderCompoundType(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetImageBorderCompoundType",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "CompoundType", out string expectedType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: CompoundType");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualType = GetImageBorderCompoundType(mainPart);

            result.ExpectedValue = expectedType;
            result.ActualValue = actualType;
            result.IsCorrect = TextEquals(actualType, expectedType);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片边框复合类型: 期望 {expectedType}, 实际 {actualType}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图片边框复合类型失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入图片边框短划线类型
    /// </summary>
    private KnowledgePointResult DetectImageBorderDashType(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetImageBorderDashType",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "DashType", out string expectedType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: DashType");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualType = GetImageBorderDashType(mainPart);

            result.ExpectedValue = expectedType;
            result.ActualValue = actualType;
            result.IsCorrect = TextEquals(actualType, expectedType);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片边框短划线类型: 期望 {expectedType}, 实际 {actualType}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图片边框短划线类型失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入图片边框线宽
    /// </summary>
    private KnowledgePointResult DetectImageBorderWidth(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetImageBorderWidth",
            Parameters = parameters
        };

        try
        {
            if (!TryGetFloatParameter(parameters, "BorderWidth", out float expectedWidth))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: BorderWidth");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            float actualWidth = GetImageBorderWidth(mainPart);

            result.ExpectedValue = expectedWidth.ToString();
            result.ActualValue = actualWidth.ToString();
            result.IsCorrect = Math.Abs(actualWidth - expectedWidth) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片边框线宽: 期望 {expectedWidth}, 实际 {actualWidth}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图片边框线宽失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入图片边框颜色
    /// </summary>
    private KnowledgePointResult DetectImageBorderColor(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetImageBorderColor",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "BorderColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: BorderColor");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualColor = GetImageBorderColor(mainPart);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = TextEquals(actualColor, expectedColor) || ColorEquals(actualColor, expectedColor);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片边框颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图片边框颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入图片阴影类型与颜色
    /// </summary>
    private KnowledgePointResult DetectImageShadow(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetImageShadow",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "ShadowType", out string expectedType) ||
                !TryGetParameter(parameters, "ShadowColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ShadowType 或 ShadowColor");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            (string Type, string Color) shadowInfo = GetImageShadowInfo(mainPart);

            result.ExpectedValue = $"类型:{expectedType}, 颜色:{expectedColor}";
            result.ActualValue = $"类型:{shadowInfo.Type}, 颜色:{shadowInfo.Color}";
            result.IsCorrect = TextEquals(shadowInfo.Type, expectedType) &&
                              (TextEquals(shadowInfo.Color, expectedColor) || ColorEquals(shadowInfo.Color, expectedColor));
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片阴影: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图片阴影失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入图片环绕方式
    /// </summary>
    private KnowledgePointResult DetectImageWrapStyle(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetImageWrapStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "WrapStyle", out string expectedStyle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: WrapStyle");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualStyle = GetImageWrapStyle(mainPart);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle;
            result.IsCorrect = TextEquals(actualStyle, expectedStyle);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片环绕方式: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图片环绕方式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入图片的高度和宽度
    /// </summary>
    private KnowledgePointResult DetectImageSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetImageSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetFloatParameter(parameters, "ImageHeight", out float expectedHeight) ||
                !TryGetFloatParameter(parameters, "ImageWidth", out float expectedWidth))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ImageHeight 或 ImageWidth");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            (float Height, float Width) sizeInfo = GetImageSize(mainPart);

            result.ExpectedValue = $"高:{expectedHeight}, 宽:{expectedWidth}";
            result.ActualValue = $"高:{sizeInfo.Height}, 宽:{sizeInfo.Width}";
            result.IsCorrect = Math.Abs(sizeInfo.Height - expectedHeight) < 0.1f &&
                              Math.Abs(sizeInfo.Width - expectedWidth) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片尺寸: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图片尺寸失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入图片的位置
    /// </summary>
    private KnowledgePointResult DetectImagePosition(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetImagePosition",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            (bool HasPosition, string Horizontal, string Vertical) = GetImagePosition(mainPart);

            result.ExpectedValue = "位置已设置";
            result.ActualValue = HasPosition ? $"水平:{Horizontal}, 垂直:{Vertical}" : "未设置位置";
            result.IsCorrect = HasPosition;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片位置: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图片位置失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测文本框边框颜色
    /// </summary>
    private KnowledgePointResult DetectTextBoxBorderColor(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextBoxBorderColor",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "BorderColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: BorderColor");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualColor = GetTextBoxBorderColor(mainPart);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = TextEquals(actualColor, expectedColor) || ColorEquals(actualColor, expectedColor);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"文本框边框颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文本框边框颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测文本框中文字内容
    /// </summary>
    private KnowledgePointResult DetectTextBoxContent(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextBoxContent",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "TextContent", out string expectedContent))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: TextContent");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualContent = GetTextBoxContent(mainPart);

            result.ExpectedValue = expectedContent;
            result.ActualValue = actualContent;
            result.IsCorrect = TextEquals(actualContent, expectedContent) || actualContent.Contains(expectedContent);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"文本框内容: 期望 {expectedContent}, 实际 {actualContent}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文本框内容失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测文本框中文字大小
    /// </summary>
    private KnowledgePointResult DetectTextBoxTextSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextBoxTextSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "TextSize", out int expectedSize))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: TextSize");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            int actualSize = GetTextBoxTextSize(mainPart);

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = actualSize.ToString();
            result.IsCorrect = actualSize == expectedSize;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"文本框文字大小: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文本框文字大小失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测文本框位置
    /// </summary>
    private KnowledgePointResult DetectTextBoxPosition(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextBoxPosition",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            (bool HasPosition, string Horizontal, string Vertical) = GetTextBoxPosition(mainPart);

            result.ExpectedValue = "位置已设置";
            result.ActualValue = HasPosition ? $"水平:{Horizontal}, 垂直:{Vertical}" : "未设置位置";
            result.IsCorrect = HasPosition;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"文本框位置: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文本框位置失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测文本框环绕方式
    /// </summary>
    private KnowledgePointResult DetectTextBoxWrapStyle(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextBoxWrapStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "WrapStyle", out string expectedStyle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: WrapStyle");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualStyle = GetTextBoxWrapStyle(mainPart);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle;
            result.IsCorrect = TextEquals(actualStyle, expectedStyle);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"文本框环绕方式: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文本框环绕方式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测查找与替换
    /// </summary>
    private KnowledgePointResult DetectFindAndReplace(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "FindAndReplace",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FindText", out string findText) ||
                !TryGetParameter(parameters, "ReplaceText", out string replaceText) ||
                !TryGetIntParameter(parameters, "ReplaceCount", out int expectedCount))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FindText, ReplaceText 或 ReplaceCount");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            int actualCount = GetFindAndReplaceCount(mainPart, findText, replaceText);

            result.ExpectedValue = expectedCount.ToString();
            result.ActualValue = actualCount.ToString();
            result.IsCorrect = actualCount == expectedCount;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"查找替换('{findText}' -> '{replaceText}'): 期望替换 {expectedCount} 次, 实际替换 {actualCount} 次";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测查找替换失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测指定文字字号
    /// </summary>
    private KnowledgePointResult DetectSpecificTextFontSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSpecificTextFontSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "TargetText", out string targetText) ||
                !TryGetIntParameter(parameters, "FontSize", out int expectedSize))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: TargetText 或 FontSize");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            int actualSize = GetSpecificTextFontSize(mainPart, targetText);

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = actualSize.ToString();
            result.IsCorrect = actualSize == expectedSize;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"指定文字('{targetText}')字号: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测指定文字字号失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 在段落中搜索匹配指定条件的段落
    /// </summary>
    /// <typeparam name="T">期望值的类型</typeparam>
    /// <param name="paragraphs">段落列表</param>
    /// <param name="paragraphNumber">段落索引（-1表示搜索所有段落）</param>
    /// <param name="expectedValue">期望值</param>
    /// <param name="getActualValue">获取段落实际值的函数</param>
    /// <param name="comparer">比较函数，如果为null则使用默认相等比较</param>
    /// <param name="matchedParagraph">匹配的段落</param>
    /// <param name="actualValue">实际找到的值</param>
    /// <param name="errorMessage">错误信息</param>
    /// <returns>是否找到匹配的段落</returns>
    private static bool FindMatchingParagraph<T>(
        List<Paragraph>? paragraphs,
        int paragraphNumber,
        T expectedValue,
        Func<Paragraph, T> getActualValue,
        Func<T, T, bool>? comparer,
        out Paragraph? matchedParagraph,
        out T? actualValue,
        out string errorMessage)
    {
        matchedParagraph = null;
        actualValue = default;
        errorMessage = string.Empty;

        if (paragraphs == null || paragraphs.Count == 0)
        {
            errorMessage = "文档中没有段落";
            return false;
        }

        // 使用默认比较器如果没有提供
        comparer ??= EqualityComparer<T>.Default.Equals;

        // 如果指定了具体段落索引
        if (paragraphNumber != -1)
        {
            if (paragraphNumber < 1 || paragraphNumber > paragraphs.Count)
            {
                errorMessage = $"段落索引超出范围: {paragraphNumber}，有效范围: 1-{paragraphs.Count}";
                return false;
            }

            matchedParagraph = paragraphs[paragraphNumber - 1];
            actualValue = getActualValue(matchedParagraph);
            bool isMatch = comparer(actualValue, expectedValue);

            if (!isMatch)
            {
                errorMessage = $"段落 {paragraphNumber} 的值不匹配期望值";
            }

            return isMatch;
        }

        // -1 表示搜索所有段落，找到任意一个匹配的即可
        List<string> debugInfo = [];
        for (int i = 0; i < paragraphs.Count; i++)
        {
            Paragraph paragraph = paragraphs[i];
            T currentValue = getActualValue(paragraph);
            debugInfo.Add($"段落{i + 1}: {currentValue}");

            if (comparer(currentValue, expectedValue))
            {
                matchedParagraph = paragraph;
                actualValue = currentValue;
                System.Diagnostics.Debug.WriteLine($"[段落匹配成功] 在段落{i + 1}找到匹配值: {currentValue}");
                return true;
            }
        }

        // 没有找到匹配的段落，返回第一个段落的值作为实际值
        if (paragraphs.Count > 0)
        {
            matchedParagraph = paragraphs[0];
            actualValue = getActualValue(matchedParagraph);
        }

        string allValues = string.Join("; ", debugInfo);
        errorMessage = $"在所有段落中都没有找到匹配期望值'{expectedValue}'的段落。实际值: {allValues}";
        System.Diagnostics.Debug.WriteLine($"[段落匹配失败] 期望: {expectedValue}, 实际检查的段落: {allValues}");
        return false;
    }

    /// <summary>
    /// 在元素列表中搜索匹配指定条件的元素（通用方法）
    /// </summary>
    /// <typeparam name="TElement">元素类型</typeparam>
    /// <typeparam name="TValue">期望值的类型</typeparam>
    /// <param name="elements">元素列表</param>
    /// <param name="elementIndex">元素索引（-1表示搜索所有元素）</param>
    /// <param name="expectedValue">期望值</param>
    /// <param name="getActualValue">获取元素实际值的函数</param>
    /// <param name="comparer">比较函数，如果为null则使用默认相等比较</param>
    /// <param name="elementTypeName">元素类型名称（用于错误信息）</param>
    /// <param name="matchedElement">匹配的元素</param>
    /// <param name="actualValue">实际找到的值</param>
    /// <param name="errorMessage">错误信息</param>
    /// <returns>是否找到匹配的元素</returns>
    private static bool FindMatchingElement<TElement, TValue>(
        List<TElement>? elements,
        int elementIndex,
        TValue expectedValue,
        Func<TElement, TValue> getActualValue,
        Func<TValue, TValue, bool>? comparer,
        string elementTypeName,
        out TElement? matchedElement,
        out TValue? actualValue,
        out string errorMessage)
    {
        matchedElement = default;
        actualValue = default;
        errorMessage = string.Empty;

        if (elements == null || elements.Count == 0)
        {
            errorMessage = $"文档中没有{elementTypeName}";
            return false;
        }

        // 使用默认比较器如果没有提供
        comparer ??= EqualityComparer<TValue>.Default.Equals;

        // 如果指定了具体元素索引
        if (elementIndex != -1)
        {
            if (elementIndex < 1 || elementIndex > elements.Count)
            {
                errorMessage = $"{elementTypeName}索引超出范围: {elementIndex}，有效范围: 1-{elements.Count}";
                return false;
            }

            matchedElement = elements[elementIndex - 1];
            actualValue = getActualValue(matchedElement);
            bool isMatch = comparer(actualValue, expectedValue);

            if (!isMatch)
            {
                errorMessage = $"{elementTypeName} {elementIndex} 的值不匹配期望值";
            }

            return isMatch;
        }

        // -1 表示搜索所有元素，找到任意一个匹配的即可
        for (int i = 0; i < elements.Count; i++)
        {
            TElement element = elements[i];
            TValue currentValue = getActualValue(element);

            if (comparer(currentValue, expectedValue))
            {
                matchedElement = element;
                actualValue = currentValue;
                return true;
            }
        }

        // 没有找到匹配的元素，返回第一个元素的值作为实际值
        if (elements.Count > 0)
        {
            matchedElement = elements[0];
            actualValue = getActualValue(matchedElement);
        }

        errorMessage = $"在所有{elementTypeName}中都没有找到匹配期望值的{elementTypeName}";
        return false;
    }

    /// <summary>
    /// 获取段落字体
    /// </summary>
    private static string GetParagraphFont(Paragraph paragraph)
    {
        try
        {
            IEnumerable<Run> runs = paragraph.Elements<Run>();
            foreach (Run run in runs)
            {
                RunProperties? runProperties = run.RunProperties;
                RunFonts? runFonts = runProperties?.RunFonts;
                if (runFonts?.Ascii?.Value != null)
                {
                    return runFonts.Ascii.Value;
                }
            }
            return "默认字体";
        }
        catch
        {
            return "未知字体";
        }
    }

    /// <summary>
    /// 获取段落字号
    /// </summary>
    private static int GetParagraphFontSize(Paragraph paragraph)
    {
        try
        {
            IEnumerable<Run> runs = paragraph.Elements<Run>();
            foreach (Run run in runs)
            {
                RunProperties? runProperties = run.RunProperties;
                FontSize? fontSize = runProperties?.FontSize;
                if (fontSize?.Val?.Value != null)
                {
                    return int.Parse(fontSize.Val.Value) / 2; // OpenXML中字号是半点值
                }
            }
            return 12; // 默认字号
        }
        catch
        {
            return 12;
        }
    }

    /// <summary>
    /// 获取段落字形
    /// </summary>
    private static string GetParagraphFontStyle(Paragraph paragraph)
    {
        try
        {
            IEnumerable<Run> runs = paragraph.Elements<Run>();
            foreach (Run run in runs)
            {
                RunProperties? runProperties = run.RunProperties;
                if (runProperties != null)
                {
                    List<string> styles = [];

                    // 检查粗体：需要检查值是否为true或者属性存在且没有明确设为false
                    if (runProperties.Bold != null &&
                        (runProperties.Bold.Val == null || runProperties.Bold.Val.Value))
                    {
                        styles.Add("粗体");
                    }

                    // 检查斜体
                    if (runProperties.Italic != null &&
                        (runProperties.Italic.Val == null || runProperties.Italic.Val.Value))
                    {
                        styles.Add("斜体");
                    }

                    // 检查下划线
                    if (runProperties.Underline != null)
                    {
                        styles.Add("下划线");
                    }

                    // 检查删除线
                    if (runProperties.Strike != null &&
                        (runProperties.Strike.Val == null || runProperties.Strike.Val.Value))
                    {
                        styles.Add("删除线");
                    }

                    // 检查双删除线
                    if (runProperties.DoubleStrike != null &&
                        (runProperties.DoubleStrike.Val == null || runProperties.DoubleStrike.Val.Value))
                    {
                        styles.Add("双删除线");
                    }

                    if (styles.Count > 0)
                    {
                        return string.Join(", ", styles);
                    }
                }
            }
            return "常规";
        }
        catch
        {
            return "未知样式";
        }
    }

    /// <summary>
    /// 获取段落字间距
    /// </summary>
    private static float GetParagraphCharacterSpacing(Paragraph paragraph)
    {
        try
        {
            IEnumerable<Run> runs = paragraph.Elements<Run>();
            foreach (Run run in runs)
            {
                RunProperties? runProperties = run.RunProperties;
                Spacing? spacing = runProperties?.Spacing;
                if (spacing?.Val?.Value != null)
                {
                    return int.Parse(spacing.Val.Value.ToString()) / 20f; // OpenXML中间距是20分之一点
                }
            }
            return 0f; // 默认无额外间距
        }
        catch
        {
            return 0f;
        }
    }

    /// <summary>
    /// 获取段落文字颜色
    /// </summary>
    private static string GetParagraphTextColor(Paragraph paragraph)
    {
        try
        {
            IEnumerable<Run> runs = paragraph.Elements<Run>();
            foreach (Run run in runs)
            {
                RunProperties? runProperties = run.RunProperties;
                Color? color = runProperties?.Color;
                if (color?.Val?.Value != null)
                {
                    string colorValue = color.Val.Value;
                    // 确保颜色值有#前缀
                    if (!colorValue.StartsWith("#") && colorValue.Length == 6)
                    {
                        colorValue = "#" + colorValue;
                    }
                    return colorValue;
                }
            }
            return "自动"; // 默认颜色
        }
        catch
        {
            return "未知颜色";
        }
    }

    /// <summary>
    /// 将十六进制颜色代码转换为颜色名称
    /// </summary>
    private static string ConvertHexToColorName(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor))
        {
            return hexColor;
        }

        // 移除#前缀进行比较
        string hex = hexColor.TrimStart('#').ToUpper();

        return hex switch
        {
            "FF0000" => "红色",
            "00FF00" => "绿色",
            "0000FF" => "蓝色",
            "FFFF00" => "黄色",
            "FF00FF" => "洋红",
            "00FFFF" => "青色",
            "000000" => "黑色",
            "FFFFFF" => "白色",
            "808080" => "灰色",
            "FFA500" => "橙色",
            "800080" => "紫色",
            "008000" => "深绿色",
            "000080" => "深蓝色",
            "800000" => "深红色",
            "05F8FF" => "浅青色",
            "FF2E45" => "深粉色",
            _ => hexColor // 如果没有匹配的颜色名称，返回原始值
        };
    }

    /// <summary>
    /// 获取段落缩进信息
    /// </summary>
    private static (int FirstLine, int Left, int Right) GetParagraphIndentation(Paragraph paragraph)
    {
        try
        {
            ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
            Indentation? indentation = paragraphProperties?.Indentation;

            if (indentation != null)
            {
                // OpenXML中缩进值以twips为单位，1 twip = 1/20 point，1 point ≈ 1.33 pixel
                // 转换为磅值（points）：twips / 20
                int firstLine = indentation.FirstLine?.Value != null ? int.Parse(indentation.FirstLine.Value) / 20 : 0;
                int left = indentation.Left?.Value != null ? int.Parse(indentation.Left.Value) / 20 : 0;
                int right = indentation.Right?.Value != null ? int.Parse(indentation.Right.Value) / 20 : 0;

                return (firstLine, left, right);
            }

            return (0, 0, 0);
        }
        catch (Exception ex)
        {
            // 添加调试信息
            System.Diagnostics.Debug.WriteLine($"获取段落缩进失败: {ex.Message}");
            return (0, 0, 0);
        }
    }

    /// <summary>
    /// 获取段落行间距
    /// </summary>
    private static float GetParagraphLineSpacing(Paragraph paragraph)
    {
        try
        {
            ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
            SpacingBetweenLines? spacingBetweenLines = paragraphProperties?.SpacingBetweenLines;

            if (spacingBetweenLines?.Line?.Value != null)
            {
                return int.Parse(spacingBetweenLines.Line.Value.ToString()) / 240f; // OpenXML中行距是240分之一
            }

            return 1.0f; // 默认单倍行距
        }
        catch
        {
            return 1.0f;
        }
    }

    /// <summary>
    /// 获取段落首字下沉
    /// </summary>
    private static string GetParagraphDropCap(Paragraph paragraph)
    {
        try
        {
            ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
            if (paragraphProperties == null)
            {
                return "无首字下沉";
            }

            // 方法1：检查FrameProperties中的首字下沉设置
            FrameProperties? framePr = paragraphProperties.GetFirstChild<FrameProperties>();
            if (framePr != null)
            {
                System.Diagnostics.Debug.WriteLine($"[首字下沉] 找到FrameProperties: {framePr.OuterXml}");

                // 检查XML中的dropCap属性
                if (framePr.OuterXml.Contains("dropCap="))
                {
                    System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(framePr.OuterXml, @"dropCap=""([^""]+)""");
                    if (match.Success)
                    {
                        string dropCapValue = match.Groups[1].Value;
                        System.Diagnostics.Debug.WriteLine($"[首字下沉] XML提取DropCap值: {dropCapValue}");

                        return dropCapValue.ToLower() switch
                        {
                            "drop" => "首字下沉到段落中",
                            "margin" => "首字下沉到页边距",
                            _ => $"首字下沉({dropCapValue})"
                        };
                    }
                }

                if (framePr.DropCap?.Value != null)
                {
                    string dropCapValue = framePr.DropCap.Value.ToString();
                    string lines = framePr.Lines?.Value.ToString() ?? "3";

                    System.Diagnostics.Debug.WriteLine($"[首字下沉] 枚举DropCap值: {dropCapValue}, Lines: {lines}");

                    return dropCapValue switch
                    {
                        "Drop" => "首字下沉到段落中",
                        "Margin" => "首字下沉到页边距",
                        _ => $"首字下沉({dropCapValue})"
                    };
                }

                if (framePr.Lines?.Value != null)
                {
                    return $"首字下沉 {framePr.Lines.Value} 行";
                }
            }

            // 方法2：检查段落样式中的首字下沉设置
            ParagraphStyleId? styleId = paragraphProperties.ParagraphStyleId;
            if (styleId?.Val?.Value != null)
            {
                System.Diagnostics.Debug.WriteLine($"[首字下沉] 段落样式ID: {styleId.Val.Value}");
                // 某些首字下沉可能通过样式定义
                if (styleId.Val.Value.Contains("DropCap") || styleId.Val.Value.Contains("首字"))
                {
                    return "首字下沉(通过样式)";
                }
            }

            // 方法3：检查段落中第一个Run的特殊格式
            Run? firstRun = paragraph.Elements<Run>().FirstOrDefault();
            if (firstRun?.RunProperties != null)
            {
                RunProperties runProps = firstRun.RunProperties;

                // 检查垂直对齐
                VerticalTextAlignment? vertAlign = runProps.VerticalTextAlignment;
                if (vertAlign?.Val?.HasValue == true)
                {
                    System.Diagnostics.Debug.WriteLine($"[首字下沉] 垂直对齐: {vertAlign.Val.Value}");
                    return $"首字特殊对齐({vertAlign.Val.Value})";
                }

                // 检查字号是否明显大于正常字号（首字下沉通常字号较大）
                FontSize? fontSize = runProps.FontSize;
                if (fontSize?.Val?.Value != null)
                {
                    if (int.TryParse(fontSize.Val.Value, out int sizeValue))
                    {
                        int size = sizeValue / 2; // OpenXML字号是半点单位
                        System.Diagnostics.Debug.WriteLine($"[首字下沉] 首字字号: {size}");
                        if (size > 20) // 如果字号大于20，可能是首字下沉
                        {
                            return $"疑似首字下沉(字号{size})";
                        }
                    }
                }

                // 检查是否有特殊的文本效果
                if (runProps.Bold != null || runProps.Italic != null)
                {
                    string text = firstRun.InnerText;
                    if (!string.IsNullOrEmpty(text) && text.Length == 1)
                    {
                        System.Diagnostics.Debug.WriteLine($"[首字下沉] 单字符特殊格式: '{text}'");
                        return $"疑似首字下沉(单字符'{text}')";
                    }
                }
            }

            // 方法4：检查段落是否以特殊格式的单个字符开始
            string paragraphText = paragraph.InnerText.Trim();
            if (!string.IsNullOrEmpty(paragraphText))
            {
                char firstChar = paragraphText[0];
                // 检查第一个字符是否在单独的Run中且有特殊格式
                Run? firstCharRun = paragraph.Elements<Run>().FirstOrDefault();
                if (firstCharRun != null && firstCharRun.InnerText.Length == 1 && firstCharRun.InnerText[0] == firstChar)
                {
                    if (firstCharRun.RunProperties != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[首字下沉] 检测到单字符Run: '{firstChar}'");
                        return $"疑似首字下沉(首字符'{firstChar}')";
                    }
                }
            }

            return "无首字下沉";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取首字下沉失败: {ex.Message}");
            return "检测失败";
        }
    }

    /// <summary>
    /// 获取段落边框颜色
    /// </summary>
    private static string GetParagraphBorderColor(Paragraph paragraph)
    {
        try
        {
            ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
            ParagraphBorders? paragraphBorders = paragraphProperties?.ParagraphBorders;

            if (paragraphBorders != null)
            {
                TopBorder? topBorder = paragraphBorders.TopBorder;
                if (topBorder?.Color?.Value != null)
                {
                    string colorValue = topBorder.Color.Value;
                    // 确保颜色值有#前缀
                    if (!colorValue.StartsWith("#") && colorValue.Length == 6)
                    {
                        colorValue = "#" + colorValue;
                    }
                    // 尝试转换为颜色名称
                    return ConvertHexToColorName(colorValue);
                }
            }

            return "无边框";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取段落边框样式
    /// </summary>
    private static string GetParagraphBorderStyle(Paragraph paragraph)
    {
        try
        {
            ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
            ParagraphBorders? paragraphBorders = paragraphProperties?.ParagraphBorders;

            if (paragraphBorders != null)
            {
                TopBorder? topBorder = paragraphBorders.TopBorder;
                if (topBorder?.Val != null)
                {
                    string borderValue = string.Empty;

                    // 方法1：尝试直接获取XML属性值
                    if (topBorder.OuterXml.Contains("val="))
                    {
                        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(topBorder.OuterXml, @"val=""([^""]+)""");
                        if (match.Success)
                        {
                            borderValue = match.Groups[1].Value;
                        }
                    }

                    // 方法2：尝试枚举值
                    if (string.IsNullOrEmpty(borderValue) && topBorder.Val.HasValue)
                    {
                        borderValue = topBorder.Val.Value.ToString();
                    }

                    // 方法3：尝试InnerText
                    if (string.IsNullOrEmpty(borderValue) && !string.IsNullOrEmpty(topBorder.Val.InnerText))
                    {
                        borderValue = topBorder.Val.InnerText;
                    }

                    System.Diagnostics.Debug.WriteLine($"[边框样式] XML: {topBorder.OuterXml}");
                    System.Diagnostics.Debug.WriteLine($"[边框样式] 提取值: '{borderValue}', HasValue: {topBorder.Val.HasValue}");

                    if (!string.IsNullOrEmpty(borderValue) && borderValue != "BorderValues { }")
                    {
                        return borderValue.ToLower() switch
                        {
                            "single" => "单线",
                            "double" => "双线",
                            "dotted" => "点线",
                            "dashed" => "虚线",
                            "dashdot" => "点划线",
                            "dashdotdot" => "双点划线",
                            "triple" => "三线",
                            "thick" => "粗线",
                            "thin" => "细线",
                            "wave" => "波浪线",
                            _ => $"边框样式({borderValue})"
                        };
                    }
                }
            }

            return "无边框";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取段落边框宽度
    /// </summary>
    private static float GetParagraphBorderWidth(Paragraph paragraph)
    {
        try
        {
            ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
            ParagraphBorders? paragraphBorders = paragraphProperties?.ParagraphBorders;

            if (paragraphBorders != null)
            {
                TopBorder? topBorder = paragraphBorders.TopBorder;
                if (topBorder?.Size?.Value != null)
                {
                    return uint.Parse(topBorder.Size.Value.ToString()) / 8f; // OpenXML中边框宽度是8分之一点
                }
            }

            return 0f;
        }
        catch
        {
            return 0f;
        }
    }

    /// <summary>
    /// 获取段落底纹信息
    /// </summary>
    private static (string Color, string Pattern) GetParagraphShading(Paragraph paragraph)
    {
        try
        {
            ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
            Shading? shading = paragraphProperties?.Shading;

            if (shading != null)
            {
                // 处理底纹颜色
                string color = shading.Fill?.Value ?? "无颜色";
                if (!string.IsNullOrEmpty(color) && color != "无颜色")
                {
                    // 确保颜色值有#前缀
                    if (!color.StartsWith("#") && color.Length == 6)
                    {
                        color = "#" + color;
                    }
                    // 尝试转换为颜色名称
                    color = ConvertHexToColorName(color);
                }

                // 处理底纹图案
                string pattern = "无图案";
                if (shading.Val != null)
                {
                    string patternValue = string.Empty;

                    // 方法1：尝试直接获取XML属性值
                    if (shading.OuterXml.Contains("val="))
                    {
                        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(shading.OuterXml, @"val=""([^""]+)""");
                        if (match.Success)
                        {
                            patternValue = match.Groups[1].Value;
                        }
                    }

                    // 方法2：尝试枚举值
                    if (string.IsNullOrEmpty(patternValue) && shading.Val.HasValue)
                    {
                        patternValue = shading.Val.Value.ToString();
                    }

                    // 方法3：尝试InnerText
                    if (string.IsNullOrEmpty(patternValue) && !string.IsNullOrEmpty(shading.Val.InnerText))
                    {
                        patternValue = shading.Val.InnerText;
                    }

                    System.Diagnostics.Debug.WriteLine($"[底纹图案] XML: {shading.OuterXml}");
                    System.Diagnostics.Debug.WriteLine($"[底纹图案] 提取值: '{patternValue}', HasValue: {shading.Val.HasValue}");

                    if (!string.IsNullOrEmpty(patternValue) && patternValue != "ShadingPatternValues { }")
                    {
                        pattern = patternValue.ToLower() switch
                        {
                            // 基础图案类型 - 对应WdTextureIndex基础值
                            "clear" => "无图案",                    // wdTextureNone = 0
                            "solid" => "实心",                      // wdTextureSolid = 1000

                            // 百分比图案 - 对应WdTextureIndex百分比值
                            "pct2" => "2.5%",                      // wdTexture2Pt5Percent = 25
                            "pct5" => "5%",                        // wdTexture5Percent = 50
                            "pct7" => "7.5%",                      // wdTexture7Pt5Percent = 75
                            "pct10" => "10%",                      // wdTexture10Percent = 100
                            "pct12" => "12.5%",                    // wdTexture12Pt5Percent = 125
                            "pct15" => "15%",                      // wdTexture15Percent = 150
                            "pct17" => "17.5%",                    // wdTexture17Pt5Percent = 175
                            "pct20" => "20%",                      // wdTexture20Percent = 200
                            "pct22" => "22.5%",                    // wdTexture22Pt5Percent = 225
                            "pct25" => "25%",                      // wdTexture25Percent = 250
                            "pct27" => "27.5%",                    // wdTexture27Pt5Percent = 275
                            "pct30" => "30%",                      // wdTexture30Percent = 300
                            "pct32" => "32.5%",                    // wdTexture32Pt5Percent = 325
                            "pct35" => "35%",                      // wdTexture35Percent = 350
                            "pct37" => "37.5%",                    // wdTexture37Pt5Percent = 375
                            "pct40" => "40%",                      // wdTexture40Percent = 400
                            "pct42" => "42.5%",                    // wdTexture42Pt5Percent = 425
                            "pct45" => "45%",                      // wdTexture45Percent = 450
                            "pct47" => "47.5%",                    // wdTexture47Pt5Percent = 475
                            "pct50" => "50%",                      // wdTexture50Percent = 500
                            "pct52" => "52.5%",                    // wdTexture52Pt5Percent = 525
                            "pct55" => "55%",                      // wdTexture55Percent = 550
                            "pct57" => "57.5%",                    // wdTexture57Pt5Percent = 575
                            "pct60" => "60%",                      // wdTexture60Percent = 600
                            "pct62" => "62.5%",                    // wdTexture62Pt5Percent = 625
                            "pct65" => "65%",                      // wdTexture65Percent = 650
                            "pct67" => "67.5%",                    // wdTexture67Pt5Percent = 675
                            "pct70" => "70%",                      // wdTexture70Percent = 700
                            "pct72" => "72.5%",                    // wdTexture72Pt5Percent = 725
                            "pct75" => "75%",                      // wdTexture75Percent = 750
                            "pct77" => "77.5%",                    // wdTexture77Pt5Percent = 775
                            "pct80" => "80%",                      // wdTexture80Percent = 800
                            "pct82" => "82.5%",                    // wdTexture82Pt5Percent = 825
                            "pct85" => "85%",                      // wdTexture85Percent = 850
                            "pct87" => "87.5%",                    // wdTexture87Pt5Percent = 875
                            "pct90" => "90%",                      // wdTexture90Percent = 900
                            "pct92" => "92.5%",                    // wdTexture92Pt5Percent = 925
                            "pct95" => "95%",                      // wdTexture95Percent = 950
                            "pct97" => "97.5%",                    // wdTexture97Pt5Percent = 975

                            // 特殊图案类型 - 对应WdTextureIndex特殊图案
                            "darkhorizontal" => "深色水平线",        // wdTextureDarkHorizontal = -1
                            "darkvertical" => "深色垂直线",          // wdTextureDarkVertical = -2
                            "darkdiagonaldown" => "深色左斜线",      // wdTextureDarkDiagonalDown = -3
                            "darkdiagonalup" => "深色右斜线",        // wdTextureDarkDiagonalUp = -4
                            "darkcross" => "深色十字线",            // wdTextureDarkCross = -5
                            "darkdiagonalcross" => "深色斜十字线",   // wdTextureDarkDiagonalCross = -6
                            "horizontal" => "水平线",               // wdTextureHorizontal = -7
                            "vertical" => "垂直线",                 // wdTextureVertical = -8
                            "diagonaldown" => "左斜线",             // wdTextureDiagonalDown = -9
                            "diagonalup" => "右斜线",               // wdTextureDiagonalUp = -10
                            "cross" => "十字线",                    // wdTextureCross = -11
                            "diagonalcross" => "斜十字线",          // wdTextureDiagonalCross = -12

                            // 未识别的值保持原有格式
                            _ => $"图案({patternValue})"
                        };
                    }
                }

                return (color, pattern);
            }

            return ("无底纹", "无图案");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取段落底纹失败: {ex.Message}");
            return ("检测失败", "检测失败");
        }
    }

    /// <summary>
    /// 获取文档纸张大小
    /// </summary>
    private static string GetDocumentPaperSize(MainDocumentPart mainPart)
    {
        try
        {
            SectionProperties? sectionProperties = mainPart.Document.Body?.Elements<SectionProperties>().FirstOrDefault();
            PageSize? pageSize = sectionProperties?.Elements<PageSize>().FirstOrDefault();

            if (pageSize != null)
            {
                // 根据宽度和高度判断纸张类型
                uint width = pageSize.Width?.Value ?? 0;
                uint height = pageSize.Height?.Value ?? 0;

                // A4纸张的OpenXML尺寸
                return Math.Abs(width - 11906) < 100 && Math.Abs(height - 16838) < 100
                    ? "A4"
                    // A3纸张的OpenXML尺寸
                    : Math.Abs(width - 16838) < 100 && Math.Abs(height - 23811) < 100 ? "A3" : "自定义";
            }

            return "A4"; // 默认A4
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取文档页边距
    /// </summary>
    private static (float Top, float Bottom, float Left, float Right) GetDocumentMargins(MainDocumentPart mainPart)
    {
        try
        {
            SectionProperties? sectionProperties = mainPart.Document.Body?.Elements<SectionProperties>().FirstOrDefault();
            PageMargin? pageMargin = sectionProperties?.Elements<PageMargin>().FirstOrDefault();

            if (pageMargin != null)
            {
                float top = pageMargin.Top?.Value != null ? int.Parse(pageMargin.Top.Value.ToString()) / 20f : 72f; // 转换为点
                float bottom = pageMargin.Bottom?.Value != null ? int.Parse(pageMargin.Bottom.Value.ToString()) / 20f : 72f;
                float left = pageMargin.Left?.Value != null ? int.Parse(pageMargin.Left.Value.ToString()) / 20f : 72f;
                float right = pageMargin.Right?.Value != null ? int.Parse(pageMargin.Right.Value.ToString()) / 20f : 72f;

                return (top, bottom, left, right);
            }

            return (72f, 72f, 72f, 72f); // 默认1英寸边距
        }
        catch
        {
            return (72f, 72f, 72f, 72f);
        }
    }

    /// <summary>
    /// 获取页眉文字
    /// </summary>
    private static string GetHeaderText(MainDocumentPart mainPart)
    {
        try
        {
            foreach (HeaderPart headerPart in mainPart.HeaderParts)
            {
                string text = headerPart.Header.InnerText;
                if (!string.IsNullOrEmpty(text))
                {
                    return text.Trim();
                }
            }
            return "无页眉文字";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取页眉字体
    /// </summary>
    private static string GetHeaderFont(MainDocumentPart mainPart)
    {
        try
        {
            foreach (HeaderPart headerPart in mainPart.HeaderParts)
            {
                IEnumerable<Run> runs = headerPart.Header.Descendants<Run>();
                foreach (Run run in runs)
                {
                    RunProperties? runProperties = run.RunProperties;
                    RunFonts? runFonts = runProperties?.RunFonts;
                    if (runFonts?.Ascii?.Value != null)
                    {
                        return runFonts.Ascii.Value;
                    }
                }
            }
            return "默认字体";
        }
        catch
        {
            return "未知字体";
        }
    }

    /// <summary>
    /// 获取页眉字号
    /// </summary>
    private static int GetHeaderFontSize(MainDocumentPart mainPart)
    {
        try
        {
            foreach (HeaderPart headerPart in mainPart.HeaderParts)
            {
                IEnumerable<Run> runs = headerPart.Header.Descendants<Run>();
                foreach (Run run in runs)
                {
                    RunProperties? runProperties = run.RunProperties;
                    FontSize? fontSize = runProperties?.FontSize;
                    if (fontSize?.Val?.Value != null)
                    {
                        return int.Parse(fontSize.Val.Value) / 2; // OpenXML中字号是半点值
                    }
                }
            }
            return 12; // 默认字号
        }
        catch
        {
            return 12;
        }
    }

    /// <summary>
    /// 获取段落对齐方式
    /// </summary>
    private static string GetParagraphAlignment(Paragraph paragraph)
    {
        try
        {
            ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
            Justification? justification = paragraphProperties?.Justification;

            if (justification?.Val != null)
            {
                // 方法1：尝试直接获取XML属性值
                string alignmentString = string.Empty;

                // 检查XML中的val属性
                if (justification.OuterXml.Contains("val="))
                {
                    // 使用正则表达式提取val属性值
                    System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(justification.OuterXml, @"val=""([^""]+)""");
                    if (match.Success)
                    {
                        alignmentString = match.Groups[1].Value;
                    }
                }

                // 方法2：尝试枚举值
                if (string.IsNullOrEmpty(alignmentString) && justification.Val.HasValue)
                {
                    alignmentString = justification.Val.Value.ToString();
                }

                // 方法3：尝试InnerText
                if (string.IsNullOrEmpty(alignmentString) && !string.IsNullOrEmpty(justification.Val.InnerText))
                {
                    alignmentString = justification.Val.InnerText;
                }

                System.Diagnostics.Debug.WriteLine($"[对齐检测] XML: {justification.OuterXml}");
                System.Diagnostics.Debug.WriteLine($"[对齐检测] 提取值: '{alignmentString}', HasValue: {justification.Val.HasValue}");

                if (!string.IsNullOrEmpty(alignmentString) && alignmentString != "JustificationValues { }")
                {
                    return alignmentString.ToLower() switch
                    {
                        "left" => "左对齐",
                        "center" => "居中",
                        "right" => "右对齐",
                        "both" => "两端对齐",
                        "distribute" => "分散对齐",
                        "start" => "左对齐",
                        "end" => "右对齐",
                        _ => $"未知对齐({alignmentString})"
                    };
                }
            }

            return "左对齐"; // 默认左对齐
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取段落对齐方式失败: {ex.Message}");
            return "未知对齐";
        }
    }

    /// <summary>
    /// 获取页眉对齐方式
    /// </summary>
    private static string GetHeaderAlignment(MainDocumentPart mainPart)
    {
        try
        {
            foreach (HeaderPart headerPart in mainPart.HeaderParts)
            {
                IEnumerable<Paragraph> paragraphs = headerPart.Header.Descendants<Paragraph>();
                foreach (Paragraph paragraph in paragraphs)
                {
                    ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
                    Justification? justification = paragraphProperties?.Justification;

                    if (justification?.Val?.Value != null)
                    {
                        return justification.Val.Value.ToString() switch
                        {
                            "Left" => "左对齐",
                            "Center" => "居中",
                            "Right" => "右对齐",
                            "Both" => "两端对齐",
                            _ => justification.Val.Value.ToString()
                        };
                    }
                }
            }
            return "左对齐"; // 默认左对齐
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取页脚文字
    /// </summary>
    private static string GetFooterText(MainDocumentPart mainPart)
    {
        try
        {
            foreach (FooterPart footerPart in mainPart.FooterParts)
            {
                string text = footerPart.Footer.InnerText;
                if (!string.IsNullOrEmpty(text))
                {
                    return text.Trim();
                }
            }
            return "无页脚文字";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取页脚字体
    /// </summary>
    private static string GetFooterFont(MainDocumentPart mainPart)
    {
        try
        {
            foreach (FooterPart footerPart in mainPart.FooterParts)
            {
                IEnumerable<Run> runs = footerPart.Footer.Descendants<Run>();
                foreach (Run run in runs)
                {
                    RunProperties? runProperties = run.RunProperties;
                    RunFonts? runFonts = runProperties?.RunFonts;
                    if (runFonts?.Ascii?.Value != null)
                    {
                        return runFonts.Ascii.Value;
                    }
                }
            }
            return "默认字体";
        }
        catch
        {
            return "未知字体";
        }
    }

    /// <summary>
    /// 获取页脚字号
    /// </summary>
    private static int GetFooterFontSize(MainDocumentPart mainPart)
    {
        try
        {
            foreach (FooterPart footerPart in mainPart.FooterParts)
            {
                IEnumerable<Run> runs = footerPart.Footer.Descendants<Run>();
                foreach (Run run in runs)
                {
                    RunProperties? runProperties = run.RunProperties;
                    FontSize? fontSize = runProperties?.FontSize;
                    if (fontSize?.Val?.Value != null)
                    {
                        return int.Parse(fontSize.Val.Value) / 2; // OpenXML中字号是半点值
                    }
                }
            }
            return 12; // 默认字号
        }
        catch
        {
            return 12;
        }
    }

    /// <summary>
    /// 获取页脚对齐方式
    /// </summary>
    private static string GetFooterAlignment(MainDocumentPart mainPart)
    {
        try
        {
            foreach (FooterPart footerPart in mainPart.FooterParts)
            {
                IEnumerable<Paragraph> paragraphs = footerPart.Footer.Descendants<Paragraph>();
                foreach (Paragraph paragraph in paragraphs)
                {
                    ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
                    Justification? justification = paragraphProperties?.Justification;

                    if (justification?.Val?.Value != null)
                    {
                        return justification.Val.Value.ToString() switch
                        {
                            "Left" => "左对齐",
                            "Center" => "居中",
                            "Right" => "右对齐",
                            "Both" => "两端对齐",
                            _ => justification.Val.Value.ToString()
                        };
                    }
                }
            }
            return "左对齐"; // 默认左对齐
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取水印文字
    /// </summary>
    private static string GetWatermarkText(MainDocumentPart mainPart)
    {
        try
        {
            // 1. 检查页眉中的VML水印
            foreach (HeaderPart headerPart in mainPart.HeaderParts)
            {
                // 检查VML形状中的水印文字
                IEnumerable<DocumentFormat.OpenXml.Vml.Shape> shapes = headerPart.Header.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
                foreach (DocumentFormat.OpenXml.Vml.Shape shape in shapes)
                {
                    // 检查形状的文本路径属性（水印通常使用TextPath）
                    DocumentFormat.OpenXml.Vml.TextPath? textPath = shape.Descendants<DocumentFormat.OpenXml.Vml.TextPath>().FirstOrDefault();
                    if (textPath?.String?.Value != null)
                    {
                        return textPath.String.Value;
                    }

                    // 检查形状内的文本内容
                    string? shapeText = shape.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(shapeText))
                    {
                        return shapeText;
                    }

                    // 检查形状的填充文本
                    DocumentFormat.OpenXml.Vml.Fill? fill = shape.GetFirstChild<DocumentFormat.OpenXml.Vml.Fill>();
                    if (fill?.Type?.Value == DocumentFormat.OpenXml.Vml.FillTypeValues.Pattern)
                    {
                        // 可能是图案填充的水印
                        return "图案水印";
                    }
                }

                // 检查页眉中的普通文本（可能是文本水印）
                IEnumerable<Paragraph> paragraphs = headerPart.Header.Descendants<Paragraph>();
                foreach (Paragraph paragraph in paragraphs)
                {
                    IEnumerable<Run> runs = paragraph.Descendants<Run>();
                    foreach (Run run in runs)
                    {
                        RunProperties? runProps = run.RunProperties;
                        // 检查是否有水印样式的文本（通常是半透明或特殊颜色）
                        if (runProps?.Color?.Val?.Value != null)
                        {
                            string colorValue = runProps.Color.Val.Value;
                            // 水印文字通常使用浅色
                            if (colorValue.ToLowerInvariant().Contains("gray") ||
                                colorValue.ToLowerInvariant().Contains("silver") ||
                                colorValue.StartsWith("C0C0C0", StringComparison.OrdinalIgnoreCase))
                            {
                                string? text = run.InnerText?.Trim();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    return text;
                                }
                            }
                        }
                    }
                }
            }

            // 2. 检查文档背景中的水印
            DocumentBackground? documentBackground = mainPart.Document.DocumentBackground;
            if (documentBackground != null)
            {
                DocumentFormat.OpenXml.Vml.Background? background = documentBackground.GetFirstChild<DocumentFormat.OpenXml.Vml.Background>();
                if (background != null)
                {
                    IEnumerable<DocumentFormat.OpenXml.Vml.Shape> backgroundShapes = background.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
                    foreach (DocumentFormat.OpenXml.Vml.Shape shape in backgroundShapes)
                    {
                        DocumentFormat.OpenXml.Vml.TextPath? textPath = shape.Descendants<DocumentFormat.OpenXml.Vml.TextPath>().FirstOrDefault();
                        if (textPath?.String?.Value != null)
                        {
                            return textPath.String.Value;
                        }
                    }
                }
            }

            return "无水印文字";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取水印字体
    /// </summary>
    private static string GetWatermarkFont(MainDocumentPart mainPart)
    {
        try
        {
            // 1. 检查页眉中的水印
            foreach (HeaderPart headerPart in mainPart.HeaderParts)
            {
                string font = ExtractWatermarkFontFromHeader(headerPart);
                if (!string.IsNullOrEmpty(font) && font != "未找到字体")
                {
                    return font;
                }
            }

            // 2. 检查文档背景中的水印
            DocumentBackground? documentBackground = mainPart.Document.DocumentBackground;
            if (documentBackground != null)
            {
                string font = ExtractWatermarkFontFromBackground(documentBackground);
                if (!string.IsNullOrEmpty(font) && font != "未找到字体")
                {
                    return font;
                }
            }

            return "无水印字体";
        }
        catch
        {
            return "字体检测失败";
        }
    }

    /// <summary>
    /// 从页眉中提取水印字体
    /// </summary>
    private static string ExtractWatermarkFontFromHeader(HeaderPart headerPart)
    {
        try
        {
            // 检查VML形状中的字体设置
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> shapes = headerPart.Header.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in shapes)
            {
                string text = shape.InnerText;
                if (!string.IsNullOrEmpty(text))
                {
                    // 检查形状的样式属性中的字体
                    string? style = shape.Style?.Value;
                    if (!string.IsNullOrEmpty(style))
                    {
                        string font = ParseFontFromStyle(style);
                        if (!string.IsNullOrEmpty(font))
                        {
                            return font;
                        }
                    }

                    // 检查TextPath中的字体设置
                    DocumentFormat.OpenXml.Vml.TextPath? textPath = shape.Descendants<DocumentFormat.OpenXml.Vml.TextPath>().FirstOrDefault();
                    if (textPath != null)
                    {
                        OpenXmlAttribute fontFamilyAttr = textPath.GetAttribute("fontfamily", "");
                        if (!string.IsNullOrEmpty(fontFamilyAttr.Value))
                        {
                            return fontFamilyAttr.Value.Trim('"');
                        }
                    }

                    // 检查Run元素中的字体设置
                    IEnumerable<Run> runs = shape.Descendants<Run>();
                    foreach (Run run in runs)
                    {
                        RunProperties? runProperties = run.RunProperties;
                        RunFonts? runFonts = runProperties?.RunFonts;
                        if (runFonts != null)
                        {
                            string? fontName = runFonts.Ascii?.Value ?? runFonts.EastAsia?.Value ?? runFonts.ComplexScript?.Value;
                            if (!string.IsNullOrEmpty(fontName))
                            {
                                return fontName;
                            }
                        }
                    }
                }
            }

            return "未找到字体";
        }
        catch
        {
            return "未找到字体";
        }
    }

    /// <summary>
    /// 获取水印字号
    /// </summary>
    private static int GetWatermarkFontSize(MainDocumentPart mainPart)
    {
        try
        {
            // 1. 检查页眉中的水印字号
            foreach (HeaderPart headerPart in mainPart.HeaderParts)
            {
                int fontSize = ExtractWatermarkFontSizeFromHeader(headerPart);
                if (fontSize > 0)
                {
                    return fontSize;
                }
            }

            // 2. 检查文档背景中的水印字号
            DocumentBackground? documentBackground = mainPart.Document.DocumentBackground;
            if (documentBackground != null)
            {
                int fontSize = ExtractWatermarkFontSizeFromBackground(documentBackground);
                if (fontSize > 0)
                {
                    return fontSize;
                }
            }

            return 0; // 未找到水印字号
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 从页眉中提取水印字号
    /// </summary>
    private static int ExtractWatermarkFontSizeFromHeader(HeaderPart headerPart)
    {
        try
        {
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> shapes = headerPart.Header.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in shapes)
            {
                string text = shape.InnerText;
                if (!string.IsNullOrEmpty(text))
                {
                    // 检查形状的样式属性中的字号
                    string? style = shape.Style?.Value;
                    if (!string.IsNullOrEmpty(style))
                    {
                        int fontSize = ParseFontSizeFromStyle(style);
                        if (fontSize > 0)
                        {
                            return fontSize;
                        }
                    }

                    // 检查TextPath中的字号设置
                    DocumentFormat.OpenXml.Vml.TextPath? textPath = shape.Descendants<DocumentFormat.OpenXml.Vml.TextPath>().FirstOrDefault();
                    if (textPath != null)
                    {
                        OpenXmlAttribute fontSizeAttr = textPath.GetAttribute("fontsize", "");
                        if (!string.IsNullOrEmpty(fontSizeAttr.Value))
                        {
                            if (int.TryParse(fontSizeAttr.Value.Replace("pt", ""), out int size))
                            {
                                return size;
                            }
                        }
                    }

                    // 检查Run元素中的字号设置
                    IEnumerable<Run> runs = shape.Descendants<Run>();
                    foreach (Run run in runs)
                    {
                        RunProperties? runProperties = run.RunProperties;
                        FontSize? fontSize = runProperties?.FontSize;
                        if (fontSize?.Val?.Value != null)
                        {
                            // OpenXML中字号以半点为单位，需要除以2
                            if (int.TryParse(fontSize.Val.Value, out int sizeValue))
                            {
                                return sizeValue / 2;
                            }
                        }
                    }
                }
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取水印方向
    /// </summary>
    private static float GetWatermarkOrientation(MainDocumentPart mainPart)
    {
        try
        {
            foreach (HeaderPart headerPart in mainPart.HeaderParts)
            {
                IEnumerable<DocumentFormat.OpenXml.Vml.Shape> shapes = headerPart.Header.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
                foreach (DocumentFormat.OpenXml.Vml.Shape shape in shapes)
                {
                    string? style = shape.Style?.Value;
                    if (!string.IsNullOrEmpty(style) && style.Contains("rotation"))
                    {
                        // 解析旋转角度
                        System.Text.RegularExpressions.Match rotationMatch = System.Text.RegularExpressions.Regex.Match(style, @"rotation:([^;]+)");
                        if (rotationMatch.Success)
                        {
                            if (float.TryParse(rotationMatch.Groups[1].Value.Trim(), out float angle))
                            {
                                return angle;
                            }
                        }
                    }
                }
            }
            return 315f; // 默认水印角度（-45度）
        }
        catch
        {
            return 315f;
        }
    }

    /// <summary>
    /// 获取表格行数和列数
    /// </summary>
    private static (int Rows, int Columns) GetTableRowsColumns(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<Table> tables = mainPart.Document.Descendants<Table>();
            Table? firstTable = tables.FirstOrDefault();

            if (firstTable != null)
            {
                int rows = firstTable.Elements<TableRow>().Count();
                int columns = 0;

                TableRow? firstRow = firstTable.Elements<TableRow>().FirstOrDefault();
                if (firstRow != null)
                {
                    columns = firstRow.Elements<TableCell>().Count();
                }

                return (rows, columns);
            }

            return (0, 0);
        }
        catch
        {
            return (0, 0);
        }
    }

    /// <summary>
    /// 获取表格底纹
    /// </summary>
    private static string GetTableShading(MainDocumentPart mainPart, string areaType, int areaNumber)
    {
        try
        {
            IEnumerable<Table> tables = mainPart.Document.Descendants<Table>();
            Table? firstTable = tables.FirstOrDefault();

            if (firstTable != null)
            {
                if (areaType.ToLower() == "row" || areaType == "行")
                {
                    List<TableRow> rows = firstTable.Elements<TableRow>().ToList();
                    if (areaNumber > 0 && areaNumber <= rows.Count)
                    {
                        TableRow targetRow = rows[areaNumber - 1];

                        // 检查行级别的底纹设置
                        TableRowProperties? rowProperties = targetRow.TableRowProperties;
                        if (rowProperties != null)
                        {
                            Shading? rowShading = rowProperties.GetFirstChild<Shading>();
                            if (rowShading != null)
                            {
                                string fill = rowShading.Fill?.Value ?? "auto";
                                string pattern = rowShading.Val?.HasValue == true ?
                                    rowShading.Val.Value.ToString() : "clear";
                                return $"行底纹: {fill}, 图案: {pattern}";
                            }
                        }

                        // 检查该行中单元格的底纹设置
                        IEnumerable<TableCell> cells = targetRow.Elements<TableCell>();
                        foreach (TableCell cell in cells)
                        {
                            TableCellProperties? cellProperties = cell.TableCellProperties;
                            Shading? cellShading = cellProperties?.GetFirstChild<Shading>();
                            if (cellShading != null)
                            {
                                string fill = cellShading.Fill?.Value ?? "auto";
                                string pattern = cellShading.Val?.HasValue == true ?
                                    cellShading.Val.Value.ToString() : "clear";
                                return $"单元格底纹: {fill}, 图案: {pattern}";
                            }
                        }
                    }
                }
                else if (areaType.ToLower() == "column" || areaType == "列")
                {
                    // 检查指定列的底纹设置
                    List<TableRow> rows = firstTable.Elements<TableRow>().ToList();
                    foreach (TableRow? row in rows)
                    {
                        List<TableCell> cells = row.Elements<TableCell>().ToList();
                        if (areaNumber > 0 && areaNumber <= cells.Count)
                        {
                            TableCell targetCell = cells[areaNumber - 1];
                            TableCellProperties? cellProperties = targetCell.TableCellProperties;
                            Shading? cellShading = cellProperties?.GetFirstChild<Shading>();
                            if (cellShading != null)
                            {
                                string fill = cellShading.Fill?.Value ?? "auto";
                                string pattern = cellShading.Val?.HasValue == true ?
                                    cellShading.Val.Value.ToString() : "clear";
                                return $"列底纹: {fill}, 图案: {pattern}";
                            }
                        }
                    }
                }
            }

            return "无底纹";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取表格行高
    /// </summary>
    private static float GetTableRowHeight(MainDocumentPart mainPart, int startRow, int endRow)
    {
        try
        {
            IEnumerable<Table> tables = mainPart.Document.Descendants<Table>();
            Table? firstTable = tables.FirstOrDefault();

            if (firstTable != null)
            {
                List<TableRow> rows = firstTable.Elements<TableRow>().ToList();
                if (startRow > 0 && startRow <= rows.Count)
                {
                    TableRow targetRow = rows[startRow - 1];
                    TableRowProperties? rowProperties = targetRow.TableRowProperties;
                    TableRowHeight? tableRowHeight = rowProperties?.GetFirstChild<TableRowHeight>();

                    if (tableRowHeight?.Val?.Value != null)
                    {
                        return int.Parse(tableRowHeight.Val.Value.ToString()) / 20f; // 转换为点
                    }
                }
            }

            return 0f; // 自动行高
        }
        catch
        {
            return 0f;
        }
    }

    /// <summary>
    /// 获取表格列宽
    /// </summary>
    private static float GetTableColumnWidth(MainDocumentPart mainPart, int startColumn, int endColumn)
    {
        try
        {
            IEnumerable<Table> tables = mainPart.Document.Descendants<Table>();
            Table? firstTable = tables.FirstOrDefault();

            if (firstTable != null)
            {
                TableRow? firstRow = firstTable.Elements<TableRow>().FirstOrDefault();
                if (firstRow != null)
                {
                    List<TableCell> cells = firstRow.Elements<TableCell>().ToList();
                    if (startColumn > 0 && startColumn <= cells.Count)
                    {
                        TableCell targetCell = cells[startColumn - 1];
                        TableCellProperties? cellProperties = targetCell.TableCellProperties;
                        TableCellWidth? tableWidth = cellProperties?.TableCellWidth;

                        if (tableWidth?.Width?.Value != null)
                        {
                            return float.Parse(tableWidth.Width.Value) / 20f; // 转换为点
                        }
                    }
                }
            }

            return 0f; // 自动列宽
        }
        catch
        {
            return 0f;
        }
    }

    /// <summary>
    /// 获取表格单元格内容
    /// </summary>
    private static string GetTableCellContent(MainDocumentPart mainPart, int rowNumber, int columnNumber)
    {
        try
        {
            IEnumerable<Table> tables = mainPart.Document.Descendants<Table>();
            Table? firstTable = tables.FirstOrDefault();

            if (firstTable != null)
            {
                List<TableRow> rows = firstTable.Elements<TableRow>().ToList();
                if (rowNumber > 0 && rowNumber <= rows.Count)
                {
                    TableRow targetRow = rows[rowNumber - 1];
                    List<TableCell> cells = targetRow.Elements<TableCell>().ToList();
                    if (columnNumber > 0 && columnNumber <= cells.Count)
                    {
                        TableCell targetCell = cells[columnNumber - 1];
                        return targetCell.InnerText.Trim();
                    }
                }
            }

            return "空单元格";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取表格单元格对齐方式
    /// </summary>
    private static (string Horizontal, string Vertical) GetTableCellAlignment(MainDocumentPart mainPart, int rowNumber, int columnNumber)
    {
        try
        {
            IEnumerable<Table> tables = mainPart.Document.Descendants<Table>();
            Table? firstTable = tables.FirstOrDefault();

            if (firstTable != null)
            {
                List<TableRow> rows = firstTable.Elements<TableRow>().ToList();
                if (rowNumber > 0 && rowNumber <= rows.Count)
                {
                    TableRow targetRow = rows[rowNumber - 1];
                    List<TableCell> cells = targetRow.Elements<TableCell>().ToList();
                    if (columnNumber > 0 && columnNumber <= cells.Count)
                    {
                        TableCell targetCell = cells[columnNumber - 1];
                        TableCellProperties? cellProperties = targetCell.TableCellProperties;

                        // 获取垂直对齐
                        string verticalAlign = cellProperties?.TableCellVerticalAlignment?.Val?.HasValue == true ?
                            cellProperties.TableCellVerticalAlignment.Val.Value.ToString() : "Top";

                        // 获取水平对齐（从段落属性）
                        Paragraph? paragraph = targetCell.Elements<Paragraph>().FirstOrDefault();
                        string horizontalAlign = "Left";
                        if (paragraph?.ParagraphProperties?.Justification?.Val?.Value != null)
                        {
                            horizontalAlign = paragraph.ParagraphProperties.Justification.Val.Value.ToString();
                        }

                        return (horizontalAlign, verticalAlign);
                    }
                }
            }

            return ("Left", "Top");
        }
        catch
        {
            return ("未知", "未知");
        }
    }

    /// <summary>
    /// 获取表格对齐方式
    /// </summary>
    private static (string Alignment, float LeftIndent) GetTableAlignment(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<Table> tables = mainPart.Document.Descendants<Table>();
            Table? firstTable = tables.FirstOrDefault();

            if (firstTable != null)
            {
                TableProperties? tableProperties = firstTable.GetFirstChild<TableProperties>();
                string tableJustification = tableProperties?.TableJustification?.Val?.HasValue == true ?
                    tableProperties.TableJustification.Val.Value.ToString() : "Left";

                TableIndentation? tableIndentation = tableProperties?.TableIndentation;
                float leftIndent = 0f;
                if (tableIndentation?.Width?.Value != null)
                {
                    leftIndent = int.Parse(tableIndentation.Width.Value.ToString()) / 20f; // 转换为点
                }

                return (tableJustification, leftIndent);
            }

            return ("Left", 0f);
        }
        catch
        {
            return ("未知", 0f);
        }
    }

    /// <summary>
    /// 检查合并单元格
    /// </summary>
    private static bool CheckMergedCells(MainDocumentPart mainPart, int startRow, int startColumn, int endRow, int endColumn)
    {
        try
        {
            IEnumerable<Table> tables = mainPart.Document.Descendants<Table>();
            Table? firstTable = tables.FirstOrDefault();

            if (firstTable != null)
            {
                List<TableRow> rows = firstTable.Elements<TableRow>().ToList();

                // 检查指定范围内是否有合并单元格
                for (int row = startRow; row <= endRow && row <= rows.Count; row++)
                {
                    TableRow targetRow = rows[row - 1];
                    List<TableCell> cells = targetRow.Elements<TableCell>().ToList();

                    for (int col = startColumn; col <= endColumn && col <= cells.Count; col++)
                    {
                        TableCell targetCell = cells[col - 1];
                        TableCellProperties? cellProperties = targetCell.TableCellProperties;

                        // 检查垂直合并
                        VerticalMerge? verticalMerge = cellProperties?.VerticalMerge;
                        if (verticalMerge != null)
                        {
                            return true;
                        }

                        // 检查水平合并（通过GridSpan）
                        GridSpan? gridSpan = cellProperties?.GridSpan;
                        if (gridSpan?.Val?.Value > 1)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取项目编号类型
    /// </summary>
    private static string GetBulletNumberingType(MainDocumentPart mainPart, string paragraphNumbers)
    {
        try
        {
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();
            if (paragraphs == null)
            {
                return "无编号";
            }

            // 解析段落号码
            IEnumerable<int> numbers = paragraphNumbers.Split(',', ';').Select(n => int.TryParse(n.Trim(), out int num) ? num : 0).Where(n => n > 0);

            foreach (int paragraphNumber in numbers)
            {
                if (paragraphNumber <= paragraphs.Count)
                {
                    Paragraph paragraph = paragraphs[paragraphNumber - 1];
                    ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
                    NumberingProperties? numberingProperties = paragraphProperties?.NumberingProperties;

                    if (numberingProperties?.NumberingId?.Val?.Value != null)
                    {
                        int numberingId = numberingProperties.NumberingId.Val.Value;
                        int levelIndex = numberingProperties.NumberingLevelReference?.Val?.Value ?? 0;

                        // 检查编号定义部分以确定具体类型
                        if (mainPart.NumberingDefinitionsPart?.Numbering != null)
                        {
                            string numberingType = AnalyzeNumberingType(mainPart.NumberingDefinitionsPart.Numbering, numberingId, levelIndex);
                            if (!string.IsNullOrEmpty(numberingType) && numberingType != "未知编号")
                            {
                                return numberingType;
                            }
                        }

                        return "检测到编号";
                    }
                }
            }

            return "无编号";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取自选图形类型
    /// </summary>
    private static string GetAutoShapeType(MainDocumentPart mainPart)
    {
        try
        {
            // 检查VML图形
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> shapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in shapes)
            {
                string? type = shape.Type?.Value;
                if (!string.IsNullOrEmpty(type))
                {
                    return type;
                }
            }

            // 检查Drawing图形
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                // 检查Drawing中的图形类型
                DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline? inline = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();
                DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor? anchor = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>();

                if (inline?.Graphic?.GraphicData != null)
                {
                    DocumentFormat.OpenXml.Drawing.GraphicData graphicData = inline.Graphic.GraphicData;
                    return graphicData.Uri?.Value == "http://schemas.openxmlformats.org/drawingml/2006/picture"
                        ? "图片"
                        : graphicData.Uri?.Value == "http://schemas.openxmlformats.org/drawingml/2006/chart" ? "图表" : "自选图形";
                }

                if (anchor != null)
                {
                    // 检查Anchor中的图形内容
                    DocumentFormat.OpenXml.Drawing.Graphic? graphic = anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Graphic>();
                    if (graphic?.GraphicData != null)
                    {
                        DocumentFormat.OpenXml.Drawing.GraphicData graphicData = graphic.GraphicData;
                        return graphicData.Uri?.Value == "http://schemas.openxmlformats.org/drawingml/2006/picture"
                            ? "浮动图片"
                            : graphicData.Uri?.Value == "http://schemas.openxmlformats.org/drawingml/2006/chart" ? "浮动图表" : "浮动自选图形";
                    }
                    return "浮动Drawing对象";
                }

                return "Drawing图形";
            }

            return "无自选图形";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取自选图形大小
    /// </summary>
    private static (float Height, float Width) GetAutoShapeSize(MainDocumentPart mainPart)
    {
        try
        {
            // 检查VML图形
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> shapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in shapes)
            {
                string? style = shape.Style?.Value;
                if (!string.IsNullOrEmpty(style))
                {
                    // 解析样式中的宽度和高度
                    System.Text.RegularExpressions.Match widthMatch = System.Text.RegularExpressions.Regex.Match(style, @"width:([^;]+)");
                    System.Text.RegularExpressions.Match heightMatch = System.Text.RegularExpressions.Regex.Match(style, @"height:([^;]+)");

                    if (widthMatch.Success && heightMatch.Success)
                    {
                        try
                        {
                            // 解析宽度和高度值
                            string widthStr = widthMatch.Groups[1].Value.Trim();
                            string heightStr = heightMatch.Groups[1].Value.Trim();

                            // 移除单位并转换为数值
                            float width = ParseSizeValue(widthStr);
                            float height = ParseSizeValue(heightStr);

                            return (height, width);
                        }
                        catch
                        {
                            return (100f, 100f); // 解析失败时的默认值
                        }
                    }
                }
            }

            // 检查Drawing图形
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                // 检查Inline图形的尺寸
                DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline? inline = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();
                if (inline?.Extent != null)
                {
                    // EMU (English Metric Units) 转换为点
                    // 1 EMU = 1/914400 英寸, 1 英寸 = 72 点
                    float width = (inline.Extent.Cx?.Value ?? 0) / 914400f * 72f;
                    float height = (inline.Extent.Cy?.Value ?? 0) / 914400f * 72f;
                    return (height, width);
                }

                // 检查Anchor图形的尺寸
                DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor? anchor = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>();
                if (anchor != null)
                {
                    DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent? extent = anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent>();
                    if (extent != null)
                    {
                        float width = (extent.Cx?.Value ?? 0) / 914400f * 72f;
                        float height = (extent.Cy?.Value ?? 0) / 914400f * 72f;
                        return (height, width);
                    }
                }
            }

            return (0f, 0f);
        }
        catch
        {
            return (0f, 0f);
        }
    }

    /// <summary>
    /// 获取自选图形线条颜色
    /// </summary>
    private static string GetAutoShapeLineColor(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> shapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in shapes)
            {
                string? strokeColor = shape.StrokeColor?.Value;
                if (!string.IsNullOrEmpty(strokeColor))
                {
                    return strokeColor;
                }
            }

            return "默认线条颜色";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取自选图形填充颜色
    /// </summary>
    private static string GetAutoShapeFillColor(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> shapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in shapes)
            {
                string? fillColor = shape.FillColor?.Value;
                if (!string.IsNullOrEmpty(fillColor))
                {
                    return fillColor;
                }
            }

            return "默认填充颜色";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取自选图形文字大小
    /// </summary>
    private static int GetAutoShapeTextSize(MainDocumentPart mainPart)
    {
        try
        {
            // 1. 检查VML自选图形
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> vmlShapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in vmlShapes)
            {
                int fontSize = ExtractTextSizeFromVmlShape(shape);
                if (fontSize > 0)
                {
                    return fontSize;
                }
            }

            // 2. 检查Drawing自选图形
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                int fontSize = ExtractTextSizeFromDrawing(drawing);
                if (fontSize > 0)
                {
                    return fontSize;
                }
            }

            return 0; // 未找到文字大小
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 从VML形状中提取文字大小
    /// </summary>
    private static int ExtractTextSizeFromVmlShape(DocumentFormat.OpenXml.Vml.Shape shape)
    {
        try
        {
            // 检查形状是否包含文本
            string text = shape.InnerText;
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            // 1. 检查Run元素中的字号设置
            IEnumerable<Run> runs = shape.Descendants<Run>();
            foreach (Run run in runs)
            {
                RunProperties? runProperties = run.RunProperties;
                FontSize? fontSize = runProperties?.FontSize;
                if (fontSize?.Val?.Value != null)
                {
                    if (int.TryParse(fontSize.Val.Value, out int size))
                    {
                        return size / 2; // OpenXML中字号以半点为单位
                    }
                }
            }

            // 2. 检查形状样式中的字号
            string? style = shape.Style?.Value;
            if (!string.IsNullOrEmpty(style))
            {
                int fontSize = ParseFontSizeFromStyle(style);
                if (fontSize > 0)
                {
                    return fontSize;
                }
            }

            // 3. 检查TextBox中的字号设置
            DocumentFormat.OpenXml.Vml.TextBox? textBox = shape.GetFirstChild<DocumentFormat.OpenXml.Vml.TextBox>();
            if (textBox != null)
            {
                IEnumerable<Run> textBoxRuns = textBox.Descendants<Run>();
                foreach (Run run in textBoxRuns)
                {
                    RunProperties? runProperties = run.RunProperties;
                    FontSize? fontSize = runProperties?.FontSize;
                    if (fontSize?.Val?.Value != null)
                    {
                        if (int.TryParse(fontSize.Val.Value, out int size))
                        {
                            return size / 2;
                        }
                    }
                }
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取自选图形文字颜色
    /// </summary>
    private static string GetAutoShapeTextColor(MainDocumentPart mainPart)
    {
        try
        {
            // 1. 检查VML自选图形
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> vmlShapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in vmlShapes)
            {
                string color = ExtractTextColorFromVmlShape(shape);
                if (!string.IsNullOrEmpty(color) && color != "未找到颜色")
                {
                    return color;
                }
            }

            // 2. 检查Drawing自选图形
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                string color = ExtractTextColorFromDrawing(drawing);
                if (!string.IsNullOrEmpty(color) && color != "未找到颜色")
                {
                    return color;
                }
            }

            return "无文字颜色";
        }
        catch
        {
            return "颜色检测失败";
        }
    }

    /// <summary>
    /// 从VML形状中提取文字颜色
    /// </summary>
    private static string ExtractTextColorFromVmlShape(DocumentFormat.OpenXml.Vml.Shape shape)
    {
        try
        {
            // 检查形状是否包含文本
            string text = shape.InnerText;
            if (string.IsNullOrEmpty(text))
            {
                return "未找到颜色";
            }

            // 1. 检查Run元素中的颜色设置
            IEnumerable<Run> runs = shape.Descendants<Run>();
            foreach (Run run in runs)
            {
                RunProperties? runProperties = run.RunProperties;
                Color? color = runProperties?.Color;
                if (color?.Val?.Value != null)
                {
                    return NormalizeColorValue(color.Val.Value);
                }
            }

            // 2. 检查形状样式中的颜色
            string? style = shape.Style?.Value;
            if (!string.IsNullOrEmpty(style))
            {
                string color = ParseColorFromStyle(style);
                if (!string.IsNullOrEmpty(color))
                {
                    return color;
                }
            }

            // 3. 检查TextBox中的颜色设置
            DocumentFormat.OpenXml.Vml.TextBox? textBox = shape.GetFirstChild<DocumentFormat.OpenXml.Vml.TextBox>();
            if (textBox != null)
            {
                IEnumerable<Run> textBoxRuns = textBox.Descendants<Run>();
                foreach (Run run in textBoxRuns)
                {
                    RunProperties? runProperties = run.RunProperties;
                    Color? color = runProperties?.Color;
                    if (color?.Val?.Value != null)
                    {
                        return NormalizeColorValue(color.Val.Value);
                    }
                }
            }

            return "未找到颜色";
        }
        catch
        {
            return "未找到颜色";
        }
    }

    /// <summary>
    /// 获取自选图形文字内容
    /// </summary>
    private static string GetAutoShapeTextContent(MainDocumentPart mainPart)
    {
        try
        {
            // 1. 检查VML自选图形
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> vmlShapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in vmlShapes)
            {
                string content = ExtractTextContentFromVmlShape(shape);
                if (!string.IsNullOrEmpty(content) && content != "无文字内容")
                {
                    return content;
                }
            }

            // 2. 检查Drawing自选图形
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                string content = ExtractTextContentFromDrawing(drawing);
                if (!string.IsNullOrEmpty(content) && content != "无文字内容")
                {
                    return content;
                }
            }

            return "无文字内容";
        }
        catch
        {
            return "内容检测失败";
        }
    }

    /// <summary>
    /// 从VML形状中提取文字内容
    /// </summary>
    private static string ExtractTextContentFromVmlShape(DocumentFormat.OpenXml.Vml.Shape shape)
    {
        try
        {
            List<string> textContents = [];

            // 1. 检查形状的直接文本内容
            string directText = shape.InnerText;
            if (!string.IsNullOrEmpty(directText))
            {
                textContents.Add(directText.Trim());
            }

            // 2. 检查TextBox中的文本内容
            DocumentFormat.OpenXml.Vml.TextBox? textBox = shape.GetFirstChild<DocumentFormat.OpenXml.Vml.TextBox>();
            if (textBox != null)
            {
                IEnumerable<Paragraph> paragraphs = textBox.Descendants<Paragraph>();
                foreach (Paragraph paragraph in paragraphs)
                {
                    string paragraphText = paragraph.InnerText;
                    if (!string.IsNullOrEmpty(paragraphText))
                    {
                        textContents.Add(paragraphText.Trim());
                    }
                }
            }

            // 3. 检查Run元素中的文本
            IEnumerable<Run> runs = shape.Descendants<Run>();
            foreach (Run run in runs)
            {
                IEnumerable<Text> texts = run.Elements<Text>();
                foreach (Text text in texts)
                {
                    if (!string.IsNullOrEmpty(text.Text))
                    {
                        textContents.Add(text.Text.Trim());
                    }
                }
            }

            // 合并所有文本内容
            return textContents.Count > 0 ? string.Join(" ", textContents.Distinct().Where(t => !string.IsNullOrEmpty(t))) : "无文字内容";
        }
        catch
        {
            return "无文字内容";
        }
    }

    /// <summary>
    /// 获取自选图形位置
    /// </summary>
    private static (bool HasPosition, string Horizontal, string Vertical) GetAutoShapePosition(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> shapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in shapes)
            {
                string? style = shape.Style?.Value;
                if (!string.IsNullOrEmpty(style))
                {
                    System.Text.RegularExpressions.Match leftMatch = System.Text.RegularExpressions.Regex.Match(style, @"left:([^;]+)");
                    System.Text.RegularExpressions.Match topMatch = System.Text.RegularExpressions.Regex.Match(style, @"top:([^;]+)");

                    if (leftMatch.Success || topMatch.Success)
                    {
                        return (true, leftMatch.Success ? leftMatch.Groups[1].Value : "0",
                                     topMatch.Success ? topMatch.Groups[1].Value : "0");
                    }
                }
            }

            return (false, "", "");
        }
        catch
        {
            return (false, "", "");
        }
    }

    /// <summary>
    /// 获取图片边框复合类型
    /// </summary>
    private static string GetImageBorderCompoundType(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                string compoundType = ExtractImageBorderCompoundType(drawing);
                if (!string.IsNullOrEmpty(compoundType) && compoundType != "无边框")
                {
                    return compoundType;
                }
            }

            return "无边框设置";
        }
        catch
        {
            return "检测失败";
        }
    }

    /// <summary>
    /// 从Drawing中提取图片边框复合类型
    /// </summary>
    private static string ExtractImageBorderCompoundType(Drawing drawing)
    {
        try
        {
            // 检查Inline图片的边框
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline? inline = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();
            if (inline != null)
            {
                DocumentFormat.OpenXml.Drawing.Graphic? graphic = inline.Graphic;
                if (graphic != null)
                {
                    string compoundType = ExtractBorderCompoundFromGraphic(graphic);
                    if (!string.IsNullOrEmpty(compoundType))
                    {
                        return compoundType;
                    }
                }
            }

            // 检查Anchor图片的边框
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor? anchor = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>();
            if (anchor != null)
            {
                DocumentFormat.OpenXml.Drawing.Graphic? graphic = anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Graphic>();
                if (graphic != null)
                {
                    string compoundType = ExtractBorderCompoundFromGraphic(graphic);
                    if (!string.IsNullOrEmpty(compoundType))
                    {
                        return compoundType;
                    }
                }
            }

            return "无边框";
        }
        catch
        {
            return "无边框";
        }
    }

    /// <summary>
    /// 获取图片边框短划线类型
    /// </summary>
    private static string GetImageBorderDashType(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                string dashType = ExtractImageBorderDashType(drawing);
                if (!string.IsNullOrEmpty(dashType) && dashType != "无短划线")
                {
                    return dashType;
                }
            }

            return "无短划线设置";
        }
        catch
        {
            return "检测失败";
        }
    }

    /// <summary>
    /// 从Drawing中提取图片边框短划线类型
    /// </summary>
    private static string ExtractImageBorderDashType(Drawing drawing)
    {
        try
        {
            // 检查Inline图片的边框
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline? inline = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();
            if (inline != null)
            {
                DocumentFormat.OpenXml.Drawing.Graphic? graphic = inline.Graphic;
                if (graphic != null)
                {
                    string dashType = ExtractDashTypeFromGraphic(graphic);
                    if (!string.IsNullOrEmpty(dashType))
                    {
                        return dashType;
                    }
                }
            }

            // 检查Anchor图片的边框
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor? anchor = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>();
            if (anchor != null)
            {
                DocumentFormat.OpenXml.Drawing.Graphic? graphic = anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Graphic>();
                if (graphic != null)
                {
                    string dashType = ExtractDashTypeFromGraphic(graphic);
                    if (!string.IsNullOrEmpty(dashType))
                    {
                        return dashType;
                    }
                }
            }

            return "无短划线";
        }
        catch
        {
            return "无短划线";
        }
    }

    /// <summary>
    /// 获取图片边框线宽
    /// </summary>
    private static float GetImageBorderWidth(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                float width = ExtractImageBorderWidth(drawing);
                if (width > 0)
                {
                    return width;
                }
            }

            return 0f;
        }
        catch
        {
            return 0f;
        }
    }

    /// <summary>
    /// 从Drawing中提取图片边框宽度
    /// </summary>
    private static float ExtractImageBorderWidth(Drawing drawing)
    {
        try
        {
            // 检查Inline图片的边框
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline? inline = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();
            if (inline != null)
            {
                DocumentFormat.OpenXml.Drawing.Graphic? graphic = inline.Graphic;
                if (graphic != null)
                {
                    float width = ExtractBorderWidthFromGraphic(graphic);
                    if (width > 0)
                    {
                        return width;
                    }
                }
            }

            // 检查Anchor图片的边框
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor? anchor = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>();
            if (anchor != null)
            {
                DocumentFormat.OpenXml.Drawing.Graphic? graphic = anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Graphic>();
                if (graphic != null)
                {
                    float width = ExtractBorderWidthFromGraphic(graphic);
                    if (width > 0)
                    {
                        return width;
                    }
                }
            }

            return 0f;
        }
        catch
        {
            return 0f;
        }
    }

    /// <summary>
    /// 获取图片边框颜色
    /// </summary>
    private static string GetImageBorderColor(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                string color = ExtractImageBorderColor(drawing);
                if (!string.IsNullOrEmpty(color) && color != "无边框颜色")
                {
                    return color;
                }
            }

            return "无边框颜色";
        }
        catch
        {
            return "颜色检测失败";
        }
    }

    /// <summary>
    /// 从Drawing中提取图片边框颜色
    /// </summary>
    private static string ExtractImageBorderColor(Drawing drawing)
    {
        try
        {
            // 检查Inline图片的边框
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline? inline = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();
            if (inline != null)
            {
                DocumentFormat.OpenXml.Drawing.Graphic? graphic = inline.Graphic;
                if (graphic != null)
                {
                    string color = ExtractBorderColorFromGraphic(graphic);
                    if (!string.IsNullOrEmpty(color))
                    {
                        return color;
                    }
                }
            }

            // 检查Anchor图片的边框
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor? anchor = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>();
            if (anchor != null)
            {
                DocumentFormat.OpenXml.Drawing.Graphic? graphic = anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Graphic>();
                if (graphic != null)
                {
                    string color = ExtractBorderColorFromGraphic(graphic);
                    if (!string.IsNullOrEmpty(color))
                    {
                        return color;
                    }
                }
            }

            return "无边框颜色";
        }
        catch
        {
            return "无边框颜色";
        }
    }

    /// <summary>
    /// 获取文本框边框颜色
    /// </summary>
    private static string GetTextBoxBorderColor(MainDocumentPart mainPart)
    {
        try
        {
            // 1. 检查VML文本框
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> vmlShapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in vmlShapes)
            {
                // 检查是否为文本框
                DocumentFormat.OpenXml.Vml.TextBox? textBox = shape.GetFirstChild<DocumentFormat.OpenXml.Vml.TextBox>();
                if (textBox != null)
                {
                    string color = ExtractVmlShapeBorderColor(shape);
                    if (!string.IsNullOrEmpty(color) && color != "无边框颜色")
                    {
                        return color;
                    }
                }
            }

            // 2. 检查Drawing中的文本框
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                string color = ExtractDrawingTextBoxBorderColor(drawing);
                if (!string.IsNullOrEmpty(color) && color != "无边框颜色")
                {
                    return color;
                }
            }

            return "无文本框边框";
        }
        catch
        {
            return "边框颜色检测失败";
        }
    }

    /// <summary>
    /// 获取文本框内容
    /// </summary>
    private static string GetTextBoxContent(MainDocumentPart mainPart)
    {
        try
        {
            // 1. 检查VML文本框
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> vmlShapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in vmlShapes)
            {
                // 检查是否为文本框
                DocumentFormat.OpenXml.Vml.TextBox? textBox = shape.GetFirstChild<DocumentFormat.OpenXml.Vml.TextBox>();
                if (textBox != null)
                {
                    string content = ExtractVmlTextBoxContent(shape);
                    if (!string.IsNullOrEmpty(content) && content != "无文本内容")
                    {
                        return content;
                    }
                }
            }

            // 2. 检查Drawing中的文本框
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                string content = ExtractDrawingTextBoxContent(drawing);
                if (!string.IsNullOrEmpty(content) && content != "无文本内容")
                {
                    return content;
                }
            }

            return "无文本框内容";
        }
        catch
        {
            return "内容检测失败";
        }
    }

    /// <summary>
    /// 获取文本框文字大小
    /// </summary>
    private static int GetTextBoxTextSize(MainDocumentPart mainPart)
    {
        try
        {
            // 1. 检查VML文本框
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> vmlShapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in vmlShapes)
            {
                // 检查是否为文本框
                DocumentFormat.OpenXml.Vml.TextBox? textBox = shape.GetFirstChild<DocumentFormat.OpenXml.Vml.TextBox>();
                if (textBox != null)
                {
                    int fontSize = ExtractVmlTextBoxFontSize(shape);
                    if (fontSize > 0)
                    {
                        return fontSize;
                    }
                }
            }

            // 2. 检查Drawing中的文本框
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                int fontSize = ExtractDrawingTextBoxFontSize(drawing);
                if (fontSize > 0)
                {
                    return fontSize;
                }
            }

            return 0; // 未找到文字大小
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取文本框位置
    /// </summary>
    private static (bool HasPosition, string Horizontal, string Vertical) GetTextBoxPosition(MainDocumentPart mainPart)
    {
        try
        {
            // 1. 检查VML文本框位置
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> vmlShapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in vmlShapes)
            {
                // 检查是否为文本框
                DocumentFormat.OpenXml.Vml.TextBox? textBox = shape.GetFirstChild<DocumentFormat.OpenXml.Vml.TextBox>();
                if (textBox != null)
                {
                    (bool hasPos, string horizontal, string vertical) = ExtractVmlTextBoxPosition(shape);
                    if (hasPos)
                    {
                        return (hasPos, horizontal, vertical);
                    }
                }
            }

            // 2. 检查Drawing中的文本框位置
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                (bool hasPos, string horizontal, string vertical) = ExtractDrawingTextBoxPosition(drawing);
                if (hasPos)
                {
                    return (hasPos, horizontal, vertical);
                }
            }

            return (false, "无位置信息", "无位置信息");
        }
        catch
        {
            return (false, "位置检测失败", "位置检测失败");
        }
    }

    /// <summary>
    /// 获取文本框环绕方式
    /// </summary>
    private static string GetTextBoxWrapStyle(MainDocumentPart mainPart)
    {
        try
        {
            // 1. 检查VML文本框的环绕设置
            IEnumerable<DocumentFormat.OpenXml.Vml.Shape> vmlShapes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
            foreach (DocumentFormat.OpenXml.Vml.Shape shape in vmlShapes)
            {
                // 检查是否为文本框
                DocumentFormat.OpenXml.Vml.TextBox? textBox = shape.GetFirstChild<DocumentFormat.OpenXml.Vml.TextBox>();
                if (textBox != null)
                {
                    string wrapStyle = ExtractVmlTextBoxWrapStyle(shape);
                    if (!string.IsNullOrEmpty(wrapStyle) && wrapStyle != "无环绕设置")
                    {
                        return wrapStyle;
                    }
                }
            }

            // 2. 检查Drawing中的文本框环绕
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                string wrapStyle = ExtractDrawingTextBoxWrapStyle(drawing);
                if (!string.IsNullOrEmpty(wrapStyle) && wrapStyle != "无环绕设置")
                {
                    return wrapStyle;
                }
            }

            return "无环绕设置";
        }
        catch
        {
            return "环绕检测失败";
        }
    }

    /// <summary>
    /// 获取查找替换次数
    /// </summary>
    private static int GetFindAndReplaceCount(MainDocumentPart mainPart, string findText, string replaceText)
    {
        try
        {
            string documentText = mainPart.Document.InnerText;

            // 计算替换文本出现的次数
            int replaceCount = 0;
            int index = 0;
            while ((index = documentText.IndexOf(replaceText, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                replaceCount++;
                index += replaceText.Length;
            }

            return replaceCount;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取指定文字的字号
    /// </summary>
    private static int GetSpecificTextFontSize(MainDocumentPart mainPart, string targetText)
    {
        try
        {
            IEnumerable<Run> runs = mainPart.Document.Descendants<Run>();
            foreach (Run run in runs)
            {
                string text = run.InnerText;
                if (!string.IsNullOrEmpty(text) && text.Contains(targetText))
                {
                    RunProperties? runProperties = run.RunProperties;
                    FontSize? fontSize = runProperties?.FontSize;
                    if (fontSize?.Val?.Value != null)
                    {
                        return int.Parse(fontSize.Val.Value) / 2;
                    }
                }
            }

            return 12; // 默认字号
        }
        catch
        {
            return 12;
        }
    }

    /// <summary>
    /// 获取段落间距信息
    /// </summary>
    private static (float Before, float After) GetParagraphSpacing(Paragraph paragraph)
    {
        try
        {
            ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
            SpacingBetweenLines? spacingBetweenLines = paragraphProperties?.SpacingBetweenLines;

            if (spacingBetweenLines != null)
            {
                float before = spacingBetweenLines.Before?.Value != null ? int.Parse(spacingBetweenLines.Before.Value.ToString()) / 20f : 0f;
                float after = spacingBetweenLines.After?.Value != null ? int.Parse(spacingBetweenLines.After.Value.ToString()) / 20f : 0f;

                return (before, after);
            }

            return (0f, 0f);
        }
        catch
        {
            return (0f, 0f);
        }
    }

    /// <summary>
    /// 获取页码信息
    /// </summary>
    private static (string Position, string Format) GetPageNumberInfo(MainDocumentPart mainPart)
    {
        try
        {
            // 检查页眉页脚中的页码
            foreach (HeaderPart headerPart in mainPart.HeaderParts)
            {
                IEnumerable<SimpleField> fields = headerPart.Header.Descendants<SimpleField>();
                foreach (SimpleField field in fields)
                {
                    if (field.Instruction?.Value?.Contains("PAGE") == true)
                    {
                        return ("页眉", "检测到页码");
                    }
                }
            }

            foreach (FooterPart footerPart in mainPart.FooterParts)
            {
                IEnumerable<SimpleField> fields = footerPart.Footer.Descendants<SimpleField>();
                foreach (SimpleField field in fields)
                {
                    if (field.Instruction?.Value?.Contains("PAGE") == true)
                    {
                        return ("页脚", "检测到页码");
                    }
                }
            }

            return ("无页码", "无格式");
        }
        catch
        {
            return ("未知", "未知");
        }
    }

    /// <summary>
    /// 获取页面背景颜色
    /// </summary>
    private static string GetPageBackgroundColor(MainDocumentPart mainPart)
    {
        try
        {
            DocumentBackground? documentBackground = mainPart.Document.DocumentBackground;
            return documentBackground != null ? "检测到页面背景" : "无页面背景";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取页面边框颜色
    /// </summary>
    private static string GetPageBorderColor(MainDocumentPart mainPart)
    {
        try
        {
            SectionProperties? sectionProperties = mainPart.Document.Body?.Elements<SectionProperties>().FirstOrDefault();
            PageBorders? pageBorders = sectionProperties?.Elements<PageBorders>().FirstOrDefault();

            if (pageBorders != null)
            {
                TopBorder? topBorder = pageBorders.TopBorder;
                return topBorder?.Color?.Value != null ? topBorder.Color.Value : "检测到页面边框";
            }

            return "无页面边框";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取表头内容
    /// </summary>
    private static string GetTableHeaderContent(MainDocumentPart mainPart, int columnNumber)
    {
        try
        {
            IEnumerable<Table> tables = mainPart.Document.Descendants<Table>();
            Table? firstTable = tables.FirstOrDefault();

            if (firstTable != null)
            {
                TableRow? firstRow = firstTable.Elements<TableRow>().FirstOrDefault();
                if (firstRow != null)
                {
                    List<TableCell> cells = firstRow.Elements<TableCell>().ToList();
                    if (columnNumber > 0 && columnNumber <= cells.Count)
                    {
                        TableCell headerCell = cells[columnNumber - 1];
                        return headerCell.InnerText.Trim();
                    }
                }
            }

            return "无表头内容";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取表头对齐方式
    /// </summary>
    private static (string Horizontal, string Vertical) GetTableHeaderAlignment(MainDocumentPart mainPart, int columnNumber)
    {
        try
        {
            IEnumerable<Table> tables = mainPart.Document.Descendants<Table>();
            Table? firstTable = tables.FirstOrDefault();

            if (firstTable != null)
            {
                TableRow? firstRow = firstTable.Elements<TableRow>().FirstOrDefault();
                if (firstRow != null)
                {
                    List<TableCell> cells = firstRow.Elements<TableCell>().ToList();
                    if (columnNumber > 0 && columnNumber <= cells.Count)
                    {
                        TableCell headerCell = cells[columnNumber - 1];
                        TableCellProperties? cellProperties = headerCell.TableCellProperties;

                        // 获取垂直对齐
                        string verticalAlign = cellProperties?.TableCellVerticalAlignment?.Val?.HasValue == true ?
                            cellProperties.TableCellVerticalAlignment.Val.Value.ToString() : "Top";

                        // 获取水平对齐（从段落属性）
                        Paragraph? paragraph = headerCell.Elements<Paragraph>().FirstOrDefault();
                        string horizontalAlign = "Left";
                        if (paragraph?.ParagraphProperties?.Justification?.Val?.Value != null)
                        {
                            horizontalAlign = paragraph.ParagraphProperties.Justification.Val.Value.ToString();
                        }

                        return (horizontalAlign, verticalAlign);
                    }
                }
            }

            return ("Left", "Top");
        }
        catch
        {
            return ("未知", "未知");
        }
    }

    /// <summary>
    /// 获取图片阴影信息
    /// </summary>
    private static (string Type, string Color) GetImageShadowInfo(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                (string shadowType, string shadowColor) = ExtractImageShadow(drawing);
                if (shadowType != "无阴影")
                {
                    return (shadowType, shadowColor);
                }
            }

            return ("无阴影", "无颜色");
        }
        catch
        {
            return ("未知", "未知");
        }
    }

    /// <summary>
    /// 获取图片环绕方式
    /// </summary>
    private static string GetImageWrapStyle(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                string wrapStyle = ExtractImageWrapStyle(drawing);
                if (!string.IsNullOrEmpty(wrapStyle) && wrapStyle != "无环绕设置")
                {
                    return wrapStyle;
                }
            }

            return "无环绕设置";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取图片尺寸
    /// </summary>
    private static (float Height, float Width) GetImageSize(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                (float width, float height) = ExtractImageSize(drawing);
                if (width > 0 && height > 0)
                {
                    return (width, height);
                }
            }

            return (0f, 0f);
        }
        catch
        {
            return (0f, 0f);
        }
    }

    /// <summary>
    /// 获取图片位置
    /// </summary>
    private static (bool HasPosition, string Horizontal, string Vertical) GetImagePosition(MainDocumentPart mainPart)
    {
        try
        {
            IEnumerable<Drawing> drawings = mainPart.Document.Descendants<Drawing>();
            foreach (Drawing drawing in drawings)
            {
                // 检查Inline类型图片（嵌入式图片）
                DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline? inline = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();
                if (inline != null)
                {
                    (string horizontal, string vertical) = ParseInlinePosition(inline);
                    return (true, horizontal, vertical);
                }

                // 检查Anchor类型图片（浮动图片）
                DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor? anchor = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>();
                if (anchor != null)
                {
                    (string horizontal, string vertical) = ParseAnchorPosition(anchor);
                    return (true, horizontal, vertical);
                }
            }

            return (false, "", "");
        }
        catch
        {
            return (false, "", "");
        }
    }

    /// <summary>
    /// 解析Anchor类型图片的位置信息
    /// </summary>
    private static (string Horizontal, string Vertical) ParseAnchorPosition(DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor anchor)
    {
        try
        {
            string horizontal = "浮动-未知水平位置";
            string vertical = "浮动-未知垂直位置";

            // 获取水平位置信息
            DocumentFormat.OpenXml.Drawing.Wordprocessing.HorizontalPosition? hPos = anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.HorizontalPosition>();
            if (hPos != null)
            {
                horizontal = ParseHorizontalPosition(hPos);
            }

            // 获取垂直位置信息
            DocumentFormat.OpenXml.Drawing.Wordprocessing.VerticalPosition? vPos = anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.VerticalPosition>();
            if (vPos != null)
            {
                vertical = ParseVerticalPosition(vPos);
            }

            // 检查环绕方式以提供更多上下文
            string wrapType = GetWrapType(anchor);

            // 将环绕信息添加到位置描述中
            horizontal = $"{horizontal}({wrapType})";

            return (horizontal, vertical);
        }
        catch
        {
            return ("浮动-解析失败", "浮动-解析失败");
        }
    }

    /// <summary>
    /// 解析水平位置信息
    /// </summary>
    private static string ParseHorizontalPosition(DocumentFormat.OpenXml.Drawing.Wordprocessing.HorizontalPosition hPos)
    {
        try
        {
            string relativeFrom = hPos.RelativeFrom?.Value.ToString() ?? "未知基准";

            // 检查对齐方式（子元素）
            DocumentFormat.OpenXml.Drawing.Wordprocessing.HorizontalAlignment? hAlign = hPos.HorizontalAlignment;
            if (hAlign?.InnerText != null)
            {
                string align = TranslateAlignment(hAlign.InnerText);
                return $"浮动-{align}对齐(相对于{TranslateRelativeFrom(relativeFrom)})";
            }

            // 检查绝对位置偏移（子元素）
            DocumentFormat.OpenXml.Drawing.Wordprocessing.PositionOffset? posOffset = hPos.PositionOffset;
            if (posOffset?.InnerText != null && long.TryParse(posOffset.InnerText, out long offsetEmu))
            {
                double offsetCm = ConvertEmuToCentimeters(offsetEmu);
                return $"浮动-绝对位置{offsetCm:F1}cm(相对于{TranslateRelativeFrom(relativeFrom)})";
            }

            return $"浮动-相对于{TranslateRelativeFrom(relativeFrom)}";
        }
        catch
        {
            return "浮动-水平位置解析失败";
        }
    }

    /// <summary>
    /// 解析垂直位置信息
    /// </summary>
    private static string ParseVerticalPosition(DocumentFormat.OpenXml.Drawing.Wordprocessing.VerticalPosition vPos)
    {
        try
        {
            string relativeFrom = vPos.RelativeFrom?.Value.ToString() ?? "未知基准";

            // 检查对齐方式（子元素）
            DocumentFormat.OpenXml.Drawing.Wordprocessing.VerticalAlignment? vAlign = vPos.VerticalAlignment;
            if (vAlign?.InnerText != null)
            {
                string align = TranslateAlignment(vAlign.InnerText);
                return $"浮动-{align}对齐(相对于{TranslateRelativeFrom(relativeFrom)})";
            }

            // 检查绝对位置偏移（子元素）
            DocumentFormat.OpenXml.Drawing.Wordprocessing.PositionOffset? posOffset = vPos.PositionOffset;
            if (posOffset?.InnerText != null && long.TryParse(posOffset.InnerText, out long offsetEmu))
            {
                double offsetCm = ConvertEmuToCentimeters(offsetEmu);
                return $"浮动-绝对位置{offsetCm:F1}cm(相对于{TranslateRelativeFrom(relativeFrom)})";
            }

            return $"浮动-相对于{TranslateRelativeFrom(relativeFrom)}";
        }
        catch
        {
            return "浮动-垂直位置解析失败";
        }
    }

    /// <summary>
    /// 解析Inline类型图片的位置信息
    /// </summary>
    private static (string Horizontal, string Vertical) ParseInlinePosition(DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline inline)
    {
        try
        {
            // Inline图片通常跟随文本流，位置相对固定
            string horizontal = "嵌入式-跟随文本流";
            string vertical = "嵌入式-基线对齐";

            // 检查是否有特殊的位置设置
            // Inline图片可能有距离边距的设置
            if (inline.DistanceFromTop?.Value != null || inline.DistanceFromBottom?.Value != null ||
                inline.DistanceFromLeft?.Value != null || inline.DistanceFromRight?.Value != null)
            {
                List<string> margins = [];

                if (inline.DistanceFromTop?.Value != null)
                {
                    double topCm = ConvertEmuToCentimeters(inline.DistanceFromTop.Value);
                    margins.Add($"上边距{topCm:F1}cm");
                }

                if (inline.DistanceFromBottom?.Value != null)
                {
                    double bottomCm = ConvertEmuToCentimeters(inline.DistanceFromBottom.Value);
                    margins.Add($"下边距{bottomCm:F1}cm");
                }

                if (inline.DistanceFromLeft?.Value != null)
                {
                    double leftCm = ConvertEmuToCentimeters(inline.DistanceFromLeft.Value);
                    margins.Add($"左边距{leftCm:F1}cm");
                }

                if (inline.DistanceFromRight?.Value != null)
                {
                    double rightCm = ConvertEmuToCentimeters(inline.DistanceFromRight.Value);
                    margins.Add($"右边距{rightCm:F1}cm");
                }

                if (margins.Count > 0)
                {
                    horizontal = $"嵌入式-{string.Join(",", margins)}";
                    vertical = "嵌入式-有边距设置";
                }
            }

            return (horizontal, vertical);
        }
        catch
        {
            return ("嵌入式-解析失败", "嵌入式-解析失败");
        }
    }

    /// <summary>
    /// 获取环绕类型
    /// </summary>
    private static string GetWrapType(DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor anchor)
    {
        try
        {
            if (anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapNone>() != null)
            {
                return "无环绕";
            }

            if (anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapSquare>() != null)
            {
                return "四周型环绕";
            }

            if (anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTight>() != null)
            {
                return "紧密型环绕";
            }

            return anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapThrough>() != null ? "穿越型环绕" : "未知环绕";
        }
        catch
        {
            return "环绕解析失败";
        }
    }

    /// <summary>
    /// 翻译对齐方式
    /// </summary>
    private static string TranslateAlignment(string alignment)
    {
        return alignment.ToLower() switch
        {
            "left" => "左",
            "center" => "居中",
            "right" => "右",
            "top" => "顶部",
            "middle" => "中间",
            "bottom" => "底部",
            "inside" => "内侧",
            "outside" => "外侧",
            _ => alignment
        };
    }

    /// <summary>
    /// 翻译相对位置基准
    /// </summary>
    private static string TranslateRelativeFrom(string relativeFrom)
    {
        return relativeFrom.ToLower() switch
        {
            "page" => "页面",
            "margin" => "页边距",
            "column" => "列",
            "character" => "字符",
            "line" => "行",
            "paragraph" => "段落",
            "leftmargin" => "左边距",
            "rightmargin" => "右边距",
            "topmargin" => "上边距",
            "bottommargin" => "下边距",
            "insidemargin" => "内边距",
            "outsidemargin" => "外边距",
            _ => relativeFrom
        };
    }

    /// <summary>
    /// 将EMU单位转换为厘米
    /// </summary>
    private static double ConvertEmuToCentimeters(long emu)
    {
        // 1 EMU = 1/914400 英寸
        // 1 英寸 = 2.54 厘米
        return emu / 914400.0 * 2.54;
    }

    /// <summary>
    /// 获取页面边框样式
    /// </summary>
    private static string GetPageBorderStyle(MainDocumentPart mainPart)
    {
        try
        {
            SectionProperties? sectionProperties = mainPart.Document.Body?.Elements<SectionProperties>().FirstOrDefault();
            PageBorders? pageBorders = sectionProperties?.Elements<PageBorders>().FirstOrDefault();

            if (pageBorders != null)
            {
                TopBorder? topBorder = pageBorders.TopBorder;
                return topBorder?.Val?.Value != null ? topBorder.Val.Value.ToString() : "检测到页面边框样式";
            }

            return "无页面边框样式";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取页面边框宽度
    /// </summary>
    private static float GetPageBorderWidth(MainDocumentPart mainPart)
    {
        try
        {
            SectionProperties? sectionProperties = mainPart.Document.Body?.Elements<SectionProperties>().FirstOrDefault();
            PageBorders? pageBorders = sectionProperties?.Elements<PageBorders>().FirstOrDefault();

            if (pageBorders != null)
            {
                TopBorder? topBorder = pageBorders.TopBorder;
                if (topBorder?.Size?.Value != null)
                {
                    return uint.Parse(topBorder.Size.Value.ToString()) / 8f; // OpenXML中边框宽度是8分之一点
                }
                return 1.0f; // 默认边框宽度
            }

            return 0f;
        }
        catch
        {
            return 0f;
        }
    }

    /// <summary>
    /// 文本相等比较（忽略大小写和空白）
    /// </summary>
    private static bool TextEquals(string actual, string expected)
    {
        return (string.IsNullOrWhiteSpace(actual) && string.IsNullOrWhiteSpace(expected)) || (!string.IsNullOrWhiteSpace(actual) && !string.IsNullOrWhiteSpace(expected) && string.Equals(actual.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 智能颜色相等比较
    /// 支持多种颜色格式：RGB、十六进制、颜色名称等
    /// 支持颜色相似度比较和常见颜色别名
    /// </summary>
    private static bool ColorEquals(string actual, string expected)
    {
        if (string.IsNullOrWhiteSpace(actual) && string.IsNullOrWhiteSpace(expected))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        // 标准化颜色值
        string normalizedActual = NormalizeColor(actual.Trim());
        string normalizedExpected = NormalizeColor(expected.Trim());

        // 1. 直接字符串比较（大小写不敏感）
        if (string.Equals(normalizedActual, normalizedExpected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 2. 尝试解析为RGB值进行相似度比较
        if (TryParseColor(normalizedActual, out RgbColor actualRgb) &&
            TryParseColor(normalizedExpected, out RgbColor expectedRgb))
        {
            return AreColorsSimilar(actualRgb, expectedRgb);
        }

        // 3. 检查颜色别名
        return AreColorAliases(normalizedActual, normalizedExpected);
    }

    /// <summary>
    /// 标准化颜色值，移除空格和特殊字符
    /// </summary>
    private static string NormalizeColor(string color)
    {
        return string.IsNullOrWhiteSpace(color) ? string.Empty : color.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
    }

    /// <summary>
    /// RGB颜色结构
    /// </summary>
    private struct RgbColor
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public RgbColor(int r, int g, int b)
        {
            R = Math.Max(0, Math.Min(255, r));
            G = Math.Max(0, Math.Min(255, g));
            B = Math.Max(0, Math.Min(255, b));
        }
    }

    /// <summary>
    /// 尝试解析颜色字符串为RGB值
    /// 支持格式：#RRGGBB、#RGB、rgb(r,g,b)、颜色名称
    /// </summary>
    private static bool TryParseColor(string colorString, out RgbColor rgb)
    {
        rgb = new RgbColor();

        if (string.IsNullOrWhiteSpace(colorString))
        {
            return false;
        }

        string color = colorString.ToLowerInvariant();

        // 1. 十六进制格式 #RRGGBB 或 #RGB
        if (color.StartsWith("#"))
        {
            return TryParseHexColor(color, out rgb);
        }

        // 2. RGB格式 rgb(r,g,b)
        if (color.StartsWith("rgb(") && color.EndsWith(")"))
        {
            return TryParseRgbFormat(color, out rgb);
        }

        // 3. 颜色名称
        return TryParseColorName(color, out rgb);
    }

    /// <summary>
    /// 解析十六进制颜色格式
    /// </summary>
    private static bool TryParseHexColor(string hexColor, out RgbColor rgb)
    {
        rgb = new RgbColor();

        try
        {
            string hex = hexColor[1..]; // 移除 #

            if (hex.Length == 3)
            {
                // #RGB 格式，扩展为 #RRGGBB
                hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
            }

            if (hex.Length == 6)
            {
                int r = Convert.ToInt32(hex[..2], 16);
                int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                rgb = new RgbColor(r, g, b);
                return true;
            }
        }
        catch
        {
            // 解析失败
        }

        return false;
    }

    /// <summary>
    /// 解析RGB格式 rgb(r,g,b)
    /// </summary>
    private static bool TryParseRgbFormat(string rgbString, out RgbColor rgb)
    {
        rgb = new RgbColor();

        try
        {
            string content = rgbString[4..^1]; // 移除 "rgb(" 和 ")"
            string[] parts = content.Split(',');

            if (parts.Length == 3)
            {
                int r = int.Parse(parts[0].Trim());
                int g = int.Parse(parts[1].Trim());
                int b = int.Parse(parts[2].Trim());
                rgb = new RgbColor(r, g, b);
                return true;
            }
        }
        catch
        {
            // 解析失败
        }

        return false;
    }

    /// <summary>
    /// 解析颜色名称为RGB值
    /// </summary>
    private static bool TryParseColorName(string colorName, out RgbColor rgb)
    {
        rgb = new RgbColor();

        Dictionary<string, RgbColor> colorMap = GetColorNameMap();
        if (colorMap.TryGetValue(colorName, out RgbColor rgbValue))
        {
            rgb = rgbValue;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取颜色名称映射表
    /// </summary>
    private static Dictionary<string, RgbColor> GetColorNameMap()
    {
        return new Dictionary<string, RgbColor>(StringComparer.OrdinalIgnoreCase)
        {
            // 中文颜色名称
            { "红色", new RgbColor(255, 0, 0) },
            { "绿色", new RgbColor(0, 128, 0) },
            { "蓝色", new RgbColor(0, 0, 255) },
            { "黄色", new RgbColor(255, 255, 0) },
            { "黑色", new RgbColor(0, 0, 0) },
            { "白色", new RgbColor(255, 255, 255) },
            { "灰色", new RgbColor(128, 128, 128) },
            { "橙色", new RgbColor(255, 165, 0) },
            { "紫色", new RgbColor(128, 0, 128) },
            { "粉色", new RgbColor(255, 192, 203) },
            { "棕色", new RgbColor(165, 42, 42) },
            { "青色", new RgbColor(0, 255, 255) },

            // 英文颜色名称
            { "red", new RgbColor(255, 0, 0) },
            { "green", new RgbColor(0, 128, 0) },
            { "blue", new RgbColor(0, 0, 255) },
            { "yellow", new RgbColor(255, 255, 0) },
            { "black", new RgbColor(0, 0, 0) },
            { "white", new RgbColor(255, 255, 255) },
            { "gray", new RgbColor(128, 128, 128) },
            { "grey", new RgbColor(128, 128, 128) },
            { "orange", new RgbColor(255, 165, 0) },
            { "purple", new RgbColor(128, 0, 128) },
            { "pink", new RgbColor(255, 192, 203) },
            { "brown", new RgbColor(165, 42, 42) },
            { "cyan", new RgbColor(0, 255, 255) },
            { "magenta", new RgbColor(255, 0, 255) },
            { "lime", new RgbColor(0, 255, 0) },
            { "navy", new RgbColor(0, 0, 128) },
            { "maroon", new RgbColor(128, 0, 0) },
            { "olive", new RgbColor(128, 128, 0) },
            { "teal", new RgbColor(0, 128, 128) },
            { "silver", new RgbColor(192, 192, 192) },

            // 常见的深浅变体
            { "darkred", new RgbColor(139, 0, 0) },
            { "darkgreen", new RgbColor(0, 100, 0) },
            { "darkblue", new RgbColor(0, 0, 139) },
            { "lightred", new RgbColor(255, 102, 102) },
            { "lightgreen", new RgbColor(144, 238, 144) },
            { "lightblue", new RgbColor(173, 216, 230) },
            { "lightgray", new RgbColor(211, 211, 211) },
            { "darkgray", new RgbColor(169, 169, 169) }
        };
    }

    /// <summary>
    /// 比较两个RGB颜色是否相似
    /// 使用欧几里得距离计算颜色差异，允许一定的容差
    /// </summary>
    private static bool AreColorsSimilar(RgbColor color1, RgbColor color2, double tolerance = 30.0)
    {
        // 计算RGB空间中的欧几里得距离
        double distance = Math.Sqrt(
            Math.Pow(color1.R - color2.R, 2) +
            Math.Pow(color1.G - color2.G, 2) +
            Math.Pow(color1.B - color2.B, 2)
        );

        return distance <= tolerance;
    }

    /// <summary>
    /// 检查两个颜色字符串是否为已知的别名
    /// </summary>
    private static bool AreColorAliases(string color1, string color2)
    {
        List<List<string>> aliases = GetColorAliases();

        foreach (List<string> aliasGroup in aliases)
        {
            if (aliasGroup.Contains(color1, StringComparer.OrdinalIgnoreCase) &&
                aliasGroup.Contains(color2, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取颜色别名组
    /// </summary>
    private static List<List<string>> GetColorAliases()
    {
        return
        [
            // 红色别名组
            ["红色", "red", "#ff0000", "#f00", "rgb(255,0,0)"],

            // 绿色别名组
            ["绿色", "green", "#008000", "#080", "rgb(0,128,0)"],

            // 蓝色别名组
            ["蓝色", "blue", "#0000ff", "#00f", "rgb(0,0,255)"],

            // 黄色别名组
            ["黄色", "yellow", "#ffff00", "#ff0", "rgb(255,255,0)"],

            // 黑色别名组
            ["黑色", "black", "#000000", "#000", "rgb(0,0,0)"],

            // 白色别名组
            ["白色", "white", "#ffffff", "#fff", "rgb(255,255,255)"],

            // 灰色别名组
            ["灰色", "gray", "grey", "#808080", "rgb(128,128,128)"],

            // 橙色别名组
            ["橙色", "orange", "#ffa500", "rgb(255,165,0)"],

            // 紫色别名组
            ["紫色", "purple", "#800080", "rgb(128,0,128)"],

            // 粉色别名组
            ["粉色", "pink", "#ffc0cb", "rgb(255,192,203)"],

            // 青色别名组
            ["青色", "cyan", "#00ffff", "#0ff", "rgb(0,255,255)"]
        ];
    }

    /// <summary>
    /// 解析尺寸值，支持多种单位
    /// </summary>
    private static float ParseSizeValue(string sizeStr)
    {
        if (string.IsNullOrWhiteSpace(sizeStr))
        {
            return 0f;
        }

        // 移除空格
        sizeStr = sizeStr.Trim();

        // 提取数值部分
        System.Text.RegularExpressions.Match numberMatch = System.Text.RegularExpressions.Regex.Match(sizeStr, @"^([\d.]+)");
        if (!numberMatch.Success)
        {
            return 0f;
        }

        if (!float.TryParse(numberMatch.Groups[1].Value, out float value))
        {
            return 0f;
        }

        // 检查单位并转换为点（pt）
        if (sizeStr.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
        {
            return value; // 点，直接返回
        }
        else if (sizeStr.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            return value * 0.75f; // 像素转点（1px = 0.75pt）
        }
        else if (sizeStr.EndsWith("in", StringComparison.OrdinalIgnoreCase))
        {
            return value * 72f; // 英寸转点（1in = 72pt）
        }
        else if (sizeStr.EndsWith("cm", StringComparison.OrdinalIgnoreCase))
        {
            return value * 28.35f; // 厘米转点（1cm ≈ 28.35pt）
        }
        else if (sizeStr.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
        {
            return value * 2.835f; // 毫米转点（1mm ≈ 2.835pt）
        }
        else
        {
            return value; // 默认当作点处理
        }
    }

    /// <summary>
    /// 调试方法：输出文档中所有段落的格式信息
    /// </summary>
    private static void DebugParagraphFormats(WordprocessingDocument document)
    {
        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            List<Paragraph>? paragraphs = mainPart.Document.Body?.Elements<Paragraph>().ToList();

            if (paragraphs == null || paragraphs.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[调试] 文档中没有段落");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[调试] 文档共有 {paragraphs.Count} 个段落");

            for (int i = 0; i < paragraphs.Count; i++)
            {
                Paragraph paragraph = paragraphs[i];
                string text = paragraph.InnerText.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                System.Diagnostics.Debug.WriteLine($"\n=== 段落 {i + 1} ===");
                System.Diagnostics.Debug.WriteLine($"文本内容: {text[..Math.Min(50, text.Length)]}...");
                System.Diagnostics.Debug.WriteLine($"字体: {GetParagraphFont(paragraph)}");
                System.Diagnostics.Debug.WriteLine($"字号: {GetParagraphFontSize(paragraph)}");
                System.Diagnostics.Debug.WriteLine($"字形: {GetParagraphFontStyle(paragraph)}");
                System.Diagnostics.Debug.WriteLine($"文字颜色: {GetParagraphTextColor(paragraph)}");
                System.Diagnostics.Debug.WriteLine($"对齐方式: {GetParagraphAlignment(paragraph)}");

                (int FirstLine, int Left, int Right) = GetParagraphIndentation(paragraph);
                System.Diagnostics.Debug.WriteLine($"缩进: 首行{FirstLine}, 左{Left}, 右{Right}");

                System.Diagnostics.Debug.WriteLine($"行间距: {GetParagraphLineSpacing(paragraph)}");
                System.Diagnostics.Debug.WriteLine($"首字下沉: {GetParagraphDropCap(paragraph)}");

                (float Before, float After) = GetParagraphSpacing(paragraph);
                System.Diagnostics.Debug.WriteLine($"段落间距: 前{Before}, 后{After}");

                System.Diagnostics.Debug.WriteLine($"边框颜色: {GetParagraphBorderColor(paragraph)}");
                System.Diagnostics.Debug.WriteLine($"边框样式: {GetParagraphBorderStyle(paragraph)}");
                System.Diagnostics.Debug.WriteLine($"边框宽度: {GetParagraphBorderWidth(paragraph)}");

                (string Color, string Pattern) shading = GetParagraphShading(paragraph);
                System.Diagnostics.Debug.WriteLine($"底纹: 颜色{shading.Color}, 图案{shading.Pattern}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[调试] 输出段落格式信息失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从文档背景中提取水印字体
    /// </summary>
    private static string ExtractWatermarkFontFromBackground(DocumentBackground documentBackground)
    {
        try
        {
            DocumentFormat.OpenXml.Vml.Background? background = documentBackground.GetFirstChild<DocumentFormat.OpenXml.Vml.Background>();
            if (background != null)
            {
                IEnumerable<DocumentFormat.OpenXml.Vml.Shape> backgroundShapes = background.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
                foreach (DocumentFormat.OpenXml.Vml.Shape shape in backgroundShapes)
                {
                    DocumentFormat.OpenXml.Vml.TextPath? textPath = shape.Descendants<DocumentFormat.OpenXml.Vml.TextPath>().FirstOrDefault();
                    if (textPath != null)
                    {
                        OpenXmlAttribute fontFamilyAttr = textPath.GetAttribute("fontfamily", "");
                        if (!string.IsNullOrEmpty(fontFamilyAttr.Value))
                        {
                            return fontFamilyAttr.Value.Trim('"');
                        }
                    }
                }
            }
            return "未找到字体";
        }
        catch
        {
            return "未找到字体";
        }
    }

    /// <summary>
    /// 从样式字符串中解析字体
    /// </summary>
    private static string ParseFontFromStyle(string style)
    {
        try
        {
            // 解析样式字符串中的font-family属性
            System.Text.RegularExpressions.Match fontMatch = System.Text.RegularExpressions.Regex.Match(style, @"font-family:\s*([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return fontMatch.Success ? fontMatch.Groups[1].Value.Trim().Trim('"').Trim('\'') : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 从文档背景中提取水印字号
    /// </summary>
    private static int ExtractWatermarkFontSizeFromBackground(DocumentBackground documentBackground)
    {
        try
        {
            DocumentFormat.OpenXml.Vml.Background? background = documentBackground.GetFirstChild<DocumentFormat.OpenXml.Vml.Background>();
            if (background != null)
            {
                IEnumerable<DocumentFormat.OpenXml.Vml.Shape> backgroundShapes = background.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
                foreach (DocumentFormat.OpenXml.Vml.Shape shape in backgroundShapes)
                {
                    DocumentFormat.OpenXml.Vml.TextPath? textPath = shape.Descendants<DocumentFormat.OpenXml.Vml.TextPath>().FirstOrDefault();
                    if (textPath != null)
                    {
                        OpenXmlAttribute fontSizeAttr = textPath.GetAttribute("fontsize", "");
                        if (!string.IsNullOrEmpty(fontSizeAttr.Value))
                        {
                            if (int.TryParse(fontSizeAttr.Value.Replace("pt", ""), out int size))
                            {
                                return size;
                            }
                        }
                    }
                }
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 从样式字符串中解析字号
    /// </summary>
    private static int ParseFontSizeFromStyle(string style)
    {
        try
        {
            // 解析样式字符串中的font-size属性
            System.Text.RegularExpressions.Match sizeMatch = System.Text.RegularExpressions.Regex.Match(style, @"font-size:\s*([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (sizeMatch.Success)
            {
                string sizeStr = sizeMatch.Groups[1].Value.Trim();
                if (int.TryParse(sizeStr.Replace("pt", "").Replace("px", ""), out int size))
                {
                    return size;
                }
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 分析编号类型
    /// </summary>
    private static string AnalyzeNumberingType(Numbering numbering, int numberingId, int levelIndex)
    {
        try
        {
            // 查找编号实例
            NumberingInstance? numberingInstance = numbering.Elements<NumberingInstance>()
                .FirstOrDefault(ni => ni.NumberID?.Value == numberingId);

            if (numberingInstance == null)
            {
                return "未知编号";
            }

            // 获取抽象编号ID
            int? abstractNumId = numberingInstance.AbstractNumId?.Val?.Value;
            if (abstractNumId == null)
            {
                return "未知编号";
            }

            // 查找抽象编号定义
            AbstractNum? abstractNum = numbering.Elements<AbstractNum>()
                .FirstOrDefault(an => an.AbstractNumberId?.Value == abstractNumId);

            if (abstractNum == null)
            {
                return "未知编号";
            }

            // 查找指定级别的编号定义
            Level? level = abstractNum.Elements<Level>()
                .FirstOrDefault(l => l.LevelIndex?.Value == levelIndex);

            if (level == null)
            {
                return "未知编号";
            }

            // 分析编号格式
            NumberingFormat? numberingFormat = level.NumberingFormat;
            if (numberingFormat?.Val?.Value != null)
            {
                return TranslateNumberingFormat(numberingFormat.Val.Value.ToString());
            }

            // 检查级别文本以确定是否为项目符号
            LevelText? levelText = level.LevelText;
            if (levelText?.Val?.Value != null)
            {
                string text = levelText.Val.Value;
                if (IsSymbolBullet(text))
                {
                    return $"项目符号({text})";
                }
            }

            return "编号列表";
        }
        catch
        {
            return "未知编号";
        }
    }

    /// <summary>
    /// 翻译编号格式
    /// </summary>
    private static string TranslateNumberingFormat(string format)
    {
        return format.ToLower() switch
        {
            "decimal" => "阿拉伯数字编号",
            "upperroman" => "大写罗马数字编号",
            "lowerroman" => "小写罗马数字编号",
            "upperletter" => "大写字母编号",
            "lowerletter" => "小写字母编号",
            "ordinal" => "序数编号",
            "cardinaltext" => "基数文本编号",
            "ordinaltext" => "序数文本编号",
            "hex" => "十六进制编号",
            "chicago" => "芝加哥编号",
            "ideographdigital" => "中文数字编号",
            "japanesecounting" => "日文计数编号",
            "aiueo" => "日文假名编号",
            "iroha" => "日文伊吕波编号",
            "decimalfullwidth" => "全角阿拉伯数字编号",
            "bullet" => "项目符号",
            _ => $"其他编号格式({format})"
        };
    }

    /// <summary>
    /// 判断是否为符号项目符号
    /// </summary>
    private static bool IsSymbolBullet(string text)
    {
        // 常见的项目符号字符
        char[] bulletChars = ['•', '◦', '▪', '▫', '■', '□', '●', '○', '★', '☆', '♦', '♢', '→', '⇒'];
        return text.Length == 1 && bulletChars.Contains(text[0]);
    }

    /// <summary>
    /// 从Drawing中提取文字大小
    /// </summary>
    private static int ExtractTextSizeFromDrawing(Drawing drawing)
    {
        try
        {
            // 检查Drawing中的文本运行
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text textElement in textElements)
            {
                // 检查文本运行的属性
                DocumentFormat.OpenXml.Drawing.RunProperties? runProperties = textElement.Parent?.Elements<DocumentFormat.OpenXml.Drawing.RunProperties>().FirstOrDefault();
                if (runProperties?.FontSize?.Value != null)
                {
                    // Drawing中的字号以百分点为单位，需要除以100
                    return runProperties.FontSize.Value / 100;
                }
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 从Drawing中提取文字颜色
    /// </summary>
    private static string ExtractTextColorFromDrawing(Drawing drawing)
    {
        try
        {
            // 检查Drawing中的文本运行
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text textElement in textElements)
            {
                // 检查文本运行的属性
                DocumentFormat.OpenXml.Drawing.RunProperties? runProperties = textElement.Parent?.Elements<DocumentFormat.OpenXml.Drawing.RunProperties>().FirstOrDefault();

                // 检查实体颜色
                DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = runProperties?.Elements<DocumentFormat.OpenXml.Drawing.SolidFill>().FirstOrDefault();
                if (solidFill != null)
                {
                    // 检查RGB颜色
                    DocumentFormat.OpenXml.Drawing.RgbColorModelHex? rgbColor = solidFill.RgbColorModelHex;
                    if (rgbColor?.Val?.Value != null)
                    {
                        return $"#{rgbColor.Val.Value}";
                    }

                    // 检查系统颜色
                    DocumentFormat.OpenXml.Drawing.SystemColor? systemColor = solidFill.SystemColor;
                    if (systemColor?.Val?.Value != null)
                    {
                        return systemColor.Val.Value.ToString();
                    }
                }
            }

            return "未找到颜色";
        }
        catch
        {
            return "未找到颜色";
        }
    }

    /// <summary>
    /// 标准化颜色值
    /// </summary>
    private static string NormalizeColorValue(string colorValue)
    {
        if (string.IsNullOrEmpty(colorValue))
        {
            return "未知颜色";
        }

        // 如果是十六进制颜色值，添加#前缀
        return colorValue.Length == 6 && !colorValue.StartsWith("#") ? $"#{colorValue}" : colorValue;
    }

    /// <summary>
    /// 从样式字符串中解析颜色
    /// </summary>
    private static string ParseColorFromStyle(string style)
    {
        try
        {
            // 解析样式字符串中的color属性
            System.Text.RegularExpressions.Match colorMatch = System.Text.RegularExpressions.Regex.Match(style, @"color:\s*([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return colorMatch.Success ? NormalizeColorValue(colorMatch.Groups[1].Value.Trim()) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 从Drawing中提取文字内容
    /// </summary>
    private static string ExtractTextContentFromDrawing(Drawing drawing)
    {
        try
        {
            List<string> textContents = [];

            // 检查Drawing中的文本元素
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text textElement in textElements)
            {
                if (!string.IsNullOrEmpty(textElement.Text))
                {
                    textContents.Add(textElement.Text.Trim());
                }
            }

            // 合并所有文本内容
            return textContents.Count > 0 ? string.Join(" ", textContents.Distinct().Where(t => !string.IsNullOrEmpty(t))) : "无文字内容";
        }
        catch
        {
            return "无文字内容";
        }
    }

    /// <summary>
    /// 从Graphic中提取边框复合类型
    /// </summary>
    private static string ExtractBorderCompoundFromGraphic(DocumentFormat.OpenXml.Drawing.Graphic graphic)
    {
        try
        {
            // 检查图形数据中的形状属性
            DocumentFormat.OpenXml.Drawing.GraphicData? graphicData = graphic.GraphicData;
            if (graphicData != null)
            {
                // 检查图片元素
                DocumentFormat.OpenXml.Drawing.Pictures.Picture? picture = graphicData.GetFirstChild<DocumentFormat.OpenXml.Drawing.Pictures.Picture>();
                if (picture != null)
                {
                    // 检查形状属性中的线条设置
                    DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties? shapeProperties = picture.ShapeProperties;
                    if (shapeProperties != null)
                    {
                        DocumentFormat.OpenXml.Drawing.Outline? outline = shapeProperties.GetFirstChild<DocumentFormat.OpenXml.Drawing.Outline>();
                        if (outline != null)
                        {
                            // 检查复合线条类型
                            if (outline.CompoundLineType?.Value != null)
                            {
                                return TranslateCompoundLineType(outline.CompoundLineType.Value.ToString());
                            }

                            // 如果有边框但没有复合类型，返回简单边框
                            if (outline.Width?.Value != null || outline.GetFirstChild<DocumentFormat.OpenXml.Drawing.SolidFill>() != null)
                            {
                                return "简单边框";
                            }
                        }
                    }
                }
            }

            return "无边框";
        }
        catch
        {
            return "无边框";
        }
    }

    /// <summary>
    /// 翻译复合线条类型
    /// </summary>
    private static string TranslateCompoundLineType(string compoundType)
    {
        return compoundType.ToLower() switch
        {
            "single" => "单线边框",
            "double" => "双线边框",
            "thickThin" => "粗细复合边框",
            "thinThick" => "细粗复合边框",
            "triple" => "三线边框",
            _ => $"复合边框({compoundType})"
        };
    }

    /// <summary>
    /// 从Graphic中提取短划线类型
    /// </summary>
    private static string ExtractDashTypeFromGraphic(DocumentFormat.OpenXml.Drawing.Graphic graphic)
    {
        try
        {
            // 检查图形数据中的形状属性
            DocumentFormat.OpenXml.Drawing.GraphicData? graphicData = graphic.GraphicData;
            if (graphicData != null)
            {
                // 检查图片元素
                DocumentFormat.OpenXml.Drawing.Pictures.Picture? picture = graphicData.GetFirstChild<DocumentFormat.OpenXml.Drawing.Pictures.Picture>();
                if (picture != null)
                {
                    // 检查形状属性中的线条设置
                    DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties? shapeProperties = picture.ShapeProperties;
                    if (shapeProperties != null)
                    {
                        DocumentFormat.OpenXml.Drawing.Outline? outline = shapeProperties.GetFirstChild<DocumentFormat.OpenXml.Drawing.Outline>();
                        if (outline != null)
                        {
                            // 检查预设短划线类型
                            DocumentFormat.OpenXml.Drawing.PresetDash? presetDash = outline.GetFirstChild<DocumentFormat.OpenXml.Drawing.PresetDash>();
                            if (presetDash?.Val?.Value != null)
                            {
                                return TranslateDashType(presetDash.Val.Value.ToString());
                            }

                            // 检查自定义短划线
                            DocumentFormat.OpenXml.Drawing.CustomDash? customDash = outline.GetFirstChild<DocumentFormat.OpenXml.Drawing.CustomDash>();
                            if (customDash != null)
                            {
                                return "自定义短划线";
                            }

                            // 如果有边框但没有短划线设置，返回实线
                            if (outline.Width?.Value != null || outline.GetFirstChild<DocumentFormat.OpenXml.Drawing.SolidFill>() != null)
                            {
                                return "实线";
                            }
                        }
                    }
                }
            }

            return "无短划线";
        }
        catch
        {
            return "无短划线";
        }
    }

    /// <summary>
    /// 翻译短划线类型
    /// </summary>
    private static string TranslateDashType(string dashType)
    {
        return dashType.ToLower() switch
        {
            "solid" => "实线",
            "dot" => "点线",
            "dash" => "短划线",
            "dashdot" => "点划线",
            "dashdotdot" => "双点划线",
            "longdash" => "长划线",
            "longdashdot" => "长点划线",
            "longdashdotdot" => "长双点划线",
            "sysdash" => "系统短划线",
            "sysdot" => "系统点线",
            "sysdashdot" => "系统点划线",
            "sysdashdotdot" => "系统双点划线",
            _ => $"短划线({dashType})"
        };
    }

    /// <summary>
    /// 从Graphic中提取边框宽度
    /// </summary>
    private static float ExtractBorderWidthFromGraphic(DocumentFormat.OpenXml.Drawing.Graphic graphic)
    {
        try
        {
            // 检查图形数据中的形状属性
            DocumentFormat.OpenXml.Drawing.GraphicData? graphicData = graphic.GraphicData;
            if (graphicData != null)
            {
                // 检查图片元素
                DocumentFormat.OpenXml.Drawing.Pictures.Picture? picture = graphicData.GetFirstChild<DocumentFormat.OpenXml.Drawing.Pictures.Picture>();
                if (picture != null)
                {
                    // 检查形状属性中的线条设置
                    DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties? shapeProperties = picture.ShapeProperties;
                    if (shapeProperties != null)
                    {
                        DocumentFormat.OpenXml.Drawing.Outline? outline = shapeProperties.GetFirstChild<DocumentFormat.OpenXml.Drawing.Outline>();
                        if (outline?.Width?.Value != null)
                        {
                            // OpenXML中宽度以EMU为单位，转换为磅
                            return ConvertEmuToPoints(outline.Width.Value);
                        }
                    }
                }
            }

            return 0f;
        }
        catch
        {
            return 0f;
        }
    }

    /// <summary>
    /// 将EMU单位转换为磅
    /// </summary>
    private static float ConvertEmuToPoints(int emu)
    {
        // 1 EMU = 1/914400 英寸
        // 1 英寸 = 72 磅
        return emu / 914400.0f * 72.0f;
    }

    /// <summary>
    /// 从Graphic中提取边框颜色
    /// </summary>
    private static string ExtractBorderColorFromGraphic(DocumentFormat.OpenXml.Drawing.Graphic graphic)
    {
        try
        {
            // 检查图形数据中的形状属性
            DocumentFormat.OpenXml.Drawing.GraphicData? graphicData = graphic.GraphicData;
            if (graphicData != null)
            {
                // 检查图片元素
                DocumentFormat.OpenXml.Drawing.Pictures.Picture? picture = graphicData.GetFirstChild<DocumentFormat.OpenXml.Drawing.Pictures.Picture>();
                if (picture != null)
                {
                    // 检查形状属性中的线条设置
                    DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties? shapeProperties = picture.ShapeProperties;
                    if (shapeProperties != null)
                    {
                        DocumentFormat.OpenXml.Drawing.Outline? outline = shapeProperties.GetFirstChild<DocumentFormat.OpenXml.Drawing.Outline>();
                        if (outline != null)
                        {
                            // 检查实体填充颜色
                            DocumentFormat.OpenXml.Drawing.SolidFill? solidFill = outline.GetFirstChild<DocumentFormat.OpenXml.Drawing.SolidFill>();
                            if (solidFill != null)
                            {
                                // 检查RGB颜色
                                DocumentFormat.OpenXml.Drawing.RgbColorModelHex? rgbColor = solidFill.RgbColorModelHex;
                                if (rgbColor?.Val?.Value != null)
                                {
                                    return $"#{rgbColor.Val.Value}";
                                }

                                // 检查系统颜色
                                DocumentFormat.OpenXml.Drawing.SystemColor? systemColor = solidFill.SystemColor;
                                if (systemColor?.Val?.Value != null)
                                {
                                    return systemColor.Val.Value.ToString();
                                }

                                // 检查主题颜色
                                DocumentFormat.OpenXml.Drawing.SchemeColor? schemeColor = solidFill.SchemeColor;
                                if (schemeColor?.Val?.Value != null)
                                {
                                    return $"主题颜色({schemeColor.Val.Value})";
                                }
                            }

                            // 检查渐变填充
                            DocumentFormat.OpenXml.Drawing.GradientFill? gradientFill = outline.GetFirstChild<DocumentFormat.OpenXml.Drawing.GradientFill>();
                            if (gradientFill != null)
                            {
                                return "渐变边框颜色";
                            }

                            // 检查图案填充
                            DocumentFormat.OpenXml.Drawing.PatternFill? patternFill = outline.GetFirstChild<DocumentFormat.OpenXml.Drawing.PatternFill>();
                            if (patternFill != null)
                            {
                                return "图案边框颜色";
                            }
                        }
                    }
                }
            }

            return "无边框颜色";
        }
        catch
        {
            return "无边框颜色";
        }
    }

    /// <summary>
    /// 从VML形状中提取边框颜色
    /// </summary>
    private static string ExtractVmlShapeBorderColor(DocumentFormat.OpenXml.Vml.Shape shape)
    {
        try
        {
            // 检查形状样式中的边框颜色
            string? style = shape.Style?.Value;
            if (!string.IsNullOrEmpty(style))
            {
                string color = ParseBorderColorFromStyle(style);
                if (!string.IsNullOrEmpty(color))
                {
                    return color;
                }
            }

            // 检查strokecolor属性
            string? strokeColor = shape.StrokeColor?.Value;
            return !string.IsNullOrEmpty(strokeColor) ? NormalizeColorValue(strokeColor) : "无边框颜色";
        }
        catch
        {
            return "无边框颜色";
        }
    }

    /// <summary>
    /// 从Drawing中提取文本框边框颜色
    /// </summary>
    private static string ExtractDrawingTextBoxBorderColor(Drawing drawing)
    {
        try
        {
            // 检查是否包含文本框相关的形状
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            if (textElements.Any())
            {
                // 使用已有的图片边框颜色提取逻辑
                return ExtractImageBorderColor(drawing);
            }

            return "无边框颜色";
        }
        catch
        {
            return "无边框颜色";
        }
    }

    /// <summary>
    /// 从样式字符串中解析边框颜色
    /// </summary>
    private static string ParseBorderColorFromStyle(string style)
    {
        try
        {
            // 解析样式字符串中的border-color或stroke属性
            System.Text.RegularExpressions.Match borderColorMatch = System.Text.RegularExpressions.Regex.Match(style, @"border-color:\s*([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (borderColorMatch.Success)
            {
                return NormalizeColorValue(borderColorMatch.Groups[1].Value.Trim());
            }

            System.Text.RegularExpressions.Match strokeMatch = System.Text.RegularExpressions.Regex.Match(style, @"stroke:\s*([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return strokeMatch.Success ? NormalizeColorValue(strokeMatch.Groups[1].Value.Trim()) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 从Drawing中提取图片阴影信息
    /// </summary>
    private static (string ShadowType, string ShadowColor) ExtractImageShadow(Drawing drawing)
    {
        try
        {
            // 检查Inline图片的阴影
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline? inline = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();
            if (inline != null)
            {
                DocumentFormat.OpenXml.Drawing.Graphic? graphic = inline.Graphic;
                if (graphic != null)
                {
                    (string shadowType, string shadowColor) = ExtractShadowFromGraphic(graphic);
                    if (shadowType != "无阴影")
                    {
                        return (shadowType, shadowColor);
                    }
                }
            }

            // 检查Anchor图片的阴影
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor? anchor = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>();
            if (anchor != null)
            {
                DocumentFormat.OpenXml.Drawing.Graphic? graphic = anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Graphic>();
                if (graphic != null)
                {
                    (string shadowType, string shadowColor) = ExtractShadowFromGraphic(graphic);
                    if (shadowType != "无阴影")
                    {
                        return (shadowType, shadowColor);
                    }
                }
            }

            return ("无阴影", "无颜色");
        }
        catch
        {
            return ("无阴影", "无颜色");
        }
    }

    /// <summary>
    /// 从Graphic中提取阴影信息
    /// </summary>
    private static (string ShadowType, string ShadowColor) ExtractShadowFromGraphic(DocumentFormat.OpenXml.Drawing.Graphic graphic)
    {
        try
        {
            // 检查图形数据中的形状属性
            DocumentFormat.OpenXml.Drawing.GraphicData? graphicData = graphic.GraphicData;
            if (graphicData != null)
            {
                // 检查图片元素
                DocumentFormat.OpenXml.Drawing.Pictures.Picture? picture = graphicData.GetFirstChild<DocumentFormat.OpenXml.Drawing.Pictures.Picture>();
                if (picture != null)
                {
                    // 检查形状属性中的效果设置
                    DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties? shapeProperties = picture.ShapeProperties;
                    if (shapeProperties != null)
                    {
                        // 检查效果列表
                        DocumentFormat.OpenXml.Drawing.EffectList? effectList = shapeProperties.GetFirstChild<DocumentFormat.OpenXml.Drawing.EffectList>();
                        if (effectList != null)
                        {
                            // 检查外阴影
                            DocumentFormat.OpenXml.Drawing.OuterShadow? outerShadow = effectList.GetFirstChild<DocumentFormat.OpenXml.Drawing.OuterShadow>();
                            if (outerShadow != null)
                            {
                                string shadowColor = ExtractShadowColor(outerShadow);
                                return ("外阴影", shadowColor);
                            }

                            // 检查内阴影
                            DocumentFormat.OpenXml.Drawing.InnerShadow? innerShadow = effectList.GetFirstChild<DocumentFormat.OpenXml.Drawing.InnerShadow>();
                            if (innerShadow != null)
                            {
                                string shadowColor = ExtractShadowColor(innerShadow);
                                return ("内阴影", shadowColor);
                            }
                        }
                    }
                }
            }

            return ("无阴影", "无颜色");
        }
        catch
        {
            return ("无阴影", "无颜色");
        }
    }

    /// <summary>
    /// 提取阴影颜色
    /// </summary>
    private static string ExtractShadowColor(OpenXmlElement shadowElement)
    {
        try
        {
            // 检查RGB颜色
            DocumentFormat.OpenXml.Drawing.RgbColorModelHex? rgbColor = shadowElement.Descendants<DocumentFormat.OpenXml.Drawing.RgbColorModelHex>().FirstOrDefault();
            if (rgbColor?.Val?.Value != null)
            {
                return $"#{rgbColor.Val.Value}";
            }

            // 检查系统颜色
            DocumentFormat.OpenXml.Drawing.SystemColor? systemColor = shadowElement.Descendants<DocumentFormat.OpenXml.Drawing.SystemColor>().FirstOrDefault();
            if (systemColor?.Val?.Value != null)
            {
                return systemColor.Val.Value.ToString();
            }

            // 检查主题颜色
            DocumentFormat.OpenXml.Drawing.SchemeColor? schemeColor = shadowElement.Descendants<DocumentFormat.OpenXml.Drawing.SchemeColor>().FirstOrDefault();
            return schemeColor?.Val?.Value != null ? $"主题颜色({schemeColor.Val.Value})" : "默认阴影颜色";
        }
        catch
        {
            return "默认阴影颜色";
        }
    }

    /// <summary>
    /// 从Drawing中提取图片尺寸
    /// </summary>
    private static (float Width, float Height) ExtractImageSize(Drawing drawing)
    {
        try
        {
            // 检查Inline图片的尺寸
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline? inline = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();
            if (inline != null)
            {
                DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent? extent = inline.Extent;
                if (extent?.Cx?.Value != null && extent?.Cy?.Value != null)
                {
                    // 转换EMU为厘米
                    float widthCm = (float)ConvertEmuToCentimeters(extent.Cx.Value);
                    float heightCm = (float)ConvertEmuToCentimeters(extent.Cy.Value);
                    return (widthCm, heightCm);
                }
            }

            // 检查Anchor图片的尺寸
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor? anchor = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>();
            if (anchor != null)
            {
                DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent? extent = anchor.Extent;
                if (extent?.Cx?.Value != null && extent?.Cy?.Value != null)
                {
                    // 转换EMU为厘米
                    float widthCm = (float)ConvertEmuToCentimeters(extent.Cx.Value);
                    float heightCm = (float)ConvertEmuToCentimeters(extent.Cy.Value);
                    return (widthCm, heightCm);
                }
            }

            return (0f, 0f);
        }
        catch
        {
            return (0f, 0f);
        }
    }

    /// <summary>
    /// 从VML文本框中提取内容
    /// </summary>
    private static string ExtractVmlTextBoxContent(DocumentFormat.OpenXml.Vml.Shape shape)
    {
        try
        {
            List<string> textContents = [];

            // 1. 检查TextBox中的段落内容
            DocumentFormat.OpenXml.Vml.TextBox? textBox = shape.GetFirstChild<DocumentFormat.OpenXml.Vml.TextBox>();
            if (textBox != null)
            {
                IEnumerable<Paragraph> paragraphs = textBox.Descendants<Paragraph>();
                foreach (Paragraph paragraph in paragraphs)
                {
                    string paragraphText = paragraph.InnerText;
                    if (!string.IsNullOrEmpty(paragraphText))
                    {
                        textContents.Add(paragraphText.Trim());
                    }
                }
            }

            // 2. 检查形状的直接文本内容
            string directText = shape.InnerText;
            if (!string.IsNullOrEmpty(directText))
            {
                textContents.Add(directText.Trim());
            }

            // 合并所有文本内容
            return textContents.Count > 0 ? string.Join(" ", textContents.Distinct().Where(t => !string.IsNullOrEmpty(t))) : "无文本内容";
        }
        catch
        {
            return "无文本内容";
        }
    }

    /// <summary>
    /// 从Drawing中提取文本框内容
    /// </summary>
    private static string ExtractDrawingTextBoxContent(Drawing drawing)
    {
        try
        {
            List<string> textContents = [];

            // 检查Drawing中的文本元素
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text textElement in textElements)
            {
                if (!string.IsNullOrEmpty(textElement.Text))
                {
                    textContents.Add(textElement.Text.Trim());
                }
            }

            // 检查段落文本
            IEnumerable<Paragraph> paragraphs = drawing.Descendants<Paragraph>();
            foreach (Paragraph paragraph in paragraphs)
            {
                string paragraphText = paragraph.InnerText;
                if (!string.IsNullOrEmpty(paragraphText))
                {
                    textContents.Add(paragraphText.Trim());
                }
            }

            // 合并所有文本内容
            return textContents.Count > 0 ? string.Join(" ", textContents.Distinct().Where(t => !string.IsNullOrEmpty(t))) : "无文本内容";
        }
        catch
        {
            return "无文本内容";
        }
    }

    /// <summary>
    /// 从VML文本框中提取字体大小
    /// </summary>
    private static int ExtractVmlTextBoxFontSize(DocumentFormat.OpenXml.Vml.Shape shape)
    {
        try
        {
            // 检查Run元素中的字号设置
            IEnumerable<Run> runs = shape.Descendants<Run>();
            foreach (Run run in runs)
            {
                RunProperties? runProperties = run.RunProperties;
                FontSize? fontSize = runProperties?.FontSize;
                if (fontSize?.Val?.Value != null)
                {
                    if (int.TryParse(fontSize.Val.Value, out int size))
                    {
                        return size / 2; // OpenXML中字号以半点为单位
                    }
                }
            }

            // 检查形状样式中的字号
            string? style = shape.Style?.Value;
            if (!string.IsNullOrEmpty(style))
            {
                int fontSize = ParseFontSizeFromStyle(style);
                if (fontSize > 0)
                {
                    return fontSize;
                }
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 从Drawing中提取文本框字体大小
    /// </summary>
    private static int ExtractDrawingTextBoxFontSize(Drawing drawing)
    {
        try
        {
            // 检查Drawing中的文本运行
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text textElement in textElements)
            {
                // 检查文本运行的属性
                DocumentFormat.OpenXml.Drawing.RunProperties? runProperties = textElement.Parent?.Elements<DocumentFormat.OpenXml.Drawing.RunProperties>().FirstOrDefault();
                if (runProperties?.FontSize?.Value != null)
                {
                    // Drawing中的字号以百分点为单位，需要除以100
                    return runProperties.FontSize.Value / 100;
                }
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 从VML文本框中提取环绕方式
    /// </summary>
    private static string ExtractVmlTextBoxWrapStyle(DocumentFormat.OpenXml.Vml.Shape shape)
    {
        try
        {
            // 检查形状的环绕属性（通过属性访问）
            OpenXmlAttribute wrapCoordsAttr = shape.GetAttribute("wrapcoords", "");
            if (!string.IsNullOrEmpty(wrapCoordsAttr.Value))
            {
                return "自定义环绕";
            }

            // 检查样式中的环绕设置
            string? style = shape.Style?.Value;
            if (!string.IsNullOrEmpty(style))
            {
                if (style.Contains("wrap"))
                {
                    return "样式环绕";
                }
            }

            return "无环绕设置";
        }
        catch
        {
            return "无环绕设置";
        }
    }

    /// <summary>
    /// 从Drawing中提取文本框环绕方式
    /// </summary>
    private static string ExtractDrawingTextBoxWrapStyle(Drawing drawing)
    {
        try
        {
            // 使用已有的图片环绕方式检测逻辑
            return ExtractImageWrapStyle(drawing);
        }
        catch
        {
            return "无环绕设置";
        }
    }

    /// <summary>
    /// 从Drawing中提取图片环绕方式
    /// </summary>
    private static string ExtractImageWrapStyle(Drawing drawing)
    {
        try
        {
            // 检查Anchor类型的环绕方式
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor? anchor = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>();
            if (anchor != null)
            {
                // 检查各种环绕类型
                if (anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapNone>() != null)
                {
                    return "无环绕";
                }

                if (anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapSquare>() != null)
                {
                    return "四周型环绕";
                }

                if (anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapTight>() != null)
                {
                    return "紧密型环绕";
                }

                if (anchor.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.WrapThrough>() != null)
                {
                    return "穿越型环绕";
                }
                // 注意：WrapTopAndBottom可能不存在，使用通用检查
                IEnumerable<OpenXmlElement> wrapElements = anchor.Elements().Where(e => e.LocalName.Contains("wrap"));
                if (wrapElements.Any())
                {
                    OpenXmlElement wrapElement = wrapElements.First();
                    return $"{wrapElement.LocalName}环绕";
                }
            }

            // Inline类型图片通常跟随文本流
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline? inline = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();
            return inline != null ? "嵌入式(跟随文本)" : "无环绕设置";
        }
        catch
        {
            return "无环绕设置";
        }
    }

    /// <summary>
    /// 从VML文本框中提取位置信息
    /// </summary>
    private static (bool HasPosition, string Horizontal, string Vertical) ExtractVmlTextBoxPosition(DocumentFormat.OpenXml.Vml.Shape shape)
    {
        try
        {
            // 检查形状样式中的位置信息
            string? style = shape.Style?.Value;
            if (!string.IsNullOrEmpty(style))
            {
                string horizontal = "未知水平位置";
                string vertical = "未知垂直位置";
                bool hasPosition = false;

                // 解析left位置
                System.Text.RegularExpressions.Match leftMatch = System.Text.RegularExpressions.Regex.Match(style, @"left:\s*([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (leftMatch.Success)
                {
                    string leftValue = leftMatch.Groups[1].Value.Trim();
                    horizontal = $"水平位置: {leftValue}";
                    hasPosition = true;
                }

                // 解析top位置
                System.Text.RegularExpressions.Match topMatch = System.Text.RegularExpressions.Regex.Match(style, @"top:\s*([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (topMatch.Success)
                {
                    string topValue = topMatch.Groups[1].Value.Trim();
                    vertical = $"垂直位置: {topValue}";
                    hasPosition = true;
                }

                // 解析position属性
                System.Text.RegularExpressions.Match positionMatch = System.Text.RegularExpressions.Regex.Match(style, @"position:\s*([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (positionMatch.Success)
                {
                    string positionType = positionMatch.Groups[1].Value.Trim();
                    if (!hasPosition)
                    {
                        horizontal = $"定位类型: {positionType}";
                        vertical = $"定位类型: {positionType}";
                    }
                    hasPosition = true;
                }

                if (hasPosition)
                {
                    return (true, horizontal, vertical);
                }
            }

            // 检查形状的直接位置属性
            OpenXmlAttribute leftAttr = shape.GetAttribute("left", "");
            OpenXmlAttribute topAttr = shape.GetAttribute("top", "");

            if (!string.IsNullOrEmpty(leftAttr.Value) || !string.IsNullOrEmpty(topAttr.Value))
            {
                string horizontal = !string.IsNullOrEmpty(leftAttr.Value) ? $"左边距: {leftAttr.Value}" : "左边距: 0";
                string vertical = !string.IsNullOrEmpty(topAttr.Value) ? $"上边距: {topAttr.Value}" : "上边距: 0";
                return (true, horizontal, vertical);
            }

            return (false, "无位置信息", "无位置信息");
        }
        catch
        {
            return (false, "位置解析失败", "位置解析失败");
        }
    }

    /// <summary>
    /// 从Drawing中提取文本框位置信息
    /// </summary>
    private static (bool HasPosition, string Horizontal, string Vertical) ExtractDrawingTextBoxPosition(Drawing drawing)
    {
        try
        {
            // 检查是否包含文本内容（判断是否为文本框）
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            if (textElements.Any())
            {
                // 使用已有的图片位置检测逻辑
                (bool hasPos, string horizontal, string vertical) = GetImagePosition(drawing);
                if (hasPos)
                {
                    return (hasPos, $"文本框-{horizontal}", $"文本框-{vertical}");
                }
            }

            return (false, "无位置信息", "无位置信息");
        }
        catch
        {
            return (false, "位置解析失败", "位置解析失败");
        }
    }

    /// <summary>
    /// 从单个Drawing中获取位置信息（辅助方法）
    /// </summary>
    private static (bool HasPosition, string Horizontal, string Vertical) GetImagePosition(Drawing drawing)
    {
        try
        {
            // 检查Inline类型
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline? inline = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline>();
            if (inline != null)
            {
                (string horizontal, string vertical) = ParseInlinePosition(inline);
                return (true, horizontal, vertical);
            }

            // 检查Anchor类型
            DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor? anchor = drawing.GetFirstChild<DocumentFormat.OpenXml.Drawing.Wordprocessing.Anchor>();
            if (anchor != null)
            {
                (string horizontal, string vertical) = ParseAnchorPosition(anchor);
                return (true, horizontal, vertical);
            }

            return (false, "无位置信息", "无位置信息");
        }
        catch
        {
            return (false, "位置解析失败", "位置解析失败");
        }
    }
}
