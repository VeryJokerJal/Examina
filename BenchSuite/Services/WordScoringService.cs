using System.Runtime.InteropServices;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using Task = System.Threading.Tasks.Task;
using Word = Microsoft.Office.Interop.Word;

namespace BenchSuite.Services;

/// <summary>
/// Word打分服务实现
/// </summary>
public class WordScoringService : IWordScoringService
{
    private readonly ScoringConfiguration _defaultConfiguration;
    private static readonly string[] SupportedExtensions = [".doc", ".docx"];

    public WordScoringService()
    {
        _defaultConfiguration = new ScoringConfiguration();
    }

    /// <summary>
    /// 对Word文件进行打分
    /// </summary>
    public async Task<ScoringResult> ScoreFileAsync(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        return await Task.Run(() => ScoreFile(filePath, examModel, configuration));
    }

    /// <summary>
    /// 对单个题目进行评分
    /// </summary>
    public async Task<ScoringResult> ScoreQuestionAsync(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        return await Task.Run(() => ScoreQuestion(filePath, question, configuration));
    }

    /// <summary>
    /// 对Word文件进行打分（同步版本）
    /// </summary>
    public ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        ScoringConfiguration config = configuration ?? _defaultConfiguration;
        ScoringResult result = new()
        {
            StartTime = DateTime.Now,
            IsSuccess = false
        };

        try
        {
            if (!CanProcessFile(filePath))
            {
                result.ErrorMessage = $"不支持的文件类型: {Path.GetExtension(filePath)}";
                return result;
            }

            if (!File.Exists(filePath))
            {
                result.ErrorMessage = $"文件不存在: {filePath}";
                return result;
            }

            // 获取Word模块
            ExamModuleModel? wordModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.Word);
            if (wordModule == null)
            {
                result.ErrorMessage = "试卷中未找到Word模块";
                return result;
            }

            // 收集所有操作点并记录题目关联关系
            List<OperationPointModel> allOperationPoints = [];
            Dictionary<string, string> operationPointToQuestionMap = [];

            foreach (QuestionModel question in wordModule.Questions)
            {
                foreach (OperationPointModel operationPoint in question.OperationPoints)
                {
                    allOperationPoints.Add(operationPoint);
                    operationPointToQuestionMap[operationPoint.Id] = question.Id;
                }
            }

