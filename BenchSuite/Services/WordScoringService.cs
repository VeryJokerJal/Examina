using System.Runtime.InteropServices;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Word;
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
            Dictionary<string, string> operationPointToQuestionMap = new();

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
                wordApp = new Word.Application();
                wordApp.Visible = false;

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
                wordApp = new Word.Application();
                wordApp.Visible = false;

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
                // 段落操作类
                case "SetParagraphFont":
                    result = DetectParagraphFont(document, parameters);
                    break;
                case "SetParagraphFontSize":
                    result = DetectParagraphFontSize(document, parameters);
                    break;
                case "SetParagraphFontStyle":
                    result = DetectParagraphFontStyle(document, parameters);
                    break;
                case "SetParagraphAlignment":
                    result = DetectParagraphAlignment(document, parameters);
                    break;
                case "SetParagraphTextColor":
                    result = DetectParagraphTextColor(document, parameters);
                    break;

                // 页面设置类
                case "SetPaperSize":
                    result = DetectPaperSize(document, parameters);
                    break;
                case "SetPageMargins":
                    result = DetectPageMargins(document, parameters);
                    break;
                case "SetHeaderText":
                    result = DetectHeaderText(document, parameters);
                    break;
                case "SetFooterText":
                    result = DetectFooterText(document, parameters);
                    break;

                // 表格操作类
                case "SetTableRowsColumns":
                    result = DetectTableRowsColumns(document, parameters);
                    break;
                case "SetTableCellContent":
                    result = DetectTableCellContent(document, parameters);
                    break;

                // 其他操作类
                case "FindAndReplace":
                    result = DetectFindAndReplace(document, parameters);
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
    /// 获取字体样式描述
    /// </summary>
    private string GetFontStyleDescription(Word.Font font)
    {
        List<string> styles = [];

        if ((int)font.Bold == 1) styles.Add("加粗");
        if ((int)font.Italic == 1) styles.Add("斜体");
        if (font.Underline != Word.WdUnderline.wdUnderlineNone) styles.Add("下划线");
        if ((int)font.StrikeThrough == 1) styles.Add("删除线");

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
            if (color is int colorValue)
            {
                return $"#{colorValue:X6}";
            }
            return color.ToString() ?? "未知颜色";
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
    /// 操作点名称映射到知识点类型
    /// </summary>
    private string MapOperationPointNameToKnowledgeType(string operationPointName)
    {
        // 这里可以根据实际的操作点名称进行映射
        // 暂时返回操作点名称本身
        return operationPointName;
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
            if (context.HasResolvedParameter(key))
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
                Marshal.ReleaseComObject(document);
            }

            if (wordApp != null)
            {
                wordApp.Quit(SaveChanges: false);
                Marshal.ReleaseComObject(wordApp);
            }
        }
        catch (Exception ex)
        {
            // 记录清理错误，但不抛出异常
            System.Diagnostics.Debug.WriteLine($"清理Word资源时发生错误: {ex.Message}");
        }
    }
}
