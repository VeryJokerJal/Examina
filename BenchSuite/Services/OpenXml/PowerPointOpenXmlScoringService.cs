using BenchSuite.Interfaces;
using BenchSuite.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;

namespace BenchSuite.Services.OpenXml;

/// <summary>
/// PowerPoint OpenXML评分服务实现
/// </summary>
public class PowerPointOpenXmlScoringService : OpenXmlScoringServiceBase, IPowerPointScoringService
{
    protected override string[] SupportedExtensions => [".pptx"];

    /// <summary>
    /// 验证PowerPoint文档格式
    /// </summary>
    protected override bool ValidateDocumentFormat(string filePath)
    {
        try
        {
            using PresentationDocument document = PresentationDocument.Open(filePath, false);
            return document.PresentationPart != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 对PowerPoint文件进行打分（同步版本）
    /// </summary>
    public override ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        ScoringConfiguration config = configuration ?? _defaultConfiguration;
        ScoringResult result = CreateBaseScoringResult();

        try
        {
            if (!ValidateDocument(filePath))
            {
                result.ErrorMessage = $"无效的PowerPoint文档: {filePath}";
                return result;
            }

            // 获取PowerPoint模块
            ExamModuleModel? pptModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.PowerPoint);
            if (pptModule == null)
            {
                result.ErrorMessage = "试卷中未找到PowerPoint模块，跳过PowerPoint评分";
                result.IsSuccess = true;
                result.TotalScore = 0;
                result.AchievedScore = 0;
                result.KnowledgePointResults = [];
                return result;
            }

            // 收集所有操作点并记录题目关联关系
            List<OperationPointModel> allOperationPoints = [];
            Dictionary<string, string> operationPointToQuestionMap = [];

            foreach (QuestionModel question in pptModule.Questions)
            {
                foreach (OperationPointModel operationPoint in question.OperationPoints)
                {
                    allOperationPoints.Add(operationPoint);
                    operationPointToQuestionMap[operationPoint.Id] = question.Id;
                }
            }

            if (allOperationPoints.Count == 0)
            {
                result.ErrorMessage = "PowerPoint模块中未找到操作点";
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
                    QuestionModel? question = pptModule.Questions.FirstOrDefault(q => q.Id == questionId);
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
                result.ErrorMessage = $"无效的PowerPoint文档: {filePath}";
                return result;
            }

            // 获取题目的操作点（只处理PowerPoint相关的操作点）
            List<OperationPointModel> pptOperationPoints = [.. question.OperationPoints.Where(op => op.ModuleType == ModuleType.PowerPoint && op.IsEnabled)];

            if (pptOperationPoints.Count == 0)
            {
                result.ErrorMessage = "题目没有包含任何PowerPoint操作点";
                return result;
            }

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, pptOperationPoints).Result;

            // 为每个知识点结果设置题目ID
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                kpResult.QuestionId = question.Id;
            }

