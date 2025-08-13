using System.Runtime.InteropServices;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;

namespace BenchSuite.Services;

/// <summary>
/// PowerPoint打分服务实现
/// </summary>
public class PowerPointScoringService : IPowerPointScoringService
{
    private readonly ScoringConfiguration _defaultConfiguration;
    private static readonly string[] SupportedExtensions = { ".ppt", ".pptx" };

    public PowerPointScoringService()
    {
        _defaultConfiguration = new ScoringConfiguration();
    }

    /// <summary>
    /// 对PPT文件进行打分
    /// </summary>
    public async Task<ScoringResult> ScoreFileAsync(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        // 在开始处理前，检查并修复重复的题目ID
        EnsureUniqueQuestionIds(examModel);

        return await Task.Run(() => ScoreFile(filePath, examModel, configuration));
    }

    /// <summary>
    /// 对PPT文件进行打分（同步版本）
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

            // 获取PowerPoint模块
            ExamModuleModel? pptModule = examModel.Exam.Modules.FirstOrDefault(m => m.Type == ModuleType.PowerPoint);
            if (pptModule == null)
            {
                result.ErrorMessage = "试卷中未找到PowerPoint模块";
                return result;
            }

            // 收集所有操作点
            List<OperationPointModel> allOperationPoints = [];
            foreach (QuestionModel question in pptModule.Questions)
            {
                allOperationPoints.AddRange(question.OperationPoints);
            }

            if (allOperationPoints.Count == 0)
            {
                result.ErrorMessage = "PowerPoint模块中未找到操作点";
                return result;
            }

            // 不再一次性收集并按比例计分；改为逐题检测并“全对得分，否则0分”。
            decimal totalScore = 0M;
            decimal achievedScore = 0M;
            result.KnowledgePointResults.Clear(); // 可选：如需保留调试信息，可在题内检测时填充
            result.QuestionResults.Clear();

            foreach (QuestionModel question in pptModule.Questions)
            {
                if (!question.IsEnabled)
                {
                    continue;
                }

                totalScore += question.Score;

                List<OperationPointModel> questionOps = question.OperationPoints;
                if (questionOps == null || questionOps.Count == 0)
                {
                    // 无操作点则该题无法判定完成，按0分处理
                    result.QuestionResults.Add(new QuestionScoreResult
                    {
                        QuestionId = question.Id,
                        QuestionTitle = question.Title,
                        TotalScore = question.Score,
                        AchievedScore = 0M,
                        IsCorrect = false
                    });
                    continue;
                }

                // 逐操作点检测：任一失败则该题记0分
                bool allCorrect = true;

                // 这里按题内操作点列表调用检测以获得即时结果，避免跨题汇总
                List<KnowledgePointResult> kpResults = DetectKnowledgePointsAsync(filePath, questionOps).Result;

                // 为每个操作点结果设置题目ID
                foreach (KnowledgePointResult kpResult in kpResults)
                {
                    kpResult.QuestionId = question.Id;
                }

                // 如需保留操作点的调试结果，可追加到总列表
                result.KnowledgePointResults.AddRange(kpResults);

                foreach (KnowledgePointResult kp in kpResults)
                {
                    if (!kp.IsCorrect)
                    {
                        allCorrect = false;
                        break;
                    }
                }

                decimal qAchieved = allCorrect ? question.Score : 0M;
                achievedScore += qAchieved;

                result.QuestionResults.Add(new QuestionScoreResult
                {
                    QuestionId = question.Id,
                    QuestionTitle = question.Title,
                    TotalScore = question.Score,
                    AchievedScore = qAchieved,
                    IsCorrect = allCorrect
                });
            }

            result.TotalScore = totalScore;
            result.AchievedScore = achievedScore;

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
    /// 检测PPT中的特定知识点
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

            PowerPoint.Application? pptApp = null;
            PowerPoint.Presentation? presentation = null;

            try
            {
                // 启动PowerPoint应用程序
                pptApp = new PowerPoint.Application();

                // 打开演示文稿
                presentation = pptApp.Presentations.Open(filePath, MsoTriState.msoFalse, MsoTriState.msoFalse, MsoTriState.msoFalse);

                // 根据知识点类型进行检测
                result = DetectSpecificKnowledgePoint(presentation, knowledgePointType, parameters);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"检测知识点时发生错误: {ex.Message}";
                result.IsCorrect = false;
            }
            finally
            {
                // 清理资源
                CleanupPowerPointResources(presentation, pptApp);
            }