            if (allOperationPoints.Count == 0)
            {
                result.ErrorMessage = "Word模块中未找到操作点";
                return result;
            }

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, allOperationPoints).Result;

            // 为每个知识点结果设置题目关联信息
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                if (operationPointToQuestionMap.TryGetValue(kpResult.KnowledgePointId, out string? questionId))
                {
                    kpResult.QuestionId = questionId;
                    // 查找题目标题
                    QuestionModel? question = wordModule.Questions.FirstOrDefault(q => q.Id == questionId);
                    if (question != null)
                    {
                        // 可以在这里添加更多题目信息，如果需要的话
                    }
                }
            }

            // 计算总分和获得分数
            result.TotalScore = allOperationPoints.Sum(op => op.Score);
            result.AchievedScore = result.KnowledgePointResults.Sum(kpr => kpr.AchievedScore);

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"打分过程中发生错误: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 对单个题目进行评分（同步版本）
    /// </summary>
    private ScoringResult ScoreQuestion(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        ScoringResult result = new()
        {
            QuestionId = question.Id,
            QuestionTitle = question.Title,
            StartTime = DateTime.Now
        };

        try
        {
            // 验证文件是否存在
            if (!File.Exists(filePath))
            {
                result.ErrorMessage = $"文件不存在: {filePath}";
                result.IsSuccess = false;
                return result;
            }

            // 验证文件扩展名
            if (!CanProcessFile(filePath))
            {
                result.ErrorMessage = $"不支持的文件类型: {Path.GetExtension(filePath)}";
                result.IsSuccess = false;
                return result;
            }

            // 获取题目的操作点（只处理Word相关的操作点）
            List<OperationPointModel> wordOperationPoints = question.OperationPoints
                .Where(op => op.ModuleType == ModuleType.Word && op.IsEnabled)
                .ToList();

            if (wordOperationPoints.Count == 0)
            {
                result.ErrorMessage = "题目没有包含任何Word操作点";
                result.IsSuccess = false;
                return result;
            }

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, wordOperationPoints).Result;

            // 为每个知识点结果设置题目ID
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                kpResult.QuestionId = question.Id;
            }

            // 计算总分和获得分数
            result.TotalScore = wordOperationPoints.Sum(op => op.Score);
            result.AchievedScore = result.KnowledgePointResults.Sum(kpr => kpr.AchievedScore);

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"打分过程中发生错误: {ex.Message}";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 检测Word文档中的特定知识点
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

            Word.Application? wordApp = null;
            Word.Document? document = null;

            try
            {
                // 启动Word应用程序
                wordApp = new Word.Application
                {
                    Visible = false
                };

                // 打开文档
                document = wordApp.Documents.Open(filePath, ReadOnly: true);

                // 根据知识点类型进行检测
                result = DetectSpecificKnowledgePoint(document, knowledgePointType, parameters);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"检测知识点时发生错误: {ex.Message}";
                result.IsCorrect = false;
            }
            finally
            {
                // 清理资源
                CleanupWordResources(document, wordApp);
            }

            return result;
        });
    }

    /// <summary>
    /// 批量检测Word文档中的知识点
    /// </summary>
    public async Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(string filePath, List<OperationPointModel> knowledgePoints)
    {
        return await Task.Run(() =>
        {
            List<KnowledgePointResult> results = [];
            Word.Application? wordApp = null;
            Word.Document? document = null;

            // 创建基于文件路径的确定性参数解析上下文
            string contextId = Path.GetFileName(filePath) + "_" + new FileInfo(filePath).Length;
            ParameterResolutionContext context = new(contextId);

            try
            {
                // 启动Word应用程序
                wordApp = new Word.Application
                {
                    Visible = false
                };

                // 打开文档
                document = wordApp.Documents.Open(filePath, ReadOnly: true);

                // 预先解析所有-1参数
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    Dictionary<string, string> parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);
                    ResolveParametersForDocument(parameters, document, context);
                }

                // 逐个检测知识点
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    try
                    {
                        Dictionary<string, string> parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);

                        // 使用解析后的参数
                        Dictionary<string, string> resolvedParameters = GetResolvedParameters(parameters, context);

                        // 根据操作点名称映射到知识点类型
                        string knowledgePointType = MapOperationPointNameToKnowledgeType(operationPoint.Name);

                        KnowledgePointResult result = document is not null
                            ? DetectSpecificKnowledgePoint(document, knowledgePointType, resolvedParameters)
                            : new KnowledgePointResult
                            {
                                ErrorMessage = "Word文档未能正确打开",
                                IsCorrect = false,
                                KnowledgePointType = knowledgePointType,
                                Parameters = resolvedParameters
                            };

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
                        results.Add(new KnowledgePointResult
                        {
                            KnowledgePointId = operationPoint.Id,
                            OperationPointId = operationPoint.Id,
                            KnowledgePointName = operationPoint.Name,
                            KnowledgePointType = operationPoint.WordKnowledgeType ?? string.Empty,
                            TotalScore = operationPoint.Score,
                            AchievedScore = 0,
                            IsCorrect = false,
                            ErrorMessage = $"检测失败: {ex.Message}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果无法打开文件，为所有知识点返回错误结果
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    results.Add(new KnowledgePointResult
                    {
                        KnowledgePointId = operationPoint.Id,
                        OperationPointId = operationPoint.Id,
                        KnowledgePointName = operationPoint.Name,
                        KnowledgePointType = operationPoint.WordKnowledgeType ?? string.Empty,
                        TotalScore = operationPoint.Score,
                        AchievedScore = 0,
                        IsCorrect = false,
                        ErrorMessage = $"无法打开Word文档: {ex.Message}"
                    });
                }

                // 添加参数解析日志到第一个结果中（用于调试）
                if (results.Count > 0)
                {
                    string resolutionLog = context.GetResolutionLog();
                    if (!string.IsNullOrEmpty(resolutionLog))
                    {
                        if (string.IsNullOrEmpty(results[0].Details))
                        {
                            results[0].Details = $"参数解析: {resolutionLog}";
                        }
                        else
                        {
                            results[0].Details += $"\n参数解析: {resolutionLog}";
                        }
                    }
                }
            }
            finally
            {
                // 清理资源
                CleanupWordResources(document, wordApp);
            }

            return results;
        });
    }

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
    /// 检测特定知识点
    /// </summary>
    private KnowledgePointResult DetectSpecificKnowledgePoint(Word.Document document, string knowledgePointType, Dictionary<string, string> parameters)
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
                // 段落操作类 (1-14)
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

                // 页面设置类 (15-29)
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

                // 水印设置类 (30-33)
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

                // 项目编号类 (34)
                case "SetBulletNumbering":
                    result = DetectBulletNumbering(document, parameters);
                    break;

                // 表格操作类 (35-44)
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

                // 图形图片类 (45-60)
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
                case "SetImagePosition":
                    result = DetectImagePosition(document, parameters);
                    break;
                case "SetImageSize":
                    result = DetectImageSize(document, parameters);
                    break;

                // 文本框类 (61-65)
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

                // 其他操作类 (66-67)
                case "FindAndReplace":
                    result = DetectFindAndReplace(document, parameters);
                    break;
                case "SetSpecificTextFontSize":
                    result = DetectSpecificTextFontSize(document, parameters);
                    break;

                default:
                    result.ErrorMessage = $"不支持的知识点类型: {knowledgePointType}";
                    result.IsCorrect = false;
                    break;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测知识点 {knowledgePointType} 时发生错误: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落字体
    /// </summary>
    private KnowledgePointResult DetectParagraphFont(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphFont",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("FontFamily", out string? expectedFont))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 FontFamily";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            string actualFont = paragraph.Range.Font.Name;

            result.ExpectedValue = expectedFont;
            result.ActualValue = actualFont;
            result.IsCorrect = string.Equals(actualFont, expectedFont, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的字体: 期望 {expectedFont}, 实际 {actualFont}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落字体失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落字号
    /// </summary>
    private KnowledgePointResult DetectParagraphFontSize(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphFontSize",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("FontSize", out string? expectedSizeStr) ||
                !float.TryParse(expectedSizeStr, out float expectedSize))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 FontSize";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            float actualSize = (float)paragraph.Range.Font.Size;

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = actualSize.ToString();
            result.IsCorrect = Math.Abs(actualSize - expectedSize) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的字号: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落字号失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落字体样式
    /// </summary>
    private KnowledgePointResult DetectParagraphFontStyle(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphFontStyle",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("FontStyle", out string? expectedStyle))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 FontStyle";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            Word.Font font = paragraph.Range.Font;

            string actualStyle = GetFontStyleDescription(font);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle;
            result.IsCorrect = string.Equals(actualStyle, expectedStyle, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的字体样式: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落字体样式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落对齐方式
    /// </summary>
    private KnowledgePointResult DetectParagraphAlignment(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphAlignment",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("Alignment", out string? expectedAlignment))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 Alignment";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            string actualAlignment = GetAlignmentDescription(paragraph.Alignment);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = actualAlignment;
            result.IsCorrect = string.Equals(actualAlignment, expectedAlignment, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的对齐方式: 期望 {expectedAlignment}, 实际 {actualAlignment}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落对齐方式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落文字颜色
    /// </summary>
    private KnowledgePointResult DetectParagraphTextColor(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphTextColor",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("TextColor", out string? expectedColor))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 TextColor";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            string actualColor = GetColorDescription(paragraph.Range.Font.Color);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = string.Equals(actualColor, expectedColor, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的文字颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落文字颜色失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测纸张大小
    /// </summary>
    private KnowledgePointResult DetectPaperSize(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPaperSize",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("PaperSize", out string? expectedSize))
            {
                result.ErrorMessage = "缺少必要参数: PaperSize";
                return result;
            }

            Word.PageSetup pageSetup = document.Sections[1].PageSetup;
            string actualSize = GetPaperSizeDescription(pageSetup.PaperSize);

            result.ExpectedValue = expectedSize;
            result.ActualValue = actualSize;
            result.IsCorrect = string.Equals(actualSize, expectedSize, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"纸张大小: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测纸张大小失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测页边距
    /// </summary>
    private KnowledgePointResult DetectPageMargins(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageMargins",
            Parameters = parameters
        };

        try
        {
            Word.PageSetup pageSetup = document.Sections[1].PageSetup;

            bool allMarginsCorrect = true;
            string details = "页边距检测: ";

            // 检查上边距
            if (parameters.TryGetValue("TopMargin", out string? topMarginStr) &&
                float.TryParse(topMarginStr, out float expectedTopMargin))
            {
                float actualTopMargin = (float)pageSetup.TopMargin;
                bool topCorrect = Math.Abs(actualTopMargin - expectedTopMargin) < 1.0f;
                allMarginsCorrect &= topCorrect;
                details += $"上边距 期望{expectedTopMargin} 实际{actualTopMargin:F1} ";
            }

            // 检查下边距
            if (parameters.TryGetValue("BottomMargin", out string? bottomMarginStr) &&
                float.TryParse(bottomMarginStr, out float expectedBottomMargin))
            {
                float actualBottomMargin = (float)pageSetup.BottomMargin;
                bool bottomCorrect = Math.Abs(actualBottomMargin - expectedBottomMargin) < 1.0f;
                allMarginsCorrect &= bottomCorrect;
                details += $"下边距 期望{expectedBottomMargin} 实际{actualBottomMargin:F1} ";
            }

            result.IsCorrect = allMarginsCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = details;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测页边距失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测页眉文字
    /// </summary>
    private KnowledgePointResult DetectHeaderText(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetHeaderText",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("HeaderText", out string? expectedText))
            {
                result.ErrorMessage = "缺少必要参数: HeaderText";
                return result;
            }

            Word.HeaderFooter header = document.Sections[1].Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary];
            string actualText = header.Range.Text.Trim();

            result.ExpectedValue = expectedText;
            result.ActualValue = actualText;
            result.IsCorrect = actualText.Contains(expectedText, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页眉文字: 期望包含 '{expectedText}', 实际 '{actualText}'";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测页眉文字失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测页脚文字
    /// </summary>
    private KnowledgePointResult DetectFooterText(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFooterText",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("FooterText", out string? expectedText))
            {
                result.ErrorMessage = "缺少必要参数: FooterText";
                return result;
            }

            Word.HeaderFooter footer = document.Sections[1].Footers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary];
            string actualText = footer.Range.Text.Trim();

            result.ExpectedValue = expectedText;
            result.ActualValue = actualText;
            result.IsCorrect = actualText.Contains(expectedText, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页脚文字: 期望包含 '{expectedText}', 实际 '{actualText}'";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测页脚文字失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测表格行列数
    /// </summary>
    private KnowledgePointResult DetectTableRowsColumns(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableRowsColumns",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("Rows", out string? expectedRowsStr) ||
                !int.TryParse(expectedRowsStr, out int expectedRows) ||
                !parameters.TryGetValue("Columns", out string? expectedColumnsStr) ||
                !int.TryParse(expectedColumnsStr, out int expectedColumns))
            {
                result.ErrorMessage = "缺少必要参数: Rows 或 Columns";
                return result;
            }

            if (document.Tables.Count == 0)
            {
                result.ErrorMessage = "文档中没有找到表格";
                result.IsCorrect = false;
                return result;
            }

            Word.Table table = document.Tables[1];
            int actualRows = table.Rows.Count;
            int actualColumns = table.Columns.Count;

            result.ExpectedValue = $"{expectedRows}行{expectedColumns}列";
            result.ActualValue = $"{actualRows}行{actualColumns}列";
            result.IsCorrect = actualRows == expectedRows && actualColumns == expectedColumns;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表格规格: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测表格行列数失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测表格单元格内容
    /// </summary>
    private KnowledgePointResult DetectTableCellContent(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableCellContent",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("RowNumber", out string? rowNumberStr) ||
                !int.TryParse(rowNumberStr, out int rowNumber) ||
                !parameters.TryGetValue("ColumnNumber", out string? columnNumberStr) ||
                !int.TryParse(columnNumberStr, out int columnNumber) ||
                !parameters.TryGetValue("CellContent", out string? expectedContent))
            {
                result.ErrorMessage = "缺少必要参数: RowNumber, ColumnNumber 或 CellContent";
                return result;
            }

            if (document.Tables.Count == 0)
            {
                result.ErrorMessage = "文档中没有找到表格";
                result.IsCorrect = false;
                return result;
            }

            Word.Table table = document.Tables[1];
            if (rowNumber < 1 || rowNumber > table.Rows.Count ||
                columnNumber < 1 || columnNumber > table.Columns.Count)
            {
                result.ErrorMessage = $"单元格位置超出范围: 行{rowNumber}, 列{columnNumber}";
                return result;
            }

            Word.Cell cell = table.Cell(rowNumber, columnNumber);
            string actualContent = cell.Range.Text.Trim().Replace("\r\a", "");

            result.ExpectedValue = expectedContent;
            result.ActualValue = actualContent;
            result.IsCorrect = string.Equals(actualContent, expectedContent, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"单元格({rowNumber},{columnNumber})内容: 期望 '{expectedContent}', 实际 '{actualContent}'";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测表格单元格内容失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测查找与替换
    /// </summary>
    private KnowledgePointResult DetectFindAndReplace(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "FindAndReplace",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("FindText", out string? findText) ||
                !parameters.TryGetValue("ReplaceText", out string? replaceText))
            {
                result.ErrorMessage = "缺少必要参数: FindText 或 ReplaceText";
                return result;
            }

            // 检查文档中是否包含替换后的文字
            string documentText = document.Content.Text;
            bool containsReplaceText = documentText.Contains(replaceText, StringComparison.OrdinalIgnoreCase);
            bool containsFindText = documentText.Contains(findText, StringComparison.OrdinalIgnoreCase);

            result.ExpectedValue = $"将 '{findText}' 替换为 '{replaceText}'";
            result.ActualValue = $"包含替换文字: {containsReplaceText}, 包含原文字: {containsFindText}";
            result.IsCorrect = containsReplaceText && !containsFindText;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"查找替换检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测查找替换失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落字符间距
    /// </summary>
    private KnowledgePointResult DetectParagraphCharacterSpacing(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphCharacterSpacing",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("CharacterSpacing", out string? expectedSpacingStr) ||
                !float.TryParse(expectedSpacingStr, out float expectedSpacing))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 CharacterSpacing";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            float actualSpacing = (float)paragraph.Range.Font.Spacing;

            result.ExpectedValue = expectedSpacing.ToString();
            result.ActualValue = actualSpacing.ToString();
            result.IsCorrect = Math.Abs(actualSpacing - expectedSpacing) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的字符间距: 期望 {expectedSpacing}, 实际 {actualSpacing}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落字符间距失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落缩进
    /// </summary>
    private KnowledgePointResult DetectParagraphIndentation(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphIndentation",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            Word.ParagraphFormat format = paragraph.Format;

            bool allIndentationsCorrect = true;
            string details = "段落缩进检测: ";

            // 检查左缩进
            if (parameters.TryGetValue("LeftIndent", out string? leftIndentStr) &&
                float.TryParse(leftIndentStr, out float expectedLeftIndent))
            {
                float actualLeftIndent = (float)format.LeftIndent;
                bool leftCorrect = Math.Abs(actualLeftIndent - expectedLeftIndent) < 1.0f;
                allIndentationsCorrect &= leftCorrect;
                details += $"左缩进 期望{expectedLeftIndent} 实际{actualLeftIndent:F1} ";
            }

            // 检查右缩进
            if (parameters.TryGetValue("RightIndent", out string? rightIndentStr) &&
                float.TryParse(rightIndentStr, out float expectedRightIndent))
            {
                float actualRightIndent = (float)format.RightIndent;
                bool rightCorrect = Math.Abs(actualRightIndent - expectedRightIndent) < 1.0f;
                allIndentationsCorrect &= rightCorrect;
                details += $"右缩进 期望{expectedRightIndent} 实际{actualRightIndent:F1} ";
            }

            // 检查首行缩进
            if (parameters.TryGetValue("FirstLineIndent", out string? firstLineIndentStr) &&
                float.TryParse(firstLineIndentStr, out float expectedFirstLineIndent))
            {
                float actualFirstLineIndent = (float)format.FirstLineIndent;
                bool firstLineCorrect = Math.Abs(actualFirstLineIndent - expectedFirstLineIndent) < 1.0f;
                allIndentationsCorrect &= firstLineCorrect;
                details += $"首行缩进 期望{expectedFirstLineIndent} 实际{actualFirstLineIndent:F1} ";
            }

            result.IsCorrect = allIndentationsCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = details;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落缩进失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落行距
    /// </summary>
    private KnowledgePointResult DetectParagraphLineSpacing(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphLineSpacing",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("LineSpacing", out string? expectedSpacingStr) ||
                !float.TryParse(expectedSpacingStr, out float expectedSpacing))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 LineSpacing";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            float actualSpacing = (float)paragraph.Format.LineSpacing;

            result.ExpectedValue = expectedSpacing.ToString();
            result.ActualValue = actualSpacing.ToString();
            result.IsCorrect = Math.Abs(actualSpacing - expectedSpacing) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的行距: 期望 {expectedSpacing}, 实际 {actualSpacing}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落行距失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落首字下沉
    /// </summary>
    private KnowledgePointResult DetectParagraphDropCap(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphDropCap",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("DropCapEnabled", out string? expectedEnabledStr) ||
                !bool.TryParse(expectedEnabledStr, out bool expectedEnabled))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 DropCapEnabled";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            bool actualEnabled = paragraph.DropCap.Position != Word.WdDropPosition.wdDropNone;

            result.ExpectedValue = expectedEnabled.ToString();
            result.ActualValue = actualEnabled.ToString();
            result.IsCorrect = actualEnabled == expectedEnabled;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的首字下沉: 期望 {expectedEnabled}, 实际 {actualEnabled}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落首字下沉失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落间距
    /// </summary>
    private KnowledgePointResult DetectParagraphSpacing(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphSpacing",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            Word.ParagraphFormat format = paragraph.Format;

            bool allSpacingCorrect = true;
            string details = "段落间距检测: ";

            // 检查段前间距
            if (parameters.TryGetValue("SpaceBefore", out string? spaceBeforeStr) &&
                float.TryParse(spaceBeforeStr, out float expectedSpaceBefore))
            {
                float actualSpaceBefore = (float)format.SpaceBefore;
                bool beforeCorrect = Math.Abs(actualSpaceBefore - expectedSpaceBefore) < 1.0f;
                allSpacingCorrect &= beforeCorrect;
                details += $"段前间距 期望{expectedSpaceBefore} 实际{actualSpaceBefore:F1} ";
            }

            // 检查段后间距
            if (parameters.TryGetValue("SpaceAfter", out string? spaceAfterStr) &&
                float.TryParse(spaceAfterStr, out float expectedSpaceAfter))
            {
                float actualSpaceAfter = (float)format.SpaceAfter;
                bool afterCorrect = Math.Abs(actualSpaceAfter - expectedSpaceAfter) < 1.0f;
                allSpacingCorrect &= afterCorrect;
                details += $"段后间距 期望{expectedSpaceAfter} 实际{actualSpaceAfter:F1} ";
            }

            result.IsCorrect = allSpacingCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = details;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落间距失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落边框颜色
    /// </summary>
    private KnowledgePointResult DetectParagraphBorderColor(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphBorderColor",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("BorderColor", out string? expectedColor))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 BorderColor";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            string actualColor = GetColorDescription(paragraph.Borders.OutsideColor);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = string.Equals(actualColor, expectedColor, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的边框颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落边框颜色失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落边框样式
    /// </summary>
    private KnowledgePointResult DetectParagraphBorderStyle(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphBorderStyle",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("BorderStyle", out string? expectedStyle))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 BorderStyle";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            string actualStyle = GetBorderStyleDescription(paragraph.Borders.OutsideLineStyle);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle;
            result.IsCorrect = string.Equals(actualStyle, expectedStyle, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的边框样式: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落边框样式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落边框宽度
    /// </summary>
    private KnowledgePointResult DetectParagraphBorderWidth(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphBorderWidth",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("BorderWidth", out string? expectedWidthStr) ||
                !float.TryParse(expectedWidthStr, out float expectedWidth))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 BorderWidth";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            float actualWidth = (float)paragraph.Borders.OutsideLineWidth;

            result.ExpectedValue = expectedWidth.ToString();
            result.ActualValue = actualWidth.ToString();
            result.IsCorrect = Math.Abs(actualWidth - expectedWidth) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的边框宽度: 期望 {expectedWidth}, 实际 {actualWidth}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落边框宽度失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测段落底纹
    /// </summary>
    private KnowledgePointResult DetectParagraphShading(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphShading",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ParagraphNumber", out string? paragraphNumberStr) ||
                !int.TryParse(paragraphNumberStr, out int paragraphNumber) ||
                !parameters.TryGetValue("ShadingColor", out string? expectedColor))
            {
                result.ErrorMessage = "缺少必要参数: ParagraphNumber 或 ShadingColor";
                return result;
            }

            if (paragraphNumber < 1 || paragraphNumber > document.Paragraphs.Count)
            {
                result.ErrorMessage = $"段落索引超出范围: {paragraphNumber}";
                return result;
            }

            Word.Paragraph paragraph = document.Paragraphs[paragraphNumber];
            string actualColor = GetColorDescription(paragraph.Shading.BackgroundPatternColor);

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColor;
            result.IsCorrect = string.Equals(actualColor, expectedColor, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落 {paragraphNumber} 的底纹颜色: 期望 {expectedColor}, 实际 {actualColor}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落底纹失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 获取字体样式描述
    /// </summary>
    private string GetFontStyleDescription(Word.Font font)
    {
        List<string> styles = [];

        if (font.Bold == 1)
        {
            styles.Add("加粗");
        }

        if (font.Italic == 1)
        {
            styles.Add("斜体");
        }

        if (font.Underline != Word.WdUnderline.wdUnderlineNone)
        {
            styles.Add("下划线");
        }

        if (font.StrikeThrough == 1)
        {
            styles.Add("删除线");
        }

        return styles.Count > 0 ? string.Join("+", styles) : "常规";
    }

    /// <summary>
    /// 获取对齐方式描述
    /// </summary>
    private string GetAlignmentDescription(Word.WdParagraphAlignment alignment)
    {
        return alignment switch
        {
            Word.WdParagraphAlignment.wdAlignParagraphLeft => "左对齐",
            Word.WdParagraphAlignment.wdAlignParagraphCenter => "居中对齐",
            Word.WdParagraphAlignment.wdAlignParagraphRight => "右对齐",
            Word.WdParagraphAlignment.wdAlignParagraphJustify => "两端对齐",
            Word.WdParagraphAlignment.wdAlignParagraphDistribute => "分散对齐",
            _ => "未知对齐"
        };
    }

    /// <summary>
    /// 获取颜色描述
    /// </summary>
    private string GetColorDescription(object color)
    {
        try
        {
            return color is int colorValue ? $"#{colorValue:X6}" : color.ToString() ?? "未知颜色";
        }
        catch
        {
            return "未知颜色";
        }
    }

    /// <summary>
    /// 获取纸张大小描述
    /// </summary>
    private string GetPaperSizeDescription(Word.WdPaperSize paperSize)
    {
        return paperSize switch
        {
            Word.WdPaperSize.wdPaperA4 => "A4纸",
            Word.WdPaperSize.wdPaperA3 => "A3纸",
            Word.WdPaperSize.wdPaperB5 => "B5纸",
            Word.WdPaperSize.wdPaperLegal => "法律纸尺寸",
            _ => "其他尺寸"
        };
    }

    /// <summary>
    /// 获取边框样式描述
    /// </summary>
    private string GetBorderStyleDescription(Word.WdLineStyle lineStyle)
    {
        return lineStyle switch
        {
            Word.WdLineStyle.wdLineStyleNone => "无边框",
            Word.WdLineStyle.wdLineStyleSingle => "单线",
            Word.WdLineStyle.wdLineStyleDouble => "双线",
            Word.WdLineStyle.wdLineStyleDot => "点线",
            Word.WdLineStyle.wdLineStyleDashSmallGap => "虚线",
            Word.WdLineStyle.wdLineStyleDashDot => "点划线",
            Word.WdLineStyle.wdLineStyleDashDotDot => "双点划线",
            Word.WdLineStyle.wdLineStyleTriple => "三线",
            _ => "其他样式"
        };
    }

    /// <summary>
    /// 检测页眉字体
    /// </summary>
    private KnowledgePointResult DetectHeaderFont(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetHeaderFont",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("FontFamily", out string? expectedFont))
            {
                result.ErrorMessage = "缺少必要参数: FontFamily";
                return result;
            }

            Word.HeaderFooter header = document.Sections[1].Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary];
            string actualFont = header.Range.Font.Name;

            result.ExpectedValue = expectedFont;
            result.ActualValue = actualFont;
            result.IsCorrect = string.Equals(actualFont, expectedFont, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页眉字体: 期望 {expectedFont}, 实际 {actualFont}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测页眉字体失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测页眉字号
    /// </summary>
    private KnowledgePointResult DetectHeaderFontSize(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetHeaderFontSize",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("FontSize", out string? expectedSizeStr) ||
                !float.TryParse(expectedSizeStr, out float expectedSize))
            {
                result.ErrorMessage = "缺少必要参数: FontSize";
                return result;
            }

            Word.HeaderFooter header = document.Sections[1].Headers[Word.WdHeaderFooterIndex.wdHeaderFooterPrimary];
            float actualSize = (float)header.Range.Font.Size;

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = actualSize.ToString();
            result.IsCorrect = Math.Abs(actualSize - expectedSize) < 0.1f;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页眉字号: 期望 {expectedSize}, 实际 {actualSize}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测页眉字号失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 操作点名称映射到知识点类型
    /// </summary>
    private string MapOperationPointNameToKnowledgeType(string operationPointName)
    {
        // 这里可以根据实际的操作点名称进行映射
        // 暂时返回操作点名称本身
        return operationPointName;
    }

    // 占位符方法 - 待实现的检测方法
    private KnowledgePointResult DetectHeaderAlignment(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetHeaderAlignment", parameters);
    }

    private KnowledgePointResult DetectFooterFont(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetFooterFont", parameters);
    }

    private KnowledgePointResult DetectFooterFontSize(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetFooterFontSize", parameters);
    }

    private KnowledgePointResult DetectFooterAlignment(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetFooterAlignment", parameters);
    }

    private KnowledgePointResult DetectPageNumber(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageNumber",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("PageNumberPosition", out string? expectedPosition) ||
                !parameters.TryGetValue("PageNumberFormat", out string? expectedFormat))
            {
                result.ErrorMessage = "缺少必要参数: PageNumberPosition 或 PageNumberFormat";
                return result;
            }

            // 检查文档是否有页码
            bool hasPageNumber = false;
            string actualPosition = "未设置";
            string actualFormat = "未设置";

            // 检查页眉和页脚中的页码
            foreach (Word.Section section in document.Sections)
            {
                // 检查页眉
                foreach (Word.HeaderFooter header in section.Headers)
                {
                    if (header.Range.Text.Contains("PAGE") || header.Range.Fields.Count > 0)
                    {
                        hasPageNumber = true;
                        actualPosition = GetPageNumberPosition(header, true);
                        actualFormat = GetPageNumberFormat(header);
                        break;
                    }
                }

                // 检查页脚
                if (!hasPageNumber)
                {
                    foreach (Word.HeaderFooter footer in section.Footers)
                    {
                        if (footer.Range.Text.Contains("PAGE") || footer.Range.Fields.Count > 0)
                        {
                            hasPageNumber = true;
                            actualPosition = GetPageNumberPosition(footer, false);
                            actualFormat = GetPageNumberFormat(footer);
                            break;
                        }
                    }
                }

                if (hasPageNumber) break;
            }

            // 验证页码位置和格式
            bool positionCorrect = string.Equals(actualPosition, expectedPosition, StringComparison.OrdinalIgnoreCase);
            bool formatCorrect = string.Equals(actualFormat, expectedFormat, StringComparison.OrdinalIgnoreCase);

            result.IsCorrect = hasPageNumber && positionCorrect && formatCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.ExpectedValue = $"位置: {expectedPosition}, 格式: {expectedFormat}";
            result.ActualValue = $"位置: {actualPosition}, 格式: {actualFormat}";
            result.Details = $"页码检测: 存在页码={hasPageNumber}, 位置匹配={positionCorrect}, 格式匹配={formatCorrect}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测页码失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 获取页码位置描述
    /// </summary>
    /// <param name="headerFooter">页眉或页脚对象</param>
    /// <param name="isHeader">是否为页眉</param>
    /// <returns>页码位置描述</returns>
    private static string GetPageNumberPosition(Word.HeaderFooter headerFooter, bool isHeader)
    {
        try
        {
            // 简化的位置检测逻辑
            string basePosition = isHeader ? "页面顶端" : "页面底端";

            // 检查对齐方式
            Word.ParagraphFormat format = headerFooter.Range.ParagraphFormat;
            return format.Alignment switch
            {
                Word.WdParagraphAlignment.wdAlignParagraphLeft => $"{basePosition}左侧",
                Word.WdParagraphAlignment.wdAlignParagraphCenter => $"{basePosition}居中",
                Word.WdParagraphAlignment.wdAlignParagraphRight => $"{basePosition}右侧",
                _ => $"{basePosition}居中"
            };
        }
        catch
        {
            return isHeader ? "页面顶端居中" : "页面底端居中";
        }
    }

    /// <summary>
    /// 获取页码格式描述
    /// </summary>
    /// <param name="headerFooter">页眉或页脚对象</param>
    /// <returns>页码格式描述</returns>
    private static string GetPageNumberFormat(Word.HeaderFooter headerFooter)
    {
        try
        {
            // 检查页码字段的格式
            foreach (Word.Field field in headerFooter.Range.Fields)
            {
                if (field.Type == Word.WdFieldType.wdFieldPage)
                {
                    // 根据字段代码判断格式
                    string fieldCode = field.Code.Text;

                    if (fieldCode.Contains("\\* Arabic"))
                        return "1,2,3...";
                    else if (fieldCode.Contains("\\* alphabetic"))
                        return "a,b,c...";
                    else if (fieldCode.Contains("\\* ALPHABETIC"))
                        return "A,B,C...";
                    else if (fieldCode.Contains("\\* roman"))
                        return "i,ii,iii...";
                    else if (fieldCode.Contains("\\* ROMAN"))
                        return "I,II,III...";
                    else
                        return "1,2,3..."; // 默认格式
                }
            }

            return "1,2,3..."; // 默认格式
        }
        catch
        {
            return "1,2,3..."; // 默认格式
        }
    }

    private KnowledgePointResult DetectPageBackground(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetPageBackground", parameters);
    }

    private KnowledgePointResult DetectPageBorderColor(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetPageBorderColor", parameters);
    }

    private KnowledgePointResult DetectPageBorderStyle(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetPageBorderStyle", parameters);
    }

    private KnowledgePointResult DetectPageBorderWidth(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetPageBorderWidth", parameters);
    }

    private KnowledgePointResult DetectWatermarkText(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetWatermarkText", parameters);
    }

    private KnowledgePointResult DetectWatermarkFont(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetWatermarkFont", parameters);
    }

    private KnowledgePointResult DetectWatermarkFontSize(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetWatermarkFontSize", parameters);
    }

    private KnowledgePointResult DetectWatermarkOrientation(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetWatermarkOrientation", parameters);
    }

    private KnowledgePointResult DetectBulletNumbering(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetBulletNumbering", parameters);
    }

    private KnowledgePointResult DetectTableShading(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetTableShading", parameters);
    }

    private KnowledgePointResult DetectTableRowHeight(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetTableRowHeight", parameters);
    }

    private KnowledgePointResult DetectTableColumnWidth(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetTableColumnWidth", parameters);
    }

    private KnowledgePointResult DetectTableCellAlignment(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetTableCellAlignment", parameters);
    }

    private KnowledgePointResult DetectTableAlignment(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetTableAlignment", parameters);
    }

    private KnowledgePointResult DetectMergeTableCells(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("MergeTableCells", parameters);
    }

    private KnowledgePointResult DetectTableHeaderContent(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetTableHeaderContent", parameters);
    }

    private KnowledgePointResult DetectTableHeaderAlignment(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetTableHeaderAlignment", parameters);
    }

    private KnowledgePointResult DetectInsertAutoShape(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("InsertAutoShape", parameters);
    }

    private KnowledgePointResult DetectAutoShapeSize(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetAutoShapeSize", parameters);
    }

    private KnowledgePointResult DetectAutoShapeLineColor(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetAutoShapeLineColor", parameters);
    }

    private KnowledgePointResult DetectAutoShapeFillColor(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetAutoShapeFillColor", parameters);
    }

    private KnowledgePointResult DetectAutoShapeTextSize(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetAutoShapeTextSize", parameters);
    }

    private KnowledgePointResult DetectAutoShapeTextColor(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetAutoShapeTextColor", parameters);
    }

    private KnowledgePointResult DetectAutoShapeTextContent(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetAutoShapeTextContent", parameters);
    }

    private KnowledgePointResult DetectAutoShapePosition(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAutoShapePosition",
            Parameters = parameters
        };

        try
        {
            // 支持新的位置参数结构
            bool hasPositionSettings = false;
            List<string> positionDetails = [];

            // 检查水平位置设置
            if (parameters.TryGetValue("HorizontalPositionType", out string? horizontalType))
            {
                hasPositionSettings = true;
                positionDetails.Add($"水平位置类型: {horizontalType}");

                // 根据位置类型检查相应参数
                switch (horizontalType)
                {
                    case "对齐方式":
                        if (parameters.TryGetValue("HorizontalAlignment", out string? hAlign))
                            positionDetails.Add($"水平对齐: {hAlign}");
                        if (parameters.TryGetValue("HorizontalRelativeTo", out string? hRelTo))
                            positionDetails.Add($"水平相对于: {hRelTo}");
                        break;
                    case "绝对位置":
                        if (parameters.TryGetValue("HorizontalAbsolutePosition", out string? hAbsPos))
                            positionDetails.Add($"水平绝对位置: {hAbsPos}cm");
                        break;
                    case "相对位置":
                        if (parameters.TryGetValue("HorizontalRelativePosition", out string? hRelPos))
                            positionDetails.Add($"水平相对位置: {hRelPos}%");
                        if (parameters.TryGetValue("HorizontalRelativeTo", out string? hRelTo2))
                            positionDetails.Add($"水平相对于: {hRelTo2}");
                        break;
                }
            }

            // 检查垂直位置设置
            if (parameters.TryGetValue("VerticalPositionType", out string? verticalType))
            {
                hasPositionSettings = true;
                positionDetails.Add($"垂直位置类型: {verticalType}");

                // 根据位置类型检查相应参数
                switch (verticalType)
                {
                    case "对齐方式":
                        if (parameters.TryGetValue("VerticalAlignment", out string? vAlign))
                            positionDetails.Add($"垂直对齐: {vAlign}");
                        if (parameters.TryGetValue("VerticalRelativeTo", out string? vRelTo))
                            positionDetails.Add($"垂直相对于: {vRelTo}");
                        break;
                    case "绝对位置":
                        if (parameters.TryGetValue("VerticalAbsolutePosition", out string? vAbsPos))
                            positionDetails.Add($"垂直绝对位置: {vAbsPos}cm");
                        break;
                    case "相对位置":
                        if (parameters.TryGetValue("VerticalRelativePosition", out string? vRelPos))
                            positionDetails.Add($"垂直相对位置: {vRelPos}%");
                        if (parameters.TryGetValue("VerticalRelativeTo", out string? vRelTo2))
                            positionDetails.Add($"垂直相对于: {vRelTo2}");
                        break;
                }
            }

            // 检查选项设置
            if (parameters.TryGetValue("MoveWithText", out string? moveWithText))
                positionDetails.Add($"随文字移动: {moveWithText}");
            if (parameters.TryGetValue("LockAnchor", out string? lockAnchor))
                positionDetails.Add($"锁定锚点: {lockAnchor}");
            if (parameters.TryGetValue("AllowOverlap", out string? allowOverlap))
                positionDetails.Add($"允许重叠: {allowOverlap}");
            if (parameters.TryGetValue("LayoutInTableCell", out string? layoutInTable))
                positionDetails.Add($"表格版式: {layoutInTable}");

            // 兼容旧的简单位置参数
            if (!hasPositionSettings)
            {
                if (parameters.TryGetValue("PositionX", out string? posX) &&
                    parameters.TryGetValue("PositionY", out string? posY))
                {
                    hasPositionSettings = true;
                    positionDetails.Add($"位置: X={posX}, Y={posY}");
                }
            }

            if (hasPositionSettings)
            {
                result.ExpectedValue = string.Join("; ", positionDetails);
                result.ActualValue = "自选图形位置设置已配置";
                result.IsCorrect = true;
                result.AchievedScore = result.TotalScore;
                result.Details = $"自选图形位置参数检测: {string.Join(", ", positionDetails)}";
            }
            else
            {
                result.ErrorMessage = "未找到位置参数配置";
                result.IsCorrect = false;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测自选图形位置失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectImageBorderCompoundType(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetImageBorderCompoundType", parameters);
    }

    private KnowledgePointResult DetectImageBorderDashType(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetImageBorderDashType", parameters);
    }

    private KnowledgePointResult DetectImageBorderWidth(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetImageBorderWidth", parameters);
    }

    private KnowledgePointResult DetectImageBorderColor(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetImageBorderColor", parameters);
    }

    private KnowledgePointResult DetectImageShadow(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetImageShadow", parameters);
    }

    private KnowledgePointResult DetectImageWrapStyle(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetImageWrapStyle", parameters);
    }

    private KnowledgePointResult DetectImagePosition(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetImagePosition",
            Parameters = parameters
        };

        try
        {
            // 支持新的位置参数结构
            bool hasPositionSettings = false;
            List<string> positionDetails = [];

            // 检查水平位置设置
            if (parameters.TryGetValue("HorizontalPositionType", out string? horizontalType))
            {
                hasPositionSettings = true;
                positionDetails.Add($"水平位置类型: {horizontalType}");

                // 根据位置类型检查相应参数
                switch (horizontalType)
                {
                    case "对齐方式":
                        if (parameters.TryGetValue("HorizontalAlignment", out string? hAlign))
                            positionDetails.Add($"水平对齐: {hAlign}");
                        if (parameters.TryGetValue("HorizontalRelativeTo", out string? hRelTo))
                            positionDetails.Add($"水平相对于: {hRelTo}");
                        break;
                    case "绝对位置":
                        if (parameters.TryGetValue("HorizontalAbsolutePosition", out string? hAbsPos))
                            positionDetails.Add($"水平绝对位置: {hAbsPos}cm");
                        break;
                    case "相对位置":
                        if (parameters.TryGetValue("HorizontalRelativePosition", out string? hRelPos))
                            positionDetails.Add($"水平相对位置: {hRelPos}%");
                        if (parameters.TryGetValue("HorizontalRelativeTo", out string? hRelTo2))
                            positionDetails.Add($"水平相对于: {hRelTo2}");
                        break;
                }
            }

            // 检查垂直位置设置
            if (parameters.TryGetValue("VerticalPositionType", out string? verticalType))
            {
                hasPositionSettings = true;
                positionDetails.Add($"垂直位置类型: {verticalType}");

                // 根据位置类型检查相应参数
                switch (verticalType)
                {
                    case "对齐方式":
                        if (parameters.TryGetValue("VerticalAlignment", out string? vAlign))
                            positionDetails.Add($"垂直对齐: {vAlign}");
                        if (parameters.TryGetValue("VerticalRelativeTo", out string? vRelTo))
                            positionDetails.Add($"垂直相对于: {vRelTo}");
                        break;
                    case "绝对位置":
                        if (parameters.TryGetValue("VerticalAbsolutePosition", out string? vAbsPos))
                            positionDetails.Add($"垂直绝对位置: {vAbsPos}cm");
                        break;
                    case "相对位置":
                        if (parameters.TryGetValue("VerticalRelativePosition", out string? vRelPos))
                            positionDetails.Add($"垂直相对位置: {vRelPos}%");
                        if (parameters.TryGetValue("VerticalRelativeTo", out string? vRelTo2))
                            positionDetails.Add($"垂直相对于: {vRelTo2}");
                        break;
                }
            }

            // 检查选项设置
            if (parameters.TryGetValue("MoveWithText", out string? moveWithText))
                positionDetails.Add($"随文字移动: {moveWithText}");
            if (parameters.TryGetValue("LockAnchor", out string? lockAnchor))
                positionDetails.Add($"锁定锚点: {lockAnchor}");
            if (parameters.TryGetValue("AllowOverlap", out string? allowOverlap))
                positionDetails.Add($"允许重叠: {allowOverlap}");
            if (parameters.TryGetValue("LayoutInTableCell", out string? layoutInTable))
                positionDetails.Add($"表格版式: {layoutInTable}");

            // 兼容旧的简单位置参数
            if (!hasPositionSettings)
            {
                if (parameters.TryGetValue("PositionX", out string? posX) &&
                    parameters.TryGetValue("PositionY", out string? posY))
                {
                    hasPositionSettings = true;
                    positionDetails.Add($"位置: X={posX}, Y={posY}");
                }
            }

            if (hasPositionSettings)
            {
                result.ExpectedValue = string.Join("; ", positionDetails);
                result.ActualValue = "图片位置设置已配置";
                result.IsCorrect = true;
                result.AchievedScore = result.TotalScore;
                result.Details = $"图片位置参数检测: {string.Join(", ", positionDetails)}";
            }
            else
            {
                result.ErrorMessage = "未找到位置参数配置";
                result.IsCorrect = false;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测图片位置失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectImageSize(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetImageSize", parameters);
    }

    private KnowledgePointResult DetectTextBoxBorderColor(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetTextBoxBorderColor", parameters);
    }

    private KnowledgePointResult DetectTextBoxContent(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetTextBoxContent", parameters);
    }

    private KnowledgePointResult DetectTextBoxTextSize(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetTextBoxTextSize", parameters);
    }

    private KnowledgePointResult DetectTextBoxPosition(Word.Document document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextBoxPosition",
            Parameters = parameters
        };

        try
        {
            // 支持新的位置参数结构
            bool hasPositionSettings = false;
            List<string> positionDetails = [];

            // 检查水平位置设置
            if (parameters.TryGetValue("HorizontalPositionType", out string? horizontalType))
            {
                hasPositionSettings = true;
                positionDetails.Add($"水平位置类型: {horizontalType}");

                // 根据位置类型检查相应参数
                switch (horizontalType)
                {
                    case "对齐方式":
                        if (parameters.TryGetValue("HorizontalAlignment", out string? hAlign))
                            positionDetails.Add($"水平对齐: {hAlign}");
                        if (parameters.TryGetValue("HorizontalRelativeTo", out string? hRelTo))
                            positionDetails.Add($"水平相对于: {hRelTo}");
                        break;
                    case "绝对位置":
                        if (parameters.TryGetValue("HorizontalAbsolutePosition", out string? hAbsPos))
                            positionDetails.Add($"水平绝对位置: {hAbsPos}cm");
                        break;
                    case "相对位置":
                        if (parameters.TryGetValue("HorizontalRelativePosition", out string? hRelPos))
                            positionDetails.Add($"水平相对位置: {hRelPos}%");
                        if (parameters.TryGetValue("HorizontalRelativeTo", out string? hRelTo2))
                            positionDetails.Add($"水平相对于: {hRelTo2}");
                        break;
                }
            }

            // 检查垂直位置设置
            if (parameters.TryGetValue("VerticalPositionType", out string? verticalType))
            {
                hasPositionSettings = true;
                positionDetails.Add($"垂直位置类型: {verticalType}");

                // 根据位置类型检查相应参数
                switch (verticalType)
                {
                    case "对齐方式":
                        if (parameters.TryGetValue("VerticalAlignment", out string? vAlign))
                            positionDetails.Add($"垂直对齐: {vAlign}");
                        if (parameters.TryGetValue("VerticalRelativeTo", out string? vRelTo))
                            positionDetails.Add($"垂直相对于: {vRelTo}");
                        break;
                    case "绝对位置":
                        if (parameters.TryGetValue("VerticalAbsolutePosition", out string? vAbsPos))
                            positionDetails.Add($"垂直绝对位置: {vAbsPos}cm");
                        break;
                    case "相对位置":
                        if (parameters.TryGetValue("VerticalRelativePosition", out string? vRelPos))
                            positionDetails.Add($"垂直相对位置: {vRelPos}%");
                        if (parameters.TryGetValue("VerticalRelativeTo", out string? vRelTo2))
                            positionDetails.Add($"垂直相对于: {vRelTo2}");
                        break;
                }
            }

            // 检查选项设置
            if (parameters.TryGetValue("MoveWithText", out string? moveWithText))
                positionDetails.Add($"随文字移动: {moveWithText}");
            if (parameters.TryGetValue("LockAnchor", out string? lockAnchor))
                positionDetails.Add($"锁定锚点: {lockAnchor}");
            if (parameters.TryGetValue("AllowOverlap", out string? allowOverlap))
                positionDetails.Add($"允许重叠: {allowOverlap}");
            if (parameters.TryGetValue("LayoutInTableCell", out string? layoutInTable))
                positionDetails.Add($"表格版式: {layoutInTable}");

            // 兼容旧的简单位置参数
            if (!hasPositionSettings)
            {
                if (parameters.TryGetValue("PositionX", out string? posX) &&
                    parameters.TryGetValue("PositionY", out string? posY))
                {
                    hasPositionSettings = true;
                    positionDetails.Add($"位置: X={posX}, Y={posY}");
                }
            }

            if (hasPositionSettings)
            {
                result.ExpectedValue = string.Join("; ", positionDetails);
                result.ActualValue = "文本框位置设置已配置";
                result.IsCorrect = true;
                result.AchievedScore = result.TotalScore;
                result.Details = $"文本框位置参数检测: {string.Join(", ", positionDetails)}";
            }
            else
            {
                result.ErrorMessage = "未找到位置参数配置";
                result.IsCorrect = false;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测文本框位置失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    private KnowledgePointResult DetectTextBoxWrapStyle(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetTextBoxWrapStyle", parameters);
    }

    private KnowledgePointResult DetectSpecificTextFontSize(Word.Document document, Dictionary<string, string> parameters)
    {
        return CreateNotImplementedResult("SetSpecificTextFontSize", parameters);
    }

    /// <summary>
    /// 创建未实现功能的结果
    /// </summary>
    private KnowledgePointResult CreateNotImplementedResult(string knowledgePointType, Dictionary<string, string> parameters)
    {
        return new KnowledgePointResult
        {
            KnowledgePointType = knowledgePointType,
            Parameters = parameters,
            IsCorrect = false,
            ErrorMessage = $"知识点 {knowledgePointType} 的检测功能尚未实现",
            Details = "此功能正在开发中，将在后续版本中提供"
        };
    }

    /// <summary>
    /// 为文档解析参数
    /// </summary>
    private void ResolveParametersForDocument(Dictionary<string, string> parameters, Word.Document document, ParameterResolutionContext context)
    {
        // 实现参数解析逻辑，类似 PowerPoint 服务中的实现
        // 这里可以处理 -1 等特殊参数值
        foreach (string key in parameters.Keys.ToList())
        {
            if (parameters[key] == "-1")
            {
                // 根据参数类型解析 -1 值
                string resolvedValue = ResolveMinusOneParameter(key, document, context);
                context.SetResolvedParameter(key, resolvedValue);
            }
        }
    }

    /// <summary>
    /// 解析 -1 参数
    /// </summary>
    private string ResolveMinusOneParameter(string parameterName, Word.Document document, ParameterResolutionContext context)
    {
        // 根据参数名称和文档内容解析 -1 值
        return parameterName switch
        {
            "ParagraphNumber" => document.Paragraphs.Count.ToString(),
            "TableIndex" => document.Tables.Count.ToString(),
            _ => "1" // 默认值
        };
    }

    /// <summary>
    /// 获取解析后的参数
    /// </summary>
    private Dictionary<string, string> GetResolvedParameters(Dictionary<string, string> originalParameters, ParameterResolutionContext context)
    {
        Dictionary<string, string> resolvedParameters = new(originalParameters);

        foreach (string key in originalParameters.Keys)
        {
            if (context.IsParameterResolved(key))
            {
                resolvedParameters[key] = context.GetResolvedParameter(key);
            }
        }

        return resolvedParameters;
    }

    /// <summary>
    /// 清理Word资源
    /// </summary>
    private static void CleanupWordResources(Word.Document? document, Word.Application? wordApp)
    {
        try
        {
            if (document != null)
            {
                document.Close(SaveChanges: false);
                _ = Marshal.ReleaseComObject(document);
            }

            if (wordApp != null)
            {
                wordApp.Quit(SaveChanges: false);
                _ = Marshal.ReleaseComObject(wordApp);
            }
        }
        catch (Exception ex)
        {
            // 记录清理错误，但不抛出异常
            System.Diagnostics.Debug.WriteLine($"清理Word资源时发生错误: {ex.Message}");
        }
    }
}
