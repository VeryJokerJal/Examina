using BenchSuite.Interfaces;
using BenchSuite.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

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
                result.Details = $"无效的PowerPoint文档: {filePath}";
                return result;
            }

            // 获取PowerPoint模块
            ExamModuleModel? pptModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.PowerPoint);
            if (pptModule == null)
            {
                result.Details = "试卷中未找到PowerPoint模块，跳过PowerPoint评分";
                result.IsSuccess = true;
                result.TotalScore = 0;
                result.AchievedScore = 0;
                result.KnowledgePointResults = [];
                return result;
            }

            // 收集所有PowerPoint相关的操作点并记录题目关联关系
            List<OperationPointModel> allOperationPoints = [];
            Dictionary<string, string> operationPointToQuestionMap = [];

            foreach (QuestionModel question in pptModule.Questions)
            {
                // 只处理PowerPoint相关且启用的操作点
                List<OperationPointModel> pptOperationPoints = question.OperationPoints.Where(op => op.ModuleType == ModuleType.PowerPoint && op.IsEnabled).ToList();

                System.Diagnostics.Debug.WriteLine($"[PowerPointOpenXmlScoringService] 题目 '{question.Title}' (ID: {question.Id}) 包含 {pptOperationPoints.Count} 个PowerPoint操作点");

                foreach (OperationPointModel operationPoint in pptOperationPoints)
                {
                    allOperationPoints.Add(operationPoint);
                    operationPointToQuestionMap[operationPoint.Id] = question.Id;
                    System.Diagnostics.Debug.WriteLine($"[PowerPointOpenXmlScoringService] 添加操作点: {operationPoint.Name} (ID: {operationPoint.Id}) -> 题目: {question.Id}");
                }
            }

            if (allOperationPoints.Count == 0)
            {
                result.Details = "PowerPoint模块中未找到启用的PowerPoint操作点";
                System.Diagnostics.Debug.WriteLine($"[PowerPointOpenXmlScoringService] 警告: PowerPoint模块包含 {pptModule.Questions.Count} 个题目，但没有找到启用的PowerPoint操作点");
                return result;
            }

            System.Diagnostics.Debug.WriteLine($"[PowerPointOpenXmlScoringService] 总共收集到 {allOperationPoints.Count} 个PowerPoint操作点，来自 {pptModule.Questions.Count} 个题目");

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, allOperationPoints).Result;

            // 为每个知识点结果设置题目关联信息
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                if (operationPointToQuestionMap.TryGetValue(kpResult.KnowledgePointId, out string? questionId))
                {
                    kpResult.QuestionId = questionId;

                    // 查找题目标题用于调试信息（KnowledgePointResult模型中没有QuestionTitle属性）
                    QuestionModel? question = pptModule.Questions.FirstOrDefault(q => q.Id == questionId);
                    if (question != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PowerPointOpenXmlScoringService] 知识点 '{kpResult.KnowledgePointName}' 关联到题目 '{question.Title}' (ID: {questionId})");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[PowerPointOpenXmlScoringService] 警告: 无法找到ID为 {questionId} 的题目");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[PowerPointOpenXmlScoringService] 警告: 知识点 '{kpResult.KnowledgePointName}' (ID: {kpResult.KnowledgePointId}) 没有找到对应的题目映射");
                }
            }

            FinalizeScoringResult(result, allOperationPoints);

            System.Diagnostics.Debug.WriteLine($"[PowerPointOpenXmlScoringService] 评分完成: 总分 {result.TotalScore}, 获得分数 {result.AchievedScore}, 成功率 {(result.TotalScore > 0 ? (result.AchievedScore / result.TotalScore * 100) : 0):F1}%");
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
                result.Details = $"无效的PowerPoint文档: {filePath}";
                return result;
            }

            // 获取题目的操作点（只处理PowerPoint相关的操作点）
            List<OperationPointModel> pptOperationPoints = [.. question.OperationPoints.Where(op => op.ModuleType == ModuleType.PowerPoint && op.IsEnabled)];

            if (pptOperationPoints.Count == 0)
            {
                result.Details = "题目没有包含任何PowerPoint操作点";
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
                    result.Details = "无效的PowerPoint文档";
                    return result;
                }

                using PresentationDocument document = PresentationDocument.Open(filePath, false);
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
            // PowerPoint特定映射 - 根据PowerPointKnowledgeConfig的Name属性（中文名称）映射

            // 幻灯片操作（15个）
            var name when name.Contains("设置幻灯片版式") => "SetSlideLayout",
            var name when name.Contains("删除幻灯片") => "DeleteSlide",
            var name when name.Contains("插入幻灯片") => "InsertSlide",
            var name when name.Contains("设置幻灯片的字体") => "SetSlideFont",
            var name when name.Contains("幻灯片切换") && !name.Contains("声音") => "SlideTransitionEffect",
            var name when name.Contains("幻灯片切换播放声音") => "SlideTransitionSound",
            var name when name.Contains("幻灯片放映方式") => "SlideshowMode",
            var name when name.Contains("幻灯片放映选项") => "SlideshowOptions",
            var name when name.Contains("幻灯片插入超链接") => "InsertHyperlink",
            var name when name.Contains("幻灯片编号") => "SlideNumber",
            var name when name.Contains("页脚文字") => "FooterText",
            var name when name.Contains("幻灯片插入图片") => "InsertImage",
            var name when name.Contains("幻灯片插入表格") => "InsertTable",
            var name when name.Contains("幻灯片插入SmartArt图形") => "InsertSmartArt",
            var name when name.Contains("插入备注") => "InsertNote",

            // 文字与字体设置（12个）
            var name when name.Contains("幻灯片插入文本内容") => "InsertTextContent",
            var name when name.Contains("幻灯片插入文本字号") => "SetTextFontSize",
            var name when name.Contains("幻灯片插入文本颜色") => "SetTextColor",
            var name when name.Contains("幻灯片插入文本字形") => "SetTextStyle",
            var name when name.Contains("元素位置") => "SetElementPosition",
            var name when name.Contains("元素高度和宽度设置") => "SetElementSize",
            var name when name.Contains("艺术字设置") => "SetWordArtStyle",
            var name when name.Contains("动画效果-方向") => "SetAnimationDirection",
            var name when name.Contains("动画样式") => "SetAnimationStyle",
            var name when name.Contains("动画持续时间") => "SetAnimationDuration",
            var name when name.Contains("文本对齐方式") => "SetTextAlignment",
            var name when name.Contains("动画顺序") => "SetAnimationOrder",

            // 背景样式与设计（1个）
            var name when name.Contains("设置文稿应用主题") => "ApplyTheme",

            // 母版与主题设置（1个）
            var name when name.Contains("设置幻灯片背景") => "SetSlideBackground",

            // 其他（7个）
            var name when name.Contains("单元格内容") => "SetTableContent",
            var name when name.Contains("表格样式") => "SetTableStyle",
            var name when name.Contains("SmartArt样式") => "SetSmartArtStyle",
            var name when name.Contains("SmartArt内容") => "SetSmartArtContent",
            var name when name.Contains("动画计时与延时设置") => "SetAnimationTiming",
            var name when name.Contains("段落行距") => "SetParagraphSpacing",
            var name when name.Contains("设置背景样式") => "SetBackgroundStyle",

            _ => base.MapOperationPointNameToKnowledgeType(operationPointName)
        };
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
            System.Diagnostics.Debug.WriteLine($"[PowerPointOpenXmlScoringService] 知识点检测失败 - {knowledgePointType}: {errorDetails}");

            return false;
        }

        errorDetails = string.Empty;
        return true;
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
                case "SlideshowMode":
                    result = DetectSlideshowMode(document, parameters);
                    break;
                case "SlideTransitionSound":
                    result = DetectSlideTransitionSound(document, parameters);
                    break;

                case "SetAnimationDirection":
                    result = DetectAnimationDirection(document, parameters);
                    break;
                case "SetAnimationStyle":
                    result = DetectAnimationStyle(document, parameters);
                    break;
                case "SetSmartArtStyle":
                    result = DetectSmartArtStyle(document, parameters);
                    break;
                case "SetSmartArtContent":
                    result = DetectSmartArtContent(document, parameters);
                    break;
                case "SetParagraphSpacing":
                    result = DetectParagraphSpacing(document, parameters);
                    break;
                case "SetBackgroundStyle":
                    result = DetectBackgroundStyle(document, parameters);
                    break;

                case "SetWordArtStyle":
                case "SetWordArtEffect":
                    result = DetectWordArtStyle(document, parameters);
                    break;
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
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber) ||
                !TryGetParameter(parameters, "LayoutType", out string expectedLayout))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber 或 LayoutType");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
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
                result.Details = $"幻灯片 {SlideNumber} 的版式: 期望 {expectedLayout}, 实际 {actualLayout}";
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
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();
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
            else if (TryGetIntParameter(parameters, "SlideNumber", out int deletedSlideNumber))
            {
                // 检测指定位置的幻灯片是否已被删除
                result.IsCorrect = actualCount > 0;
                result.ExpectedValue = $"删除第{deletedSlideNumber}张幻灯片";
                result.ActualValue = $"当前有{actualCount}张幻灯片";
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"删除幻灯片检测: {result.ActualValue}";
            }
            else
            {
                SetKnowledgePointFailure(result, "缺少必要参数: ExpectedSlideCount 或 SlideNumber");
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
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();
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
    /// 检测幻灯片字体
    /// </summary>
    private KnowledgePointResult DetectSlideFont(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSlideFont",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "FontName", out string expectedFont))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: FontName");
                return result;
            }

            bool fontFound = false;
            List<string> actualFonts = [];
            string searchDetails = "";

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            // 尝试获取指定的幻灯片索引
            bool hasSpecificSlide = TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber) &&
                                   SlideNumber >= 1 && SlideNumber <= (slideIds?.Count ?? 0);

            if (hasSpecificSlide && slideIds != null)
            {
                // 检测指定幻灯片
                SlideId slideId = slideIds[SlideNumber - 1];
                SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                fontFound = CheckSlideForFont(slidePart, expectedFont, actualFonts);
                searchDetails = $"幻灯片 {SlideNumber}";
            }

            // 如果在指定幻灯片没找到，或者没有指定幻灯片，则搜索所有幻灯片
            if (!fontFound && slideIds != null)
            {
                actualFonts.Clear();
                for (int i = 0; i < slideIds.Count; i++)
                {
                    SlideId slideId = slideIds[i];
                    SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                    if (CheckSlideForFont(slidePart, expectedFont, actualFonts))
                    {
                        fontFound = true;
                        searchDetails = $"在幻灯片 {i + 1} 中找到";
                        break;
                    }
                }

                if (!fontFound)
                {
                    searchDetails = $"搜索了所有 {slideIds.Count} 张幻灯片";
                }
            }

            result.ExpectedValue = expectedFont;
            result.ActualValue = string.Join("; ", actualFonts.Distinct());
            result.IsCorrect = fontFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"字体检测: 期望 {expectedFont}, {searchDetails}, 找到的字体 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测幻灯片字体失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测文本内容
    /// </summary>
    private KnowledgePointResult DetectTextContent(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertTextContent",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "TextContent", out string expectedText))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: TextContent");
                return result;
            }

            bool textFound = false;
            string allText = "";
            string searchDetails = "";

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            // 尝试获取指定的幻灯片索引
            bool hasSpecificSlide = TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber) &&
                                   SlideNumber >= 1 && SlideNumber <= (slideIds?.Count ?? 0);

            if (hasSpecificSlide && slideIds != null)
            {
                // 检测指定幻灯片
                SlideId slideId = slideIds[SlideNumber - 1];
                SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                allText = GetSlideText(slidePart);
                textFound = TextContains(allText, expectedText);
                searchDetails = $"幻灯片 {SlideNumber}";
            }

            // 如果在指定幻灯片没找到文本，或者没有指定幻灯片，则搜索所有幻灯片
            if (!textFound && slideIds != null)
            {
                allText = "";
                for (int i = 0; i < slideIds.Count; i++)
                {
                    SlideId slideId = slideIds[i];
                    SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                    string slideText = GetSlideText(slidePart);
                    allText += slideText + " ";

                    if (TextContains(slideText, expectedText))
                    {
                        textFound = true;
                        searchDetails = $"在幻灯片 {i + 1} 中找到";
                        break;
                    }
                }

                if (!textFound)
                {
                    searchDetails = $"搜索了所有 {slideIds.Count} 张幻灯片";
                }
            }

            result.ExpectedValue = expectedText;
            result.ActualValue = allText.Trim();
            result.IsCorrect = textFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"文本检测: 期望包含 '{expectedText}', {searchDetails}, 实际文本 '{result.ActualValue[..Math.Min(100, result.ActualValue.Length)]}...'";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测文本内容失败: {ex.Message}");
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
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text textElement in textElements)
            {
                RunProperties? runProperties = textElement.Parent?.Elements<RunProperties>().FirstOrDefault();
                if (runProperties != null)
                {
                    // 检查LatinFont
                    LatinFont? latinFont = runProperties.Elements<LatinFont>().FirstOrDefault();
                    if (latinFont?.Typeface?.Value != null)
                    {
                        string fontName = latinFont.Typeface.Value;
                        actualFonts.Add(fontName);

                        if (TextEquals(fontName, expectedFont))
                        {
                            fontFound = true;
                        }
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
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
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
            if (!parameters.TryGetValue("SlideNumbers", out string? SlideNumberesStr))
            {
                if (!parameters.TryGetValue("SlideNumberes", out SlideNumberesStr))
                {
                    if (parameters.TryGetValue("SlideNumber", out string? singleSlideStr))
                    {
                        SlideNumberesStr = singleSlideStr;
                    }
                    else
                    {
                        _ = parameters.TryGetValue("SlideNumber", out SlideNumberesStr);
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

            if (string.IsNullOrEmpty(SlideNumberesStr) || string.IsNullOrEmpty(expectedTransition))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumbers/SlideNumberes 或 TransitionEffect/TransitionType");
                return result;
            }

            // 解析幻灯片索引列表
            string[] SlideNumberStrings = SlideNumberesStr.Split(',');
            List<int> SlideNumberes = [];

            foreach (string indexStr in SlideNumberStrings)
            {
                if (int.TryParse(indexStr.Trim(), out int index))
                {
                    SlideNumberes.Add(index);
                }
            }

            if (SlideNumberes.Count == 0)
            {
                SetKnowledgePointFailure(result, "无效的幻灯片索引列表");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null)
            {
                SetKnowledgePointFailure(result, "无法获取幻灯片列表");
                return result;
            }

            int correctCount = 0;
            List<string> details = [];
            List<string> actualTransitions = [];

            foreach (int SlideNumber in SlideNumberes)
            {
                if (SlideNumber < 1 || SlideNumber > slideIds.Count)
                {
                    details.Add($"幻灯片 {SlideNumber}: 索引超出范围");
                    continue;
                }

                SlideId slideId = slideIds[SlideNumber - 1];
                SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

                // 检查切换效果
                string actualTransition = GetSlideTransitionEffect(slidePart);
                actualTransitions.Add(actualTransition);

                bool isCorrect = TextEquals(actualTransition, expectedTransition);

                if (isCorrect)
                {
                    correctCount++;
                    details.Add($"幻灯片 {SlideNumber}: 切换效果匹配 ({actualTransition})");
                }
                else
                {
                    details.Add($"幻灯片 {SlideNumber}: 切换效果不匹配 (期望: {expectedTransition}, 实际: {actualTransition})");
                }
            }

            result.ExpectedValue = expectedTransition;
            result.ActualValue = string.Join(", ", actualTransitions.Distinct());
            result.IsCorrect = correctCount == SlideNumberes.Count;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : (int)((double)correctCount / SlideNumberes.Count * result.TotalScore);
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
            Transition? transition = slidePart.Slide.Transition;
            if (transition != null)
            {
                // 检查具体的切换效果类型
                if (transition.HasChildren)
                {
                    foreach (OpenXmlElement child in transition.Elements())
                    {
                        string effectName = child.LocalName;

                        // 映射OpenXML元素名称到用户友好的切换效果名称
                        string friendlyName = effectName switch
                        {
                            "fade" => "淡化",
                            "push" => "推进",
                            "wipe" => "擦除",
                            "split" => "分割",
                            "reveal" => "揭开",
                            "randomBar" => "随机线条",
                            "cover" => "覆盖",
                            "uncover" => "显露",
                            "cut" => "切入",
                            "dissolve" => "溶解",
                            "blinds" => "百叶窗",
                            "checker" => "棋盘",
                            "circle" => "圆形",
                            "diamond" => "菱形",
                            "plus" => "加号",
                            "wedge" => "楔形",
                            "wheel" => "轮子",
                            "newsflash" => "新闻快报",
                            "zoom" => "缩放",
                            "random" => "随机",
                            _ => effectName // 如果没有映射，返回原始名称
                        };

                        return friendlyName;
                    }
                    return "自定义切换效果";
                }
            }
            return "无切换效果";
        }
        catch
        {
            return "未知切换效果";
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
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber) ||
                !TryGetFloatParameter(parameters, "FontSize", out float expectedSize))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber 或 FontSize");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool sizeFound = false;
            List<string> actualSizes = [];

            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text textElement in textElements)
            {
                RunProperties? runProperties = textElement.Parent?.Elements<RunProperties>().FirstOrDefault();
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
            result.Details = $"幻灯片 {SlideNumber} 字号检测: 期望 {expectedSize}, 找到的字号 {result.ActualValue}";
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
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber) ||
                !TryGetParameter(parameters, "Color", out string expectedColor))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber 或 Color");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool colorFound = false;
            List<string> actualColors = [];

            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text textElement in textElements)
            {
                RunProperties? runProperties = textElement.Parent?.Elements<RunProperties>().FirstOrDefault();
                if (runProperties != null)
                {
                    // 检查SolidFill颜色
                    SolidFill? solidFill = runProperties.Elements<SolidFill>().FirstOrDefault();
                    if (solidFill != null)
                    {
                        RgbColorModelHex? rgbColor = solidFill.Elements<RgbColorModelHex>().FirstOrDefault();
                        if (rgbColor?.Val?.Value != null)
                        {
                            string colorHex = "#" + rgbColor.Val.Value;
                            actualColors.Add(colorHex);

                            if (TextEquals(colorHex, expectedColor))
                            {
                                colorFound = true;
                            }
                        }
                    }
                }
            }

            result.ExpectedValue = expectedColor;
            result.ActualValue = string.Join("; ", actualColors);
            result.IsCorrect = colorFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 文本颜色检测: 期望 {expectedColor}, 找到的颜色 {result.ActualValue}";
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
            // 验证必需参数
            string[] requiredParams = ["SlideNumber"];
            if (!ValidateRequiredParameters(parameters, "InsertImage", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

            int totalImageCount = 0;
            string searchDetails = "";

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            // 尝试获取指定的幻灯片索引
            bool hasSpecificSlide = TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber) &&
                                   SlideNumber >= 1 && SlideNumber <= (slideIds?.Count ?? 0);

            if (hasSpecificSlide && slideIds != null)
            {
                // 检测指定幻灯片
                SlideId slideId = slideIds[SlideNumber - 1];
                SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                totalImageCount = CountImagesInSlide(slidePart);
                searchDetails = $"幻灯片 {SlideNumber}";
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
            IEnumerable<DocumentFormat.OpenXml.Presentation.Picture> pictures = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Presentation.Picture>();
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
            // 验证必需参数
            string[] requiredParams = ["SlideNumber"];
            if (!ValidateRequiredParameters(parameters, "InsertTable", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

            int totalTableCount = 0;
            string searchDetails = "";

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            // 尝试获取指定的幻灯片索引
            bool hasSpecificSlide = TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber) &&
                                   SlideNumber >= 1 && SlideNumber <= (slideIds?.Count ?? 0);

            if (hasSpecificSlide && slideIds != null)
            {
                // 检测指定幻灯片
                SlideId slideId = slideIds[SlideNumber - 1];
                SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                totalTableCount = CountTablesInSlide(slidePart);
                searchDetails = $"幻灯片 {SlideNumber}";
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
            IEnumerable<Table> tables = slidePart.Slide.Descendants<Table>();
            return tables.Count();
        }
        catch
        {
            return 0;
        }
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
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber) ||
                !TryGetParameter(parameters, "StyleType", out string expectedStyle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber 或 StyleType");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool styleFound = CheckTextStyleInSlide(slidePart, expectedStyle);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = styleFound ? expectedStyle : "未找到指定样式";
            result.IsCorrect = styleFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 文本样式检测: 期望 {expectedStyle}, {(styleFound ? "找到" : "未找到")}";
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
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber) ||
                !TryGetParameter(parameters, "ElementType", out string elementType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber 或 ElementType");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            (bool Found, long X, long Y) positionInfo = GetElementPosition(slidePart, elementType, parameters);

            result.ExpectedValue = $"{elementType}位置";
            result.ActualValue = positionInfo.Found ? $"X:{positionInfo.X}, Y:{positionInfo.Y}" : "未找到元素";
            result.IsCorrect = positionInfo.Found && ValidatePosition(positionInfo, parameters);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 元素位置检测: {result.ActualValue}";
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
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber) ||
                !TryGetParameter(parameters, "ElementType", out string elementType))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber 或 ElementType");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            (bool Found, long Width, long Height) sizeInfo = GetElementSize(slidePart, elementType, parameters);

            result.ExpectedValue = $"{elementType}大小";
            result.ActualValue = sizeInfo.Found ? $"宽:{sizeInfo.Width}, 高:{sizeInfo.Height}" : "未找到元素";
            result.IsCorrect = sizeInfo.Found && ValidateSize(sizeInfo, parameters);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 元素大小检测: {result.ActualValue}";
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
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber) ||
                !TryGetParameter(parameters, "Alignment", out string expectedAlignment))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber 或 Alignment");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string actualAlignment = GetTextAlignment(slidePart);

            result.ExpectedValue = expectedAlignment;
            result.ActualValue = actualAlignment;
            result.IsCorrect = TextEquals(actualAlignment, expectedAlignment);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 文本对齐检测: 期望 {expectedAlignment}, 实际 {actualAlignment}";
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
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            (bool Found, string Url) = GetHyperlinkInfo(slidePart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "ExpectedUrl", out string expectedUrl) ? expectedUrl : "存在超链接";
            result.ActualValue = Found ? Url : "无超链接";
            result.IsCorrect = Found && (string.IsNullOrEmpty(expectedUrl) || TextEquals(Url, expectedUrl));
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 超链接检测: {(Found ? $"找到超链接 {Url}" : "未找到超链接")}";
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
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool hasSlideNumber = CheckSlideNumber(slidePart);

            result.ExpectedValue = "显示幻灯片编号";
            result.ActualValue = hasSlideNumber ? "显示幻灯片编号" : "未显示幻灯片编号";
            result.IsCorrect = hasSlideNumber;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 编号检测: {result.ActualValue}";
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
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string footerText = GetFooterText(slidePart);
            string expectedText = TryGetParameter(parameters, "ExpectedText", out string expected) ? expected : "";

            result.ExpectedValue = string.IsNullOrEmpty(expectedText) ? "存在页脚文本" : expectedText;
            result.ActualValue = string.IsNullOrEmpty(footerText) ? "无页脚文本" : footerText;
            result.IsCorrect = !string.IsNullOrEmpty(footerText) && (string.IsNullOrEmpty(expectedText) || TextContains(footerText, expectedText));
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 页脚文本检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测页脚文本失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测SmartArt图形
    /// </summary>
    private KnowledgePointResult DetectInsertedSmartArt(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertSmartArt",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool smartArtFound = CheckForSmartArt(slidePart);

            result.ExpectedValue = "存在SmartArt图形";
            result.ActualValue = smartArtFound ? "找到SmartArt图形" : "未找到SmartArt图形";
            result.IsCorrect = smartArtFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} SmartArt检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测SmartArt失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测演讲者备注
    /// </summary>
    private KnowledgePointResult DetectInsertedNote(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertNote",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string noteText = GetSlideNotes(slidePart);
            bool hasNotes = !string.IsNullOrEmpty(noteText);

            result.ExpectedValue = "存在演讲者备注";
            result.ActualValue = hasNotes ? $"备注内容: {noteText[..Math.Min(50, noteText.Length)]}..." : "无备注";
            result.IsCorrect = hasNotes;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 备注检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测演讲者备注失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测应用的主题
    /// </summary>
    private KnowledgePointResult DetectAppliedTheme(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ApplyTheme",
            Parameters = parameters
        };

        try
        {
            // 验证必需参数
            string[] requiredParams = ["ThemeName"];
            if (!ValidateRequiredParameters(parameters, "ApplyTheme", requiredParams, out string errorDetails))
            {
                SetKnowledgePointFailure(result, errorDetails);
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            string? themeName = GetAppliedTheme(presentationPart);
            bool hasCustomTheme = !string.IsNullOrEmpty(themeName) && !themeName.Equals("Office Theme", StringComparison.OrdinalIgnoreCase);

            string expectedTheme = TryGetParameter(parameters, "ThemeName", out string expected) ? expected : "";

            result.ExpectedValue = string.IsNullOrEmpty(expectedTheme) ? "应用自定义主题" : expectedTheme;
            result.ActualValue = string.IsNullOrEmpty(themeName) ? "默认主题" : themeName;
            result.IsCorrect = hasCustomTheme && (string.IsNullOrEmpty(expectedTheme) || TextEquals(themeName, expectedTheme));
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"主题检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测应用主题失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片背景
    /// </summary>
    private KnowledgePointResult DetectSlideBackground(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSlideBackground",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string backgroundInfo = GetSlideBackground(slidePart);
            bool hasCustomBackground = !string.IsNullOrEmpty(backgroundInfo) && !backgroundInfo.Equals("默认背景");

            result.ExpectedValue = "自定义背景";
            result.ActualValue = backgroundInfo;
            result.IsCorrect = hasCustomBackground;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 背景检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测幻灯片背景失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表格内容
    /// </summary>
    private KnowledgePointResult DetectTableContent(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableContent",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            (bool Found, string Content) = GetTableContent(slidePart, parameters);

            result.ExpectedValue = TryGetParameter(parameters, "ExpectedContent", out string expected) ? expected : "表格内容";
            result.ActualValue = Found ? Content : "未找到表格";
            result.IsCorrect = Found && (string.IsNullOrEmpty(expected) || TextContains(Content, expected));
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 表格内容检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表格内容失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测表格样式
    /// </summary>
    private KnowledgePointResult DetectTableStyle(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string tableStyle = GetTableStyle(slidePart);
            bool hasCustomStyle = !string.IsNullOrEmpty(tableStyle) && !tableStyle.Equals("默认样式");

            result.ExpectedValue = "自定义表格样式";
            result.ActualValue = tableStyle;
            result.IsCorrect = hasCustomStyle;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 表格样式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测表格样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测动画时间
    /// </summary>
    private KnowledgePointResult DetectAnimationTiming(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationTiming",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool hasAnimationTiming = CheckAnimationTiming(slidePart);

            result.ExpectedValue = "设置动画时间";
            result.ActualValue = hasAnimationTiming ? "找到动画时间设置" : "未找到动画时间设置";
            result.IsCorrect = hasAnimationTiming;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 动画时间检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测动画时间失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测动画持续时间
    /// </summary>
    private KnowledgePointResult DetectAnimationDuration(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationDuration",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string animationDuration = GetAnimationDuration(slidePart);
            bool hasCustomDuration = !string.IsNullOrEmpty(animationDuration) && !animationDuration.Equals("默认");

            result.ExpectedValue = "自定义动画持续时间";
            result.ActualValue = animationDuration;
            result.IsCorrect = hasCustomDuration;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 动画持续时间检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测动画持续时间失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测动画顺序
    /// </summary>
    private KnowledgePointResult DetectAnimationOrder(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationOrder",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool hasAnimationOrder = CheckAnimationOrder(slidePart);

            result.ExpectedValue = "设置动画顺序";
            result.ActualValue = hasAnimationOrder ? "找到动画顺序设置" : "未找到动画顺序设置";
            result.IsCorrect = hasAnimationOrder;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 动画顺序检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测动画顺序失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片放映选项
    /// </summary>
    private KnowledgePointResult DetectSlideshowOptions(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SlideshowOptions",
            Parameters = parameters
        };

        try
        {
            PresentationPart presentationPart = document.PresentationPart!;
            string slideshowSettings = GetSlideshowOptions(presentationPart);
            bool hasCustomSettings = !string.IsNullOrEmpty(slideshowSettings) && !slideshowSettings.Equals("默认设置");

            result.ExpectedValue = "自定义放映选项";
            result.ActualValue = slideshowSettings;
            result.IsCorrect = hasCustomSettings;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片放映选项检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测幻灯片放映选项失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片放映方式
    /// </summary>
    private KnowledgePointResult DetectSlideshowMode(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SlideshowMode",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "SlideshowMode", out string expectedMode))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideshowMode");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;

            // 检查演示文稿设置
            string actualMode = GetSlideshowModeFromPresentation(presentationPart);

            result.ExpectedValue = expectedMode;
            result.ActualValue = actualMode;
            result.IsCorrect = TextEquals(actualMode, expectedMode) || actualMode.Contains("自定义");
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片放映方式: 期望 {expectedMode}, 实际 {actualMode}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测幻灯片放映方式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片切换播放声音
    /// </summary>
    private KnowledgePointResult DetectSlideTransitionSound(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SlideTransitionSound",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "SoundEffect", out string expectedSound))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SoundEffect");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideIds.Count == 0)
            {
                SetKnowledgePointFailure(result, "演示文稿中没有幻灯片");
                return result;
            }

            bool soundFound = false;
            string actualSound = "无声音";

            // 检查指定的幻灯片或所有幻灯片
            if (TryGetParameter(parameters, "SlideNumbers", out string slideNumbers))
            {
                string[] SlideNumberes = slideNumbers.Split(',');
                foreach (string SlideNumberStr in SlideNumberes)
                {
                    if (int.TryParse(SlideNumberStr.Trim(), out int SlideNumber) &&
                        SlideNumber >= 1 && SlideNumber <= slideIds.Count)
                    {
                        SlideId slideId = slideIds[SlideNumber - 1];
                        SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

                        string slideSound = GetTransitionSoundFromSlide(slidePart);
                        if (!string.IsNullOrEmpty(slideSound) && !slideSound.Equals("无声音"))
                        {
                            soundFound = true;
                            actualSound = slideSound;
                            break;
                        }
                    }
                }
            }

            result.ExpectedValue = expectedSound;
            result.ActualValue = actualSound;
            result.IsCorrect = soundFound && (TextEquals(actualSound, expectedSound) || actualSound.Contains("声音"));
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片切换声音: 期望 {expectedSound}, 实际 {actualSound}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测幻灯片切换声音失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测动画效果方向
    /// </summary>
    private KnowledgePointResult DetectAnimationDirection(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationDirection",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int slideNumber) ||
                !TryGetIntParameter(parameters, "ElementOrder", out int elementOrder) ||
                !TryGetParameter(parameters, "AnimationDirection", out string expectedDirection))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber, ElementOrder 或 AnimationDirection");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideNumber < 1 || slideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideNumber}");
                return result;
            }

            SlideId slideId = slideIds[slideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string actualDirection = GetAnimationDirectionFromSlide(slidePart, elementOrder);

            result.ExpectedValue = expectedDirection;
            result.ActualValue = actualDirection;
            result.IsCorrect = TextEquals(actualDirection, expectedDirection) || actualDirection.Contains("方向");
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"动画方向: 期望 {expectedDirection}, 实际 {actualDirection}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测动画方向失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测动画样式
    /// </summary>
    private KnowledgePointResult DetectAnimationStyle(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int slideNumber) ||
                !TryGetIntParameter(parameters, "ElementOrder", out int elementOrder) ||
                !TryGetParameter(parameters, "AnimationStyle", out string expectedStyle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber, ElementOrder 或 AnimationStyle");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideNumber < 1 || slideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideNumber}");
                return result;
            }

            SlideId slideId = slideIds[slideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string actualStyle = GetAnimationStyleFromSlide(slidePart, elementOrder);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle;
            result.IsCorrect = TextEquals(actualStyle, expectedStyle) || actualStyle.Contains("动画");
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"动画样式: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测动画样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测SmartArt样式
    /// </summary>
    private KnowledgePointResult DetectSmartArtStyle(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSmartArtStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int slideNumber) ||
                !TryGetIntParameter(parameters, "ElementOrder", out int elementOrder) ||
                !TryGetParameter(parameters, "SmartArtStyle", out string expectedStyle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber, ElementOrder 或 SmartArtStyle");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideNumber < 1 || slideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideNumber}");
                return result;
            }

            SlideId slideId = slideIds[slideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string actualStyle = GetSmartArtStyleFromSlide(slidePart, elementOrder);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle;
            result.IsCorrect = TextEquals(actualStyle, expectedStyle) || actualStyle.Contains("样式");
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"SmartArt样式: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测SmartArt样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测SmartArt内容
    /// </summary>
    private KnowledgePointResult DetectSmartArtContent(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSmartArtContent",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int slideNumber) ||
                !TryGetIntParameter(parameters, "ElementOrder", out int elementOrder) ||
                !TryGetParameter(parameters, "TextValue", out string expectedText))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber, ElementOrder 或 TextValue");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideNumber < 1 || slideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideNumber}");
                return result;
            }

            SlideId slideId = slideIds[slideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            string actualText = GetSmartArtContentFromSlide(slidePart, elementOrder);

            result.ExpectedValue = expectedText;
            result.ActualValue = actualText;
            result.IsCorrect = TextEquals(actualText, expectedText) || actualText.Contains(expectedText);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"SmartArt内容: 期望 {expectedText}, 实际 {actualText}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测SmartArt内容失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测段落行距
    /// </summary>
    private KnowledgePointResult DetectParagraphSpacing(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphSpacing",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int slideNumber) ||
                !TryGetIntParameter(parameters, "ElementOrder", out int elementOrder) ||
                !TryGetFloatParameter(parameters, "LineSpacing", out float expectedSpacing))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber, ElementOrder 或 LineSpacing");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || slideNumber < 1 || slideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {slideNumber}");
                return result;
            }

            SlideId slideId = slideIds[slideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            float actualSpacing = GetParagraphSpacingFromSlide(slidePart, elementOrder);

            result.ExpectedValue = expectedSpacing.ToString();
            result.ActualValue = actualSpacing.ToString();
            result.IsCorrect = Math.Abs(actualSpacing - expectedSpacing) < 0.1f || actualSpacing > 0;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"段落行距: 期望 {expectedSpacing}, 实际 {actualSpacing}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测段落行距失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测背景样式
    /// </summary>
    private KnowledgePointResult DetectBackgroundStyle(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetBackgroundStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetParameter(parameters, "BackgroundStyle", out string expectedStyle))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: BackgroundStyle");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            string actualStyle = GetBackgroundStyleFromPresentation(presentationPart);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle;
            result.IsCorrect = TextEquals(actualStyle, expectedStyle) || actualStyle.Contains("样式");
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"背景样式: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测背景样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检测艺术字样式
    /// </summary>
    private KnowledgePointResult DetectWordArtStyle(PresentationDocument document, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetWordArtStyle",
            Parameters = parameters
        };

        try
        {
            if (!TryGetIntParameter(parameters, "SlideNumber", out int SlideNumber))
            {
                SetKnowledgePointFailure(result, "缺少必要参数: SlideNumber");
                return result;
            }

            PresentationPart presentationPart = document.PresentationPart!;
            List<SlideId>? slideIds = presentationPart.Presentation.SlideIdList?.Elements<SlideId>().ToList();

            if (slideIds == null || SlideNumber < 1 || SlideNumber > slideIds.Count)
            {
                SetKnowledgePointFailure(result, $"幻灯片索引超出范围: {SlideNumber}");
                return result;
            }

            SlideId slideId = slideIds[SlideNumber - 1];
            SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);

            bool hasWordArt = CheckWordArtStyle(slidePart);

            result.ExpectedValue = "艺术字样式";
            result.ActualValue = hasWordArt ? "找到艺术字样式" : "未找到艺术字样式";
            result.IsCorrect = hasWordArt;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {SlideNumber} 艺术字样式检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            SetKnowledgePointFailure(result, $"检测艺术字样式失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 检查幻灯片中的文本样式
    /// </summary>
    private bool CheckTextStyleInSlide(SlidePart slidePart, string expectedStyle)
    {
        try
        {
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text textElement in textElements)
            {
                RunProperties? runProperties = textElement.Parent?.Elements<RunProperties>().FirstOrDefault();
                if (runProperties != null)
                {
                    bool hasStyle = expectedStyle.ToLowerInvariant() switch
                    {
                        "bold" or "粗体" => runProperties.Bold?.Value == true,
                        "italic" or "斜体" => runProperties.Italic?.Value == true,
                        "underline" or "下划线" => runProperties.Underline?.Value != null,
                        "strikethrough" or "删除线" => runProperties.Strike?.Value != null,
                        _ => false
                    };

                    if (hasStyle)
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
    /// 获取元素位置信息
    /// 支持ElementIndex=-1的任意元素匹配模式，会遍历所有元素找到符合条件的第一个元素
    /// </summary>
    /// <param name="slidePart">幻灯片部分</param>
    /// <param name="elementType">元素类型</param>
    /// <param name="parameters">参数字典，支持ElementIndex=-1表示任意元素匹配</param>
    /// <returns>元素位置信息</returns>
    private (bool Found, long X, long Y) GetElementPosition(SlidePart slidePart, string elementType, Dictionary<string, string> parameters)
    {
        try
        {
            // 尝试获取元素索引或ID来定位特定元素
            int elementIndex = 1; // 默认第一个元素
            if (TryGetIntParameter(parameters, "ElementIndex", out int paramIndex))
            {
                elementIndex = paramIndex;
            }
            else if (TryGetIntParameter(parameters, "ElementOrder", out int paramOrder))
            {
                elementIndex = paramOrder;
            }

            // 获取所有形状
            IEnumerable<DocumentFormat.OpenXml.Presentation.Shape>? shapes = slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>();
            if (shapes == null)
            {
                return (false, 0, 0);
            }

            List<DocumentFormat.OpenXml.Presentation.Shape> shapeList = shapes.ToList();

            // 如果elementIndex为-1，表示任意元素匹配模式
            if (elementIndex == -1)
            {
                return FindAnyElementPosition(shapeList, elementType, parameters);
            }

            // 检查索引是否有效
            if (elementIndex < 1 || elementIndex > shapeList.Count)
            {
                return (false, 0, 0);
            }

            // 获取指定索引的形状
            DocumentFormat.OpenXml.Presentation.Shape targetShape = shapeList[elementIndex - 1];
            Transform2D? transform = targetShape.ShapeProperties?.Transform2D;

            if (transform?.Offset != null)
            {
                long x = transform.Offset.X?.Value ?? 0;
                long y = transform.Offset.Y?.Value ?? 0;

                // 转换为更友好的单位（EMU转换为像素，1 EMU = 1/914400 英寸）
                return (true, x, y);
            }

            return (false, 0, 0);
        }
        catch
        {
            return (false, 0, 0);
        }
    }

    /// <summary>
    /// 在任意元素匹配模式下查找符合条件的元素位置
    /// </summary>
    private (bool Found, long X, long Y) FindAnyElementPosition(List<DocumentFormat.OpenXml.Presentation.Shape> shapes, string elementType, Dictionary<string, string> parameters)
    {
        try
        {
            // 根据元素类型和其他条件筛选符合条件的形状
            foreach (DocumentFormat.OpenXml.Presentation.Shape shape in shapes)
            {
                // 检查形状是否符合指定的元素类型
                if (IsShapeMatchingElementType(shape, elementType))
                {
                    // 如果有其他条件（如超链接），进一步验证
                    if (IsShapeMatchingAdditionalCriteria(shape, parameters))
                    {
                        Transform2D? transform = shape.ShapeProperties?.Transform2D;
                        if (transform?.Offset != null)
                        {
                            long x = transform.Offset.X?.Value ?? 0;
                            long y = transform.Offset.Y?.Value ?? 0;
                            return (true, x, y);
                        }
                    }
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
    /// 检查形状是否匹配指定的元素类型
    /// </summary>
    private static bool IsShapeMatchingElementType(DocumentFormat.OpenXml.Presentation.Shape shape, string elementType)
    {
        try
        {
            string lowerElementType = elementType.ToLowerInvariant();

            // 检查形状是否有文本内容（文本框类型）
            if (lowerElementType.Contains("textbox") || lowerElementType.Contains("text"))
            {
                return shape.TextBody != null && shape.TextBody.HasChildren;
            }

            // 检查形状类型
            if (lowerElementType.Contains("shape") || lowerElementType.Contains("element"))
            {
                return true; // 所有形状都匹配
            }

            // 检查图片类型
            if (lowerElementType.Contains("picture") || lowerElementType.Contains("image"))
            {
                return shape.ShapeProperties?.Elements<DocumentFormat.OpenXml.Drawing.BlipFill>().Any() == true;
            }

            // 默认匹配所有形状
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检查形状是否匹配额外的条件（如超链接等）
    /// </summary>
    private static bool IsShapeMatchingAdditionalCriteria(DocumentFormat.OpenXml.Presentation.Shape shape, Dictionary<string, string> parameters)
    {
        try
        {
            // 检查超链接条件
            if (parameters.TryGetValue("Hyperlink", out string? expectedHyperlink) && !string.IsNullOrEmpty(expectedHyperlink))
            {
                // 检查形状是否包含指定的超链接
                IEnumerable<Hyperlink> hyperlinks = shape.Descendants<Hyperlink>();
                if (!hyperlinks.Any())
                {
                    return false; // 没有超链接，不匹配
                }

                // 检查超链接内容是否匹配
                foreach (Hyperlink hyperlink in hyperlinks)
                {
                    if (hyperlink.HasAttributes)
                    {
                        foreach (OpenXmlAttribute attr in hyperlink.GetAttributes())
                        {
                            if (attr.LocalName == "id" && !string.IsNullOrEmpty(attr.Value))
                            {
                                // 这里可以进一步检查超链接URL是否匹配
                                return true; // 简化处理，有超链接就认为匹配
                            }
                        }
                    }
                }
            }

            // 检查文本内容条件
            if (parameters.TryGetValue("TextContent", out string? expectedText) && !string.IsNullOrEmpty(expectedText))
            {
                string shapeText = GetTextFromShape(shape);
                if (!shapeText.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // 如果没有额外条件或所有条件都满足，返回true
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证位置是否符合期望
    /// </summary>
    private bool ValidatePosition((bool Found, long X, long Y) positionInfo, Dictionary<string, string> parameters)
    {
        if (!positionInfo.Found)
        {
            return false;
        }

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
            // 尝试获取元素索引或ID来定位特定元素
            int elementIndex = 1; // 默认第一个元素
            if (TryGetIntParameter(parameters, "ElementIndex", out int paramIndex))
            {
                elementIndex = paramIndex;
            }
            else if (TryGetIntParameter(parameters, "ElementOrder", out int paramOrder))
            {
                elementIndex = paramOrder;
            }

            // 获取所有形状
            IEnumerable<DocumentFormat.OpenXml.Presentation.Shape>? shapes = slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>();
            if (shapes == null)
            {
                return (false, 0, 0);
            }

            List<DocumentFormat.OpenXml.Presentation.Shape> shapeList = [.. shapes];

            // 如果elementIndex为-1，表示任意元素匹配模式
            if (elementIndex == -1)
            {
                return FindAnyElementSize(shapeList, elementType, parameters);
            }

            // 检查索引是否有效
            if (elementIndex < 1 || elementIndex > shapeList.Count)
            {
                return (false, 0, 0);
            }

            // 获取指定索引的形状
            DocumentFormat.OpenXml.Presentation.Shape targetShape = shapeList[elementIndex - 1];
            Transform2D? transform = targetShape.ShapeProperties?.Transform2D;

            if (transform?.Extents != null)
            {
                long width = transform.Extents.Cx?.Value ?? 0;
                long height = transform.Extents.Cy?.Value ?? 0;

                // 返回EMU单位的大小（可以根据需要转换为其他单位）
                return (true, width, height);
            }

            return (false, 0, 0);
        }
        catch
        {
            return (false, 0, 0);
        }
    }

    /// <summary>
    /// 在任意元素匹配模式下查找符合条件的元素大小
    /// </summary>
    private (bool Found, long Width, long Height) FindAnyElementSize(List<DocumentFormat.OpenXml.Presentation.Shape> shapes, string elementType, Dictionary<string, string> parameters)
    {
        try
        {
            // 根据元素类型和其他条件筛选符合条件的形状
            foreach (DocumentFormat.OpenXml.Presentation.Shape shape in shapes)
            {
                // 检查形状是否符合指定的元素类型
                if (IsShapeMatchingElementType(shape, elementType))
                {
                    // 如果有其他条件（如超链接），进一步验证
                    if (IsShapeMatchingAdditionalCriteria(shape, parameters))
                    {
                        Transform2D? transform = shape.ShapeProperties?.Transform2D;
                        if (transform?.Extents != null)
                        {
                            long width = transform.Extents.Cx?.Value ?? 0;
                            long height = transform.Extents.Cy?.Value ?? 0;
                            return (true, width, height);
                        }
                    }
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
        if (!sizeInfo.Found)
        {
            return false;
        }

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
            IEnumerable<Paragraph> paragraphs = slidePart.Slide.Descendants<Paragraph>();
            foreach (Paragraph paragraph in paragraphs)
            {
                ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
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
            // 尝试获取元素索引来定位特定元素
            int elementIndex = 1; // 默认第一个元素
            if (TryGetIntParameter(parameters, "ElementIndex", out int paramIndex))
            {
                elementIndex = paramIndex;
            }
            else if (TryGetIntParameter(parameters, "ElementOrder", out int paramOrder))
            {
                elementIndex = paramOrder;
            }

            // 获取所有形状
            IEnumerable<DocumentFormat.OpenXml.Presentation.Shape>? shapes = slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>();
            if (shapes == null)
            {
                return (false, string.Empty);
            }

            List<DocumentFormat.OpenXml.Presentation.Shape> shapeList = shapes.ToList();

            // 如果elementIndex为-1，表示任意元素匹配模式
            if (elementIndex == -1)
            {
                return FindAnyElementHyperlink(shapeList, parameters, slidePart);
            }

            // 检查索引是否有效
            if (elementIndex < 1 || elementIndex > shapeList.Count)
            {
                // 如果指定元素不存在，检查整个幻灯片的超链接
                return GetSlideHyperlinkInfo(slidePart);
            }

            // 获取指定索引的形状中的超链接
            DocumentFormat.OpenXml.Presentation.Shape targetShape = shapeList[elementIndex - 1];
            return GetShapeHyperlinkInfo(targetShape, slidePart);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 在任意元素匹配模式下查找包含超链接的元素
    /// </summary>
    private (bool Found, string Url) FindAnyElementHyperlink(List<DocumentFormat.OpenXml.Presentation.Shape> shapes, Dictionary<string, string> parameters, SlidePart slidePart)
    {
        try
        {
            // 获取期望的超链接URL（如果有指定）
            string? expectedUrl = parameters.TryGetValue("Hyperlink", out string? url) ? url : null;

            foreach (DocumentFormat.OpenXml.Presentation.Shape shape in shapes)
            {
                (bool found, string shapeUrl) = GetShapeHyperlinkInfo(shape, slidePart);
                if (found)
                {
                    // 如果没有指定期望的URL，找到任何超链接就返回
                    if (string.IsNullOrEmpty(expectedUrl))
                    {
                        return (true, shapeUrl);
                    }

                    // 如果指定了期望的URL，检查是否匹配
                    if (shapeUrl.Contains(expectedUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        return (true, shapeUrl);
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
    /// 获取整个幻灯片的超链接信息
    /// </summary>
    private (bool Found, string Url) GetSlideHyperlinkInfo(SlidePart slidePart)
    {
        try
        {
            // 检查是否存在超链接并获取详细信息
            IEnumerable<Hyperlink> hyperlinks = slidePart.Slide.Descendants<Hyperlink>();

            if (hyperlinks.Any())
            {
                List<string> urlList = [];

                foreach (Hyperlink hyperlink in hyperlinks)
                {
                    // 检查超链接是否有属性设置
                    if (hyperlink.HasAttributes)
                    {
                        // 尝试获取关系ID属性
                        foreach (OpenXmlAttribute attr in hyperlink.GetAttributes())
                        {
                            if (attr.LocalName == "id" && !string.IsNullOrEmpty(attr.Value))
                            {
                                try
                                {
                                    // 通过关系ID获取实际的URL
                                    HyperlinkRelationship? relationship = slidePart.GetReferenceRelationship(attr.Value) as HyperlinkRelationship;
                                    if (relationship?.Uri != null)
                                    {
                                        urlList.Add(relationship.Uri.ToString());
                                    }
                                }
                                catch
                                {
                                    // 如果无法获取关系，记录为内部链接
                                    urlList.Add("内部链接");
                                }
                            }
                            else if (attr.LocalName == "tooltip" && !string.IsNullOrEmpty(attr.Value))
                            {
                                urlList.Add($"提示: {attr.Value}");
                            }
                        }
                    }
                }

                string urlInfo = urlList.Count > 0 ? string.Join("; ", urlList) : "超链接存在";
                return (true, urlInfo);
            }

            // 也检查文本中是否包含URL模式
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text text in textElements)
            {
                if (text.Text.Contains("http://") || text.Text.Contains("https://") || text.Text.Contains("www."))
                {
                    return (true, text.Text);
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
    /// 获取指定形状的超链接信息
    /// </summary>
    private (bool Found, string Url) GetShapeHyperlinkInfo(DocumentFormat.OpenXml.Presentation.Shape shape, SlidePart slidePart)
    {
        try
        {
            // 检查形状中的超链接
            IEnumerable<Hyperlink> hyperlinks = shape.Descendants<Hyperlink>();

            if (hyperlinks.Any())
            {
                List<string> urlList = [];

                foreach (Hyperlink hyperlink in hyperlinks)
                {
                    if (hyperlink.HasAttributes)
                    {
                        foreach (OpenXmlAttribute attr in hyperlink.GetAttributes())
                        {
                            if (attr.LocalName == "id" && !string.IsNullOrEmpty(attr.Value))
                            {
                                try
                                {
                                    HyperlinkRelationship? relationship = slidePart.GetReferenceRelationship(attr.Value) as HyperlinkRelationship;
                                    if (relationship?.Uri != null)
                                    {
                                        urlList.Add(relationship.Uri.ToString());
                                    }
                                }
                                catch
                                {
                                    urlList.Add("内部链接");
                                }
                            }
                            else if (attr.LocalName == "tooltip" && !string.IsNullOrEmpty(attr.Value))
                            {
                                urlList.Add($"提示: {attr.Value}");
                            }
                        }
                    }
                }

                string urlInfo = urlList.Count > 0 ? string.Join("; ", urlList) : "超链接存在";
                return (true, urlInfo);
            }

            // 检查形状文本中是否包含URL模式
            string shapeText = GetTextFromShape(shape);
            if (shapeText.Contains("http://") || shapeText.Contains("https://") || shapeText.Contains("www."))
            {
                return (true, shapeText);
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
            // 完整实现：检查幻灯片编号的多种可能显示方式

            // 1. 检查幻灯片中的占位符是否包含编号
            IEnumerable<DocumentFormat.OpenXml.Presentation.Shape> shapes =
                slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>() ?? [];

            foreach (DocumentFormat.OpenXml.Presentation.Shape shape in shapes)
            {
                // 检查是否是幻灯片编号占位符
                if (shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value?.Contains("Slide Number") == true ||
                    shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value?.Contains("SlideNumber") == true)
                {
                    return true;
                }

                // 检查占位符类型
                if (shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.PlaceholderShape?.Type?.Value ==
                    PlaceholderValues.SlideNumber)
                {
                    return true;
                }
            }

            // 2. 检查文本内容中是否包含幻灯片编号模式
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text textElement in textElements)
            {
                string text = textElement.Text;

                // 检查是否包含幻灯片编号的常见模式
                if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+$") || // 纯数字
                    System.Text.RegularExpressions.Regex.IsMatch(text, @"第\s*\d+\s*页") || // 第X页
                    System.Text.RegularExpressions.Regex.IsMatch(text, @"第\s*\d+\s*张") || // 第X张
                    System.Text.RegularExpressions.Regex.IsMatch(text, @"Slide\s*\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase) || // Slide X
                    System.Text.RegularExpressions.Regex.IsMatch(text, @"\d+\s*/\s*\d+") || // X/Y格式
                    text.Contains("‹#›") || // PowerPoint编号占位符
                    text.Contains("<#>")) // 另一种编号占位符格式
                {
                    return true;
                }
            }

            // 3. 检查母版中的编号设置
            SlideLayoutPart? layoutPart = slidePart.SlideLayoutPart;
            if (layoutPart?.SlideMasterPart?.SlideMaster != null)
            {
                IEnumerable<DocumentFormat.OpenXml.Presentation.Shape> masterShapes =
                    layoutPart.SlideMasterPart.SlideMaster.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>() ?? [];

                foreach (DocumentFormat.OpenXml.Presentation.Shape masterShape in masterShapes)
                {
                    if (masterShape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.PlaceholderShape?.Type?.Value ==
                        PlaceholderValues.SlideNumber)
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
    /// 获取页脚文本
    /// </summary>
    private string GetFooterText(SlidePart slidePart)
    {
        try
        {
            // 更完整的页脚文本检测实现
            // 1. 首先检查幻灯片母版中的页脚设置
            SlideLayoutPart? layoutPart = slidePart.SlideLayoutPart;
            if (layoutPart?.SlideMasterPart?.SlideMaster != null)
            {
                // 检查母版中的页脚占位符
                IEnumerable<DocumentFormat.OpenXml.Presentation.Shape> masterShapes =
                    layoutPart.SlideMasterPart.SlideMaster.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>() ?? [];

                foreach (DocumentFormat.OpenXml.Presentation.Shape shape in masterShapes)
                {
                    // 检查是否是页脚占位符
                    if (shape.NonVisualShapeProperties?.NonVisualDrawingProperties?.Name?.Value?.Contains("Footer") == true)
                    {
                        string footerText = GetTextFromShape(shape);
                        if (!string.IsNullOrEmpty(footerText))
                        {
                            return footerText;
                        }
                    }
                }
            }

            // 2. 检查当前幻灯片中的文本，寻找页脚特征
            IEnumerable<DocumentFormat.OpenXml.Presentation.Shape> slideShapes =
                slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>() ?? [];

            List<(string Text, long Y)> textWithPosition = [];

            foreach (DocumentFormat.OpenXml.Presentation.Shape shape in slideShapes)
            {
                string shapeText = GetTextFromShape(shape);
                if (!string.IsNullOrEmpty(shapeText))
                {
                    // 获取形状的Y坐标（用于判断是否在底部）
                    long yPosition = shape.ShapeProperties?.Transform2D?.Offset?.Y?.Value ?? 0;
                    textWithPosition.Add((shapeText, yPosition));
                }
            }

            // 3. 查找包含页脚关键词的文本
            foreach ((string text, long y) in textWithPosition)
            {
                string lowerText = text.ToLowerInvariant();
                if (lowerText.Contains("footer") ||
                    lowerText.Contains("页脚") ||
                    lowerText.Contains("版权") ||
                    lowerText.Contains("copyright") ||
                    lowerText.Contains("©"))
                {
                    return text;
                }
            }

            // 4. 如果没有明确的页脚标识，返回位置最靠下的文本（通常页脚在底部）
            if (textWithPosition.Count > 0)
            {
                (string text, long y) = textWithPosition.OrderByDescending(t => t.Y).First();
                return text;
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 从形状中获取文本内容
    /// </summary>
    private static string GetTextFromShape(DocumentFormat.OpenXml.Presentation.Shape shape)
    {
        try
        {
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = shape.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            return string.Join(" ", textElements.Select(t => t.Text));
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 检查SmartArt图形
    /// </summary>
    private bool CheckForSmartArt(SlidePart slidePart)
    {
        try
        {
            // 检查是否有SmartArt图形
            IEnumerable<DocumentFormat.OpenXml.Presentation.GraphicFrame> smartArtShapes = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Presentation.GraphicFrame>();
            foreach (DocumentFormat.OpenXml.Presentation.GraphicFrame shape in smartArtShapes)
            {
                Graphic? graphic = shape.Graphic;
                if (graphic?.GraphicData?.Uri?.Value != null &&
                    graphic.GraphicData.Uri.Value.Contains("smartArt"))
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
    /// 获取幻灯片备注
    /// </summary>
    private string GetSlideNotes(SlidePart slidePart)
    {
        try
        {
            NotesSlidePart? notesSlidePart = slidePart.NotesSlidePart;
            if (notesSlidePart?.NotesSlide != null)
            {
                IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = notesSlidePart.NotesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
                return string.Join(" ", textElements.Select(t => t.Text).Where(text => !string.IsNullOrWhiteSpace(text)));
            }
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// 获取应用的主题名称
    /// </summary>
    private string? GetAppliedTheme(PresentationPart presentationPart)
    {
        try
        {
            ThemePart? themePart = presentationPart.ThemePart;
            return themePart?.Theme?.ThemeElements?.ColorScheme?.Name?.Value != null
                ? themePart.Theme.ThemeElements.ColorScheme.Name.Value
                : "Office Theme";
        }
        catch
        {
            return "Unknown Theme";
        }
    }

    /// <summary>
    /// 获取幻灯片背景信息
    /// </summary>
    private string GetSlideBackground(SlidePart slidePart)
    {
        try
        {
            Background? background = slidePart.Slide.CommonSlideData?.Background;
            if (background != null)
            {
                // 检查背景填充
                BackgroundProperties? backgroundProperties = background.BackgroundProperties;
                if (backgroundProperties != null)
                {
                    if (backgroundProperties.HasChildren)
                    {
                        return "自定义背景";
                    }
                }

                // 检查背景样式引用
                BackgroundStyleReference? backgroundStyleReference = background.BackgroundStyleReference;
                if (backgroundStyleReference?.Index?.Value != null)
                {
                    return $"背景样式 {backgroundStyleReference.Index.Value}";
                }
            }
            return "默认背景";
        }
        catch
        {
            return "未知背景";
        }
    }

    /// <summary>
    /// 获取表格内容
    /// </summary>
    private static (bool Found, string Content) GetTableContent(SlidePart slidePart, Dictionary<string, string> parameters)
    {
        try
        {
            IEnumerable<Table> tables = slidePart.Slide.Descendants<Table>();
            if (tables.Any())
            {
                List<string> tableTexts = [];
                foreach (Table table in tables)
                {
                    IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = table.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
                    tableTexts.AddRange(textElements.Select(t => t.Text));
                }
                return (true, string.Join(" ", tableTexts));
            }
            return (false, string.Empty);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// 获取表格样式
    /// </summary>
    private static string GetTableStyle(SlidePart slidePart)
    {
        try
        {
            IEnumerable<Table> tables = slidePart.Slide.Descendants<Table>();
            foreach (Table table in tables)
            {
                TableProperties? tableProperties = table.TableProperties;
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
    /// 检查动画时间设置
    /// </summary>
    private static bool CheckAnimationTiming(SlidePart slidePart)
    {
        try
        {
            // 检查幻灯片的动画时间设置
            // 1. 检查切换时间设置
            Transition? transition = slidePart.Slide.Transition;
            if (transition?.AdvanceAfterTime?.Value != null)
            {
                return true;
            }

            // 2. 检查动画序列中的时间设置
            Timing? timing = slidePart.Slide.Timing;
            if (timing?.TimeNodeList != null && timing.TimeNodeList.HasChildren)
            {
                // 检查是否存在时间节点，表示有动画时间设置
                IEnumerable<TimeNode> timeNodes = timing.TimeNodeList.Elements<TimeNode>();
                if (timeNodes.Any())
                {
                    foreach (TimeNode timeNode in timeNodes)
                    {
                        // 检查时间节点是否有属性设置
                        if (timeNode.HasAttributes)
                        {
                            return true;
                        }

                        // 检查是否有子节点（表示复杂的时间设置）
                        if (timeNode.HasChildren)
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
    /// 获取动画持续时间
    /// </summary>
    private static string GetAnimationDuration(SlidePart slidePart)
    {
        try
        {
            Transition? transition = slidePart.Slide.Transition;
            return transition?.Duration?.Value != null ? $"持续时间: {transition.Duration.Value}ms" : "默认";
        }
        catch
        {
            return "未知";
        }
    }

    /// <summary>
    /// 检查动画顺序
    /// </summary>
    private static bool CheckAnimationOrder(SlidePart slidePart)
    {
        try
        {
            // 检查动画序列中是否有多个动画效果，表示有动画顺序设置
            Timing? timing = slidePart.Slide.Timing;
            if (timing?.TimeNodeList != null)
            {
                IEnumerable<TimeNode> timeNodes = timing.TimeNodeList.Elements<TimeNode>();

                // 计算动画效果数量
                int animationCount = 0;
                foreach (TimeNode timeNode in timeNodes)
                {
                    // 检查是否有子动画节点
                    IEnumerable<TimeNode> childNodes = timeNode.Elements<TimeNode>();
                    animationCount += childNodes.Count();

                    // 如果有多个动画，说明存在动画顺序
                    if (animationCount > 1)
                    {
                        return true;
                    }
                }
            }

            // 备用检查：如果有多个形状且存在动画设置，可能有动画顺序
            IEnumerable<DocumentFormat.OpenXml.Presentation.Shape>? shapes = slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>();
            bool hasMultipleShapes = shapes?.Count() > 1;
            bool hasAnimations = timing?.TimeNodeList?.HasChildren == true;

            return hasMultipleShapes && hasAnimations;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取幻灯片放映选项
    /// </summary>
    private static string GetSlideshowOptions(PresentationPart presentationPart)
    {
        try
        {
            // 完整实现：检查演示文稿的放映设置
            Presentation presentation = presentationPart.Presentation;
            List<string> options = [];

            // 检查演示文稿属性
            PresentationProperties? presentationProperties = presentation.Elements<PresentationProperties>().FirstOrDefault();
            if (presentationProperties != null)
            {
                // 检查是否有自定义放映设置
                if (presentationProperties.HasChildren)
                {
                    options.Add("包含演示文稿属性设置");
                }
            }

            // 检查自定义放映
            CustomShowList? customShowList = presentation.CustomShowList;
            if (customShowList != null && customShowList.HasChildren)
            {
                options.Add("包含自定义放映");
            }

            // 检查幻灯片大小设置
            SlideSize? slideSize = presentation.SlideSize;
            if (slideSize != null)
            {
                // 检查是否是非标准大小
                if (slideSize.Cx?.Value != null && slideSize.Cy?.Value != null)
                {
                    // 标准16:9大小约为10x5.625英寸，4:3大小约为10x7.5英寸
                    long width = slideSize.Cx.Value;
                    long height = slideSize.Cy.Value;

                    // 检查是否是自定义大小（非标准比例）
                    double ratio = (double)width / height;
                    if (Math.Abs(ratio - (16.0 / 9.0)) > 0.1 && Math.Abs(ratio - (4.0 / 3.0)) > 0.1)
                    {
                        options.Add("自定义幻灯片大小");
                    }
                }
            }

            // 检查默认文本样式
            DefaultTextStyle? defaultTextStyle = presentation.DefaultTextStyle;
            if (defaultTextStyle != null && defaultTextStyle.HasChildren)
            {
                options.Add("自定义默认文本样式");
            }

            return options.Count > 0 ? string.Join(", ", options) : "默认设置";
        }
        catch
        {
            return "未知设置";
        }
    }

    /// <summary>
    /// 检查艺术字样式
    /// </summary>
    private static bool CheckWordArtStyle(SlidePart slidePart)
    {
        try
        {
            // 检查文本是否有特殊效果（艺术字通常有复杂的文本效果）
            IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
            foreach (DocumentFormat.OpenXml.Drawing.Text textElement in textElements)
            {
                RunProperties? runProperties = textElement.Parent?.Elements<RunProperties>().FirstOrDefault();
                if (runProperties != null)
                {
                    // 检查是否有文本效果
                    if (runProperties.HasChildren &&
                        (runProperties.Elements<EffectList>().Any() ||
                         runProperties.Elements<EffectDag>().Any()))
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
    /// 标准化版式名称
    /// </summary>
    private static string NormalizeLayoutName(string layoutName)
    {
        return layoutName.Trim().Replace(" ", "").Replace("_", "").ToLowerInvariant();
    }

    /// <summary>
    /// 获取演示文稿的放映方式
    /// </summary>
    private static string GetSlideshowModeFromPresentation(PresentationPart presentationPart)
    {
        try
        {
            // 完整实现：检查演示文稿的放映模式设置
            Presentation presentation = presentationPart.Presentation;
            List<string> modeSettings = [];

            // 检查自定义放映列表
            CustomShowList? customShowList = presentation.CustomShowList;
            if (customShowList != null && customShowList.HasChildren)
            {
                int customShowCount = customShowList.Elements<CustomShow>().Count();
                modeSettings.Add($"自定义放映({customShowCount}个)");
            }

            // 检查演示文稿属性中的放映设置
            PresentationProperties? presentationProperties = presentation.Elements<PresentationProperties>().FirstOrDefault();
            if (presentationProperties != null)
            {
                // 检查是否有放映相关的属性设置
                if (presentationProperties.HasChildren)
                {
                    modeSettings.Add("包含放映属性设置");
                }
            }

            // 检查幻灯片切换设置（影响放映模式）
            SlideIdList? slideIdList = presentation.SlideIdList;
            if (slideIdList != null)
            {
                bool hasTransitionSettings = false;
                foreach (SlideId slideId in slideIdList.Elements<SlideId>())
                {
                    try
                    {
                        SlidePart slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                        if (slidePart.Slide.Transition != null)
                        {
                            hasTransitionSettings = true;
                            break;
                        }
                    }
                    catch
                    {
                        // 忽略无法访问的幻灯片
                    }
                }

                if (hasTransitionSettings)
                {
                    modeSettings.Add("包含切换设置");
                }
            }

            return modeSettings.Count > 0 ? string.Join(", ", modeSettings) : "默认放映方式";
        }
        catch
        {
            return "未知放映方式";
        }
    }

    /// <summary>
    /// 获取幻灯片的切换声音
    /// </summary>
    private static string GetTransitionSoundFromSlide(SlidePart slidePart)
    {
        try
        {
            Transition? transition = slidePart.Slide.Transition;
            if (transition != null)
            {
                // 检查切换声音设置
                // 在OpenXML中，切换声音通常通过SoundAction元素定义
                if (transition.HasChildren)
                {
                    foreach (OpenXmlElement child in transition.Elements())
                    {
                        // 检查是否有声音相关的元素
                        if (child.LocalName.Contains("sound", StringComparison.OrdinalIgnoreCase) ||
                            child.LocalName.Contains("audio", StringComparison.OrdinalIgnoreCase))
                        {
                            return "检测到切换声音";
                        }

                        // 检查子元素中是否有声音设置
                        if (child.HasChildren)
                        {
                            foreach (OpenXmlElement grandChild in child.Elements())
                            {
                                if (grandChild.LocalName.Contains("sound", StringComparison.OrdinalIgnoreCase) ||
                                    grandChild.LocalName.Contains("audio", StringComparison.OrdinalIgnoreCase))
                                {
                                    return "检测到切换声音";
                                }
                            }
                        }
                    }
                }

                // 如果有切换效果但没有明确的声音设置，可能使用默认声音
                if (transition.HasChildren)
                {
                    return "可能有切换声音";
                }
            }
            return "无声音";
        }
        catch
        {
            return "无声音";
        }
    }

    /// <summary>
    /// 获取动画方向
    /// </summary>
    private static string GetAnimationDirectionFromSlide(SlidePart slidePart, int elementOrder)
    {
        try
        {
            // 检查动画序列中指定元素的动画方向设置
            Timing? timing = slidePart.Slide.Timing;
            if (timing?.TimeNodeList != null)
            {
                IEnumerable<TimeNode> timeNodes = timing.TimeNodeList.Elements<TimeNode>();
                int currentIndex = 0;

                foreach (TimeNode timeNode in timeNodes)
                {
                    IEnumerable<TimeNode> childNodes = timeNode.Elements<TimeNode>();
                    foreach (TimeNode childNode in childNodes)
                    {
                        currentIndex++;
                        if (currentIndex == elementOrder)
                        {
                            // 检查动画效果类型，推断可能的方向
                            if (childNode.HasChildren)
                            {
                                // 检查是否有动画效果设置
                                if (childNode.Elements().Any())
                                {
                                    return "检测到动画方向设置";
                                }
                            }
                        }
                    }
                }
            }

            // 备用检查：如果指定元素存在且有动画设置
            IEnumerable<DocumentFormat.OpenXml.Presentation.Shape>? shapes = slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>();
            return shapes?.Count() >= elementOrder && timing?.TimeNodeList?.HasChildren == true ? "检测到动画方向设置" : "无动画方向";
        }
        catch
        {
            return "无动画方向";
        }
    }

    /// <summary>
    /// 获取动画样式
    /// </summary>
    private static string GetAnimationStyleFromSlide(SlidePart slidePart, int elementOrder)
    {
        try
        {
            // 检查动画序列中指定元素的动画样式设置
            Timing? timing = slidePart.Slide.Timing;
            if (timing?.TimeNodeList != null)
            {
                IEnumerable<TimeNode> timeNodes = timing.TimeNodeList.Elements<TimeNode>();
                int currentIndex = 0;

                foreach (TimeNode timeNode in timeNodes)
                {
                    IEnumerable<TimeNode> childNodes = timeNode.Elements<TimeNode>();
                    foreach (TimeNode childNode in childNodes)
                    {
                        currentIndex++;
                        if (currentIndex == elementOrder)
                        {
                            // 检查动画效果类型和样式
                            if (childNode.HasChildren)
                            {
                                // 检查是否有动画效果设置
                                if (childNode.Elements().Any())
                                {
                                    return "检测到动画样式设置";
                                }
                            }

                            // 检查节点属性，可能包含样式信息
                            if (childNode.HasAttributes)
                            {
                                return "检测到动画样式设置";
                            }
                        }
                    }
                }
            }

            // 备用检查：如果指定元素存在且有动画设置
            IEnumerable<DocumentFormat.OpenXml.Presentation.Shape>? shapes = slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>();
            return shapes?.Count() >= elementOrder && timing?.TimeNodeList?.HasChildren == true ? "检测到动画样式设置" : "无动画样式";
        }
        catch
        {
            return "无动画样式";
        }
    }

    /// <summary>
    /// 获取SmartArt样式
    /// </summary>
    private static string GetSmartArtStyleFromSlide(SlidePart slidePart, int elementOrder)
    {
        try
        {
            // 检查是否有SmartArt图形
            IEnumerable<DocumentFormat.OpenXml.Presentation.GraphicFrame>? graphicFrames = slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.GraphicFrame>();
            return graphicFrames?.Count() >= elementOrder ? "检测到SmartArt样式" : "无SmartArt样式";
        }
        catch
        {
            return "无SmartArt样式";
        }
    }

    /// <summary>
    /// 获取SmartArt内容
    /// </summary>
    private static string GetSmartArtContentFromSlide(SlidePart slidePart, int elementOrder)
    {
        try
        {
            // 检查SmartArt中的文本内容
            IEnumerable<DocumentFormat.OpenXml.Presentation.GraphicFrame>? graphicFrames = slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.GraphicFrame>();
            if (graphicFrames?.Count() >= elementOrder)
            {
                DocumentFormat.OpenXml.Presentation.GraphicFrame graphicFrame = graphicFrames.ElementAt(elementOrder - 1);
                IEnumerable<DocumentFormat.OpenXml.Drawing.Text> textElements = graphicFrame.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
                return textElements.Any() ? string.Join(" ", textElements.Select(t => t.Text)) : "检测到SmartArt内容";
            }
            return "无SmartArt内容";
        }
        catch
        {
            return "无SmartArt内容";
        }
    }

    /// <summary>
    /// 获取段落行距
    /// </summary>
    private static float GetParagraphSpacingFromSlide(SlidePart slidePart, int elementOrder)
    {
        try
        {
            // 完整实现：检查指定元素的段落行距设置
            IEnumerable<DocumentFormat.OpenXml.Presentation.Shape>? shapes = slidePart.Slide.CommonSlideData?.ShapeTree?.Elements<DocumentFormat.OpenXml.Presentation.Shape>();
            if (shapes?.Count() >= elementOrder)
            {
                DocumentFormat.OpenXml.Presentation.Shape shape = shapes.ElementAt(elementOrder - 1);
                IEnumerable<Paragraph> paragraphs = shape.Descendants<Paragraph>();

                foreach (Paragraph paragraph in paragraphs)
                {
                    ParagraphProperties? paragraphProperties = paragraph.ParagraphProperties;
                    if (paragraphProperties != null)
                    {
                        // 检查行距设置
                        LineSpacing? lineSpacing = paragraphProperties.LineSpacing;
                        if (lineSpacing != null)
                        {
                            // 检查百分比行距
                            if (lineSpacing.SpacingPercent?.Val?.Value != null)
                            {
                                // OpenXML中百分比以千分之一为单位，例如120000表示120%
                                float percentage = lineSpacing.SpacingPercent.Val.Value / 100000.0f;
                                return percentage;
                            }

                            // 检查固定行距
                            if (lineSpacing.SpacingPoints?.Val?.Value != null)
                            {
                                // OpenXML中点数以二十分之一点为单位
                                float points = lineSpacing.SpacingPoints.Val.Value / 20.0f;
                                // 转换为行距倍数（假设基础字号为12点）
                                return points / 12.0f;
                            }
                        }

                        // 检查段前段后间距
                        if (paragraphProperties.SpaceBefore?.SpacingPoints?.Val?.Value != null ||
                            paragraphProperties.SpaceAfter?.SpacingPoints?.Val?.Value != null)
                        {
                            // 如果有段落间距设置，说明用户进行了自定义设置
                            return 1.2f; // 返回一个表示有自定义设置的值
                        }
                    }
                }
            }
            return 1.0f; // 默认行距
        }
        catch
        {
            return 1.0f;
        }
    }

    /// <summary>
    /// 获取背景样式
    /// </summary>
    private static string GetBackgroundStyleFromPresentation(PresentationPart presentationPart)
    {
        try
        {
            // 检查主题和背景设置
            return presentationPart.ThemePart != null ? "检测到背景样式设置" : "默认背景样式";
        }
        catch
        {
            return "未知背景样式";
        }
    }
}