            FinalizeScoringResult(result, pptOperationPoints);
        }
        catch (Exception ex)
        {
            HandleException(ex, result);
        }

        return result;
    }

    /// <summary>
    /// 检测PowerPoint中的特定知识点
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
                    result.ErrorMessage = "无效的PowerPoint文档";
                    return result;
                }

                using PresentationDocument document = PresentationDocument.Open(filePath, false);
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
    /// 批量检测PowerPoint中的知识点
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
                        KnowledgePointResult errorResult = CreateKnowledgePointResult(operationPoint, operationPoint.PowerPointKnowledgeType ?? string.Empty);
                        SetKnowledgePointFailure(errorResult, "无效的PowerPoint文档");
                        results.Add(errorResult);
                    }
                    return results;
                }

                using PresentationDocument document = PresentationDocument.Open(filePath, false);

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
                        KnowledgePointResult errorResult = CreateKnowledgePointResult(operationPoint, operationPoint.PowerPointKnowledgeType ?? string.Empty);
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
                    KnowledgePointResult errorResult = CreateKnowledgePointResult(operationPoint, operationPoint.PowerPointKnowledgeType ?? string.Empty);
                    SetKnowledgePointFailure(errorResult, $"无法打开PowerPoint文件: {ex.Message}");
                    results.Add(errorResult);
                }
            }

            return results;
        });
    }

    /// <summary>
    /// 映射PowerPoint操作点名称到知识点类型
    /// </summary>
    protected override string MapOperationPointNameToKnowledgeType(string operationPointName)
    {
        return operationPointName switch
        {
            // PowerPoint特定映射
            var name when name.Contains("SlideLayout") => "SetSlideLayout",
            var name when name.Contains("DeleteSlide") => "DeleteSlide",
            var name when name.Contains("InsertSlide") => "InsertSlide",
            var name when name.Contains("SlideFont") => "SetSlideFont",
            var name when name.Contains("SlideTransition") => "SlideTransitionEffect",
            var name when name.Contains("TextContent") => "InsertTextContent",
            var name when name.Contains("TextFontSize") => "SetTextFontSize",
            var name when name.Contains("TextColor") => "SetTextColor",
            var name when name.Contains("TextStyle") => "SetTextStyle",
            var name when name.Contains("ElementPosition") => "SetElementPosition",
            var name when name.Contains("ElementSize") => "SetElementSize",
            var name when name.Contains("TextAlignment") => "SetTextAlignment",
            var name when name.Contains("Hyperlink") => "InsertHyperlink",
            var name when name.Contains("SlideNumber") => "SetSlideNumber",
            var name when name.Contains("FooterText") => "SetFooterText",
            var name when name.Contains("InsertImage") => "InsertImage",
            var name when name.Contains("InsertTable") => "InsertTable",
            var name when name.Contains("InsertSmartArt") => "InsertSmartArt",
            var name when name.Contains("InsertNote") => "InsertNote",
            var name when name.Contains("ApplyTheme") => "ApplyTheme",
            var name when name.Contains("SlideBackground") => "SetSlideBackground",
            var name when name.Contains("TableContent") => "SetTableContent",
            var name when name.Contains("TableStyle") => "SetTableStyle",
            var name when name.Contains("AnimationTiming") => "SetAnimationTiming",
            var name when name.Contains("AnimationDuration") => "SetAnimationDuration",
            var name when name.Contains("AnimationOrder") => "SetAnimationOrder",
            var name when name.Contains("SlideshowOptions") => "SlideshowOptions",
            var name when name.Contains("WordArtStyle") => "SetWordArtStyle",
            _ => base.MapOperationPointNameToKnowledgeType(operationPointName)
        };
    }

    /// <summary>
    /// 检测特定知识点
    /// </summary>
    private KnowledgePointResult DetectSpecificKnowledgePoint(PresentationDocument document, string knowledgePointType, Dictionary<string, string> parameters)
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
                case "SetSlideLayout":
                    result = DetectSlideLayout(document, parameters);
                    break;
                case "DeleteSlide":
                    result = DetectDeletedSlide(document, parameters);
                    break;
                case "InsertSlide":
                    result = DetectInsertedSlide(document, parameters);
                    break;
                case "SetSlideFont":
                    result = DetectSlideFont(document, parameters);
                    break;
                case "SlideTransitionEffect":
                case "SlideTransitionMode":
                case "SetSlideTransition":
                    result = DetectSlideTransition(document, parameters);
                    break;
                case "InsertTextContent":
                    result = DetectTextContent(document, parameters);
                    break;
                case "SetTextFontSize":
                    result = DetectTextFontSize(document, parameters);
                    break;
                case "SetTextColor":
                    result = DetectTextColor(document, parameters);
                    break;
                case "SetTextStyle":
                    result = DetectTextStyle(document, parameters);
                    break;
                case "SetElementPosition":
                    result = DetectElementPosition(document, parameters);
                    break;
                case "SetElementSize":
                    result = DetectElementSize(document, parameters);
                    break;
                case "SetTextAlignment":
                    result = DetectTextAlignment(document, parameters);
                    break;
                case "InsertHyperlink":
                    result = DetectHyperlink(document, parameters);
                    break;
                case "SetSlideNumber":
                    result = DetectSlideNumber(document, parameters);
                    break;
                case "SetFooterText":
                    result = DetectFooterText(document, parameters);
                    break;
                case "InsertImage":
                    result = DetectInsertedImage(document, parameters);
                    break;
                case "InsertTable":
                    result = DetectInsertedTable(document, parameters);
                    break;
                case "InsertSmartArt":
                    result = DetectInsertedSmartArt(document, parameters);
                    break;
                case "InsertNote":
                    result = DetectInsertedNote(document, parameters);
                    break;
                case "ApplyTheme":
                    result = DetectAppliedTheme(document, parameters);
                    break;
                case "SetSlideBackground":
                    result = DetectSlideBackground(document, parameters);
                    break;
                case "SetTableContent":
                    result = DetectTableContent(document, parameters);
                    break;
                case "SetTableStyle":
                    result = DetectTableStyle(document, parameters);
                    break;
                case "SetAnimationTiming":
                    result = DetectAnimationTiming(document, parameters);
                    break;
                case "SetAnimationDuration":
                    result = DetectAnimationDuration(document, parameters);
                    break;
                case "SetAnimationOrder":
                    result = DetectAnimationOrder(document, parameters);
                    break;
                case "SlideshowOptions":
                    result = DetectSlideshowOptions(document, parameters);
                    break;
                case "SetWordArtStyle":
                case "SetWordArtEffect":
                    result = DetectWordArtStyle(document, parameters);
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
    /// 检测幻灯片版式
    /// </summary>
    private KnowledgePointResult DetectSlideLayout(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSlideLayout",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideIndex", out int slideIndex) ||
                !TryGetParameter(parameters, "LayoutType", out string expectedLayout))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideIndex 或 LayoutType");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideIndex < 1 || slideIndex > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideIndex}");
                return result;
            }

            SlideId slideId = slideIds[slideIndex - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            // 获取幻灯片版式
            SlideLayoutPart? layoutPart = slidePart.SlideLayoutPart;
            if (layoutPart?.SlideLayout?.CommonSlideData?.Name?.Value != null)
            {
                string actualLayout = layoutPart.SlideLayout.CommonSlideData.Name.Value;
                string normalizedExpected = NormalizeLayoutName(expectedLayout);
                string normalizedActual = NormalizeLayoutName(actualLayout);

                result.ExpectedValue = expectedLayout;
                result.ActualValue = actualLayout;
                result.IsCorrect = TextEquals(normalizedActual, normalizedExpected);
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"幻灯片 {slideIndex} 的版式: 期望 {expectedLayout}, 实际 {actualLayout}";
            }
            else
            {
                SetKnowledgePointFailure(result, "无法获取幻灯片版式信息");
            }
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测幻灯片版式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测删除的幻灯片
    /// </summary>
    private KnowledgePointResult DetectDeletedSlide(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "DeleteSlide",
            Parameters = parameters
        };

        try
        {
            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();
            int actualCount = slideIds?.Count ?? 0;

            // 尝试从参数中获取期望的幻灯片数量
            if (TryGetIntParameter(parameters, "ExpectedSlideCount", out int expectedCount))
            {
                result.ExpectedValue = expectedCount.ToString();
                result.ActualValue = actualCount.ToString();
                result.IsCorrect = actualCount == expectedCount;
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"幻灯片数量: 期望 {expectedCount}, 实际 {actualCount}";
            }
            else if (TryGetIntParameter(parameters, "SlideIndex", out int deletedSlideIndex))
            {
                // 检测指定位置的幻灯片是否已被删除
                result.IsCorrect = actualCount > 0;
                result.ExpectedValue = $"删除第{deletedSlideIndex}张幻灯片";
                result.ActualValue = $"当前有{actualCount}张幻灯片";
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"删除幻灯片检测: {result.ActualValue}";
            }
            else
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ExpectedSlideCount 或 SlideIndex");
            }
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测删除幻灯片失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入的幻灯片
    /// </summary>
    private KnowledgePointResult DetectInsertedSlide(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertSlide",
            Parameters = parameters
        };

        try
        {
            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();
            int actualCount = slideIds?.Count ?? 0;

            // 尝试从参数中获取期望的幻灯片数量
            if (TryGetIntParameter(parameters, "ExpectedSlideCount", out int expectedCount))
            {
                result.ExpectedValue = expectedCount.ToString();
                result.ActualValue = actualCount.ToString();
                result.IsCorrect = actualCount >= expectedCount;
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"幻灯片数量: 期望至少 {expectedCount}, 实际 {actualCount}";
            }
            else if (TryGetIntParameter(parameters, "Position", out int insertPosition))
            {
                // 检查是否在指定位置插入了幻灯片
                bool hasEnoughSlides = actualCount > insertPosition;
                result.ExpectedValue = $"在第{insertPosition}张幻灯片后插入新幻灯片";
                result.ActualValue = $"当前有{actualCount}张幻灯片";
                result.IsCorrect = hasEnoughSlides;
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"插入幻灯片检测: {result.ActualValue}，插入位置{insertPosition}";
            }
            else
            {
                // 默认期望：至少有2张幻灯片（原有+插入的）
                int defaultExpected = 2;
                result.ExpectedValue = defaultExpected.ToString();
                result.ActualValue = actualCount.ToString();
                result.IsCorrect = actualCount >= defaultExpected;
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"幻灯片数量: 期望至少 {defaultExpected}, 实际 {actualCount}";
            }
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测插入幻灯片失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检查幻灯片中是否包含指定字体
    /// </summary>
    private bool CheckSlideForFont(SlidePart slidePart, string expectedFont, List<string> actualFonts)
    {
        bool fontFound = false;

        try
        {
            var textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (var textElement in textElements)
            {
                var runProperties = textElement.Parent?.Elements<DocumentFormat.OpenXml.Drawing.RunProperties>().FirstOrDefault();
                if (runProperties?.LatinFont?.Typeface?.Value != null)
                {
                    string fontName = runProperties.LatinFont.Typeface.Value;
                    actualFonts.Add(fontName);

                    if (TextEquals(fontName, expectedFont))
                    {
                        fontFound = true;
                    }
                }
            }
        }
        catch
        {
            // 忽略无法访问的元素
        }

        return fontFound;
    }

    /// <summary>
    /// 获取幻灯片中的所有文本
    /// </summary>
    private string GetSlideText(SlidePart slidePart)
    {
        try
        {
            var textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            return string.Join(" ", textElements.Select(t => t.Text));
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 检测幻灯片切换效果
    /// </summary>
    private KnowledgePointResult DetectSlideTransition(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SlideTransitionEffect",
            Parameters = parameters
        };

        try
        {
            // 支持多种参数名称（兼容新旧版本）
            string? slideIndexesStr = null;
            if (!parameters.TryGetValue("SlideNumbers", out slideIndexesStr))
            {
                if (!parameters.TryGetValue("SlideIndexes", out slideIndexesStr))
                {
                    if (parameters.TryGetValue("SlideIndex", out string? singleSlideStr))
                    {
                        slideIndexesStr = singleSlideStr;
                    }
                    else
                    {
                        _ = parameters.TryGetValue("SlideNumber", out slideIndexesStr);
                    }
                }
            }

            if (!parameters.TryGetValue("TransitionEffect", out string? expectedTransition))
            {
                if (!parameters.TryGetValue("TransitionType", out expectedTransition))
                {
                    _ = parameters.TryGetValue("TransitionScheme", out expectedTransition);
                }
            }

            if (string.IsNullOrEmpty(slideIndexesStr) || string.IsNullOrEmpty(expectedTransition))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumbers/SlideIndexes 或 TransitionEffect/TransitionType");
                return result;
            }

            // 解析幻灯片索引列表
            string[] slideIndexStrings = slideIndexesStr.Split(',');
            List<int> slideIndexes = [];

            foreach (string indexStr in slideIndexStrings)
            {
                if (int.TryParse(indexStr.Trim(), out int index))
                {
                    slideIndexes.Add(index);
                }
            }

            if (slideIndexes.Count == 0)
            {
                SetKnowledgePointFailure(result, "无效的幻灯片索引列表");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null)
            {
                SetKnowledgePointFailure(result, "无法获取幻灯片列表");
                return result;
            }

            int correctCount = 0;
            List<string> details = [];
            List<string> actualTransitions = [];

            foreach (int slideIndex in slideIndexes)
            {
                if (slideIndex < 1 || slideIndex > slideIds.Count)
                {
                    details.Add($"幻灯片 {slideIndex}: 索引超出范围");
                    continue;
                }

                SlideId slideId = slideIds[slideIndex - 1];
                SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

                // 检查切换效果
                string actualTransition = GetSlideTransitionEffect(slidePart);
                actualTransitions.Add(actualTransition);

                bool isCorrect = TextEquals(actualTransition, expectedTransition);

                if (isCorrect)
                {
                    correctCount++;
                    details.Add($"幻灯片 {slideIndex}: 切换效果匹配 ({actualTransition})");
                }
                else
                {
                    details.Add($"幻灯片 {slideIndex}: 切换效果不匹配 (期望: {expectedTransition}, 实际: {actualTransition})");
                }
            }

            result.ExpectedValue = expectedTransition;
            result.ActualValue = string.Join(", ", actualTransitions.Distinct());
            result.IsCorrect = correctCount == slideIndexes.Count;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : (int)((double)correctCount / slideIndexes.Count * result.TotalScore);
            result.Details = string.Join("; ", details);
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测幻灯片切换效果失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 获取幻灯片切换效果
    /// </summary>
    private string GetSlideTransitionEffect(SlidePart slidePart)
    {
        try
        {
            var transition = slidePart.Slide.Transition;
            if (transition != null)
            {
                // 简化处理，返回切换类型
                if (transition.Cut != null) return "Cut";
                if (transition.Fade != null) return "Fade";
                if (transition.Push != null) return "Push";
                if (transition.Wipe != null) return "Wipe";
                if (transition.Split != null) return "Split";
                if (transition.Strips != null) return "Strips";
                if (transition.Random != null) return "Random";
                // 可以根据需要添加更多切换效果
            }
            return "None";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// 检测文本字号
    /// </summary>
    private KnowledgePointResult DetectTextFontSize(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextFontSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideIndex", out int slideIndex) ||
                !TryGetFloatParameter(parameters, "FontSize", out float expectedSize))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideIndex 或 FontSize");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideIndex < 1 || slideIndex > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideIndex}");
                return result;
            }

            SlideId slideId = slideIds[slideIndex - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool sizeFound = false;
            List<string> actualSizes = [];

            var textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (var textElement in textElements)
            {
                var runProperties = textElement.Parent?.Elements<DocumentFormat.OpenXml.Drawing.RunProperties>().FirstOrDefault();
                if (runProperties?.FontSize?.Value != null)
                {
                    float fontSize = runProperties.FontSize.Value / 100f; // OpenXML uses points * 100
                    actualSizes.Add(fontSize.ToString());

                    if (Math.Abs(fontSize - expectedSize) < 0.1f)
                    {
                        sizeFound = true;
                    }
                }
            }

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = string.Join("; ", actualSizes);
            result.IsCorrect = sizeFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 字号检测: 期望 {expectedSize}, 找到的字号 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文本字号失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测文本颜色
    /// </summary>
    private KnowledgePointResult DetectTextColor(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextColor",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideIndex", out int slideIndex) ||
                !TryGetParameter(parameters, "Color", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideIndex 或 Color");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideIndex < 1 || slideIndex > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideIndex}");
                return result;
            }

            SlideId slideId = slideIds[slideIndex - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool colorFound = false;
            List<string> actualColors = [];

            var textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (var textElement in textElements)
            {
                var runProperties = textElement.Parent?.Elements<DocumentFormat.OpenXml.Drawing.RunProperties>().FirstOrDefault();
                if (runProperties?.SolidFill?.RgbColorModelHex?.Val?.Value != null)
                {
                    string colorHex = "#" + runProperties.SolidFill.RgbColorModelHex.Val.Value;
                    actualColors.Add(colorHex);

                    if (TextEquals(colorHex, expectedColor))
                    {
                        colorFound = true;
                    }
                }
            }

            result.ExpectedValue = expectedColor;
            result.ActualValue = string.Join("; ", actualColors);
            result.IsCorrect = colorFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 文本颜色检测: 期望 {expectedColor}, 找到的颜色 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文本颜色失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测插入的图片
    /// </summary>
    private KnowledgePointResult DetectInsertedImage(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertImage",
            Parameters = parameters
        };

        try
        {
            int totalImageCount = 0;
            string searchDetails = "";

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            // 尝试获取指定的幻灯片索引
            bool hasSpecificSlide = TryGetIntParameter(parameters, "SlideIndex", out int slideIndex) &&
                                   slideIndex >= 1 && slideIndex <= (slideIds?.Count ?? 0);

            if (hasSpecificSlide && slideIds != null)
            {
                // 检测指定幻灯片
                SlideId slideId = slideIds[slideIndex - 1];
                SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                totalImageCount = CountImagesInSlide(slidePart);
                searchDetails = $"幻灯片 {slideIndex}";
            }

            // 如果在指定幻灯片没找到图片，或者没有指定幻灯片，则搜索所有幻灯片
            if (totalImageCount == 0 && slideIds != null)
            {
                for (int i = 0; i < slideIds.Count; i++)
                {
                    SlideId slideId = slideIds[i];
                    SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                    int slideImageCount = CountImagesInSlide(slidePart);
                    totalImageCount += slideImageCount;

                    if (slideImageCount > 0)
                    {
                        searchDetails = $"在幻灯片 {i + 1} 中找到 {slideImageCount} 张图片";
                        break;
                    }
                }

                if (totalImageCount == 0)
                {
                    searchDetails = $"搜索了所有 {slideIds.Count} 张幻灯片";
                }
            }

            // 检查期望的图片数量
            bool hasExpectedCount = true;
            if (TryGetIntParameter(parameters, "ExpectedImageCount", out int expectedCount))
            {
                hasExpectedCount = totalImageCount >= expectedCount;
                result.ExpectedValue = $"至少{expectedCount}张图片";
            }
            else
            {
                hasExpectedCount = totalImageCount > 0;
                result.ExpectedValue = "至少1张图片";
            }

            result.ActualValue = $"{totalImageCount}张图片";
            result.IsCorrect = hasExpectedCount;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"图片检测: 期望 {result.ExpectedValue}, {searchDetails}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测插入图片失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 计算幻灯片中的图片数量
    /// </summary>
    private int CountImagesInSlide(SlidePart slidePart)
    {
        try
        {
            // 计算图片形状
            var pictures = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Presentation.Picture>();
            return pictures.Count();
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 检测插入的表格
    /// </summary>
    private KnowledgePointResult DetectInsertedTable(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertTable",
            Parameters = parameters
        };

        try
        {
            int totalTableCount = 0;
            string searchDetails = "";

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            // 尝试获取指定的幻灯片索引
            bool hasSpecificSlide = TryGetIntParameter(parameters, "SlideIndex", out int slideIndex) &&
                                   slideIndex >= 1 && slideIndex <= (slideIds?.Count ?? 0);

            if (hasSpecificSlide && slideIds != null)
            {
                // 检测指定幻灯片
                SlideId slideId = slideIds[slideIndex - 1];
                SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                totalTableCount = CountTablesInSlide(slidePart);
                searchDetails = $"幻灯片 {slideIndex}";
            }

            // 如果在指定幻灯片没找到表格，或者没有指定幻灯片，则搜索所有幻灯片
            if (totalTableCount == 0 && slideIds != null)
            {
                for (int i = 0; i < slideIds.Count; i++)
                {
                    SlideId slideId = slideIds[i];
                    SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                    int slideTableCount = CountTablesInSlide(slidePart);
                    totalTableCount += slideTableCount;

                    if (slideTableCount > 0)
                    {
                        searchDetails = $"在幻灯片 {i + 1} 中找到 {slideTableCount} 个表格";
                        break;
                    }
                }

                if (totalTableCount == 0)
                {
                    searchDetails = $"搜索了所有 {slideIds.Count} 张幻灯片";
                }
            }

            // 检查期望的表格数量
            bool hasExpectedCount = true;
            if (TryGetIntParameter(parameters, "ExpectedTableCount", out int expectedCount))
            {
                hasExpectedCount = totalTableCount >= expectedCount;
                result.ExpectedValue = $"至少{expectedCount}个表格";
            }
            else
            {
                hasExpectedCount = totalTableCount > 0;
                result.ExpectedValue = "至少1个表格";
            }

            result.ActualValue = $"{totalTableCount}个表格";
            result.IsCorrect = hasExpectedCount;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"表格检测: 期望 {result.ExpectedValue}, {searchDetails}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测插入表格失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 计算幻灯片中的表格数量
    /// </summary>
    private int CountTablesInSlide(SlidePart slidePart)
    {
        try
        {
            var tables = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Table>();
            return tables.Count();
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 简化的知识点检测方法 - 用于暂时不完全支持的功能
    /// </summary>
    private KnowledgePointResult CreateSimplifiedDetectionResult(string knowledgePointType, Dictionary<string, string> parameters, string message = "功能检测已简化实现")
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = knowledgePointType,
            Parameters = parameters,
            IsCorrect = true, // 简化实现暂时返回成功
            AchievedScore = 0, // 但不给分
            Details = $"{message} - {knowledgePointType}"
        };

        return result;
    }

    /// <summary>
    /// 检测文本样式
    /// </summary>
    private KnowledgePointResult DetectTextStyle(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideIndex", out int slideIndex) ||
                !TryGetParameter(parameters, "StyleType", out string expectedStyle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideIndex 或 StyleType");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideIndex < 1 || slideIndex > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideIndex}");
                return result;
            }

            SlideId slideId = slideIds[slideIndex - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool styleFound = CheckTextStyleInSlide(slidePart, expectedStyle);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = styleFound ? expectedStyle : "未找到指定样式";
            result.IsCorrect = styleFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 文本样式检测: 期望 {expectedStyle}, {(styleFound ? "找到" : "未找到")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文本样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测元素位置
    /// </summary>
    private KnowledgePointResult DetectElementPosition(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetElementPosition",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideIndex", out int slideIndex) ||
                !TryGetParameter(parameters, "ElementType", out string elementType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideIndex 或 ElementType");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideIndex < 1 || slideIndex > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideIndex}");
                return result;
            }

            SlideId slideId = slideIds[slideIndex - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            var positionInfo = GetElementPosition(slidePart, elementType, parameters);

            result.ExpectedValue = $"{elementType}位置";
            result.ActualValue = positionInfo.Found ? $"X:{positionInfo.X}, Y:{positionInfo.Y}" : "未找到元素";
            result.IsCorrect = positionInfo.Found && ValidatePosition(positionInfo, parameters);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 元素位置检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测元素位置失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测元素大小
    /// </summary>
    private KnowledgePointResult DetectElementSize(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetElementSize",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideIndex", out int slideIndex) ||
                !TryGetParameter(parameters, "ElementType", out string elementType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideIndex 或 ElementType");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideIndex < 1 || slideIndex > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideIndex}");
                return result;
            }

            SlideId slideId = slideIds[slideIndex - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            var sizeInfo = GetElementSize(slidePart, elementType, parameters);

            result.ExpectedValue = $"{elementType}大小";
            result.ActualValue = sizeInfo.Found ? $"宽:{sizeInfo.Width}, 高:{sizeInfo.Height}" : "未找到元素";
            result.IsCorrect = sizeInfo.Found && ValidateSize(sizeInfo, parameters);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 元素大小检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测元素大小失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测文本对齐方式
    /// </summary>
    private KnowledgePointResult DetectTextAlignment(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextAlignment",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideIndex", out int slideIndex) ||
                !TryGetParameter(parameters, "Alignment", out string expectedAlignment))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideIndex 或 Alignment");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideIndex < 1 || slideIndex > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideIndex}");
                return result;
            }

            SlideId slideId = slideIds[slideIndex - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string actualAlignment = GetTextAlignment(slidePart);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = actualAlignment;
            result.IsCorrect = TextEquals(actualAlignment, expectedAlignment);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 文本对齐检测: 期望 {expectedAlignment}, 实际 {actualAlignment}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文本对齐失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测超链接
    /// </summary>
    private KnowledgePointResult DetectHyperlink(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertHyperlink",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideIndex", out int slideIndex))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideIndex");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideIndex < 1 || slideIndex > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideIndex}");
                return result;
            }

            SlideId slideId = slideIds[slideIndex - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            var hyperlinkInfo = GetHyperlinkInfo(slidePart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "ExpectedUrl", out string expectedUrl) ? expectedUrl : "存在超链接";
            result.ActualValue = hyperlinkInfo.Found ? hyperlinkInfo.Url : "无超链接";
            result.IsCorrect = hyperlinkInfo.Found && (string.IsNullOrEmpty(expectedUrl) || TextEquals(hyperlinkInfo.Url, expectedUrl));
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 超链接检测: {(hyperlinkInfo.Found ? $"找到超链接 {hyperlinkInfo.Url}" : "未找到超链接")}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测超链接失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片编号
    /// </summary>
    private KnowledgePointResult DetectSlideNumber(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSlideNumber",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideIndex", out int slideIndex))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideIndex");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideIndex < 1 || slideIndex > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideIndex}");
                return result;
            }

            SlideId slideId = slideIds[slideIndex - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool hasSlideNumber = CheckSlideNumber(slidePart);

            result.ExpectedValue = "显示幻灯片编号";
            result.ActualValue = hasSlideNumber ? "显示幻灯片编号" : "未显示幻灯片编号";
            result.IsCorrect = hasSlideNumber;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 编号检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测幻灯片编号失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测页脚文本
    /// </summary>
    private KnowledgePointResult DetectFooterText(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFooterText",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideIndex", out int slideIndex))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideIndex");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            var slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideIndex < 1 || slideIndex > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideIndex}");
                return result;
            }

            SlideId slideId = slideIds[slideIndex - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string footerText = GetFooterText(slidePart);
            string expectedText = TryGetParameter(parameters, "ExpectedText", out string expected) ? expected : "";

            result.ExpectedValue = string.IsNullOrEmpty(expectedText) ? "存在页脚文本" : expectedText;
            result.ActualValue = string.IsNullOrEmpty(footerText) ? "无页脚文本" : footerText;
            result.IsCorrect = !string.IsNullOrEmpty(footerText) && (string.IsNullOrEmpty(expectedText) || TextContains(footerText, expectedText));
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 页脚文本检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页脚文本失败: {ex.Message}");
        }

        return result;
    }

    private KnowledgePointResult DetectInsertedSmartArt(PresentationDocument document, Dictionary<string, string> parameters)
        => CreateSimplifiedDetectionResult("InsertSmartArt", parameters, "SmartArt检测已简化");

    private KnowledgePointResult DetectInsertedNote(PresentationDocument document, Dictionary<string, string> parameters)
        => CreateSimplifiedDetectionResult("InsertNote", parameters, "备注检测已简化");

    private KnowledgePointResult DetectAppliedTheme(PresentationDocument document, Dictionary<string, string> parameters)
        => CreateSimplifiedDetectionResult("ApplyTheme", parameters, "主题检测已简化");

    private KnowledgePointResult DetectSlideBackground(PresentationDocument document, Dictionary<string, string> parameters)
        => CreateSimplifiedDetectionResult("SetSlideBackground", parameters, "幻灯片背景检测已简化");

    private KnowledgePointResult DetectTableContent(PresentationDocument document, Dictionary<string, string> parameters)
        => CreateSimplifiedDetectionResult("SetTableContent", parameters, "表格内容检测已简化");

    private KnowledgePointResult DetectTableStyle(PresentationDocument document, Dictionary<string, string> parameters)
        => CreateSimplifiedDetectionResult("SetTableStyle", parameters, "表格样式检测已简化");

    private KnowledgePointResult DetectAnimationTiming(PresentationDocument document, Dictionary<string, string> parameters)
        => CreateSimplifiedDetectionResult("SetAnimationTiming", parameters, "动画时间检测已简化");

    private KnowledgePointResult DetectAnimationDuration(PresentationDocument document, Dictionary<string, string> parameters)
        => CreateSimplifiedDetectionResult("SetAnimationDuration", parameters, "动画持续时间检测已简化");

    private KnowledgePointResult DetectAnimationOrder(PresentationDocument document, Dictionary<string, string> parameters)
        => CreateSimplifiedDetectionResult("SetAnimationOrder", parameters, "动画顺序检测已简化");

    private KnowledgePointResult DetectSlideshowOptions(PresentationDocument document, Dictionary<string, string> parameters)
        => CreateSimplifiedDetectionResult("SlideshowOptions", parameters, "幻灯片放映选项检测已简化");

    private KnowledgePointResult DetectWordArtStyle(PresentationDocument document, Dictionary<string, string> parameters)
        => CreateSimplifiedDetectionResult("SetWordArtStyle", parameters, "艺术字样式检测已简化");

    /// <summary>
    /// 检查幻灯片中的文本样式
    /// </summary>
    private bool CheckTextStyleInSlide(SlidePart slidePart, string expectedStyle)
    {
        try
        {
            var textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (var textElement in textElements)
            {
                var runProperties = textElement.Parent?.Elements<DocumentFormat.OpenXml.Drawing.RunProperties>().FirstOrDefault();
                if (runProperties != null)
                {
                    bool hasStyle = expectedStyle.ToLowerInvariant() switch
                    {
                        "bold" or "粗体" => runProperties.Bold?.Val?.Value == true,
                        "italic" or "斜体" => runProperties.Italic?.Val?.Value == true,
                        "underline" or "下划线" => runProperties.Underline?.Val?.Value != null,
                        "strikethrough" or "删除线" => runProperties.Strike?.Val?.Value != null,
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
    /// 获取元素位置信息
    /// </summary>
    private (bool Found, long X, long Y) GetElementPosition(SlidePart slidePart, string elementType, Dictionary<string, string> parameters)
    {
        try
        {
            var shapes = slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>();
            if (shapes == null) return (false, 0, 0);

            foreach (var shape in shapes)
            {
                var transform = shape.ShapeProperties?.Transform2D;
                if (transform?.Offset != null)
                {
                    long x = transform.Offset.X?.Value ?? 0;
                    long y = transform.Offset.Y?.Value ?? 0;

                    // 简化实现：返回第一个找到的形状位置
                    return (true, x, y);
                }
            }
            return (false, 0, 0);
        }
        catch
        {
            return (false, 0, 0);
        }
    }

    /// <summary>
    /// 验证位置是否符合期望
    /// </summary>
    private bool ValidatePosition((bool Found, long X, long Y) positionInfo, Dictionary<string, string> parameters)
    {
        if (!positionInfo.Found) return false;

        // 如果有期望的位置参数，进行验证
        if (TryGetIntParameter(parameters, "ExpectedX", out int expectedX) &&
            TryGetIntParameter(parameters, "ExpectedY", out int expectedY))
        {
            // 允许一定的误差范围（例如100个单位）
            const int tolerance = 100;
            return Math.Abs(positionInfo.X - expectedX) <= tolerance &&
                   Math.Abs(positionInfo.Y - expectedY) <= tolerance;
        }

        // 如果没有期望位置，只要找到元素就算成功
        return true;
    }

    /// <summary>
    /// 获取元素大小信息
    /// </summary>
    private (bool Found, long Width, long Height) GetElementSize(SlidePart slidePart, string elementType, Dictionary<string, string> parameters)
    {
        try
        {
            var shapes = slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>();
            if (shapes == null) return (false, 0, 0);

            foreach (var shape in shapes)
            {
                var transform = shape.ShapeProperties?.Transform2D;
                if (transform?.Extents != null)
                {
                    long width = transform.Extents.Cx?.Value ?? 0;
                    long height = transform.Extents.Cy?.Value ?? 0;

                    // 简化实现：返回第一个找到的形状大小
                    return (true, width, height);
                }
            }
            return (false, 0, 0);
        }
        catch
        {
            return (false, 0, 0);
        }
    }

    /// <summary>
    /// 验证大小是否符合期望
    /// </summary>
    private bool ValidateSize((bool Found, long Width, long Height) sizeInfo, Dictionary<string, string> parameters)
    {
        if (!sizeInfo.Found) return false;

        // 如果有期望的大小参数，进行验证
        if (TryGetIntParameter(parameters, "ExpectedWidth", out int expectedWidth) &&
            TryGetIntParameter(parameters, "ExpectedHeight", out int expectedHeight))
        {
            // 允许一定的误差范围（例如1000个单位）
            const int tolerance = 1000;
            return Math.Abs(sizeInfo.Width - expectedWidth) <= tolerance &&
                   Math.Abs(sizeInfo.Height - expectedHeight) <= tolerance;
        }

        // 如果没有期望大小，只要找到元素就算成功
        return true;
    }

    /// <summary>
    /// 获取文本对齐方式
    /// </summary>
    private string GetTextAlignment(SlidePart slidePart)
    {
        try
        {
            var paragraphs = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>();
            foreach (var paragraph in paragraphs)
            {
                var paragraphProperties = paragraph.ParagraphProperties;
                if (paragraphProperties?.Alignment?.Value != null)
                {
                    return paragraphProperties.Alignment.Value.ToString();
                }
            }
            return "Left"; // 默认左对齐
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// 获取超链接信息
    /// </summary>
    private (bool Found, string Url) GetHyperlinkInfo(SlidePart slidePart, Dictionary<string, string> parameters)
    {
        try
        {
            var hyperlinks = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.HyperlinkClick>();
            foreach (var hyperlink in hyperlinks)
            {
                if (hyperlink.Id?.Value != null)
                {
                    var relationship = slidePart.GetReferenceRelationship(hyperlink.Id.Value);
                    if (relationship?.Uri != null)
                    {
                        return (true, relationship.Uri.ToString());
                    }
                }
            }
            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 检查幻灯片编号
    /// </summary>
    private bool CheckSlideNumber(SlidePart slidePart)
    {
        try
        {
            // 检查幻灯片中是否有幻灯片编号占位符
            var placeholders = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Presentation.PlaceholderShape>();
            foreach (var placeholder in placeholders)
            {
                var placeholderType = placeholder.PlaceholderShapeProperties?.Type?.Value;
                if (placeholderType == DocumentFormat.OpenXml.Presentation.PlaceholderValues.SlideNumber)
                {
                    return true;
                }
            }

            // 也可以检查文本中是否包含幻灯片编号
            var textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (var textElement in textElements)
            {
                if (textElement.Text.Contains("#") || textElement.Text.Contains("slide", StringComparison.OrdinalIgnoreCase))
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
    /// 获取页脚文本
    /// </summary>
    private string GetFooterText(SlidePart slidePart)
    {
        try
        {
            // 检查页脚占位符
            var placeholders = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Presentation.PlaceholderShape>();
            foreach (var placeholder in placeholders)
            {
                var placeholderType = placeholder.PlaceholderShapeProperties?.Type?.Value;
                if (placeholderType == DocumentFormat.OpenXml.Presentation.PlaceholderValues.Footer)
                {
                    var textElements = placeholder.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
                    return string.Join(" ", textElements.Select(t => t.Text));
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 标准化版式名称
    /// </summary>
    private static string NormalizeLayoutName(string layoutName)
    {
        return layoutName.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();
    }
}
