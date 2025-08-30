using BenchSuite.Interfaces;
using BenchSuite.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;

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
                result.ErrorMessage = $"无效的Word文档: {filePath}";
                return result;
            }

            // 获取Word模块
            ExamModuleModel? wordModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.Word);
            if (wordModule == null)
            {
                result.ErrorMessage = "试卷中未找到Word模块，跳过Word评分";
                result.IsSuccess = true;
                result.TotalScore = 0;
                result.AchievedScore = 0;
                result.KnowledgePointResults = [];
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

            FinalizeScoringResult(result, allOperationPoints);
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
                result.ErrorMessage = $"无效的Word文档: {filePath}";
                return result;
            }

            // 获取题目的操作点（只处理Word相关的操作点）
            List<OperationPointModel> wordOperationPoints = [.. question.OperationPoints.Where(op => op.ModuleType == ModuleType.Word && op.IsEnabled)];

            if (wordOperationPoints.Count == 0)
            {
                result.ErrorMessage = "题目没有包含任何Word操作点";
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
                    result.ErrorMessage = "无效的Word文档";
                    return result;
                }

                using WordprocessingDocument document = WordprocessingDocument.Open(filePath, false);
                result = DetectSpecificKnowledgePoint(document, knowledgePointType, parameters);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"检测知识点时发生错误: {ex.Message}";
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
    /// 映射Word操作点名称到知识点类型
    /// </summary>
    protected override string MapOperationPointNameToKnowledgeType(string operationPointName)
    {
        return operationPointName switch
        {
            // Word特定映射
            var name when name.Contains("DocumentContent") => "SetDocumentContent",
            var name when name.Contains("DocumentFont") => "SetDocumentFont",
            var name when name.Contains("FontStyle") => "SetFontStyle",
            var name when name.Contains("FontSize") => "SetFontSize",
            var name when name.Contains("FontColor") => "SetFontColor",
            var name when name.Contains("ParagraphAlignment") => "SetParagraphAlignment",
            var name when name.Contains("LineSpacing") => "SetLineSpacing",
            var name when name.Contains("ParagraphSpacing") => "SetParagraphSpacing",
            var name when name.Contains("Indent") => "SetIndentation",
            var name when name.Contains("BulletList") => "CreateBulletList",
            var name when name.Contains("NumberedList") => "CreateNumberedList",
            var name when name.Contains("InsertTable") => "InsertTable",
            var name when name.Contains("TableStyle") => "SetTableStyle",
            var name when name.Contains("TableBorder") => "SetTableBorder",
            var name when name.Contains("InsertImage") => "InsertImage",
            var name when name.Contains("ImagePosition") => "SetImagePosition",
            var name when name.Contains("ImageSize") => "SetImageSize",
            var name when name.Contains("HeaderFooter") => "SetHeaderFooter",
            var name when name.Contains("PageNumber") => "SetPageNumber",
            var name when name.Contains("PageMargin") => "SetPageMargin",
            var name when name.Contains("PageOrientation") => "SetPageOrientation",
            var name when name.Contains("PageSize") => "SetPageSize",
            var name when name.Contains("Section") => "ManageSection",
            var name when name.Contains("PageBreak") => "InsertPageBreak",
            var name when name.Contains("ColumnBreak") => "InsertColumnBreak",
            var name when name.Contains("Hyperlink") => "InsertHyperlink",
            var name when name.Contains("Bookmark") => "InsertBookmark",
            var name when name.Contains("CrossReference") => "InsertCrossReference",
            var name when name.Contains("TableOfContents") => "InsertTableOfContents",
            var name when name.Contains("Footnote") => "InsertFootnote",
            var name when name.Contains("Endnote") => "InsertEndnote",
            var name when name.Contains("Comment") => "InsertComment",
            var name when name.Contains("TrackChanges") => "EnableTrackChanges",
            var name when name.Contains("Protection") => "SetDocumentProtection",
            var name when name.Contains("Watermark") => "SetWatermark",
            var name when name.Contains("Background") => "SetPageBackground",
            var name when name.Contains("Border") => "SetPageBorder",
            var name when name.Contains("Style") => "ApplyStyle",
            var name when name.Contains("Template") => "ApplyTemplate",
            _ => base.MapOperationPointNameToKnowledgeType(operationPointName)
        };
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
                case "SetDocumentContent":
                    result = DetectDocumentContent(document, parameters);
                    break;
                case "SetDocumentFont":
                    result = DetectDocumentFont(document, parameters);
                    break;
                case "SetFontStyle":
                    result = DetectFontStyle(document, parameters);
                    break;
                case "SetFontSize":
                    result = DetectFontSize(document, parameters);
                    break;
                case "SetFontColor":
                    result = DetectFontColor(document, parameters);
                    break;
                case "SetParagraphAlignment":
                    result = DetectParagraphAlignment(document, parameters);
                    break;
                case "SetLineSpacing":
                    result = DetectLineSpacing(document, parameters);
                    break;
                case "SetParagraphSpacing":
                    result = DetectParagraphSpacing(document, parameters);
                    break;
                case "SetIndentation":
                    result = DetectIndentation(document, parameters);
                    break;
                case "CreateBulletList":
                    result = DetectBulletList(document, parameters);
                    break;
                case "CreateNumberedList":
                    result = DetectNumberedList(document, parameters);
                    break;
                case "InsertTable":
                    result = DetectInsertedTable(document, parameters);
                    break;
                case "SetTableStyle":
                    result = DetectTableStyle(document, parameters);
                    break;
                case "SetTableBorder":
                    result = DetectTableBorder(document, parameters);
                    break;
                case "InsertImage":
                    result = DetectInsertedImage(document, parameters);
                    break;
                case "SetImagePosition":
                    result = DetectImagePosition(document, parameters);
                    break;
                case "SetImageSize":
                    result = DetectImageSize(document, parameters);
                    break;
                case "SetHeaderFooter":
                    result = DetectHeaderFooter(document, parameters);
                    break;
                case "SetPageNumber":
                    result = DetectPageNumber(document, parameters);
                    break;
                case "SetPageMargin":
                    result = DetectPageMargin(document, parameters);
                    break;
                case "SetPageOrientation":
                    result = DetectPageOrientation(document, parameters);
                    break;
                case "SetPageSize":
                    result = DetectPageSize(document, parameters);
                    break;
                case "ManageSection":
                    result = DetectManageSection(document, parameters);
                    break;
                case "InsertPageBreak":
                    result = DetectPageBreak(document, parameters);
                    break;
                case "InsertColumnBreak":
                    result = DetectColumnBreak(document, parameters);
                    break;
                case "InsertHyperlink":
                    result = DetectHyperlink(document, parameters);
                    break;
                case "InsertBookmark":
                    result = DetectBookmark(document, parameters);
                    break;
                case "InsertCrossReference":
                    result = DetectCrossReference(document, parameters);
                    break;
                case "InsertTableOfContents":
                    result = DetectTableOfContents(document, parameters);
                    break;
                case "InsertFootnote":
                    result = DetectFootnote(document, parameters);
                    break;
                case "InsertEndnote":
                    result = DetectEndnote(document, parameters);
                    break;
                case "InsertComment":
                    result = DetectComment(document, parameters);
                    break;
                case "EnableTrackChanges":
                    result = DetectTrackChanges(document, parameters);
                    break;
                case "SetDocumentProtection":
                    result = DetectDocumentProtection(document, parameters);
                    break;
                case "SetWatermark":
                    result = DetectWatermark(document, parameters);
                    break;
                case "SetPageBackground":
                    result = DetectPageBackground(document, parameters);
                    break;
                case "SetPageBorder":
                    result = DetectPageBorder(document, parameters);
                    break;
                case "ApplyStyle":
                    result = DetectAppliedStyle(document, parameters);
                    break;
                case "ApplyTemplate":
                    result = DetectAppliedTemplate(document, parameters);
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
    /// 检测文档内容
    /// </summary>
    private KnowledgePointResult DetectDocumentContent(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetDocumentContent",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "ExpectedContent", out string expectedContent))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ExpectedContent");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            string actualContent = GetDocumentText(mainPart);

            result.ExpectedValue = expectedContent;
            result.ActualValue = actualContent;
            result.IsCorrect = TextContains(actualContent, expectedContent);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"文档内容检测: 期望包含 '{expectedContent}', 实际内容长度 {actualContent.Length} 字符";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文档内容失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测文档字体
    /// </summary>
    private KnowledgePointResult DetectDocumentFont(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetDocumentFont",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FontName", out string expectedFont))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FontName");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool fontFound = CheckDocumentForFont(mainPart, expectedFont);

            result.ExpectedValue = expectedFont;
            result.ActualValue = fontFound ? expectedFont : "未找到指定字体";
            result.IsCorrect = fontFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"文档字体检测: 期望 {expectedFont}, {(fontFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文档字体失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 获取文档文本内容
    /// </summary>
    private string GetDocumentText(MainDocumentPart mainPart)
    {
        try
        {
            Body? body = mainPart.Document.Body;
            if (body == null) return string.Empty;

            return body.InnerText;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 检查文档中是否包含指定字体
    /// </summary>
    private bool CheckDocumentForFont(MainDocumentPart mainPart, string expectedFont)
    {
        try
        {
            Body? body = mainPart.Document.Body;
            if (body == null) return false;

            var runProperties = body.Descendants<RunProperties>();
            foreach (var runProp in runProperties)
            {
                var runFonts = runProp.Elements<RunFonts>().FirstOrDefault();
                if (runFonts?.Ascii?.Value != null && TextEquals(runFonts.Ascii.Value, expectedFont))
                {
                    return true;
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
    /// 检测插入的表格
    /// </summary>
    private KnowledgePointResult DetectInsertedTable(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertTable",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            Body? body = mainPart.Document.Body;

            if (body == null)
            {
                SetKnowledgePointFailure(result, "无法获取文档主体");
                return result;
            }

            var tables = body.Descendants<Table>();
            int tableCount = tables.Count();

            // 检查期望的表格数量
            bool hasExpectedCount = true;
            if (TryGetIntParameter(parameters, "ExpectedTableCount", out int expectedCount))
            {
                hasExpectedCount = tableCount >= expectedCount;
                result.ExpectedValue = $"至少{expectedCount}个表格";
            }
            else
            {
                hasExpectedCount = tableCount > 0;
                result.ExpectedValue = "至少1个表格";
            }

            result.ActualValue = $"{tableCount}个表格";
            result.IsCorrect = hasExpectedCount;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表格检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测插入表格失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入的图片
    /// </summary>
    private KnowledgePointResult DetectInsertedImage(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertImage",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;

            // 计算图片数量（通过ImagePart计算）
            int imageCount = mainPart.ImageParts.Count();

            // 检查期望的图片数量
            bool hasExpectedCount = true;
            if (TryGetIntParameter(parameters, "ExpectedImageCount", out int expectedCount))
            {
                hasExpectedCount = imageCount >= expectedCount;
                result.ExpectedValue = $"至少{expectedCount}张图片";
            }
            else
            {
                hasExpectedCount = imageCount > 0;
                result.ExpectedValue = "至少1张图片";
            }

            result.ActualValue = $"{imageCount}张图片";
            result.IsCorrect = hasExpectedCount;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测插入图片失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测字体样式
    /// </summary>
    private KnowledgePointResult DetectFontStyle(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFontStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "StyleType", out string expectedStyle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: StyleType");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool styleFound = CheckFontStyleInDocument(mainPart, expectedStyle);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = styleFound ? expectedStyle : "未找到指定样式";
            result.IsCorrect = styleFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"字体样式检测: 期望 {expectedStyle}, {(styleFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测字体样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测字体大小
    /// </summary>
    private KnowledgePointResult DetectFontSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFontSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FontSize", out string expectedSize))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FontSize");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool sizeFound = CheckFontSizeInDocument(mainPart, expectedSize);

            result.ExpectedValue = expectedSize;
            result.ActualValue = sizeFound ? expectedSize : "未找到指定字号";
            result.IsCorrect = sizeFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"字体大小检测: 期望 {expectedSize}, {(sizeFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测字体大小失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测字体颜色
    /// </summary>
    private KnowledgePointResult DetectFontColor(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFontColor",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FontColor", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FontColor");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool colorFound = CheckFontColorInDocument(mainPart, expectedColor);

            result.ExpectedValue = expectedColor;
            result.ActualValue = colorFound ? expectedColor : "未找到指定颜色";
            result.IsCorrect = colorFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"字体颜色检测: 期望 {expectedColor}, {(colorFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测字体颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落对齐
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
            if (!TryGetParameter(parameters, "Alignment", out string expectedAlignment))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: Alignment");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool alignmentFound = CheckParagraphAlignmentInDocument(mainPart, expectedAlignment);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = alignmentFound ? expectedAlignment : "未找到指定对齐方式";
            result.IsCorrect = alignmentFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落对齐检测: 期望 {expectedAlignment}, {(alignmentFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落对齐失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测行间距
    /// </summary>
    private KnowledgePointResult DetectLineSpacing(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetLineSpacing",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "LineSpacing", out string expectedSpacing))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: LineSpacing");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool spacingFound = CheckLineSpacingInDocument(mainPart, expectedSpacing);

            result.ExpectedValue = expectedSpacing;
            result.ActualValue = spacingFound ? expectedSpacing : "未找到指定行间距";
            result.IsCorrect = spacingFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"行间距检测: 期望 {expectedSpacing}, {(spacingFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测行间距失败: {ex.Message}");
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
            if (!TryGetParameter(parameters, "ParagraphSpacing", out string expectedSpacing))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ParagraphSpacing");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool spacingFound = CheckParagraphSpacingInDocument(mainPart, expectedSpacing);

            result.ExpectedValue = expectedSpacing;
            result.ActualValue = spacingFound ? expectedSpacing : "未找到指定段落间距";
            result.IsCorrect = spacingFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落间距检测: 期望 {expectedSpacing}, {(spacingFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落间距失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测缩进
    /// </summary>
    private KnowledgePointResult DetectIndentation(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetIndentation",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "IndentationType", out string indentationType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: IndentationType");
                return result;
            }

            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool indentationFound = CheckIndentationInDocument(mainPart, indentationType, parameters);

            result.ExpectedValue = indentationType;
            result.ActualValue = indentationFound ? indentationType : "未找到指定缩进";
            result.IsCorrect = indentationFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"缩进检测: 期望 {indentationType}, {(indentationFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测缩进失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测项目符号列表
    /// </summary>
    private KnowledgePointResult DetectBulletList(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "CreateBulletList",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool bulletListFound = CheckBulletListInDocument(mainPart);

            result.ExpectedValue = "项目符号列表";
            result.ActualValue = bulletListFound ? "找到项目符号列表" : "未找到项目符号列表";
            result.IsCorrect = bulletListFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"项目符号列表检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测项目符号列表失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测编号列表
    /// </summary>
    private KnowledgePointResult DetectNumberedList(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "CreateNumberedList",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool numberedListFound = CheckNumberedListInDocument(mainPart);

            result.ExpectedValue = "编号列表";
            result.ActualValue = numberedListFound ? "找到编号列表" : "未找到编号列表";
            result.IsCorrect = numberedListFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"编号列表检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测编号列表失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表格样式
    /// </summary>
    private KnowledgePointResult DetectTableStyle(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableStyle",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            string tableStyle = GetTableStyleInDocument(mainPart);
            bool hasCustomStyle = !string.IsNullOrEmpty(tableStyle) && !tableStyle.Equals("默认样式");

            string expectedStyle = TryGetParameter(parameters, "TableStyle", out string expected) ? expected : "";

            result.ExpectedValue = string.IsNullOrEmpty(expectedStyle) ? "自定义表格样式" : expectedStyle;
            result.ActualValue = tableStyle;
            result.IsCorrect = hasCustomStyle && (string.IsNullOrEmpty(expectedStyle) || TextEquals(tableStyle, expectedStyle));
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表格样式检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表格样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表格边框
    /// </summary>
    private KnowledgePointResult DetectTableBorder(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableBorder",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasBorder = CheckTableBorderInDocument(mainPart);

            result.ExpectedValue = "表格边框";
            result.ActualValue = hasBorder ? "找到表格边框" : "未找到表格边框";
            result.IsCorrect = hasBorder;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表格边框检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表格边框失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图片位置
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
            var imageInfo = GetImagePositionInDocument(mainPart, parameters);

            result.ExpectedValue = "图片位置设置";
            result.ActualValue = imageInfo.Found ? $"位置: {imageInfo.Position}" : "未找到图片位置信息";
            result.IsCorrect = imageInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片位置检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图片位置失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测图片大小
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
            MainDocumentPart mainPart = document.MainDocumentPart!;
            var imageInfo = GetImageSizeInDocument(mainPart, parameters);

            result.ExpectedValue = "图片大小设置";
            result.ActualValue = imageInfo.Found ? $"大小: {imageInfo.Width}x{imageInfo.Height}" : "未找到图片大小信息";
            result.IsCorrect = imageInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片大小检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测图片大小失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页眉页脚
    /// </summary>
    private KnowledgePointResult DetectHeaderFooter(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetHeaderFooter",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            var headerFooterInfo = GetHeaderFooterInDocument(mainPart);

            result.ExpectedValue = "页眉或页脚";
            result.ActualValue = headerFooterInfo.HasHeader || headerFooterInfo.HasFooter ?
                $"页眉: {(headerFooterInfo.HasHeader ? "有" : "无")}, 页脚: {(headerFooterInfo.HasFooter ? "有" : "无")}" :
                "无页眉页脚";
            result.IsCorrect = headerFooterInfo.HasHeader || headerFooterInfo.HasFooter;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页眉页脚检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页眉页脚失败: {ex.Message}");
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
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasPageNumber = CheckPageNumberInDocument(mainPart);

            result.ExpectedValue = "页码";
            result.ActualValue = hasPageNumber ? "找到页码" : "未找到页码";
            result.IsCorrect = hasPageNumber;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页码检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页码失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页边距
    /// </summary>
    private KnowledgePointResult DetectPageMargin(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageMargin",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            var marginInfo = GetPageMarginInDocument(mainPart);

            result.ExpectedValue = "自定义页边距";
            result.ActualValue = marginInfo.HasCustomMargin ? $"上:{marginInfo.Top}, 下:{marginInfo.Bottom}, 左:{marginInfo.Left}, 右:{marginInfo.Right}" : "默认页边距";
            result.IsCorrect = marginInfo.HasCustomMargin;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页边距检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页边距失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页面方向
    /// </summary>
    private KnowledgePointResult DetectPageOrientation(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageOrientation",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            string orientation = GetPageOrientationInDocument(mainPart);
            string expectedOrientation = TryGetParameter(parameters, "Orientation", out string expected) ? expected : "";

            result.ExpectedValue = string.IsNullOrEmpty(expectedOrientation) ? "页面方向设置" : expectedOrientation;
            result.ActualValue = orientation;
            result.IsCorrect = !string.IsNullOrEmpty(orientation) && (string.IsNullOrEmpty(expectedOrientation) || TextEquals(orientation, expectedOrientation));
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页面方向检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页面方向失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页面大小
    /// </summary>
    private KnowledgePointResult DetectPageSize(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageSize",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            var sizeInfo = GetPageSizeInDocument(mainPart);

            result.ExpectedValue = "自定义页面大小";
            result.ActualValue = sizeInfo.HasCustomSize ? $"宽:{sizeInfo.Width}, 高:{sizeInfo.Height}" : "默认页面大小";
            result.IsCorrect = sizeInfo.HasCustomSize;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页面大小检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页面大小失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测节管理
    /// </summary>
    private KnowledgePointResult DetectManageSection(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ManageSection",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            int sectionCount = GetSectionCountInDocument(mainPart);

            result.ExpectedValue = "多个节";
            result.ActualValue = $"文档包含 {sectionCount} 个节";
            result.IsCorrect = sectionCount > 1;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"节管理检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测节管理失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测分页符
    /// </summary>
    private KnowledgePointResult DetectPageBreak(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertPageBreak",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasPageBreak = CheckPageBreakInDocument(mainPart);

            result.ExpectedValue = "分页符";
            result.ActualValue = hasPageBreak ? "找到分页符" : "未找到分页符";
            result.IsCorrect = hasPageBreak;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"分页符检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测分页符失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测分栏符
    /// </summary>
    private KnowledgePointResult DetectColumnBreak(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertColumnBreak",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasColumnBreak = CheckColumnBreakInDocument(mainPart);

            result.ExpectedValue = "分栏符";
            result.ActualValue = hasColumnBreak ? "找到分栏符" : "未找到分栏符";
            result.IsCorrect = hasColumnBreak;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"分栏符检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测分栏符失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测超链接
    /// </summary>
    private KnowledgePointResult DetectHyperlink(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertHyperlink",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            var hyperlinkInfo = GetHyperlinkInDocument(mainPart, parameters);

            result.ExpectedValue = "超链接";
            result.ActualValue = hyperlinkInfo.Found ? $"找到超链接: {hyperlinkInfo.Url}" : "未找到超链接";
            result.IsCorrect = hyperlinkInfo.Found;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"超链接检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测超链接失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测书签
    /// </summary>
    private KnowledgePointResult DetectBookmark(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertBookmark",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasBookmark = CheckBookmarkInDocument(mainPart);

            result.ExpectedValue = "书签";
            result.ActualValue = hasBookmark ? "找到书签" : "未找到书签";
            result.IsCorrect = hasBookmark;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"书签检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测书签失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测交叉引用
    /// </summary>
    private KnowledgePointResult DetectCrossReference(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertCrossReference",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasCrossReference = CheckCrossReferenceInDocument(mainPart);

            result.ExpectedValue = "交叉引用";
            result.ActualValue = hasCrossReference ? "找到交叉引用" : "未找到交叉引用";
            result.IsCorrect = hasCrossReference;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"交叉引用检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测交叉引用失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测目录
    /// </summary>
    private KnowledgePointResult DetectTableOfContents(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertTableOfContents",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasTableOfContents = CheckTableOfContentsInDocument(mainPart);

            result.ExpectedValue = "目录";
            result.ActualValue = hasTableOfContents ? "找到目录" : "未找到目录";
            result.IsCorrect = hasTableOfContents;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"目录检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测目录失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测脚注
    /// </summary>
    private KnowledgePointResult DetectFootnote(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertFootnote",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasFootnote = CheckFootnoteInDocument(mainPart);

            result.ExpectedValue = "脚注";
            result.ActualValue = hasFootnote ? "找到脚注" : "未找到脚注";
            result.IsCorrect = hasFootnote;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"脚注检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测脚注失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测尾注
    /// </summary>
    private KnowledgePointResult DetectEndnote(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertEndnote",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasEndnote = CheckEndnoteInDocument(mainPart);

            result.ExpectedValue = "尾注";
            result.ActualValue = hasEndnote ? "找到尾注" : "未找到尾注";
            result.IsCorrect = hasEndnote;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"尾注检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测尾注失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测批注
    /// </summary>
    private KnowledgePointResult DetectComment(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertComment",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasComment = CheckCommentInDocument(mainPart);

            result.ExpectedValue = "批注";
            result.ActualValue = hasComment ? "找到批注" : "未找到批注";
            result.IsCorrect = hasComment;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"批注检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测批注失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测修订跟踪
    /// </summary>
    private KnowledgePointResult DetectTrackChanges(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "EnableTrackChanges",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasTrackChanges = CheckTrackChangesInDocument(mainPart);

            result.ExpectedValue = "修订跟踪";
            result.ActualValue = hasTrackChanges ? "启用修订跟踪" : "未启用修订跟踪";
            result.IsCorrect = hasTrackChanges;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"修订跟踪检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测修订跟踪失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测文档保护
    /// </summary>
    private KnowledgePointResult DetectDocumentProtection(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetDocumentProtection",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasProtection = CheckDocumentProtectionInDocument(mainPart);

            result.ExpectedValue = "文档保护";
            result.ActualValue = hasProtection ? "启用文档保护" : "未启用文档保护";
            result.IsCorrect = hasProtection;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"文档保护检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文档保护失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测水印
    /// </summary>
    private KnowledgePointResult DetectWatermark(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetWatermark",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasWatermark = CheckWatermarkInDocument(mainPart);

            result.ExpectedValue = "水印";
            result.ActualValue = hasWatermark ? "找到水印" : "未找到水印";
            result.IsCorrect = hasWatermark;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"水印检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测水印失败: {ex.Message}");
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
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasBackground = CheckPageBackgroundInDocument(mainPart);

            result.ExpectedValue = "页面背景";
            result.ActualValue = hasBackground ? "找到页面背景" : "未找到页面背景";
            result.IsCorrect = hasBackground;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页面背景检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页面背景失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页面边框
    /// </summary>
    private KnowledgePointResult DetectPageBorder(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetPageBorder",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasBorder = CheckPageBorderInDocument(mainPart);

            result.ExpectedValue = "页面边框";
            result.ActualValue = hasBorder ? "找到页面边框" : "未找到页面边框";
            result.IsCorrect = hasBorder;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页面边框检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页面边框失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测应用样式
    /// </summary>
    private KnowledgePointResult DetectAppliedStyle(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ApplyStyle",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasCustomStyle = CheckAppliedStyleInDocument(mainPart);

            result.ExpectedValue = "应用样式";
            result.ActualValue = hasCustomStyle ? "找到应用样式" : "未找到应用样式";
            result.IsCorrect = hasCustomStyle;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"样式应用检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测应用样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测应用模板
    /// </summary>
    private KnowledgePointResult DetectAppliedTemplate(WordprocessingDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ApplyTemplate",
            Parameters = parameters
        };

        try
        {
            MainDocumentPart mainPart = document.MainDocumentPart!;
            bool hasTemplate = CheckAppliedTemplateInDocument(mainPart);

            result.ExpectedValue = "应用模板";
            result.ActualValue = hasTemplate ? "找到应用模板" : "未找到应用模板";
            result.IsCorrect = hasTemplate;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"模板应用检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测应用模板失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检查文档中的字体样式
    /// </summary>
    private bool CheckFontStyleInDocument(MainDocumentPart mainPart, string expectedStyle)
    {
        try
        {
            var runs = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>();
            foreach (var run in runs)
            {
                var runProperties = run.RunProperties;
                if (runProperties != null)
                {
                    bool hasStyle = expectedStyle.ToLowerInvariant() switch
                    {
                        "bold" or "粗体" => runProperties.Bold?.Val?.Value == true,
                        "italic" or "斜体" => runProperties.Italic?.Val?.Value == true,
                        "underline" or "下划线" => runProperties.Underline?.Val?.Value != null,
                        "strikethrough" or "删除线" => runProperties.Strike?.Val?.Value == true,
                        _ => false
                    };

                    if (hasStyle) return true;
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
    /// 检查文档中的字体大小
    /// </summary>
    private bool CheckFontSizeInDocument(MainDocumentPart mainPart, string expectedSize)
    {
        try
        {
            var runs = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>();
            foreach (var run in runs)
            {
                var runProperties = run.RunProperties;
                if (runProperties?.FontSize?.Val?.Value != null)
                {
                    string fontSize = runProperties.FontSize.Val.Value;
                    if (TextEquals(fontSize, expectedSize))
                    {
                        return true;
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
    /// 检查文档中的字体颜色
    /// </summary>
    private bool CheckFontColorInDocument(MainDocumentPart mainPart, string expectedColor)
    {
        try
        {
            var runs = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>();
            foreach (var run in runs)
            {
                var runProperties = run.RunProperties;
                if (runProperties?.Color?.Val?.Value != null)
                {
                    string fontColor = runProperties.Color.Val.Value;
                    if (TextEquals(fontColor, expectedColor) || TextEquals("#" + fontColor, expectedColor))
                    {
                        return true;
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
    /// 检查文档中的段落对齐
    /// </summary>
    private bool CheckParagraphAlignmentInDocument(MainDocumentPart mainPart, string expectedAlignment)
    {
        try
        {
            var paragraphs = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
            foreach (var paragraph in paragraphs)
            {
                var paragraphProperties = paragraph.ParagraphProperties;
                if (paragraphProperties?.Justification?.Val?.Value != null)
                {
                    string alignment = paragraphProperties.Justification.Val.Value.ToString();
                    if (TextEquals(alignment, expectedAlignment))
                    {
                        return true;
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
    /// 检查文档中的行间距
    /// </summary>
    private bool CheckLineSpacingInDocument(MainDocumentPart mainPart, string expectedSpacing)
    {
        try
        {
            var paragraphs = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
            foreach (var paragraph in paragraphs)
            {
                var paragraphProperties = paragraph.ParagraphProperties;
                var spacing = paragraphProperties?.SpacingBetweenLines;
                if (spacing != null)
                {
                    // 简化实现：检查是否有行间距设置
                    if (spacing.Line?.Value != null || spacing.LineRule?.Value != null)
                    {
                        return true;
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
    /// 检查文档中的段落间距
    /// </summary>
    private bool CheckParagraphSpacingInDocument(MainDocumentPart mainPart, string expectedSpacing)
    {
        try
        {
            var paragraphs = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
            foreach (var paragraph in paragraphs)
            {
                var paragraphProperties = paragraph.ParagraphProperties;
                var spacing = paragraphProperties?.SpacingBetweenLines;
                if (spacing != null)
                {
                    // 检查段前段后间距
                    if (spacing.Before?.Value != null || spacing.After?.Value != null)
                    {
                        return true;
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
    /// 检查文档中的缩进
    /// </summary>
    private bool CheckIndentationInDocument(MainDocumentPart mainPart, string indentationType, Dictionary<string, string> parameters)
    {
        try
        {
            var paragraphs = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
            foreach (var paragraph in paragraphs)
            {
                var paragraphProperties = paragraph.ParagraphProperties;
                var indentation = paragraphProperties?.Indentation;
                if (indentation != null)
                {
                    bool hasIndentation = indentationType.ToLowerInvariant() switch
                    {
                        "left" or "左缩进" => indentation.Left?.Value != null,
                        "right" or "右缩进" => indentation.Right?.Value != null,
                        "firstline" or "首行缩进" => indentation.FirstLine?.Value != null,
                        "hanging" or "悬挂缩进" => indentation.Hanging?.Value != null,
                        _ => indentation.Left?.Value != null || indentation.Right?.Value != null ||
                             indentation.FirstLine?.Value != null || indentation.Hanging?.Value != null
                    };

                    if (hasIndentation) return true;
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
    /// 检查文档中的项目符号列表
    /// </summary>
    private bool CheckBulletListInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var paragraphs = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
            foreach (var paragraph in paragraphs)
            {
                var paragraphProperties = paragraph.ParagraphProperties;
                var numberingProperties = paragraphProperties?.NumberingProperties;
                if (numberingProperties?.NumberingId?.Val?.Value != null)
                {
                    // 简化实现：如果有编号属性，认为是列表
                    return true;
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
    /// 检查文档中的编号列表
    /// </summary>
    private bool CheckNumberedListInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var paragraphs = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
            foreach (var paragraph in paragraphs)
            {
                var paragraphProperties = paragraph.ParagraphProperties;
                var numberingProperties = paragraphProperties?.NumberingProperties;
                if (numberingProperties?.NumberingId?.Val?.Value != null)
                {
                    // 简化实现：如果有编号属性，认为是列表
                    return true;
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
    /// 获取文档中的表格样式
    /// </summary>
    private string GetTableStyleInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var tables = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Table>();
            foreach (var table in tables)
            {
                var tableProperties = table.TableProperties;
                var tableStyle = tableProperties?.TableStyle;
                if (tableStyle?.Val?.Value != null)
                {
                    return tableStyle.Val.Value;
                }
                if (tableProperties?.HasChildren == true)
                {
                    return "自定义样式";
                }
            }
            return "默认样式";
        }
        catch
        {
            return "未知样式";
        }
    }

    /// <summary>
    /// 检查文档中的表格边框
    /// </summary>
    private bool CheckTableBorderInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var tables = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Table>();
            foreach (var table in tables)
            {
                var tableProperties = table.TableProperties;
                var tableBorders = tableProperties?.TableBorders;
                if (tableBorders?.HasChildren == true)
                {
                    return true;
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
    /// 获取文档中的图片位置信息
    /// </summary>
    private (bool Found, string Position) GetImagePositionInDocument(MainDocumentPart mainPart, Dictionary<string, string> parameters)
    {
        try
        {
            var drawings = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>();
            if (drawings.Any())
            {
                return (true, "图片位置已设置");
            }
            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 获取文档中的图片大小信息
    /// </summary>
    private (bool Found, string Width, string Height) GetImageSizeInDocument(MainDocumentPart mainPart, Dictionary<string, string> parameters)
    {
        try
        {
            var drawings = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>();
            if (drawings.Any())
            {
                return (true, "自定义", "自定义");
            }
            return (false, string.Empty, string.Empty);
        }
        catch
        {
            return (false, string.Empty, string.Empty);
        }
    }

    /// <summary>
    /// 获取文档中的页眉页脚信息
    /// </summary>
    private (bool HasHeader, bool HasFooter) GetHeaderFooterInDocument(MainDocumentPart mainPart)
    {
        try
        {
            bool hasHeader = mainPart.HeaderParts.Any();
            bool hasFooter = mainPart.FooterParts.Any();
            return (hasHeader, hasFooter);
        }
        catch
        {
            return (false, false);
        }
    }

    /// <summary>
    /// 检查文档中的页码
    /// </summary>
    private bool CheckPageNumberInDocument(MainDocumentPart mainPart)
    {
        try
        {
            // 检查页眉页脚中的页码
            foreach (var headerPart in mainPart.HeaderParts)
            {
                var pageNumbers = headerPart.Header.Descendants<DocumentFormat.OpenXml.Wordprocessing.SimpleField>()
                    .Where(sf => sf.Instruction?.Value?.Contains("PAGE") == true);
                if (pageNumbers.Any()) return true;
            }

            foreach (var footerPart in mainPart.FooterParts)
            {
                var pageNumbers = footerPart.Footer.Descendants<DocumentFormat.OpenXml.Wordprocessing.SimpleField>()
                    .Where(sf => sf.Instruction?.Value?.Contains("PAGE") == true);
                if (pageNumbers.Any()) return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取页边距信息
    /// </summary>
    private (bool HasCustomMargin, string Top, string Bottom, string Left, string Right) GetPageMarginInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var sectionProperties = mainPart.Document.Body?.Elements<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>().FirstOrDefault();
            var pageMargin = sectionProperties?.Elements<DocumentFormat.OpenXml.Wordprocessing.PageMargin>().FirstOrDefault();

            if (pageMargin != null)
            {
                return (true,
                    pageMargin.Top?.Value?.ToString() ?? "默认",
                    pageMargin.Bottom?.Value?.ToString() ?? "默认",
                    pageMargin.Left?.Value?.ToString() ?? "默认",
                    pageMargin.Right?.Value?.ToString() ?? "默认");
            }
            return (false, "默认", "默认", "默认", "默认");
        }
        catch
        {
            return (false, "未知", "未知", "未知", "未知");
        }
    }

    /// <summary>
    /// 获取页面方向
    /// </summary>
    private string GetPageOrientationInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var sectionProperties = mainPart.Document.Body?.Elements<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>().FirstOrDefault();
            var pageSize = sectionProperties?.Elements<DocumentFormat.OpenXml.Wordprocessing.PageSize>().FirstOrDefault();

            if (pageSize?.Orient?.Value != null)
            {
                return pageSize.Orient.Value.ToString();
            }
            return "Portrait"; // 默认纵向
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 获取页面大小信息
    /// </summary>
    private (bool HasCustomSize, string Width, string Height) GetPageSizeInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var sectionProperties = mainPart.Document.Body?.Elements<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>().FirstOrDefault();
            var pageSize = sectionProperties?.Elements<DocumentFormat.OpenXml.Wordprocessing.PageSize>().FirstOrDefault();

            if (pageSize != null)
            {
                return (true,
                    pageSize.Width?.Value?.ToString() ?? "默认",
                    pageSize.Height?.Value?.ToString() ?? "默认");
            }
            return (false, "默认", "默认");
        }
        catch
        {
            return (false, "未知", "未知");
        }
    }

    /// <summary>
    /// 获取文档节数量
    /// </summary>
    private int GetSectionCountInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var sectionProperties = mainPart.Document.Body?.Elements<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>();
            return sectionProperties?.Count() ?? 1;
        }
        catch
        {
            return 1;
        }
    }

    /// <summary>
    /// 检查分页符
    /// </summary>
    private bool CheckPageBreakInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var pageBreaks = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Break>()
                .Where(b => b.Type?.Value == DocumentFormat.OpenXml.Wordprocessing.BreakValues.Page);
            return pageBreaks.Any();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查分栏符
    /// </summary>
    private bool CheckColumnBreakInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var columnBreaks = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Break>()
                .Where(b => b.Type?.Value == DocumentFormat.OpenXml.Wordprocessing.BreakValues.Column);
            return columnBreaks.Any();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取超链接信息
    /// </summary>
    private (bool Found, string Url) GetHyperlinkInDocument(MainDocumentPart mainPart, Dictionary<string, string> parameters)
    {
        try
        {
            var hyperlinks = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Hyperlink>();
            if (hyperlinks.Any())
            {
                var firstHyperlink = hyperlinks.First();
                if (firstHyperlink.Id?.Value != null)
                {
                    try
                    {
                        var relationship = mainPart.GetReferenceRelationship(firstHyperlink.Id.Value);
                        return (true, relationship?.Uri?.ToString() ?? "内部链接");
                    }
                    catch
                    {
                        return (true, "超链接存在");
                    }
                }
                return (true, "超链接存在");
            }
            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查书签
    /// </summary>
    private bool CheckBookmarkInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var bookmarks = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.BookmarkStart>();
            return bookmarks.Any();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查交叉引用
    /// </summary>
    private bool CheckCrossReferenceInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var fieldCodes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.FieldCode>();
            return fieldCodes.Any(fc => fc.Text?.Contains("REF") == true);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查目录
    /// </summary>
    private bool CheckTableOfContentsInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var fieldCodes = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.FieldCode>();
            return fieldCodes.Any(fc => fc.Text?.Contains("TOC") == true);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查脚注
    /// </summary>
    private bool CheckFootnoteInDocument(MainDocumentPart mainPart)
    {
        try
        {
            return mainPart.FootnotesPart?.Footnotes?.HasChildren == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查尾注
    /// </summary>
    private bool CheckEndnoteInDocument(MainDocumentPart mainPart)
    {
        try
        {
            return mainPart.EndnotesPart?.Endnotes?.HasChildren == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查批注
    /// </summary>
    private bool CheckCommentInDocument(MainDocumentPart mainPart)
    {
        try
        {
            return mainPart.WordprocessingCommentsPart?.Comments?.HasChildren == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查修订跟踪
    /// </summary>
    private bool CheckTrackChangesInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var documentSettings = mainPart.DocumentSettingsPart?.Settings;
            var trackRevisions = documentSettings?.Elements<DocumentFormat.OpenXml.Wordprocessing.TrackRevisions>().FirstOrDefault();
            return trackRevisions != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查文档保护
    /// </summary>
    private bool CheckDocumentProtectionInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var documentSettings = mainPart.DocumentSettingsPart?.Settings;
            var documentProtection = documentSettings?.Elements<DocumentFormat.OpenXml.Wordprocessing.DocumentProtection>().FirstOrDefault();
            return documentProtection != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查水印
    /// </summary>
    private bool CheckWatermarkInDocument(MainDocumentPart mainPart)
    {
        try
        {
            // 简化实现：检查页眉中是否有水印相关内容
            foreach (var headerPart in mainPart.HeaderParts)
            {
                var shapes = headerPart.Header.Descendants<DocumentFormat.OpenXml.Vml.Shape>();
                if (shapes.Any()) return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查页面背景
    /// </summary>
    private bool CheckPageBackgroundInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var background = mainPart.Document.Body?.Elements<DocumentFormat.OpenXml.Wordprocessing.DocumentBackground>().FirstOrDefault();
            return background != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查页面边框
    /// </summary>
    private bool CheckPageBorderInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var sectionProperties = mainPart.Document.Body?.Elements<DocumentFormat.OpenXml.Wordprocessing.SectionProperties>().FirstOrDefault();
            var pageBorders = sectionProperties?.Elements<DocumentFormat.OpenXml.Wordprocessing.PageBorders>().FirstOrDefault();
            return pageBorders?.HasChildren == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查应用样式
    /// </summary>
    private bool CheckAppliedStyleInDocument(MainDocumentPart mainPart)
    {
        try
        {
            var paragraphs = mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
            foreach (var paragraph in paragraphs)
            {
                var paragraphProperties = paragraph.ParagraphProperties;
                if (paragraphProperties?.ParagraphStyleId?.Val?.Value != null)
                {
                    return true;
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
    /// 检查应用模板
    /// </summary>
    private bool CheckAppliedTemplateInDocument(MainDocumentPart mainPart)
    {
        try
        {
            // 简化实现：检查是否有样式定义部分
            return mainPart.StyleDefinitionsPart?.Styles?.HasChildren == true;
        }
        catch
        {
            return false;
        }
    }
}