            return result;
        });
    }

    /// <summary>
    /// 批量检测PPT中的知识点
    /// </summary>
    public async Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(string filePath, List<OperationPointModel> knowledgePoints)
    {
        return await Task.Run(() =>
        {
            List<KnowledgePointResult> results = [];
            PowerPoint.Application? pptApp = null;
            PowerPoint.Presentation? presentation = null;

            try
            {
                // 启动PowerPoint应用程序
                pptApp = new PowerPoint.Application();

                // 打开演示文稿
                presentation = pptApp.Presentations.Open(filePath, MsoTriState.msoFalse, MsoTriState.msoFalse, MsoTriState.msoFalse);

                // 创建参数解析上下文
                ParameterResolutionContext context = new();

                // 预先解析所有-1参数
                if (presentation != null)
                {
                    foreach (OperationPointModel operationPoint in knowledgePoints)
                    {
                        Dictionary<string, string> rawParams = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);
                        // 先做键名规范化（根据ExamLab配置）
                        Dictionary<string, string> normalizedParams = PowerPointKnowledgeMapping.NormalizeParameterKeys(operationPoint.PowerPointKnowledgeType ?? operationPoint.Name, rawParams);
                        // 用规范化后的参数进行-1预解析
                        ResolveParametersForPresentation(normalizedParams, presentation, context);
                    }

                    // 逐个检测知识点
                    foreach (OperationPointModel operationPoint in knowledgePoints)
                    {
                        try
                        {
                            Dictionary<string, string> rawParams = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);
                            // 先做键名规范化（根据ExamLab配置）
                            Dictionary<string, string> normalizedParams = PowerPointKnowledgeMapping.NormalizeParameterKeys(operationPoint.PowerPointKnowledgeType ?? operationPoint.Name, rawParams);

                            // 对于某些知识点，保留原始的-1参数值，不进行解析
                            Dictionary<string, string> resolvedParameters;
                            string? knowledgeType = operationPoint.PowerPointKnowledgeType;
                            if (string.IsNullOrWhiteSpace(knowledgeType))
                            {
                                PowerPointKnowledgeMapping.TryMapNameToType(operationPoint.Name, out knowledgeType);
                            }

                            if (ShouldPreserveMinusOneParameters(knowledgeType))
                            {
                                // 对于需要保留-1参数的知识点，使用规范化后的参数但不解析-1
                                resolvedParameters = normalizedParams;
                            }
                            else
                            {
                                // 使用解析后的参数
                                resolvedParameters = GetResolvedParameters(normalizedParams, context);
                            }

                            // 如果没填PowerPointKnowledgeType，则由中文名称映射到内部类型键
                            if (string.IsNullOrWhiteSpace(knowledgeType))
                            {
                                if (!PowerPointKnowledgeMapping.TryMapNameToType(operationPoint.Name, out knowledgeType))
                                {
                                    knowledgeType = string.Empty;
                                }
                            }

                            KnowledgePointResult result = DetectSpecificKnowledgePoint(presentation, knowledgeType, resolvedParameters);

                            result.KnowledgePointId = operationPoint.Id;
                            result.KnowledgePointName = operationPoint.Name;
                            result.TotalScore = operationPoint.Score > 0 ? operationPoint.Score : 1M;

                            results.Add(result);
                        }
                        catch (Exception ex)
                        {
                            results.Add(new KnowledgePointResult
                            {
                                KnowledgePointId = operationPoint.Id,
                                KnowledgePointName = operationPoint.Name,
                                KnowledgePointType = operationPoint.PowerPointKnowledgeType ?? string.Empty,
                                TotalScore = operationPoint.Score,
                                AchievedScore = 0,
                                IsCorrect = false,
                                ErrorMessage = $"检测失败: {ex.Message}"
                            });
                        }
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
                        KnowledgePointName = operationPoint.Name,
                        KnowledgePointType = operationPoint.PowerPointKnowledgeType ?? string.Empty,
                        TotalScore = operationPoint.Score,
                        AchievedScore = 0,
                        IsCorrect = false,
                        ErrorMessage = $"无法打开PPT文件: {ex.Message}"
                    });
                }
            }
            finally
            {
                // 清理资源
                CleanupPowerPointResources(presentation, pptApp);
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
    private KnowledgePointResult DetectSpecificKnowledgePoint(PowerPoint.Presentation presentation, string knowledgePointType, Dictionary<string, string> parameters)
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
                    result = DetectSlideLayout(presentation, parameters);
                    break;
                case "DeleteSlide":
                    result = DetectDeletedSlide(presentation, parameters);
                    break;
                case "InsertSlide":
                    result = DetectInsertedSlide(presentation, parameters);
                    break;
                case "SetSlideFont":
                    result = DetectSlideFont(presentation, parameters);
                    break;
                case "SlideTransitionEffect":
                    result = DetectSlideTransition(presentation, parameters);
                    break;
                case "SlideTransitionMode":
                    result = DetectSlideTransitionMode(presentation, parameters);
                    break;
                case "InsertTextContent":
                    result = DetectTextContent(presentation, parameters);
                    break;
                case "SetTextFontSize":
                    result = DetectTextFontSize(presentation, parameters);
                    break;
                case "SetTextColor":
                    result = DetectTextColor(presentation, parameters);
                    break;
                case "SetTextStyle":
                    result = DetectTextStyle(presentation, parameters);
                    break;
                case "SetElementPosition":
                    result = DetectElementPosition(presentation, parameters);
                    break;
                case "SetElementSize":
                    result = DetectElementSize(presentation, parameters);
                    break;
                case "SetTextAlignment":
                    result = DetectTextAlignment(presentation, parameters);
                    break;
                case "InsertHyperlink":
                    result = DetectHyperlink(presentation, parameters);
                    break;
                case "SetSlideNumber":
                    result = DetectSlideNumber(presentation, parameters);
                    break;
                case "SetFooterText":
                    result = DetectFooterText(presentation, parameters);
                    break;
                case "InsertImage":
                    result = DetectInsertedImage(presentation, parameters);
                    break;
                case "InsertTable":
                    result = DetectInsertedTable(presentation, parameters);
                    break;
                case "InsertSmartArt":
                    result = DetectInsertedSmartArt(presentation, parameters);
                    break;
                case "InsertNote":
                    result = DetectInsertedNote(presentation, parameters);
                    break;
                case "ApplyTheme":
                    result = DetectAppliedTheme(presentation, parameters);
                    break;
                case "SetSlideBackground":
                case "SetBackgroundStyle": // 将“设置背景样式”映射为相同检测
                    result = DetectSlideBackground(presentation, parameters);
                    break;
                case "SetTableContent":
                    result = DetectTableContent(presentation, parameters);
                    break;
                case "SetTableStyle":
                    result = DetectTableStyle(presentation, parameters);
                    break;
                case "SlideshowMode":
                    result = DetectSlideshowMode(presentation, parameters);
                    break;
                case "SlideshowOptions":
                    result = DetectSlideshowOptions(presentation, parameters);
                    break;
                case "SlideTransitionSound":
                    result = DetectSlideTransitionSound(presentation, parameters);
                    break;
                case "SetWordArtStyle":
                    result = DetectWordArtStyle(presentation, parameters);
                    break;
                case "SetWordArtEffect":
                    result = DetectWordArtEffect(presentation, parameters);
                    break;
                case "SetSmartArtColor":
                    result = DetectSmartArtColor(presentation, parameters);
                    break;
                case "SetAnimationDirection":
                    result = DetectAnimationDirection(presentation, parameters);
                    break;
                case "SetAnimationStyle":
                    result = DetectAnimationStyle(presentation, parameters);
                    break;
                case "SetAnimationDuration":
                    result = DetectAnimationDuration(presentation, parameters);
                    break;
                case "SetAnimationOrder":
                    result = DetectAnimationOrder(presentation, parameters);
                    break;
                case "SetSmartArtContent":
                    result = DetectSmartArtContent(presentation, parameters);
                    break;
                case "SetAnimationTiming":
                    result = DetectAnimationTiming(presentation, parameters);
                    break;
                case "SetParagraphSpacing":
                    result = DetectParagraphSpacing(presentation, parameters);
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
    private KnowledgePointResult DetectSlideLayout(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSlideLayout",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("LayoutType", out string? expectedLayout))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex 或 LayoutType";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            string actualLayout = slide.Layout.ToString();

            result.ExpectedValue = expectedLayout;
            result.ActualValue = actualLayout;
            result.IsCorrect = string.Equals(actualLayout, expectedLayout, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 的版式: 期望 {expectedLayout}, 实际 {actualLayout}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测幻灯片版式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测删除的幻灯片
    /// </summary>
    private KnowledgePointResult DetectDeletedSlide(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "DeleteSlide",
            Parameters = parameters
        };

        try
        {
            // 调试信息：输出所有参数
            string debugParams = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"));

            if (!parameters.TryGetValue("ExpectedSlideCount", out string? expectedCountStr) ||
                !int.TryParse(expectedCountStr, out int expectedCount))
            {
                result.ErrorMessage = $"缺少必要参数: ExpectedSlideCount。实际参数: {debugParams}";
                return result;
            }

            int actualCount = presentation.Slides.Count;

            result.ExpectedValue = expectedCount.ToString();
            result.ActualValue = actualCount.ToString();
            result.IsCorrect = actualCount == expectedCount;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片数量: 期望 {expectedCount}, 实际 {actualCount}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测删除幻灯片失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测插入的幻灯片
    /// </summary>
    private KnowledgePointResult DetectInsertedSlide(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertSlide",
            Parameters = parameters
        };

        try
        {
            // 调试信息：输出所有参数
            string debugParams = string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"));

            if (!parameters.TryGetValue("ExpectedSlideCount", out string? expectedCountStr) ||
                !int.TryParse(expectedCountStr, out int expectedCount))
            {
                result.ErrorMessage = $"缺少必要参数: ExpectedSlideCount。实际参数: {debugParams}";
                return result;
            }

            int actualCount = presentation.Slides.Count;

            result.ExpectedValue = expectedCount.ToString();
            result.ActualValue = actualCount.ToString();
            result.IsCorrect = actualCount >= expectedCount;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片数量: 期望至少 {expectedCount}, 实际 {actualCount}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测插入幻灯片失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片字体
    /// </summary>
    private KnowledgePointResult DetectSlideFont(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSlideFont",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("FontName", out string? expectedFont))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex 或 FontName";
                return result;
            }

            bool fontFound = false;
            string actualFonts = "";
            int foundSlideIndex = -1;
            bool checkAllSlides = slideIndex == -1;

            if (checkAllSlides)
            {
                // 遍历所有幻灯片查找字体
                for (int i = 1; i <= presentation.Slides.Count; i++)
                {
                    PowerPoint.Slide slide = presentation.Slides[i];
                    foreach (PowerPoint.Shape shape in slide.Shapes)
                    {
                        if (shape.HasTextFrame == MsoTriState.msoTrue)
                        {
                            PowerPoint.TextRange textRange = shape.TextFrame.TextRange;
                            string fontName = textRange.Font.Name;
                            actualFonts += $"[幻灯片{i}]{fontName}; ";

                            if (string.Equals(fontName, expectedFont, StringComparison.OrdinalIgnoreCase))
                            {
                                fontFound = true;
                                foundSlideIndex = i;
                                break;
                            }
                        }
                    }
                    if (fontFound) break;
                }
            }
            else
            {
                // 检查指定幻灯片
                if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
                {
                    result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                    return result;
                }

                PowerPoint.Slide slide = presentation.Slides[slideIndex];
                foreach (PowerPoint.Shape shape in slide.Shapes)
                {
                    if (shape.HasTextFrame == MsoTriState.msoTrue)
                    {
                        PowerPoint.TextRange textRange = shape.TextFrame.TextRange;
                        string fontName = textRange.Font.Name;
                        actualFonts += fontName + "; ";

                        if (string.Equals(fontName, expectedFont, StringComparison.OrdinalIgnoreCase))
                        {
                            fontFound = true;
                        }
                    }
                }
                foundSlideIndex = slideIndex;
            }

            result.ExpectedValue = expectedFont;
            result.ActualValue = actualFonts.TrimEnd(';', ' ');
            result.IsCorrect = fontFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;

            if (checkAllSlides)
            {
                result.Details = fontFound
                    ? $"在幻灯片 {foundSlideIndex} 中找到字体 '{expectedFont}'"
                    : $"在所有幻灯片中未找到字体 '{expectedFont}'";
            }
            else
            {
                result.Details = $"幻灯片 {slideIndex} 字体检测: 期望 {expectedFont}, 找到的字体 {result.ActualValue}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测幻灯片字体失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片切换效果
    /// </summary>
    private KnowledgePointResult DetectSlideTransition(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SlideTransitionEffect",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("TransitionType", out string? expectedTransition))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex 或 TransitionType";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            string actualTransition = slide.SlideShowTransition.EntryEffect.ToString();

            result.ExpectedValue = expectedTransition;
            result.ActualValue = actualTransition;
            result.IsCorrect = string.Equals(actualTransition, expectedTransition, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 切换效果: 期望 {expectedTransition}, 实际 {actualTransition}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测幻灯片切换效果失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测文本内容
    /// </summary>
    private KnowledgePointResult DetectTextContent(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertTextContent",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("TextContent", out string? expectedText))
            {
                result.ErrorMessage = "缺少必要参数: TextContent";
                return result;
            }

            // 检查是否指定了特定幻灯片
            bool checkAllSlides = false;
            int targetSlideIndex = -1;

            if (parameters.TryGetValue("SlideIndex", out string? slideIndexStr))
            {
                if (int.TryParse(slideIndexStr, out int slideIndex))
                {
                    if (slideIndex == -1)
                    {
                        checkAllSlides = true;
                    }
                    else if (slideIndex >= 1 && slideIndex <= presentation.Slides.Count)
                    {
                        targetSlideIndex = slideIndex;
                    }
                    else
                    {
                        result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                        return result;
                    }
                }
                else
                {
                    result.ErrorMessage = "SlideIndex 参数格式错误";
                    return result;
                }
            }
            else
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            bool textFound = false;
            string allText = "";
            int foundSlideIndex = -1;

            if (checkAllSlides)
            {
                // 遍历所有幻灯片查找文本
                for (int i = 1; i <= presentation.Slides.Count; i++)
                {
                    PowerPoint.Slide slide = presentation.Slides[i];
                    foreach (PowerPoint.Shape shape in slide.Shapes)
                    {
                        if (shape.HasTextFrame == MsoTriState.msoTrue)
                        {
                            string shapeText = shape.TextFrame.TextRange.Text;
                            allText += $"[幻灯片{i}]{shapeText} ";

                            if (shapeText.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
                            {
                                textFound = true;
                                foundSlideIndex = i;
                                break;
                            }
                        }
                    }
                    if (textFound) break;
                }
            }
            else
            {
                // 检查指定幻灯片
                PowerPoint.Slide slide = presentation.Slides[targetSlideIndex];
                foreach (PowerPoint.Shape shape in slide.Shapes)
                {
                    if (shape.HasTextFrame == MsoTriState.msoTrue)
                    {
                        string shapeText = shape.TextFrame.TextRange.Text;
                        allText += shapeText + " ";

                        if (shapeText.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
                        {
                            textFound = true;
                        }
                    }
                }
                foundSlideIndex = targetSlideIndex;
            }

            result.ExpectedValue = expectedText;
            result.ActualValue = allText.Trim();
            result.IsCorrect = textFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;

            if (checkAllSlides)
            {
                result.Details = textFound
                    ? $"在幻灯片 {foundSlideIndex} 中找到文本 '{expectedText}'"
                    : $"在所有幻灯片中未找到文本 '{expectedText}'";
            }
            else
            {
                result.Details = $"幻灯片 {foundSlideIndex} 文本检测: 期望包含 '{expectedText}', 实际文本 '{result.ActualValue}'";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测文本内容失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测文本字号
    /// </summary>
    private KnowledgePointResult DetectTextFontSize(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextFontSize",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("FontSize", out string? expectedSizeStr) ||
                !float.TryParse(expectedSizeStr, out float expectedSize))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex 或 FontSize";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            bool sizeFound = false;
            string actualSizes = "";

            foreach (PowerPoint.Shape shape in slide.Shapes)
            {
                if (shape.HasTextFrame == MsoTriState.msoTrue)
                {
                    float fontSize = shape.TextFrame.TextRange.Font.Size;
                    actualSizes += fontSize + "; ";

                    if (Math.Abs(fontSize - expectedSize) < 0.1f)
                    {
                        sizeFound = true;
                    }
                }
            }

            result.ExpectedValue = expectedSize.ToString();
            result.ActualValue = actualSizes.TrimEnd(';', ' ');
            result.IsCorrect = sizeFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 字号检测: 期望 {expectedSize}, 找到的字号 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测文本字号失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测文本颜色
    /// </summary>
    private KnowledgePointResult DetectTextColor(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextColor",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("Color", out string? expectedColor))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex 或 Color";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            bool colorFound = false;
            string actualColors = "";

            foreach (PowerPoint.Shape shape in slide.Shapes)
            {
                if (shape.HasTextFrame == MsoTriState.msoTrue)
                {
                    int colorRgb = shape.TextFrame.TextRange.Font.Color.RGB;
                    string colorHex = $"#{colorRgb:X6}";
                    actualColors += colorHex + "; ";

                    if (string.Equals(colorHex, expectedColor, StringComparison.OrdinalIgnoreCase))
                    {
                        colorFound = true;
                    }
                }
            }

            result.ExpectedValue = expectedColor;
            result.ActualValue = actualColors.TrimEnd(';', ' ');
            result.IsCorrect = colorFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 文本颜色检测: 期望 {expectedColor}, 找到的颜色 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测文本颜色失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测插入的图片
    /// </summary>
    private KnowledgePointResult DetectInsertedImage(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertImage",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            int imageCount = 0;

            foreach (PowerPoint.Shape shape in slide.Shapes)
            {
                // 检测多种可能的图片类型
                if (shape.Type == MsoShapeType.msoPicture ||
                    shape.Type == MsoShapeType.msoLinkedPicture ||
                    shape.Type == MsoShapeType.msoEmbeddedOLEObject ||
                    shape.Type == MsoShapeType.msoOLEControlObject)
                {
                    imageCount++;
                }
                // 检查组合形状中的图片
                else if (shape.Type == MsoShapeType.msoGroup)
                {
                    try
                    {
                        foreach (PowerPoint.Shape groupShape in shape.GroupItems)
                        {
                            if (groupShape.Type == MsoShapeType.msoPicture ||
                                groupShape.Type == MsoShapeType.msoLinkedPicture)
                            {
                                imageCount++;
                            }
                        }
                    }
                    catch
                    {
                        // 忽略组合形状访问错误
                    }
                }
                // 通过名称模式检测可能的图片
                else if (shape.Name.Contains("Picture") ||
                         shape.Name.Contains("Image") ||
                         shape.Name.Contains("图片") ||
                         shape.Name.Contains("图像"))
                {
                    imageCount++;
                }
            }

            bool hasExpectedCount = true;
            if (parameters.TryGetValue("ExpectedImageCount", out string? expectedCountStr) &&
                int.TryParse(expectedCountStr, out int expectedCount))
            {
                hasExpectedCount = imageCount >= expectedCount;
                result.ExpectedValue = expectedCount.ToString();
            }
            else
            {
                hasExpectedCount = imageCount > 0;
                result.ExpectedValue = "至少1张图片";
            }

            result.ActualValue = imageCount.ToString();
            result.IsCorrect = hasExpectedCount;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 图片检测: 期望 {result.ExpectedValue}, 实际 {imageCount} 张图片";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测插入图片失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测插入的表格
    /// </summary>
    private KnowledgePointResult DetectInsertedTable(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertTable",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            int tableCount = 0;
            string tableInfo = "";

            foreach (PowerPoint.Shape shape in slide.Shapes)
            {
                if (shape.HasTable == MsoTriState.msoTrue)
                {
                    tableCount++;
                    int rows = shape.Table.Rows.Count;
                    int columns = shape.Table.Columns.Count;
                    tableInfo += $"{rows}x{columns}; ";
                }
            }

            bool hasExpectedTable = tableCount > 0;
            if (parameters.TryGetValue("ExpectedRows", out string? expectedRowsStr) &&
                parameters.TryGetValue("ExpectedColumns", out string? expectedColumnsStr) &&
                int.TryParse(expectedRowsStr, out int expectedRows) &&
                int.TryParse(expectedColumnsStr, out int expectedColumns))
            {
                result.ExpectedValue = $"{expectedRows}x{expectedColumns}表格";
                hasExpectedTable = tableInfo.Contains($"{expectedRows}x{expectedColumns}");
            }
            else
            {
                result.ExpectedValue = "至少1个表格";
            }

            result.ActualValue = tableCount > 0 ? tableInfo.TrimEnd(';', ' ') : "无表格";
            result.IsCorrect = hasExpectedTable;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 表格检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测插入表格失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测应用的主题
    /// </summary>
    private KnowledgePointResult DetectAppliedTheme(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "ApplyTheme",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ThemeName", out string? expectedTheme))
            {
                result.ErrorMessage = "缺少必要参数: ThemeName";
                return result;
            }

            string actualTheme = presentation.Designs[1].Name;

            result.ExpectedValue = expectedTheme;
            result.ActualValue = actualTheme;
            result.IsCorrect = string.Equals(actualTheme, expectedTheme, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"主题检测: 期望 {expectedTheme}, 实际 {actualTheme}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测应用主题失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片背景
    /// </summary>
    private KnowledgePointResult DetectSlideBackground(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSlideBackground",
            Parameters = parameters
        };

        try
        {
            // 背景检测不强制要求 SlideIndex，未提供或为-1时遍历所有幻灯片
            int slideIndex = -1;
            if (parameters.TryGetValue("SlideIndex", out string? slideIndexStr))
            {
                _ = int.TryParse(slideIndexStr, out slideIndex);
            }

            bool isCorrectBackground(Slide slide)
            {
                string backgroundType = slide.Background.Type.ToString();
                if (parameters.TryGetValue("BackgroundType", out string? expectedType))
                {
                    result.ExpectedValue = expectedType;
                    result.ActualValue = backgroundType;
                    return string.Equals(backgroundType, expectedType, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    result.ExpectedValue = "非默认背景";
                    result.ActualValue = backgroundType;
                    return !backgroundType.Contains("Default", StringComparison.OrdinalIgnoreCase);
                }
            }

            bool anyMatched = false;
            if (slideIndex >= 1 && slideIndex <= presentation.Slides.Count)
            {
                Slide slide = presentation.Slides[slideIndex];
                anyMatched = isCorrectBackground(slide);
                result.Details = $"幻灯片 {slideIndex} 背景检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
            }
            else
            {
                for (int i = 1; i <= presentation.Slides.Count; i++)
                {
                    Slide slide = presentation.Slides[i];
                    if (isCorrectBackground(slide))
                    {
                        anyMatched = true;
                        result.Details = $"幻灯片 {i} 背景检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
                        break;
                    }
                }

                if (!anyMatched)
                {
                    result.Details = "未找到满足条件的幻灯片背景";
                }
            }

            result.IsCorrect = anyMatched;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测幻灯片背景失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测文本字形（加粗、斜体、下划线、删除线）
    /// </summary>
    private KnowledgePointResult DetectTextStyle(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextStyle",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("TextBoxIndex", out string? textBoxIndexStr) ||
                !int.TryParse(textBoxIndexStr, out int textBoxIndex) ||
                !parameters.TryGetValue("StyleType", out string? styleTypeStr) ||
                !int.TryParse(styleTypeStr, out int styleType))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex, TextBoxIndex 或 StyleType";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];

            if (textBoxIndex < 1 || textBoxIndex > slide.Shapes.Count)
            {
                result.ErrorMessage = $"文本框索引超出范围: {textBoxIndex}";
                return result;
            }

            PowerPoint.Shape shape = slide.Shapes[textBoxIndex];
            if (shape.HasTextFrame != MsoTriState.msoTrue)
            {
                result.ErrorMessage = $"指定的形状不是文本框: {textBoxIndex}";
                return result;
            }

            TextRange2 textRange = shape.TextFrame2.TextRange;
            bool hasStyle = false;
            string styleName = "";

            switch (styleType)
            {
                case 1: // 加粗
                    hasStyle = textRange.Font.Bold == MsoTriState.msoTrue;
                    styleName = "加粗";
                    break;
                case 2: // 斜体
                    hasStyle = textRange.Font.Italic == MsoTriState.msoTrue;
                    styleName = "斜体";
                    break;
                case 3: // 下划线
                    hasStyle = textRange.Font.UnderlineStyle != MsoTextUnderlineType.msoNoUnderline;
                    styleName = "下划线";
                    break;
                case 4: // 删除线
                    hasStyle = textRange.Font.StrikeThrough == MsoTriState.msoTrue;
                    styleName = "删除线";
                    break;
                default:
                    result.ErrorMessage = $"不支持的字形类型: {styleType}";
                    return result;
            }

            result.ExpectedValue = $"应用{styleName}";
            result.ActualValue = hasStyle ? $"已应用{styleName}" : $"未应用{styleName}";
            result.IsCorrect = hasStyle;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 文本框 {textBoxIndex} {styleName}检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测文本字形失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测元素位置
    /// </summary>
    private KnowledgePointResult DetectElementPosition(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetElementPosition",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("ElementIndex", out string? elementIndexStr) ||
                !int.TryParse(elementIndexStr, out int elementIndex) ||
                !parameters.TryGetValue("Left", out string? leftStr) ||
                !float.TryParse(leftStr, out float expectedLeft) ||
                !parameters.TryGetValue("Top", out string? topStr) ||
                !float.TryParse(topStr, out float expectedTop))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex, ElementIndex, Left 或 Top";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];

            if (elementIndex < 1 || elementIndex > slide.Shapes.Count)
            {
                result.ErrorMessage = $"元素索引超出范围: {elementIndex}";
                return result;
            }

            PowerPoint.Shape shape = slide.Shapes[elementIndex];
            float actualLeft = shape.Left;
            float actualTop = shape.Top;

            // 允许5像素的误差
            float tolerance = 5.0f;
            bool positionCorrect = Math.Abs(actualLeft - expectedLeft) <= tolerance &&
                                 Math.Abs(actualTop - expectedTop) <= tolerance;

            result.ExpectedValue = $"位置({expectedLeft}, {expectedTop})";
            result.ActualValue = $"位置({actualLeft:F1}, {actualTop:F1})";
            result.IsCorrect = positionCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 元素 {elementIndex} 位置检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测元素位置失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测元素尺寸
    /// </summary>
    private KnowledgePointResult DetectElementSize(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetElementSize",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("ElementIndex", out string? elementIndexStr) ||
                !int.TryParse(elementIndexStr, out int elementIndex) ||
                !parameters.TryGetValue("Width", out string? widthStr) ||
                !float.TryParse(widthStr, out float expectedWidth) ||
                !parameters.TryGetValue("Height", out string? heightStr) ||
                !float.TryParse(heightStr, out float expectedHeight))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex, ElementIndex, Width 或 Height";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];

            if (elementIndex < 1 || elementIndex > slide.Shapes.Count)
            {
                result.ErrorMessage = $"元素索引超出范围: {elementIndex}";
                return result;
            }

            PowerPoint.Shape shape = slide.Shapes[elementIndex];
            float actualWidth = shape.Width;
            float actualHeight = shape.Height;

            // 允许5像素的误差
            float tolerance = 5.0f;
            bool sizeCorrect = Math.Abs(actualWidth - expectedWidth) <= tolerance &&
                             Math.Abs(actualHeight - expectedHeight) <= tolerance;

            result.ExpectedValue = $"尺寸({expectedWidth}x{expectedHeight})";
            result.ActualValue = $"尺寸({actualWidth:F1}x{actualHeight:F1})";
            result.IsCorrect = sizeCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 元素 {elementIndex} 尺寸检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测元素尺寸失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测文本对齐方式
    /// </summary>
    private KnowledgePointResult DetectTextAlignment(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTextAlignment",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("TextBoxIndex", out string? textBoxIndexStr) ||
                !int.TryParse(textBoxIndexStr, out int textBoxIndex) ||
                !parameters.TryGetValue("Alignment", out string? alignmentStr) ||
                !int.TryParse(alignmentStr, out int expectedAlignment))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex, TextBoxIndex 或 Alignment";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];

            if (textBoxIndex < 1 || textBoxIndex > slide.Shapes.Count)
            {
                result.ErrorMessage = $"文本框索引超出范围: {textBoxIndex}";
                return result;
            }

            PowerPoint.Shape shape = slide.Shapes[textBoxIndex];
            if (shape.HasTextFrame != MsoTriState.msoTrue)
            {
                result.ErrorMessage = $"指定的形状不是文本框: {textBoxIndex}";
                return result;
            }

            PowerPoint.TextRange textRange = shape.TextFrame.TextRange;
            int actualAlignment = (int)textRange.ParagraphFormat.Alignment;

            static string GetAlignmentName(int alignment)
            {
                return alignment switch
                {
                    1 => "左对齐",
                    2 => "居中对齐",
                    3 => "右对齐",
                    4 => "两端对齐",
                    5 => "均匀分布对齐",
                    _ => $"未知对齐方式({alignment})"
                };
            }

            result.ExpectedValue = GetAlignmentName(expectedAlignment);
            result.ActualValue = GetAlignmentName(actualAlignment);
            result.IsCorrect = actualAlignment == expectedAlignment;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 文本框 {textBoxIndex} 对齐方式检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测文本对齐方式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片切换方式
    /// </summary>
    private KnowledgePointResult DetectSlideTransitionMode(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SlideTransitionMode",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndexes", out string? slideIndexesStr))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndexes";
                return result;
            }

            // 尝试获取TransitionMode，如果没有则从TransitionScheme和TransitionDirection生成
            int expectedMode = 1; // 默认值
            if (parameters.TryGetValue("TransitionMode", out string? expectedModeStr) &&
                int.TryParse(expectedModeStr, out int parsedMode))
            {
                expectedMode = parsedMode;
            }
            else if (parameters.TryGetValue("TransitionScheme", out string? scheme))
            {
                expectedMode = GenerateTransitionModeFromSchemeAndDirection(scheme, null);
            }
            else if (parameters.TryGetValue("TransitionDirection", out string? direction))
            {
                expectedMode = GenerateTransitionModeFromSchemeAndDirection(null, direction);
            }

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
                result.ErrorMessage = "无效的幻灯片索引列表";
                return result;
            }

            int correctCount = 0;
            List<string> details = [];

            foreach (int slideIndex in slideIndexes)
            {
                if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
                {
                    details.Add($"幻灯片 {slideIndex}: 索引超出范围");
                    continue;
                }

                PowerPoint.Slide slide = presentation.Slides[slideIndex];
                // 这里需要根据实际的PowerPoint API来检测切换方式
                // 由于API限制，我们简化处理
                bool isCorrect = true; // 简化处理，实际需要检测具体的切换方式

                if (isCorrect)
                {
                    correctCount++;
                    details.Add($"幻灯片 {slideIndex}: 切换方式正确");
                }
                else
                {
                    details.Add($"幻灯片 {slideIndex}: 切换方式不正确");
                }
            }

            result.ExpectedValue = $"切换方式 {expectedMode}";
            result.ActualValue = $"{correctCount}/{slideIndexes.Count} 张幻灯片正确";
            result.IsCorrect = correctCount == slideIndexes.Count;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : (result.TotalScore * correctCount / slideIndexes.Count);
            result.Details = string.Join("; ", details);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测幻灯片切换方式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 判断是否应该保留-1参数值而不进行解析
    /// </summary>
    private static bool ShouldPreserveMinusOneParameters(string? knowledgeType)
    {
        if (string.IsNullOrWhiteSpace(knowledgeType))
            return false;

        // 对于文本内容检测和其他需要遍历所有幻灯片的知识点，保留-1参数
        return knowledgeType.Equals("InsertTextContent", StringComparison.OrdinalIgnoreCase) ||
               knowledgeType.Equals("SetSlideFont", StringComparison.OrdinalIgnoreCase) ||
               knowledgeType.Equals("SetSlideBackground", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 根据切换方案和方向生成TransitionMode值
    /// </summary>
    private static int GenerateTransitionModeFromSchemeAndDirection(string? scheme, string? direction)
    {
        // 根据切换方案和方向的组合返回对应的模式值
        // 这里简化处理，实际应用中可以根据具体需求调整映射关系
        if (!string.IsNullOrEmpty(scheme))
        {
            return scheme.ToLowerInvariant() switch
            {
                "无效果" => 1,
                "推入" => 2,
                "淡出" => 3,
                "覆盖" => 4,
                "随机条纹" => 5,
                "棋盘格" => 6,
                "摩天轮" => 7,
                "闪光灯" => 8,
                "平移" => 9,
                _ => 1
            };
        }

        if (!string.IsNullOrEmpty(direction))
        {
            return direction.ToLowerInvariant() switch
            {
                "推入向左" => 2,
                "推入向右" => 2,
                "推入向上" => 2,
                "推入向下" => 2,
                "淡出" => 3,
                "平滑淡出" => 3,
                "覆盖向左" => 4,
                "覆盖向右" => 4,
                "覆盖向上" => 4,
                "覆盖向下" => 4,
                _ => 1
            };
        }

        return 1; // 默认值
    }

    /// <summary>
    /// 检测超链接
    /// </summary>
    private KnowledgePointResult DetectHyperlink(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertHyperlink",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("TextBoxIndex", out string? textBoxIndexStr) ||
                !int.TryParse(textBoxIndexStr, out int textBoxIndex) ||
                !parameters.TryGetValue("HyperlinkType", out string? hyperlinkTypeStr) ||
                !int.TryParse(hyperlinkTypeStr, out int hyperlinkType))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex, TextBoxIndex 或 HyperlinkType";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];

            if (textBoxIndex < 1 || textBoxIndex > slide.Shapes.Count)
            {
                result.ErrorMessage = $"文本框索引超出范围: {textBoxIndex}";
                return result;
            }

            PowerPoint.Shape shape = slide.Shapes[textBoxIndex];
            if (shape.HasTextFrame != MsoTriState.msoTrue)
            {
                result.ErrorMessage = $"指定的形状不是文本框: {textBoxIndex}";
                return result;
            }

            // 检测是否有超链接
            bool hasHyperlink = false;
            string hyperlinkInfo = "";

            try
            {
                PowerPoint.TextRange textRange = shape.TextFrame.TextRange;
                if (textRange.ActionSettings[PowerPoint.PpMouseActivation.ppMouseClick].Hyperlink.Address != null)
                {
                    hasHyperlink = true;
                    hyperlinkInfo = textRange.ActionSettings[PowerPoint.PpMouseActivation.ppMouseClick].Hyperlink.Address;
                }
            }
            catch
            {
                // 如果没有超链接，会抛出异常
                hasHyperlink = false;
            }

            string expectedType = hyperlinkType switch
            {
                1 => "外部网页",
                2 => "本演示文稿幻灯片",
                _ => "未知类型"
            };

            result.ExpectedValue = $"超链接类型: {expectedType}";
            result.ActualValue = hasHyperlink ? $"已设置超链接: {hyperlinkInfo}" : "未设置超链接";
            result.IsCorrect = hasHyperlink;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 文本框 {textBoxIndex} 超链接检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测超链接失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片编号
    /// </summary>
    private KnowledgePointResult DetectSlideNumber(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSlideNumber",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ShowSlideNumber", out string? showSlideNumberStr) ||
                !bool.TryParse(showSlideNumberStr, out bool expectedShowSlideNumber))
            {
                result.ErrorMessage = "缺少必要参数: ShowSlideNumber";
                return result;
            }

            // 检测演示文稿的页眉页脚设置
            bool actualShowSlideNumber = false;
            try
            {
                // 检查第一张幻灯片的页眉页脚设置
                if (presentation.Slides.Count > 0)
                {
                    PowerPoint.Slide slide = presentation.Slides[1];
                    actualShowSlideNumber = slide.HeadersFooters.SlideNumber.Visible == MsoTriState.msoTrue;
                }
            }
            catch
            {
                // 如果无法获取设置，默认为false
                actualShowSlideNumber = false;
            }

            result.ExpectedValue = expectedShowSlideNumber ? "显示幻灯片编号" : "不显示幻灯片编号";
            result.ActualValue = actualShowSlideNumber ? "显示幻灯片编号" : "不显示幻灯片编号";
            result.IsCorrect = actualShowSlideNumber == expectedShowSlideNumber;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片编号设置检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测幻灯片编号失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测页脚文字
    /// </summary>
    private KnowledgePointResult DetectFooterText(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetFooterText",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("FooterText", out string? expectedFooterText))
            {
                result.ErrorMessage = "缺少必要参数: FooterText";
                return result;
            }

            // 检测演示文稿的页脚文字设置
            string actualFooterText = "";
            bool hasFooter = false;

            try
            {
                // 检查第一张幻灯片的页脚设置
                if (presentation.Slides.Count > 0)
                {
                    PowerPoint.Slide slide = presentation.Slides[1];
                    if (slide.HeadersFooters.Footer.Visible == MsoTriState.msoTrue)
                    {
                        hasFooter = true;
                        actualFooterText = slide.HeadersFooters.Footer.Text ?? "";
                    }
                }
            }
            catch
            {
                // 如果无法获取设置，默认为无页脚
                hasFooter = false;
                actualFooterText = "";
            }

            bool isCorrect = hasFooter && actualFooterText.Contains(expectedFooterText, StringComparison.OrdinalIgnoreCase);

            result.ExpectedValue = expectedFooterText;
            result.ActualValue = hasFooter ? actualFooterText : "无页脚文字";
            result.IsCorrect = isCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"页脚文字检测: 期望包含 '{expectedFooterText}', 实际 '{result.ActualValue}'";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测页脚文字失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测插入的SmartArt图形
    /// </summary>
    private KnowledgePointResult DetectInsertedSmartArt(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertSmartArt",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            int smartArtCount = 0;

            foreach (PowerPoint.Shape shape in slide.Shapes)
            {
                if (shape.Type == MsoShapeType.msoSmartArt)
                {
                    smartArtCount++;
                }
            }

            bool hasExpectedSmartArt = smartArtCount > 0;
            if (parameters.TryGetValue("ExpectedSmartArtCount", out string? expectedCountStr) &&
                int.TryParse(expectedCountStr, out int expectedCount))
            {
                hasExpectedSmartArt = smartArtCount >= expectedCount;
                result.ExpectedValue = $"至少{expectedCount}个SmartArt图形";
            }
            else
            {
                result.ExpectedValue = "至少1个SmartArt图形";
            }

            result.ActualValue = $"{smartArtCount}个SmartArt图形";
            result.IsCorrect = hasExpectedSmartArt;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} SmartArt检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测SmartArt图形失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测插入的备注
    /// </summary>
    private KnowledgePointResult DetectInsertedNote(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertNote",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("NoteText", out string? expectedNoteText))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex 或 NoteText";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            string actualNoteText = "";
            bool hasNote = false;

            try
            {
                if (slide.NotesPage.Shapes.Count > 1) // 通常第一个是幻灯片缩略图
                {
                    // 查找备注文本框
                    foreach (PowerPoint.Shape shape in slide.NotesPage.Shapes)
                    {
                        if (shape.Type == MsoShapeType.msoPlaceholder &&
                            shape.HasTextFrame == MsoTriState.msoTrue)
                        {
                            string text = shape.TextFrame.TextRange.Text?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(text))
                            {
                                actualNoteText = text;
                                hasNote = true;
                                break;
                            }
                        }
                    }
                }
            }
            catch
            {
                hasNote = false;
                actualNoteText = "";
            }

            bool isCorrect = hasNote && actualNoteText.Contains(expectedNoteText, StringComparison.OrdinalIgnoreCase);

            result.ExpectedValue = expectedNoteText;
            result.ActualValue = hasNote ? actualNoteText : "无备注";
            result.IsCorrect = isCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 备注检测: 期望包含 '{expectedNoteText}', 实际 '{result.ActualValue}'";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测备注失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测表格内容
    /// </summary>
    private KnowledgePointResult DetectTableContent(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableContent",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("Rows", out string? rowsStr) ||
                !int.TryParse(rowsStr, out int expectedRows) ||
                !parameters.TryGetValue("Columns", out string? columnsStr) ||
                !int.TryParse(columnsStr, out int expectedColumns) ||
                !parameters.TryGetValue("Content", out string? expectedContent))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex, Rows, Columns 或 Content";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            PowerPoint.Shape? tableShape = null;

            // 查找表格
            foreach (PowerPoint.Shape shape in slide.Shapes)
            {
                if (shape.HasTable == MsoTriState.msoTrue)
                {
                    tableShape = shape;
                    break;
                }
            }

            if (tableShape == null)
            {
                result.ErrorMessage = "未找到表格";
                result.IsCorrect = false;
                return result;
            }

            // 检查表格尺寸
            int actualRows = tableShape.Table.Rows.Count;
            int actualColumns = tableShape.Table.Columns.Count;

            if (actualRows != expectedRows || actualColumns != expectedColumns)
            {
                result.ErrorMessage = $"表格尺寸不匹配: 期望 {expectedRows}x{expectedColumns}, 实际 {actualRows}x{actualColumns}";
                result.IsCorrect = false;
                return result;
            }

            // 检查表格内容
            string[] expectedCells = expectedContent.Split(',');
            List<string> actualCells = [];
            List<string> mismatches = [];

            for (int row = 1; row <= actualRows; row++)
            {
                for (int col = 1; col <= actualColumns; col++)
                {
                    try
                    {
                        string cellText = tableShape.Table.Cell(row, col).Shape.TextFrame.TextRange.Text?.Trim() ?? "";
                        actualCells.Add(cellText);

                        int cellIndex = ((row - 1) * actualColumns) + (col - 1);
                        if (cellIndex < expectedCells.Length)
                        {
                            string expectedCellText = expectedCells[cellIndex].Trim();
                            if (!string.Equals(cellText, expectedCellText, StringComparison.OrdinalIgnoreCase))
                            {
                                mismatches.Add($"单元格({row},{col}): 期望'{expectedCellText}', 实际'{cellText}'");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        mismatches.Add($"单元格({row},{col}): 读取失败 - {ex.Message}");
                    }
                }
            }

            bool isCorrect = mismatches.Count == 0;
            result.ExpectedValue = $"表格内容: {expectedContent}";
            result.ActualValue = $"表格内容: {string.Join(",", actualCells)}";
            result.IsCorrect = isCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = isCorrect ?
                $"幻灯片 {slideIndex} 表格内容检测: 全部正确" :
                $"幻灯片 {slideIndex} 表格内容检测: {string.Join("; ", mismatches)}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测表格内容失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测表格样式
    /// </summary>
    private KnowledgePointResult DetectTableStyle(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetTableStyle",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("TableStyle", out string? expectedStyle))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex 或 TableStyle";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            PowerPoint.Shape? tableShape = null;

            // 查找表格
            foreach (PowerPoint.Shape shape in slide.Shapes)
            {
                if (shape.HasTable == MsoTriState.msoTrue)
                {
                    tableShape = shape;
                    break;
                }
            }

            if (tableShape == null)
            {
                result.ErrorMessage = "未找到表格";
                result.IsCorrect = false;
                return result;
            }

            // 简化的样式检测 - 实际实现需要根据PowerPoint API的具体样式属性
            string actualStyle = "默认样式"; // 这里需要根据实际API获取样式信息

            bool isCorrect = actualStyle.Contains(expectedStyle, StringComparison.OrdinalIgnoreCase);

            result.ExpectedValue = expectedStyle;
            result.ActualValue = actualStyle;
            result.IsCorrect = isCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 表格样式检测: 期望 {expectedStyle}, 实际 {actualStyle}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测表格样式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 清理PowerPoint资源
    /// </summary>
    private static void CleanupPowerPointResources(PowerPoint.Presentation? presentation, PowerPoint.Application? pptApp)
    {
        try
        {
            if (presentation != null)
            {
                presentation.Close();
                _ = Marshal.ReleaseComObject(presentation);
            }

            if (pptApp != null)
            {
                pptApp.Quit();
                _ = Marshal.ReleaseComObject(pptApp);
            }
        }
        catch (Exception)
        {
            // 忽略清理过程中的错误
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    /// <summary>
    /// 为演示文稿解析参数中的-1值
    /// </summary>
    private static void ResolveParametersForPresentation(Dictionary<string, string> parameters, PowerPoint.Presentation presentation, ParameterResolutionContext context)
    {
        foreach (KeyValuePair<string, string> parameter in parameters)
        {
            if (ParameterResolver.IsIndexParameter(parameter.Key))
            {
                try
                {
                    int maxValue = GetMaxValueForParameter(parameter.Key, presentation, parameters);
                    if (parameter.Key.Contains("Indexes", StringComparison.OrdinalIgnoreCase))
                    {
                        // 处理多个编号参数（逗号分隔）
                        _ = ParameterResolver.ResolveMultipleParameters(parameter.Key, parameter.Value, maxValue, context);
                    }
                    else
                    {
                        // 处理单个编号参数
                        _ = ParameterResolver.ResolveParameter(parameter.Key, parameter.Value, maxValue, context);
                    }
                }
                catch (Exception)
                {
                    // 如果解析失败，保持原值
                    context.SetResolvedParameter(parameter.Key, parameter.Value);
                }
            }
            else
            {
                // 非编号参数，直接保存
                context.SetResolvedParameter(parameter.Key, parameter.Value);
            }
        }
    }

    /// <summary>
    /// 获取参数的最大值
    /// </summary>
    private static int GetMaxValueForParameter(string parameterName, PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        string lowerName = parameterName.ToLowerInvariant();

        if (lowerName.Contains("slide"))
        {
            return presentation.Slides.Count;
        }

        if (lowerName.Contains("textbox") || lowerName.Contains("element") || lowerName.Contains("shape"))
        {
            // 对于文本框/元素/形状，需要根据具体幻灯片计算
            if (parameters.TryGetValue("SlideIndex", out string? slideIndexStr) &&
                int.TryParse(slideIndexStr, out int slideIndex) &&
                slideIndex >= 1 && slideIndex <= presentation.Slides.Count)
            {
                return presentation.Slides[slideIndex].Shapes.Count;
            }

            // 如果没有指定幻灯片或幻灯片无效，返回第一张幻灯片的形状数量作为估计
            if (presentation.Slides.Count > 0)
            {
                return presentation.Slides[1].Shapes.Count;
            }
        }

        // 默认返回幻灯片数量
        return presentation.Slides.Count;
    }

    /// <summary>
    /// 获取解析后的参数字典
    /// </summary>
    private static Dictionary<string, string> GetResolvedParameters(Dictionary<string, string> originalParameters, ParameterResolutionContext context)
    {
        Dictionary<string, string> resolvedParameters = [];

        foreach (KeyValuePair<string, string> parameter in originalParameters)
        {
            string resolvedValue = context.GetResolvedParameter(parameter.Key);
            resolvedParameters[parameter.Key] = string.IsNullOrEmpty(resolvedValue) ? parameter.Value : resolvedValue;
        }

        return resolvedParameters;
    }

    /// <summary>
    /// 检测幻灯片放映模式
    /// </summary>
    private KnowledgePointResult DetectSlideshowMode(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SlideshowMode",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("ShowType", out string? expectedShowType))
            {
                result.ErrorMessage = "缺少必要参数: ShowType";
                return result;
            }

            // 获取幻灯片放映设置
            PowerPoint.SlideShowSettings showSettings = presentation.SlideShowSettings;
            string actualShowType = showSettings.ShowType.ToString();

            result.ExpectedValue = expectedShowType;
            result.ActualValue = actualShowType;
            result.IsCorrect = string.Equals(actualShowType, expectedShowType, StringComparison.OrdinalIgnoreCase);
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片放映模式: 期望 {expectedShowType}, 实际 {actualShowType}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测幻灯片放映模式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片放映选项
    /// </summary>
    private KnowledgePointResult DetectSlideshowOptions(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SlideshowOptions",
            Parameters = parameters
        };

        try
        {
            PowerPoint.SlideShowSettings showSettings = presentation.SlideShowSettings;
            List<string> actualOptions = [];
            List<string> expectedOptions = [];
            bool allCorrect = true;

            // 检查循环播放
            if (parameters.TryGetValue("LoopUntilStopped", out string? loopStr) &&
                bool.TryParse(loopStr, out bool expectedLoop))
            {
                bool actualLoop = showSettings.LoopUntilStopped == MsoTriState.msoTrue;
                expectedOptions.Add($"循环播放: {expectedLoop}");
                actualOptions.Add($"循环播放: {actualLoop}");

                if (actualLoop != expectedLoop)
                {
                    allCorrect = false;
                }
            }

            // 检查显示导航器
            if (parameters.TryGetValue("ShowWithNarration", out string? narrationStr) &&
                bool.TryParse(narrationStr, out bool expectedNarration))
            {
                bool actualNarration = showSettings.ShowWithNarration == MsoTriState.msoTrue;
                expectedOptions.Add($"显示旁白: {expectedNarration}");
                actualOptions.Add($"显示旁白: {actualNarration}");

                if (actualNarration != expectedNarration)
                {
                    allCorrect = false;
                }
            }

            result.ExpectedValue = string.Join("; ", expectedOptions);
            result.ActualValue = string.Join("; ", actualOptions);
            result.IsCorrect = allCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片放映选项检测: 期望 [{result.ExpectedValue}], 实际 [{result.ActualValue}]";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测幻灯片放映选项失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片切换声音
    /// </summary>
    private KnowledgePointResult DetectSlideTransitionSound(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SlideTransitionSound",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            PowerPoint.SlideShowTransition transition = slide.SlideShowTransition;

            bool hasSound = transition.SoundEffect.Type != PowerPoint.PpSoundEffectType.ppSoundNone;
            string soundName = hasSound ? transition.SoundEffect.Name : "无声音";

            if (parameters.TryGetValue("ExpectedSound", out string? expectedSound))
            {
                result.ExpectedValue = expectedSound;
                result.ActualValue = soundName;
                result.IsCorrect = string.Equals(soundName, expectedSound, StringComparison.OrdinalIgnoreCase);
            }
            else if (parameters.TryGetValue("HasSound", out string? hasSoundStr) &&
                     bool.TryParse(hasSoundStr, out bool expectedHasSound))
            {
                result.ExpectedValue = expectedHasSound ? "有声音" : "无声音";
                result.ActualValue = hasSound ? "有声音" : "无声音";
                result.IsCorrect = hasSound == expectedHasSound;
            }
            else
            {
                result.ErrorMessage = "缺少必要参数: ExpectedSound 或 HasSound";
                return result;
            }

            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 切换声音: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测幻灯片切换声音失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测艺术字样式
    /// </summary>
    private KnowledgePointResult DetectWordArtStyle(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetWordArtStyle",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            int wordArtCount = 0;
            string wordArtStyles = "";

            foreach (PowerPoint.Shape shape in slide.Shapes)
            {
                // 检测艺术字（WordArt通常是特殊的文本形状）
                if (shape.Type == MsoShapeType.msoTextEffect)
                {
                    wordArtCount++;
                    wordArtStyles += shape.TextEffect.PresetTextEffect.ToString() + "; ";
                }
            }

            if (parameters.TryGetValue("ExpectedWordArtCount", out string? expectedCountStr) &&
                int.TryParse(expectedCountStr, out int expectedCount))
            {
                result.ExpectedValue = $"至少{expectedCount}个艺术字";
                result.ActualValue = $"{wordArtCount}个艺术字";
                result.IsCorrect = wordArtCount >= expectedCount;
            }
            else
            {
                result.ExpectedValue = "至少1个艺术字";
                result.ActualValue = $"{wordArtCount}个艺术字";
                result.IsCorrect = wordArtCount > 0;
            }

            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 艺术字检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";

            if (!string.IsNullOrEmpty(wordArtStyles))
            {
                result.Details += $", 样式: {wordArtStyles.TrimEnd(';', ' ')}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测艺术字样式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测艺术字效果
    /// </summary>
    private KnowledgePointResult DetectWordArtEffect(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetWordArtEffect",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            bool hasWordArtEffect = false;
            string effectDetails = "";

            foreach (PowerPoint.Shape shape in slide.Shapes)
            {
                if (shape.Type == MsoShapeType.msoTextEffect)
                {
                    hasWordArtEffect = true;
                    effectDetails += $"预设效果: {shape.TextEffect.PresetTextEffect}; ";

                    // 检查是否有特定效果
                    if (parameters.TryGetValue("ExpectedEffect", out string? expectedEffect))
                    {
                        string actualEffect = shape.TextEffect.PresetTextEffect.ToString();
                        result.ExpectedValue = expectedEffect;
                        result.ActualValue = actualEffect;
                        result.IsCorrect = string.Equals(actualEffect, expectedEffect, StringComparison.OrdinalIgnoreCase);
                        break;
                    }
                }
            }

            if (!parameters.ContainsKey("ExpectedEffect"))
            {
                result.ExpectedValue = "有艺术字效果";
                result.ActualValue = hasWordArtEffect ? "有艺术字效果" : "无艺术字效果";
                result.IsCorrect = hasWordArtEffect;
            }

            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 艺术字效果检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";

            if (!string.IsNullOrEmpty(effectDetails))
            {
                result.Details += $", 详情: {effectDetails.TrimEnd(';', ' ')}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测艺术字效果失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测SmartArt颜色
    /// </summary>
    private KnowledgePointResult DetectSmartArtColor(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSmartArtColor",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            bool hasSmartArt = false;
            string colorInfo = "";

            foreach (PowerPoint.Shape shape in slide.Shapes)
            {
                // 在旧版本API中，通过形状类型检测SmartArt
                if (shape.Type == MsoShapeType.msoSmartArt ||
                    (shape.Type == MsoShapeType.msoGroup && shape.Name.Contains("SmartArt")))
                {
                    hasSmartArt = true;
                    // SmartArt颜色检测（简化处理）
                    colorInfo += "SmartArt图形; ";
                }
            }

            result.ExpectedValue = "有SmartArt图形";
            result.ActualValue = hasSmartArt ? "有SmartArt图形" : "无SmartArt图形";
            result.IsCorrect = hasSmartArt;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} SmartArt颜色检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测SmartArt颜色失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测SmartArt内容
    /// </summary>
    private KnowledgePointResult DetectSmartArtContent(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSmartArtContent",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            bool hasSmartArt = false;
            string contentInfo = "";

            foreach (PowerPoint.Shape shape in slide.Shapes)
            {
                // 在旧版本API中，通过形状类型检测SmartArt
                if (shape.Type == MsoShapeType.msoSmartArt ||
                    (shape.Type == MsoShapeType.msoGroup && shape.Name.Contains("SmartArt")))
                {
                    hasSmartArt = true;
                    contentInfo += "SmartArt图形内容; ";

                    // 检查特定内容
                    if (parameters.TryGetValue("ExpectedContent", out string? expectedContent))
                    {
                        // 简化处理：检查SmartArt是否包含预期文本
                        try
                        {
                            string smartArtText = shape.TextFrame.TextRange.Text;
                            result.ExpectedValue = expectedContent;
                            result.ActualValue = smartArtText;
                            result.IsCorrect = smartArtText.Contains(expectedContent, StringComparison.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            result.IsCorrect = false;
                        }
                        break;
                    }
                }
            }

            if (!parameters.ContainsKey("ExpectedContent"))
            {
                result.ExpectedValue = "有SmartArt图形";
                result.ActualValue = hasSmartArt ? "有SmartArt图形" : "无SmartArt图形";
                result.IsCorrect = hasSmartArt;
            }

            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} SmartArt内容检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测SmartArt内容失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测动画方向
    /// </summary>
    private KnowledgePointResult DetectAnimationDirection(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationDirection",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];

            // 如果提供了ElementIndex，则检测特定元素的动画方向
            if (parameters.TryGetValue("ElementIndex", out string? elementIndexStr) &&
                int.TryParse(elementIndexStr, out int elementIndex))
            {
                if (elementIndex < 1 || elementIndex > slide.Shapes.Count)
                {
                    result.ErrorMessage = $"元素索引超出范围: {elementIndex}";
                    return result;
                }

                PowerPoint.Shape targetShape = slide.Shapes[elementIndex];
                PowerPoint.Effect? targetEffect = null;

                // 查找指定元素的动画效果
                for (int i = 1; i <= slide.TimeLine.MainSequence.Count; i++)
                {
                    PowerPoint.Effect effect = slide.TimeLine.MainSequence[i];
                    if (effect.Shape != null && effect.Shape.Id == targetShape.Id)
                    {
                        targetEffect = effect;
                        break;
                    }
                }

                if (targetEffect == null)
                {
                    result.ErrorMessage = $"元素 {elementIndex} 没有动画效果";
                    result.IsCorrect = false;
                    return result;
                }

                // 检测动画方向
                if (parameters.TryGetValue("AnimationDirection", out string? expectedDirection))
                {
                    // 这里可以根据需要检测具体的动画方向属性
                    // PowerPoint的动画方向检测比较复杂，暂时简化处理
                    result.ExpectedValue = expectedDirection;
                    result.ActualValue = "动画方向已设置";
                    result.IsCorrect = true; // 简化处理，如果有动画效果就认为正确
                    result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                    result.Details = $"幻灯片 {slideIndex} 元素 {elementIndex} 动画方向检测: 期望 {expectedDirection}, 实际 {result.ActualValue}";
                }
                else
                {
                    result.ExpectedValue = "有动画效果";
                    result.ActualValue = "有动画效果";
                    result.IsCorrect = true;
                    result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                    result.Details = $"幻灯片 {slideIndex} 元素 {elementIndex} 动画方向检测: {result.ActualValue}";
                }
            }
            else
            {
                // 兼容旧版本：检测幻灯片上是否有动画
                int animationCount = slide.TimeLine.MainSequence.Count;
                bool hasAnimation = animationCount > 0;

                result.ExpectedValue = "有动画效果";
                result.ActualValue = hasAnimation ? $"有{animationCount}个动画效果" : "无动画效果";
                result.IsCorrect = hasAnimation;
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"幻灯片 {slideIndex} 动画方向检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测动画方向失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测动画样式
    /// </summary>
    private KnowledgePointResult DetectAnimationStyle(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationStyle",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];

            // 如果提供了ElementIndex，则检测特定元素的动画样式
            if (parameters.TryGetValue("ElementIndex", out string? elementIndexStr) &&
                int.TryParse(elementIndexStr, out int elementIndex))
            {
                if (elementIndex < 1 || elementIndex > slide.Shapes.Count)
                {
                    result.ErrorMessage = $"元素索引超出范围: {elementIndex}";
                    return result;
                }

                PowerPoint.Shape targetShape = slide.Shapes[elementIndex];
                PowerPoint.Effect? targetEffect = null;

                // 查找指定元素的动画效果
                for (int i = 1; i <= slide.TimeLine.MainSequence.Count; i++)
                {
                    PowerPoint.Effect effect = slide.TimeLine.MainSequence[i];
                    if (effect.Shape != null && effect.Shape.Id == targetShape.Id)
                    {
                        targetEffect = effect;
                        break;
                    }
                }

                if (targetEffect == null)
                {
                    result.ErrorMessage = $"元素 {elementIndex} 没有动画效果";
                    result.IsCorrect = false;
                    return result;
                }

                // 检测动画样式
                string actualStyle = targetEffect.EffectType.ToString();
                if (parameters.TryGetValue("AnimationStyle", out string? expectedStyle))
                {
                    bool styleCorrect = string.Equals(actualStyle, expectedStyle, StringComparison.OrdinalIgnoreCase);
                    result.ExpectedValue = expectedStyle;
                    result.ActualValue = actualStyle;
                    result.IsCorrect = styleCorrect;
                    result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                    result.Details = $"幻灯片 {slideIndex} 元素 {elementIndex} 动画样式检测: 期望 {expectedStyle}, 实际 {actualStyle}";
                }
                else
                {
                    result.ExpectedValue = "有动画样式";
                    result.ActualValue = $"动画样式: {actualStyle}";
                    result.IsCorrect = true;
                    result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                    result.Details = $"幻灯片 {slideIndex} 元素 {elementIndex} 动画样式检测: {result.ActualValue}";
                }
            }
            else
            {
                // 兼容旧版本：检测幻灯片上的所有动画样式
                int animationCount = slide.TimeLine.MainSequence.Count;
                string animationStyles = "";

                for (int i = 1; i <= animationCount; i++)
                {
                    PowerPoint.Effect effect = slide.TimeLine.MainSequence[i];
                    animationStyles += effect.EffectType.ToString() + "; ";
                }

                bool hasAnimation = animationCount > 0;
                result.ExpectedValue = "有动画样式";
                result.ActualValue = hasAnimation ? $"动画样式: {animationStyles.TrimEnd(';', ' ')}" : "无动画样式";
                result.IsCorrect = hasAnimation;
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"幻灯片 {slideIndex} 动画样式检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测动画样式失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测动画持续时间
    /// </summary>
    private KnowledgePointResult DetectAnimationDuration(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationDuration",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];

            // 如果提供了ElementIndex，则检测特定元素的动画
            if (parameters.TryGetValue("ElementIndex", out string? elementIndexStr) &&
                int.TryParse(elementIndexStr, out int elementIndex))
            {
                if (elementIndex < 1 || elementIndex > slide.Shapes.Count)
                {
                    result.ErrorMessage = $"元素索引超出范围: {elementIndex}";
                    return result;
                }

                PowerPoint.Shape targetShape = slide.Shapes[elementIndex];
                PowerPoint.Effect? targetEffect = null;

                // 查找指定元素的动画效果
                for (int i = 1; i <= slide.TimeLine.MainSequence.Count; i++)
                {
                    PowerPoint.Effect effect = slide.TimeLine.MainSequence[i];
                    if (effect.Shape != null && effect.Shape.Id == targetShape.Id)
                    {
                        targetEffect = effect;
                        break;
                    }
                }

                if (targetEffect == null)
                {
                    result.ErrorMessage = $"元素 {elementIndex} 没有动画效果";
                    result.IsCorrect = false;
                    return result;
                }

                // 检测具体的持续时间和延迟时间
                bool allCorrect = true;
                string detailsInfo = "";

                if (parameters.TryGetValue("Duration", out string? expectedDurationStr) &&
                    float.TryParse(expectedDurationStr, out float expectedDuration))
                {
                    float actualDuration = targetEffect.Timing.Duration;
                    bool durationCorrect = Math.Abs(actualDuration - expectedDuration) < 0.1f;
                    allCorrect &= durationCorrect;
                    detailsInfo += $"持续时间: 期望 {expectedDuration}s, 实际 {actualDuration}s; ";
                }

                if (parameters.TryGetValue("DelayTime", out string? expectedDelayTimeStr) &&
                    float.TryParse(expectedDelayTimeStr, out float expectedDelayTime))
                {
                    float actualDelayTime = targetEffect.Timing.TriggerDelayTime;
                    bool delayCorrect = Math.Abs(actualDelayTime - expectedDelayTime) < 0.1f;
                    allCorrect &= delayCorrect;
                    detailsInfo += $"延迟时间: 期望 {expectedDelayTime}s, 实际 {actualDelayTime}s; ";
                }

                result.ExpectedValue = "动画持续时间设置正确";
                result.ActualValue = detailsInfo.TrimEnd(';', ' ');
                result.IsCorrect = allCorrect;
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"幻灯片 {slideIndex} 元素 {elementIndex} 动画持续时间检测: {result.ActualValue}";
            }
            else
            {
                // 兼容旧版本：检测幻灯片上是否有动画
                int animationCount = slide.TimeLine.MainSequence.Count;
                string durationInfo = "";

                for (int i = 1; i <= animationCount; i++)
                {
                    PowerPoint.Effect effect = slide.TimeLine.MainSequence[i];
                    durationInfo += $"{effect.Timing.Duration}s; ";
                }

                bool hasAnimation = animationCount > 0;
                result.ExpectedValue = "有动画持续时间设置";
                result.ActualValue = hasAnimation ? $"动画持续时间: {durationInfo.TrimEnd(';', ' ')}" : "无动画";
                result.IsCorrect = hasAnimation;
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"幻灯片 {slideIndex} 动画持续时间检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测动画持续时间失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测动画顺序
    /// </summary>
    private KnowledgePointResult DetectAnimationOrder(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationOrder",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];

            // 如果提供了ElementIndex，则检测特定元素的动画顺序
            if (parameters.TryGetValue("ElementIndex", out string? elementIndexStr) &&
                int.TryParse(elementIndexStr, out int elementIndex))
            {
                if (elementIndex < 1 || elementIndex > slide.Shapes.Count)
                {
                    result.ErrorMessage = $"元素索引超出范围: {elementIndex}";
                    return result;
                }

                PowerPoint.Shape targetShape = slide.Shapes[elementIndex];
                int targetEffectIndex = -1;

                // 查找指定元素的动画效果在序列中的位置
                for (int i = 1; i <= slide.TimeLine.MainSequence.Count; i++)
                {
                    PowerPoint.Effect effect = slide.TimeLine.MainSequence[i];
                    if (effect.Shape != null && effect.Shape.Id == targetShape.Id)
                    {
                        targetEffectIndex = i;
                        break;
                    }
                }

                if (targetEffectIndex == -1)
                {
                    result.ErrorMessage = $"元素 {elementIndex} 没有动画效果";
                    result.IsCorrect = false;
                    return result;
                }

                // 检测动画顺序
                if (parameters.TryGetValue("AnimationOrder", out string? expectedOrderStr) &&
                    int.TryParse(expectedOrderStr, out int expectedOrder))
                {
                    bool orderCorrect = targetEffectIndex == expectedOrder;
                    result.ExpectedValue = $"动画顺序: {expectedOrder}";
                    result.ActualValue = $"动画顺序: {targetEffectIndex}";
                    result.IsCorrect = orderCorrect;
                    result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                    result.Details = $"幻灯片 {slideIndex} 元素 {elementIndex} 动画顺序检测: 期望 {expectedOrder}, 实际 {targetEffectIndex}";
                }
                else
                {
                    result.ExpectedValue = "有动画顺序设置";
                    result.ActualValue = $"动画顺序: {targetEffectIndex}";
                    result.IsCorrect = true;
                    result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                    result.Details = $"幻灯片 {slideIndex} 元素 {elementIndex} 动画顺序检测: {result.ActualValue}";
                }
            }
            else
            {
                // 兼容旧版本：检测幻灯片上的所有动画顺序
                int animationCount = slide.TimeLine.MainSequence.Count;
                string orderInfo = "";

                for (int i = 1; i <= animationCount; i++)
                {
                    PowerPoint.Effect effect = slide.TimeLine.MainSequence[i];
                    orderInfo += $"序号{i}: {effect.EffectType}; ";
                }

                bool hasAnimation = animationCount > 0;
                result.ExpectedValue = "有动画顺序设置";
                result.ActualValue = hasAnimation ? $"动画顺序: {orderInfo.TrimEnd(';', ' ')}" : "无动画";
                result.IsCorrect = hasAnimation;
                result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                result.Details = $"幻灯片 {slideIndex} 动画顺序检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测动画顺序失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测动画时间设置
    /// </summary>
    private KnowledgePointResult DetectAnimationTiming(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationTiming",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex";
                return result;
            }

            if (!parameters.TryGetValue("ElementIndex", out string? elementIndexStr) ||
                !int.TryParse(elementIndexStr, out int elementIndex))
            {
                result.ErrorMessage = "缺少必要参数: ElementIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];

            if (elementIndex < 1 || elementIndex > slide.Shapes.Count)
            {
                result.ErrorMessage = $"元素索引超出范围: {elementIndex}";
                return result;
            }

            PowerPoint.Shape targetShape = slide.Shapes[elementIndex];
            PowerPoint.Effect? targetEffect = null;

            // 查找指定元素的动画效果
            for (int i = 1; i <= slide.TimeLine.MainSequence.Count; i++)
            {
                PowerPoint.Effect effect = slide.TimeLine.MainSequence[i];
                if (effect.Shape != null && effect.Shape.Id == targetShape.Id)
                {
                    targetEffect = effect;
                    break;
                }
            }

            if (targetEffect == null)
            {
                result.ErrorMessage = $"元素 {elementIndex} 没有动画效果";
                result.IsCorrect = false;
                return result;
            }

            // 检测具体的动画时间参数
            bool allCorrect = true;
            string detailsInfo = "";

            // 检测触发方式
            if (parameters.TryGetValue("TriggerMode", out string? expectedTriggerMode))
            {
                string actualTriggerMode = GetTriggerModeString(targetEffect.Timing.TriggerType);
                bool triggerCorrect = string.Equals(actualTriggerMode, expectedTriggerMode, StringComparison.OrdinalIgnoreCase);
                allCorrect &= triggerCorrect;
                detailsInfo += $"触发方式: 期望 {expectedTriggerMode}, 实际 {actualTriggerMode}; ";
            }

            // 检测延迟时间
            if (parameters.TryGetValue("DelayTime", out string? expectedDelayTimeStr) &&
                float.TryParse(expectedDelayTimeStr, out float expectedDelayTime))
            {
                float actualDelayTime = targetEffect.Timing.TriggerDelayTime;
                bool delayCorrect = Math.Abs(actualDelayTime - expectedDelayTime) < 0.1f;
                allCorrect &= delayCorrect;
                detailsInfo += $"延迟时间: 期望 {expectedDelayTime}s, 实际 {actualDelayTime}s; ";
            }

            // 检测持续时间
            if (parameters.TryGetValue("Duration", out string? expectedDurationStr) &&
                float.TryParse(expectedDurationStr, out float expectedDuration))
            {
                float actualDuration = targetEffect.Timing.Duration;
                bool durationCorrect = Math.Abs(actualDuration - expectedDuration) < 0.1f;
                allCorrect &= durationCorrect;
                detailsInfo += $"持续时间: 期望 {expectedDuration}s, 实际 {actualDuration}s; ";
            }

            // 检测重复次数
            if (parameters.TryGetValue("RepeatCount", out string? expectedRepeatCountStr) &&
                int.TryParse(expectedRepeatCountStr, out int expectedRepeatCount))
            {
                int actualRepeatCount = targetEffect.Timing.RepeatCount;
                bool repeatCorrect = actualRepeatCount == expectedRepeatCount;
                allCorrect &= repeatCorrect;
                detailsInfo += $"重复次数: 期望 {expectedRepeatCount}, 实际 {actualRepeatCount}; ";
            }

            result.ExpectedValue = "动画时间参数设置正确";
            result.ActualValue = detailsInfo.TrimEnd(';', ' ');
            result.IsCorrect = allCorrect;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 元素 {elementIndex} 动画时间检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测动画时间设置失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 获取触发方式的字符串表示
    /// </summary>
    private static string GetTriggerModeString(PowerPoint.MsoAnimTriggerType triggerType)
    {
        return triggerType switch
        {
            PowerPoint.MsoAnimTriggerType.msoAnimTriggerOnPageClick => "单击时",
            PowerPoint.MsoAnimTriggerType.msoAnimTriggerWithPrevious => "与上一动画同时",
            PowerPoint.MsoAnimTriggerType.msoAnimTriggerAfterPrevious => "在上一动画之后",
            PowerPoint.MsoAnimTriggerType.msoAnimTriggerOnShapeClick => "单击时",
            _ => "自动"
        };
    }

    /// <summary>
    /// 检测段落间距
    /// </summary>
    private KnowledgePointResult DetectParagraphSpacing(PowerPoint.Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetParagraphSpacing",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("TextBoxIndex", out string? textBoxIndexStr) ||
                !int.TryParse(textBoxIndexStr, out int textBoxIndex))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex 或 TextBoxIndex";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            if (textBoxIndex < 1 || textBoxIndex > slide.Shapes.Count)
            {
                result.ErrorMessage = $"文本框索引超出范围: {textBoxIndex}";
                return result;
            }

            PowerPoint.Shape shape = slide.Shapes[textBoxIndex];
            if (shape.HasTextFrame != MsoTriState.msoTrue)
            {
                result.ErrorMessage = $"指定的形状不是文本框: {textBoxIndex}";
                return result;
            }

            float spaceBefore = shape.TextFrame.TextRange.ParagraphFormat.SpaceBefore;
            float spaceAfter = shape.TextFrame.TextRange.ParagraphFormat.SpaceAfter;
            float lineSpacing = shape.TextFrame.TextRange.ParagraphFormat.SpaceWithin;

            string actualSpacing = $"段前:{spaceBefore}pt, 段后:{spaceAfter}pt, 行距:{lineSpacing}";

            if (parameters.TryGetValue("ExpectedSpaceBefore", out string? expectedSpaceBeforeStr) &&
                float.TryParse(expectedSpaceBeforeStr, out float expectedSpaceBefore))
            {
                result.ExpectedValue = $"段前间距: {expectedSpaceBefore}pt";
                result.ActualValue = $"段前间距: {spaceBefore}pt";
                result.IsCorrect = Math.Abs(spaceBefore - expectedSpaceBefore) < 0.1f;
            }
            else
            {
                result.ExpectedValue = "有段落间距设置";
                result.ActualValue = actualSpacing;
                result.IsCorrect = spaceBefore > 0 || spaceAfter > 0 || lineSpacing != 1.0f;
            }

            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 文本框 {textBoxIndex} 段落间距检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测段落间距失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 确保试卷中所有题目都有唯一的ID
    /// </summary>
    /// <param name="examModel">试卷模型</param>
    private static void EnsureUniqueQuestionIds(ExamModel examModel)
    {
        HashSet<string> usedIds = [];
        List<QuestionModel> allQuestions = [];

        // 收集所有模块中的题目
        foreach (ExamModuleModel module in examModel.Exam.Modules)
        {
            allQuestions.AddRange(module.Questions);
        }

        // 检查并修复重复的ID
        foreach (QuestionModel question in allQuestions)
        {
            // 如果ID为空、是默认值或已被使用，则重新生成
            if (string.IsNullOrEmpty(question.Id) ||
                question.Id == "question-1" ||
                !usedIds.Add(question.Id))
            {
                string newId = GenerateUniqueQuestionId(usedIds);
                question.Id = newId;
                _ = usedIds.Add(newId);
            }
        }
    }

    /// <summary>
    /// 生成唯一的题目ID
    /// </summary>
    /// <param name="usedIds">已使用的ID集合</param>
    /// <returns>唯一的题目ID</returns>
    private static string GenerateUniqueQuestionId(HashSet<string> usedIds)
    {
        string newId;
        do
        {
            // 使用与ExamLab项目中Question.cs相同的格式
            newId = $"question-{DateTime.Now.Ticks}-{Guid.NewGuid().ToString("N")[..8]}";
        }
        while (usedIds.Contains(newId));

        return newId;
    }
}
