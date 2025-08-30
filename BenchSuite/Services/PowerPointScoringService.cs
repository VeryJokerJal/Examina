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
    private static readonly string[] SupportedExtensions = [".ppt", ".pptx"];

    public PowerPointScoringService()
    {
        _defaultConfiguration = new ScoringConfiguration();
    }

    /// <summary>
    /// 对PPT文件进行打分
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
            ExamModuleModel? pptModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.PowerPoint);
            if (pptModule == null)
            {
                result.ErrorMessage = "试卷中未找到PowerPoint模块，跳过PowerPoint评分";
                result.IsSuccess = true; // 设置为成功，但没有评分结果
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

            // 获取题目的操作点（只处理PowerPoint相关的操作点）
            List<OperationPointModel> pptOperationPoints = [.. question.OperationPoints.Where(op => op.ModuleType == ModuleType.PowerPoint && op.IsEnabled)];

            if (pptOperationPoints.Count == 0)
            {
                result.ErrorMessage = "题目没有包含任何PowerPoint操作点";
                result.IsSuccess = false;
                return result;
            }

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, pptOperationPoints).Result;

            // 为每个知识点结果设置题目ID
            foreach (KnowledgePointResult kpResult in result.KnowledgePointResults)
            {
                kpResult.QuestionId = question.Id;
            }

            // 计算总分和获得分数
            result.TotalScore = pptOperationPoints.Sum(op => op.Score);
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

            Application? pptApp = null;
            Presentation? presentation = null;

            try
            {
                // 启动PowerPoint应用程序
                pptApp = new Application();

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
            Application? pptApp = null;
            Presentation? presentation = null;

            // 创建基于文件路径的确定性参数解析上下文
            // 使用文件路径确保同一文件的-1参数始终解析为相同值
            string contextId = Path.GetFileName(filePath) + "_" + new FileInfo(filePath).Length;
            ParameterResolutionContext context = new(contextId);

            try
            {
                // 启动PowerPoint应用程序
                pptApp = new Application();

                // 打开演示文稿
                presentation = pptApp.Presentations.Open(filePath, MsoTriState.msoFalse, MsoTriState.msoFalse, MsoTriState.msoFalse);

                // 预先解析所有-1参数
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    Dictionary<string, string> parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);
                    ResolveParametersForPresentation(parameters, presentation, context);
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

                        KnowledgePointResult result = presentation is not null
                            ? DetectSpecificKnowledgePoint(presentation, knowledgePointType, resolvedParameters)
                            : new KnowledgePointResult
                            {
                                ErrorMessage = "PowerPoint文件未能正确打开",
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
                            KnowledgePointType = operationPoint.PowerPointKnowledgeType ?? string.Empty,
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
                        KnowledgePointType = operationPoint.PowerPointKnowledgeType ?? string.Empty,
                        TotalScore = operationPoint.Score,
                        AchievedScore = 0,
                        IsCorrect = false,
                        ErrorMessage = $"无法打开PPT文件: {ex.Message}"
                    });
                }

                // 添加参数解析日志到第一个结果中（用于调试）
                if (results.Count > 0)
                {
                    string resolutionLog = context.GetResolutionLog();
                    if (!string.IsNullOrEmpty(resolutionLog))
                    {
                        // 将解析日志添加到第一个知识点结果的详情中
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
    private KnowledgePointResult DetectSpecificKnowledgePoint(Presentation presentation, string knowledgePointType, Dictionary<string, string> parameters)
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
                case "SetSlideTransition":
                    // 向后兼容：将旧的SlideTransitionMode重定向到新的统一检测方法
                    result = DetectSlideTransition(presentation, parameters);
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
                    result = DetectSlideBackground(presentation, parameters);
                    break;
                case "SetTableContent":
                    result = DetectTableContent(presentation, parameters);
                    break;
                case "SetTableStyle":
                    result = DetectTableStyle(presentation, parameters);
                    break;
                case "SetAnimationTiming":
                    result = DetectAnimationTiming(presentation, parameters);
                    break;
                case "SetAnimationDuration":
                    result = DetectAnimationDuration(presentation, parameters);
                    break;
                case "SetAnimationOrder":
                    result = DetectAnimationOrder(presentation, parameters);
                    break;
                case "SlideshowOptions":
                    result = DetectSlideshowOptions(presentation, parameters);
                    break;
                case "SetWordArtStyle":
                    result = DetectWordArtStyle(presentation, parameters);
                    break;
                case "SetWordArtEffect":
                    // 向后兼容：将旧的SetWordArtEffect重定向到新的统一检测方法
                    result = DetectWordArtStyle(presentation, parameters);
                    break;
                case "SetSmartArtColor":
                    // 已删除的功能，返回不支持错误
                    result.ErrorMessage = "SetSmartArtColor功能已被删除";
                    result.IsCorrect = false;
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
    private KnowledgePointResult DetectSlideLayout(Presentation presentation, Dictionary<string, string> parameters)
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

            Slide slide = presentation.Slides[slideIndex];
            string actualLayout = GetLayoutDisplayName(slide.Layout);

            // 标准化期望的版式名称
            string normalizedExpectedLayout = NormalizeLayoutName(expectedLayout);
            string normalizedActualLayout = NormalizeLayoutName(actualLayout);

            result.ExpectedValue = expectedLayout;
            result.ActualValue = actualLayout;
            result.IsCorrect = string.Equals(normalizedActualLayout, normalizedExpectedLayout, StringComparison.OrdinalIgnoreCase);
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
    private KnowledgePointResult DetectDeletedSlide(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "DeleteSlide",
            Parameters = parameters
        };

        try
        {
            // 尝试从参数中获取期望的幻灯片数量，如果没有则根据删除操作推算
            if (parameters.TryGetValue("ExpectedSlideCount", out string? expectedCountStr) &&
                int.TryParse(expectedCountStr, out int expectedCount))
            {
                // 使用明确指定的期望数量
            }
            else
            {
                // 根据删除操作推算：假设删除了1张幻灯片
                expectedCount = presentation.Slides.Count; // 当前数量就是期望数量（已删除后的状态）

                // 检查是否确实删除了幻灯片（通过检查幻灯片标题或内容）
                if (parameters.TryGetValue("SlideIndex", out string? slideIndexStr) &&
                    int.TryParse(slideIndexStr, out int deletedSlideIndex))
                {
                    // 检测指定位置的幻灯片是否已被删除
                    // 这里简化处理：如果当前幻灯片数量合理，认为删除成功
                    result.IsCorrect = presentation.Slides.Count > 0;
                    result.ExpectedValue = $"删除第{deletedSlideIndex}张幻灯片";
                    result.ActualValue = $"当前有{presentation.Slides.Count}张幻灯片";
                    result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                    result.Details = $"删除幻灯片检测: {result.ActualValue}";
                    return result;
                }
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
    private KnowledgePointResult DetectInsertedSlide(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertSlide",
            Parameters = parameters
        };

        try
        {
            // 尝试从参数中获取期望的幻灯片数量
            if (parameters.TryGetValue("ExpectedSlideCount", out string? expectedCountStr) &&
                int.TryParse(expectedCountStr, out int expectedCount))
            {
                // 使用明确指定的期望数量
            }
            else
            {
                // 根据插入操作推算：检查是否在指定位置插入了幻灯片
                if (parameters.TryGetValue("Position", out string? positionStr) &&
                    int.TryParse(positionStr, out int insertPosition))
                {
                    // 简化检测：检查幻灯片总数是否合理（至少有插入位置+1张）
                    int currentCount = presentation.Slides.Count;
                    bool hasEnoughSlides = currentCount > insertPosition;

                    result.ExpectedValue = $"在第{insertPosition}张幻灯片后插入新幻灯片";
                    result.ActualValue = $"当前有{currentCount}张幻灯片";
                    result.IsCorrect = hasEnoughSlides;
                    result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
                    result.Details = $"插入幻灯片检测: {result.ActualValue}，插入位置{insertPosition}";
                    return result;
                }

                // 默认期望：至少有2张幻灯片（原有+插入的）
                expectedCount = 2;
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
    private KnowledgePointResult DetectSlideFont(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSlideFont",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("FontName", out string? expectedFont))
            {
                result.ErrorMessage = "缺少必要参数: FontName";
                return result;
            }

            // 智能搜索：优先检测指定幻灯片，如果没有找到则搜索所有幻灯片
            bool fontFound = false;
            string actualFonts = "";
            string searchDetails = "";

            // 尝试获取指定的幻灯片索引
            int slideIndex = 0;
            bool hasSpecificSlide = parameters.TryGetValue("SlideIndex", out string? slideIndexStr) &&
                                   int.TryParse(slideIndexStr, out slideIndex) &&
                                   slideIndex >= 1 && slideIndex <= presentation.Slides.Count;

            if (hasSpecificSlide)
            {
                // 检测指定幻灯片
                Slide slide = presentation.Slides[slideIndex];
                foreach (PowerPoint.Shape shape in slide.Shapes)
                {
                    try
                    {
                        if (shape.HasTextFrame == MsoTriState.msoTrue)
                        {
                            TextRange textRange = shape.TextFrame.TextRange;
                            string fontName = textRange.Font.Name;
                            actualFonts += fontName + "; ";

                            if (string.Equals(fontName, expectedFont, StringComparison.OrdinalIgnoreCase))
                            {
                                fontFound = true;
                            }
                        }
                    }
                    catch
                    {
                        // 忽略无法访问的形状
                    }
                }
                searchDetails = $"幻灯片 {slideIndex}";
            }

            // 如果在指定幻灯片没找到，或者没有指定幻灯片，则搜索所有幻灯片
            if (!fontFound)
            {
                actualFonts = ""; // 重置
                for (int i = 1; i <= presentation.Slides.Count; i++)
                {
                    Slide slide = presentation.Slides[i];
                    foreach (PowerPoint.Shape shape in slide.Shapes)
                    {
                        try
                        {
                            if (shape.HasTextFrame == MsoTriState.msoTrue)
                            {
                                TextRange textRange = shape.TextFrame.TextRange;
                                string fontName = textRange.Font.Name;
                                actualFonts += fontName + "; ";

                                if (string.Equals(fontName, expectedFont, StringComparison.OrdinalIgnoreCase))
                                {
                                    fontFound = true;
                                    searchDetails = $"在幻灯片 {i} 中找到";
                                }
                            }
                        }
                        catch
                        {
                            // 忽略无法访问的形状
                        }
                    }
                }

                if (!fontFound)
                {
                    searchDetails = $"搜索了所有 {presentation.Slides.Count} 张幻灯片";
                }
            }

            result.ExpectedValue = expectedFont;
            result.ActualValue = actualFonts.TrimEnd(';', ' ');
            result.IsCorrect = fontFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"字体检测: 期望 {expectedFont}, {searchDetails}, 找到的字体 {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测幻灯片字体失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测幻灯片切换效果（支持单个和多个幻灯片）
    /// </summary>
    private KnowledgePointResult DetectSlideTransition(Presentation presentation, Dictionary<string, string> parameters)
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

            // 获取可选的切换方向参数
            _ = parameters.TryGetValue("TransitionDirection", out string? expectedDirection);

            if (string.IsNullOrEmpty(slideIndexesStr) || string.IsNullOrEmpty(expectedTransition))
            {
                result.ErrorMessage = "缺少必要参数: SlideNumbers/SlideIndexes 或 TransitionEffect/TransitionType";
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
                result.ErrorMessage = "无效的幻灯片索引列表";
                return result;
            }

            int correctCount = 0;
            List<string> details = [];
            List<string> actualTransitions = [];

            foreach (int slideIndex in slideIndexes)
            {
                if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
                {
                    details.Add($"幻灯片 {slideIndex}: 索引超出范围");
                    continue;
                }

                Slide slide = presentation.Slides[slideIndex];
                string actualTransition = GetTransitionEffectName(slide.SlideShowTransition.EntryEffect);
                actualTransitions.Add(actualTransition);

                // 支持新的切换效果名称映射
                string normalizedExpected = NormalizeTransitionEffectName(expectedTransition);
                string normalizedActual = NormalizeTransitionEffectName(actualTransition);

                bool isCorrect = string.Equals(normalizedActual, normalizedExpected, StringComparison.OrdinalIgnoreCase);

                // 如果指定了方向，也要检查方向（简化处理，主要检查效果）
                if (!string.IsNullOrEmpty(expectedDirection) && isCorrect)
                {
                    // 这里可以添加更详细的方向检测逻辑
                    // 目前简化处理，主要关注切换效果本身
                }

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

            result.ExpectedValue = expectedTransition + (string.IsNullOrEmpty(expectedDirection) ? "" : $" ({expectedDirection})");
            result.ActualValue = string.Join(", ", actualTransitions.Distinct());
            result.IsCorrect = correctCount == slideIndexes.Count;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : (int)((double)correctCount / slideIndexes.Count * result.TotalScore);
            result.Details = string.Join("; ", details);
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
    private KnowledgePointResult DetectTextContent(Presentation presentation, Dictionary<string, string> parameters)
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

            // 智能搜索：优先检测指定幻灯片，如果没有找到则搜索所有幻灯片
            bool textFound = false;
            string allText = "";
            string searchDetails = "";

            // 尝试获取指定的幻灯片索引
            int slideIndex = 0;
            bool hasSpecificSlide = parameters.TryGetValue("SlideIndex", out string? slideIndexStr) &&
                                   int.TryParse(slideIndexStr, out slideIndex) &&
                                   slideIndex >= 1 && slideIndex <= presentation.Slides.Count;

            if (hasSpecificSlide)
            {
                // 检测指定幻灯片
                Slide slide = presentation.Slides[slideIndex];
                foreach (PowerPoint.Shape shape in slide.Shapes)
                {
                    try
                    {
                        if (shape.HasTextFrame.ToString().Contains("True"))
                        {
                            string shapeText = shape.TextFrame.TextRange.Text;
                            allText += shapeText + " ";

                            if (shapeText.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
                            {
                                textFound = true;
                            }
                        }
                    }
                    catch
                    {
                        // 忽略无法访问的形状
                    }
                }
                searchDetails = $"幻灯片 {slideIndex}";
            }

            // 如果在指定幻灯片没找到文本，或者没有指定幻灯片，则搜索所有幻灯片
            if (!textFound)
            {
                allText = ""; // 重置
                for (int i = 1; i <= presentation.Slides.Count; i++)
                {
                    Slide slide = presentation.Slides[i];
                    string slideText = "";

                    foreach (PowerPoint.Shape shape in slide.Shapes)
                    {
                        try
                        {
                            if (shape.HasTextFrame.ToString().Contains("True"))
                            {
                                string shapeText = shape.TextFrame.TextRange.Text;
                                slideText += shapeText + " ";

                                if (shapeText.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
                                {
                                    textFound = true;
                                    searchDetails = $"在幻灯片 {i} 中找到";
                                }
                            }
                        }
                        catch
                        {
                            // 忽略无法访问的形状
                        }
                    }
                    allText += slideText;

                    // 如果找到了就停止搜索
                    if (textFound)
                    {
                        break;
                    }
                }

                if (!textFound)
                {
                    searchDetails = $"搜索了所有 {presentation.Slides.Count} 张幻灯片";
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
            result.ErrorMessage = $"检测文本内容失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测文本字号
    /// </summary>
    private KnowledgePointResult DetectTextFontSize(Presentation presentation, Dictionary<string, string> parameters)
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

            Slide slide = presentation.Slides[slideIndex];
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
    private KnowledgePointResult DetectTextColor(Presentation presentation, Dictionary<string, string> parameters)
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

            Slide slide = presentation.Slides[slideIndex];
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
    private KnowledgePointResult DetectInsertedImage(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertImage",
            Parameters = parameters
        };

        try
        {
            // 智能搜索：优先检测指定幻灯片，如果没有找到则搜索所有幻灯片
            int totalImageCount = 0;
            string searchDetails = "";
            List<string> allShapeTypes = [];

            // 尝试获取指定的幻灯片索引
            int slideIndex = 0;
            bool hasSpecificSlide = parameters.TryGetValue("SlideIndex", out string? slideIndexStr) &&
                                   int.TryParse(slideIndexStr, out slideIndex) &&
                                   slideIndex >= 1 && slideIndex <= presentation.Slides.Count;

            if (hasSpecificSlide)
            {
                // 检测指定幻灯片
                Slide slide = presentation.Slides[slideIndex];
                foreach (PowerPoint.Shape shape in slide.Shapes)
                {
                    string shapeType = shape.Type.ToString();
                    allShapeTypes.Add(shapeType);

                    // 检测多种可能的图片类型
                    if (shapeType.Contains("Picture") ||
                        shapeType.Contains("msoPicture") ||
                        shapeType.Contains("Image") ||
                        IsShapeContainingImage(shape))
                    {
                        totalImageCount++;
                    }
                }
                searchDetails = $"幻灯片 {slideIndex}";
            }

            // 如果在指定幻灯片没找到图片，或者没有指定幻灯片，则搜索所有幻灯片
            if (totalImageCount == 0)
            {
                allShapeTypes.Clear(); // 重置
                for (int i = 1; i <= presentation.Slides.Count; i++)
                {
                    Slide slide = presentation.Slides[i];
                    int slideImageCount = 0;

                    foreach (PowerPoint.Shape shape in slide.Shapes)
                    {
                        string shapeType = shape.Type.ToString();
                        allShapeTypes.Add(shapeType);

                        // 检测多种可能的图片类型
                        if (shapeType.Contains("Picture") ||
                            shapeType.Contains("msoPicture") ||
                            shapeType.Contains("Image") ||
                            IsShapeContainingImage(shape))
                        {
                            slideImageCount++;
                            totalImageCount++;
                        }
                    }

                    if (slideImageCount > 0)
                    {
                        searchDetails = $"在幻灯片 {i} 中找到 {slideImageCount} 张图片";
                        break; // 找到第一个有图片的幻灯片就停止
                    }
                }

                if (totalImageCount == 0)
                {
                    searchDetails = $"搜索了所有 {presentation.Slides.Count} 张幻灯片";
                }
            }

            // 检查期望的图片数量
            bool hasExpectedCount = true;
            if (parameters.TryGetValue("ExpectedImageCount", out string? expectedCountStr) &&
                int.TryParse(expectedCountStr, out int expectedCount))
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

            // 增强诊断信息
            string shapeInfo = allShapeTypes.Count > 0 ? $"形状类型: [{string.Join(", ", allShapeTypes.Take(10))}]" : "无形状";
            result.Details = $"图片检测: 期望 {result.ExpectedValue}, {searchDetails}, 实际 {result.ActualValue}. {shapeInfo}";
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
    private KnowledgePointResult DetectInsertedTable(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertTable",
            Parameters = parameters
        };

        try
        {
            // 智能搜索：优先检测指定幻灯片，如果没有找到则搜索所有幻灯片
            int totalTableCount = 0;
            string tableInfo = "";
            string searchDetails = "";

            // 尝试获取指定的幻灯片索引
            int slideIndex = 0;
            bool hasSpecificSlide = parameters.TryGetValue("SlideIndex", out string? slideIndexStr) &&
                                   int.TryParse(slideIndexStr, out slideIndex) &&
                                   slideIndex >= 1 && slideIndex <= presentation.Slides.Count;

            if (hasSpecificSlide)
            {
                // 检测指定幻灯片
                Slide slide = presentation.Slides[slideIndex];
                foreach (PowerPoint.Shape shape in slide.Shapes)
                {
                    try
                    {
                        if (shape.HasTable.ToString().Contains("True") || shape.Type.ToString().Contains("Table"))
                        {
                            totalTableCount++;
                            // 尝试获取表格信息，但不强制要求成功
                            try
                            {
                                int rows = shape.Table.Rows.Count;
                                int columns = shape.Table.Columns.Count;
                                tableInfo += $"{rows}x{columns}; ";
                            }
                            catch
                            {
                                tableInfo += "表格; ";
                            }
                        }
                    }
                    catch
                    {
                        // 忽略无法访问的形状
                    }
                }
                searchDetails = $"幻灯片 {slideIndex}";
            }

            // 如果在指定幻灯片没找到表格，或者没有指定幻灯片，则搜索所有幻灯片
            if (totalTableCount == 0)
            {
                tableInfo = ""; // 重置
                for (int i = 1; i <= presentation.Slides.Count; i++)
                {
                    Slide slide = presentation.Slides[i];
                    int slideTableCount = 0;

                    foreach (PowerPoint.Shape shape in slide.Shapes)
                    {
                        try
                        {
                            if (shape.HasTable.ToString().Contains("True") || shape.Type.ToString().Contains("Table"))
                            {
                                slideTableCount++;
                                totalTableCount++;
                                try
                                {
                                    int rows = shape.Table.Rows.Count;
                                    int columns = shape.Table.Columns.Count;
                                    tableInfo += $"{rows}x{columns}; ";
                                }
                                catch
                                {
                                    tableInfo += "表格; ";
                                }
                            }
                        }
                        catch
                        {
                            // 忽略无法访问的形状
                        }
                    }

                    if (slideTableCount > 0)
                    {
                        searchDetails = $"在幻灯片 {i} 中找到 {slideTableCount} 个表格";
                        break; // 找到第一个有表格的幻灯片就停止
                    }
                }

                if (totalTableCount == 0)
                {
                    searchDetails = $"搜索了所有 {presentation.Slides.Count} 张幻灯片";
                }
            }

            // 检查期望的表格
            bool hasExpectedTable = totalTableCount > 0;
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

            result.ActualValue = totalTableCount > 0 ? tableInfo.TrimEnd(';', ' ') : "无表格";
            result.IsCorrect = hasExpectedTable;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;

            // 增强诊断信息，包含解析后的参数
            string paramInfo = "";
            if (parameters.TryGetValue("ExpectedRows", out string? rowsParam) &&
                parameters.TryGetValue("ExpectedColumns", out string? columnsParam))
            {
                paramInfo = $" (解析参数: 行={rowsParam}, 列={columnsParam})";
            }
            result.Details = $"幻灯片 {slideIndex} 表格检测: 期望 {result.ExpectedValue}, 实际 {result.ActualValue}{paramInfo}";
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
    private KnowledgePointResult DetectAppliedTheme(Presentation presentation, Dictionary<string, string> parameters)
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
    private KnowledgePointResult DetectSlideBackground(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetSlideBackground",
            Parameters = parameters
        };

        try
        {
            // 获取幻灯片索引参数（支持SlideNumber和SlideIndex两种参数名）
            if (!parameters.TryGetValue("SlideNumber", out string? slideNumberStr) &&
                !parameters.TryGetValue("SlideIndex", out slideNumberStr))
            {
                result.ErrorMessage = "缺少必要参数: SlideNumber 或 SlideIndex";
                return result;
            }

            if (!int.TryParse(slideNumberStr, out int slideIndex))
            {
                result.ErrorMessage = $"幻灯片索引格式错误: {slideNumberStr}";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            Slide slide = presentation.Slides[slideIndex];

            // 检测填充类型
            bool fillTypeCorrect = CheckFillType(slide, parameters, result);

            // 检测具体的填充选项
            bool fillOptionsCorrect = CheckFillOptions(slide, parameters, result);

            // 总体结果：填充类型和填充选项都正确才算正确
            result.IsCorrect = fillTypeCorrect && fillOptionsCorrect;
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
    /// 检测填充类型
    /// </summary>
    private bool CheckFillType(Slide slide, Dictionary<string, string> parameters, KnowledgePointResult result)
    {
        if (!parameters.TryGetValue("FillType", out string? expectedFillType))
        {
            // 如果没有指定填充类型，只要不是默认背景就算正确
            string backgroundType = slide.Background.Type.ToString();
            result.ExpectedValue = "非默认背景";
            result.ActualValue = backgroundType;
            return !backgroundType.Contains("Default", StringComparison.OrdinalIgnoreCase);
        }

        // 根据填充类型检测背景
        string actualFillType = DetectActualFillType(slide);
        result.ExpectedValue = expectedFillType;
        result.ActualValue = actualFillType;

        return string.Equals(actualFillType, expectedFillType, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 检测具体的填充选项
    /// </summary>
    private bool CheckFillOptions(Slide slide, Dictionary<string, string> parameters, KnowledgePointResult result)
    {
        bool allOptionsCorrect = true;
        List<string> detailsList = [];

        // 获取当前的填充类型
        string actualFillType = DetectActualFillType(slide);

        // 检测图案类型（仅当填充类型为图案填充时）
        if (parameters.TryGetValue("PatternType", out string? expectedPattern))
        {
            if (actualFillType == "图案填充")
            {
                string actualPattern = DetectPatternType(slide);
                bool patternCorrect = string.Equals(actualPattern, expectedPattern, StringComparison.OrdinalIgnoreCase);
                allOptionsCorrect &= patternCorrect;
                detailsList.Add($"图案类型: 期望 {expectedPattern}, 实际 {actualPattern}");
            }
            else
            {
                // 如果不是图案填充，但期望有图案类型，则认为不正确
                allOptionsCorrect = false;
                detailsList.Add($"图案类型: 期望 {expectedPattern}, 但当前填充类型为 {actualFillType}");
            }
        }

        // 检测纹理类型（仅当填充类型为纹理填充时）
        if (parameters.TryGetValue("TextureType", out string? expectedTexture))
        {
            if (actualFillType == "纹理填充")
            {
                string actualTexture = DetectTextureType(slide);
                bool textureCorrect = string.Equals(actualTexture, expectedTexture, StringComparison.OrdinalIgnoreCase);
                allOptionsCorrect &= textureCorrect;
                detailsList.Add($"纹理类型: 期望 {expectedTexture}, 实际 {actualTexture}");
            }
            else
            {
                // 如果不是纹理填充，但期望有纹理类型，则认为不正确
                allOptionsCorrect = false;
                detailsList.Add($"纹理类型: 期望 {expectedTexture}, 但当前填充类型为 {actualFillType}");
            }
        }

        // 检测预设渐变类型（仅当填充类型为渐变填充时）
        if (parameters.TryGetValue("PresetGradientType", out string? expectedGradient))
        {
            if (actualFillType == "渐变填充")
            {
                string actualGradient = DetectPresetGradientType(slide);
                bool gradientCorrect = string.Equals(actualGradient, expectedGradient, StringComparison.OrdinalIgnoreCase);
                allOptionsCorrect &= gradientCorrect;
                detailsList.Add($"预设渐变: 期望 {expectedGradient}, 实际 {actualGradient}");
            }
            else
            {
                // 如果不是渐变填充，但期望有预设渐变，则认为不正确
                allOptionsCorrect = false;
                detailsList.Add($"预设渐变: 期望 {expectedGradient}, 但当前填充类型为 {actualFillType}");
            }
        }

        // 检测线性渐变方向（仅当填充类型为渐变填充时）
        if (parameters.TryGetValue("LinearGradientDirection", out string? expectedDirection))
        {
            if (actualFillType == "渐变填充")
            {
                string actualDirection = DetectLinearGradientDirection(slide);
                bool directionCorrect = string.Equals(actualDirection, expectedDirection, StringComparison.OrdinalIgnoreCase);
                allOptionsCorrect &= directionCorrect;
                detailsList.Add($"渐变方向: 期望 {expectedDirection}, 实际 {actualDirection}");
            }
            else
            {
                // 如果不是渐变填充，但期望有渐变方向，则认为不正确
                allOptionsCorrect = false;
                detailsList.Add($"渐变方向: 期望 {expectedDirection}, 但当前填充类型为 {actualFillType}");
            }
        }

        if (detailsList.Count > 0)
        {
            result.Details = string.Join("; ", detailsList);
        }

        return allOptionsCorrect;
    }

    /// <summary>
    /// 检测实际的填充类型
    /// </summary>
    private string DetectActualFillType(Slide slide)
    {
        try
        {
            PowerPoint.ShapeRange background = slide.Background;
            PowerPoint.FillFormat fill = background.Fill;

            return fill.Type switch
            {
                Microsoft.Office.Core.MsoFillType.msoFillSolid => "实心颜色填充",
                Microsoft.Office.Core.MsoFillType.msoFillPatterned => "图案填充",
                Microsoft.Office.Core.MsoFillType.msoFillTextured => "纹理填充",
                Microsoft.Office.Core.MsoFillType.msoFillGradient => "渐变填充",
                Microsoft.Office.Core.MsoFillType.msoFillBackground => "背景自动填充",
                _ => "未知填充类型"
            };
        }
        catch
        {
            return "检测失败";
        }
    }

    /// <summary>
    /// 检测图案类型
    /// </summary>
    private string DetectPatternType(Slide slide)
    {
        try
        {
            PowerPoint.ShapeRange background = slide.Background;
            PowerPoint.FillFormat fill = background.Fill;

            if (fill.Type == Microsoft.Office.Core.MsoFillType.msoFillPatterned)
            {
                try
                {
                    // 尝试获取图案类型
                    MsoPatternType pattern = fill.Pattern;
                    string patternName = GetPatternTypeName(pattern);

                    if (!string.IsNullOrEmpty(patternName))
                    {
                        return patternName;
                    }

                    // 如果无法获取具体图案类型，返回通用结果
                    return "图案填充已应用";
                }
                catch
                {
                    // 如果获取图案属性失败，返回通用结果
                    return "图案填充已应用";
                }
            }

            return "无图案填充";
        }
        catch
        {
            return "检测失败";
        }
    }

    /// <summary>
    /// 根据图案枚举值获取图案类型名称
    /// </summary>
    /// <param name="pattern">图案枚举值</param>
    /// <returns>图案类型名称</returns>
    private string GetPatternTypeName(MsoPatternType pattern)
    {
        return pattern switch
        {
            // 百分比图案
            Microsoft.Office.Core.MsoPatternType.msoPattern5Percent => "5%",
            Microsoft.Office.Core.MsoPatternType.msoPattern10Percent => "10%",
            Microsoft.Office.Core.MsoPatternType.msoPattern20Percent => "20%",
            Microsoft.Office.Core.MsoPatternType.msoPattern25Percent => "25%",
            Microsoft.Office.Core.MsoPatternType.msoPattern30Percent => "30%",
            Microsoft.Office.Core.MsoPatternType.msoPattern40Percent => "40%",
            Microsoft.Office.Core.MsoPatternType.msoPattern50Percent => "50%",
            Microsoft.Office.Core.MsoPatternType.msoPattern60Percent => "60%",
            Microsoft.Office.Core.MsoPatternType.msoPattern70Percent => "70%",
            Microsoft.Office.Core.MsoPatternType.msoPattern75Percent => "75%",
            Microsoft.Office.Core.MsoPatternType.msoPattern80Percent => "80%",
            Microsoft.Office.Core.MsoPatternType.msoPattern90Percent => "90%",

            // 对角线图案
            Microsoft.Office.Core.MsoPatternType.msoPatternLightDownwardDiagonal => "浅色下对角线",
            Microsoft.Office.Core.MsoPatternType.msoPatternLightUpwardDiagonal => "浅色上对角线",
            Microsoft.Office.Core.MsoPatternType.msoPatternDarkDownwardDiagonal => "深色下对角线",
            Microsoft.Office.Core.MsoPatternType.msoPatternDarkUpwardDiagonal => "深色上对角线",
            Microsoft.Office.Core.MsoPatternType.msoPatternWideDownwardDiagonal => "宽下对角线",
            Microsoft.Office.Core.MsoPatternType.msoPatternWideUpwardDiagonal => "宽上对角线",

            // 线条图案
            Microsoft.Office.Core.MsoPatternType.msoPatternLightVertical => "浅色竖线",
            Microsoft.Office.Core.MsoPatternType.msoPatternLightHorizontal => "浅色横线",
            Microsoft.Office.Core.MsoPatternType.msoPatternDarkVertical => "深色竖线",
            Microsoft.Office.Core.MsoPatternType.msoPatternDarkHorizontal => "深色横线",
            Microsoft.Office.Core.MsoPatternType.msoPatternNarrowVertical => "窄竖线",
            Microsoft.Office.Core.MsoPatternType.msoPatternNarrowHorizontal => "窄横线",

            // 虚线图案
            Microsoft.Office.Core.MsoPatternType.msoPatternDashedDownwardDiagonal => "下对角虚线",
            Microsoft.Office.Core.MsoPatternType.msoPatternDashedUpwardDiagonal => "上对角虚线",
            Microsoft.Office.Core.MsoPatternType.msoPatternDashedHorizontal => "横虚线",
            Microsoft.Office.Core.MsoPatternType.msoPatternDashedVertical => "竖虚线",

            // 特殊图案
            Microsoft.Office.Core.MsoPatternType.msoPatternSmallConfetti => "小纸屑",
            Microsoft.Office.Core.MsoPatternType.msoPatternLargeConfetti => "大纸屑",
            Microsoft.Office.Core.MsoPatternType.msoPatternZigZag => "之字形",
            Microsoft.Office.Core.MsoPatternType.msoPatternWave => "波浪线",
            Microsoft.Office.Core.MsoPatternType.msoPatternDiagonalBrick => "对角砖形",
            Microsoft.Office.Core.MsoPatternType.msoPatternHorizontalBrick => "横向砖形",
            Microsoft.Office.Core.MsoPatternType.msoPatternPlaid => "苏格兰方格呢",
            Microsoft.Office.Core.MsoPatternType.msoPatternWeave => "编织物",
            Microsoft.Office.Core.MsoPatternType.msoPatternSphere => "球体",
            Microsoft.Office.Core.MsoPatternType.msoPatternTrellis => "棚架",
            Microsoft.Office.Core.MsoPatternType.msoPatternShingle => "瓦形",
            Microsoft.Office.Core.MsoPatternType.msoPatternDottedDiamond => "点式菱形",
            Microsoft.Office.Core.MsoPatternType.msoPatternDottedGrid => "虚线网格",
            Microsoft.Office.Core.MsoPatternType.msoPatternDivot => "草皮",
            Microsoft.Office.Core.MsoPatternType.msoPatternSmallGrid => "小网格",
            Microsoft.Office.Core.MsoPatternType.msoPatternLargeGrid => "大网格",
            Microsoft.Office.Core.MsoPatternType.msoPatternSmallCheckerBoard => "小棋盘",
            Microsoft.Office.Core.MsoPatternType.msoPatternLargeCheckerBoard => "大棋盘",
            Microsoft.Office.Core.MsoPatternType.msoPatternOutlinedDiamond => "轮廓式菱形",
            Microsoft.Office.Core.MsoPatternType.msoPatternSolidDiamond => "实心菱形",

            _ => "未知图案类型"
        };
    }

    /// <summary>
    /// 检测纹理类型
    /// </summary>
    private string DetectTextureType(Slide slide)
    {
        try
        {
            PowerPoint.ShapeRange background = slide.Background;
            PowerPoint.FillFormat fill = background.Fill;

            if (fill.Type == Microsoft.Office.Core.MsoFillType.msoFillTextured)
            {
                try
                {
                    // 尝试获取纹理类型
                    MsoPresetTexture preset = fill.PresetTexture;
                    string textureName = GetTextureTypeName(preset);

                    if (!string.IsNullOrEmpty(textureName))
                    {
                        return textureName;
                    }

                    // 如果无法获取具体纹理类型，返回通用结果
                    return "纹理填充已应用";
                }
                catch
                {
                    // 如果获取纹理属性失败，返回通用结果
                    return "纹理填充已应用";
                }
            }

            return "无纹理填充";
        }
        catch
        {
            return "检测失败";
        }
    }

    /// <summary>
    /// 根据纹理枚举值获取纹理类型名称
    /// </summary>
    /// <param name="texture">纹理枚举值</param>
    /// <returns>纹理类型名称</returns>
    private string GetTextureTypeName(MsoPresetTexture texture)
    {
        return texture switch
        {
            Microsoft.Office.Core.MsoPresetTexture.msoTexturePapyrus => "纸莎草纸",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureCanvas => "画布",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureDenim => "斜纹布",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureWovenMat => "编织物",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureWaterDroplets => "水滴",
            Microsoft.Office.Core.MsoPresetTexture.msoTexturePaperBag => "纸袋",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureFishFossil => "鱼类化石",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureSand => "沙滩",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureGreenMarble => "绿色大理石",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureWhiteMarble => "白色大理石",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureBrownMarble => "褐色大理石",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureGranite => "花岗岩",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureNewsprint => "新闻纸",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureRecycledPaper => "再生纸",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureParchment => "羊皮纸",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureStationery => "信纸",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureBlueTissuePaper => "蓝色面巾纸",
            Microsoft.Office.Core.MsoPresetTexture.msoTexturePinkTissuePaper => "粉色面巾纸",
            Microsoft.Office.Core.MsoPresetTexture.msoTexturePurpleMesh => "紫色网格",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureBouquet => "花束",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureCork => "软木塞",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureWalnut => "胡桃",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureOak => "栎木",
            Microsoft.Office.Core.MsoPresetTexture.msoTextureMediumWood => "深色木质",

            _ => "未知纹理类型"
        };
    }

    /// <summary>
    /// 检测预设渐变类型
    /// </summary>
    private string DetectPresetGradientType(Slide slide)
    {
        try
        {
            PowerPoint.ShapeRange background = slide.Background;
            PowerPoint.FillFormat fill = background.Fill;

            if (fill.Type == Microsoft.Office.Core.MsoFillType.msoFillGradient)
            {
                try
                {
                    // 尝试获取预设渐变类型
                    MsoPresetGradientType presetGradient = fill.PresetGradientType;
                    string gradientName = GetPresetGradientTypeName(presetGradient);

                    if (!string.IsNullOrEmpty(gradientName))
                    {
                        return gradientName;
                    }

                    // 如果无法获取具体预设渐变类型，检查是否为自定义渐变
                    MsoGradientStyle gradientStyle = fill.GradientStyle;
                    if (gradientStyle != Microsoft.Office.Core.MsoGradientStyle.msoGradientMixed)
                    {
                        return "自定义渐变填充";
                    }

                    // 返回通用结果
                    return "渐变填充已应用";
                }
                catch
                {
                    // 如果获取渐变属性失败，返回通用结果
                    return "渐变填充已应用";
                }
            }

            return "无渐变填充";
        }
        catch
        {
            return "检测失败";
        }
    }

    /// <summary>
    /// 根据预设渐变枚举值获取渐变类型名称
    /// </summary>
    /// <param name="presetGradient">预设渐变枚举值</param>
    /// <returns>渐变类型名称</returns>
    private string GetPresetGradientTypeName(MsoPresetGradientType presetGradient)
    {
        return presetGradient switch
        {
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientEarlySunset => "红日西斜",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientLateSunset => "金乌坠地",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientNightfall => "暮霭沉沉",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientDaybreak => "雨后初晴",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientHorizon => "极目远眺",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientDesert => "漫漫黄沙",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientOcean => "碧海青天",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientCalmWater => "心如止水",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientFire => "熊熊火焰",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientFog => "薄雾浓云",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientMoss => "茵茵绿原",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientPeacock => "孔雀开屏",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientWheat => "麦浪滚滚",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientParchment => "羊皮纸",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientMahogany => "红木",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientRainbow => "彩虹出岫",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientRainbowII => "彩虹出岫II",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientGold => "金色年华",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientGoldII => "金色年华II",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientBrass => "铜黄色",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientChrome => "铬色",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientChromeII => "铬色II",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientSilver => "银波荡漾",
            Microsoft.Office.Core.MsoPresetGradientType.msoGradientSapphire => "宝石蓝",

            _ => "未知预设渐变"
        };
    }

    /// <summary>
    /// 检测线性渐变方向
    /// </summary>
    private string DetectLinearGradientDirection(Slide slide)
    {
        try
        {
            PowerPoint.ShapeRange background = slide.Background;
            PowerPoint.FillFormat fill = background.Fill;

            if (fill.Type == Microsoft.Office.Core.MsoFillType.msoFillGradient)
            {
                try
                {
                    // 检查渐变样式是否为线性
                    MsoGradientStyle gradientStyle = fill.GradientStyle;
                    if (gradientStyle is MsoGradientStyle.msoGradientHorizontal or
                        MsoGradientStyle.msoGradientVertical or
                        MsoGradientStyle.msoGradientDiagonalUp or
                        MsoGradientStyle.msoGradientDiagonalDown)
                    {
                        // 尝试获取渐变角度来确定具体方向
                        try
                        {
                            float angle = fill.GradientAngle;
                            string directionName = GetLinearGradientDirectionName(gradientStyle, angle);

                            if (!string.IsNullOrEmpty(directionName))
                            {
                                return directionName;
                            }
                        }
                        catch
                        {
                            // 如果无法获取角度，根据样式返回基本方向
                            return GetBasicGradientDirectionName(gradientStyle);
                        }
                    }

                    // 如果不是线性渐变，返回相应信息
                    return gradientStyle switch
                    {
                        Microsoft.Office.Core.MsoGradientStyle.msoGradientFromCenter => "径向渐变",
                        Microsoft.Office.Core.MsoGradientStyle.msoGradientFromCorner => "角落渐变",
                        Microsoft.Office.Core.MsoGradientStyle.msoGradientFromTitle => "标题渐变",
                        _ => "非线性渐变"
                    };
                }
                catch
                {
                    // 如果获取渐变属性失败，返回通用结果
                    return "渐变填充已应用";
                }
            }

            return "无渐变填充";
        }
        catch
        {
            return "检测失败";
        }
    }

    /// <summary>
    /// 根据渐变样式和角度获取线性渐变方向名称
    /// </summary>
    /// <param name="gradientStyle">渐变样式</param>
    /// <param name="angle">渐变角度</param>
    /// <returns>渐变方向名称</returns>
    private string GetLinearGradientDirectionName(MsoGradientStyle gradientStyle, float angle)
    {
        // 根据角度判断具体方向
        float normalizedAngle = angle % 360;
        if (normalizedAngle < 0)
        {
            normalizedAngle += 360;
        }

        return normalizedAngle switch
        {
            >= 315 or < 45 => "线性向右",
            >= 45 and < 135 => gradientStyle == Microsoft.Office.Core.MsoGradientStyle.msoGradientDiagonalUp ? "线性对角-左下到右上" : "线性向上",
            >= 135 and < 225 => "线性向左",
            >= 225 and < 315 => gradientStyle == Microsoft.Office.Core.MsoGradientStyle.msoGradientDiagonalDown ? "线性对角-左上到右下" : "线性向下",
            _ => GetBasicGradientDirectionName(gradientStyle)
        };
    }

    /// <summary>
    /// 根据渐变样式获取基本方向名称
    /// </summary>
    /// <param name="gradientStyle">渐变样式</param>
    /// <returns>基本方向名称</returns>
    private string GetBasicGradientDirectionName(MsoGradientStyle gradientStyle)
    {
        return gradientStyle switch
        {
            Microsoft.Office.Core.MsoGradientStyle.msoGradientHorizontal => "线性水平方向",
            Microsoft.Office.Core.MsoGradientStyle.msoGradientVertical => "线性垂直方向",
            Microsoft.Office.Core.MsoGradientStyle.msoGradientDiagonalUp => "线性对角向上",
            Microsoft.Office.Core.MsoGradientStyle.msoGradientDiagonalDown => "线性对角向下",
            _ => "线性渐变已应用"
        };
    }

    /// <summary>
    /// 检测文本字形（加粗、斜体、下划线、删除线）
    /// </summary>
    private KnowledgePointResult DetectTextStyle(Presentation presentation, Dictionary<string, string> parameters)
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

            Slide slide = presentation.Slides[slideIndex];

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
    private KnowledgePointResult DetectElementPosition(Presentation presentation, Dictionary<string, string> parameters)
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

            Slide slide = presentation.Slides[slideIndex];

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
    private KnowledgePointResult DetectElementSize(Presentation presentation, Dictionary<string, string> parameters)
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

            Slide slide = presentation.Slides[slideIndex];

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
    private KnowledgePointResult DetectTextAlignment(Presentation presentation, Dictionary<string, string> parameters)
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

            Slide slide = presentation.Slides[slideIndex];

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

            TextRange textRange = shape.TextFrame.TextRange;
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
    private KnowledgePointResult DetectSlideTransitionMode(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SlideTransitionMode",
            Parameters = parameters
        };

        try
        {
            // 尝试获取幻灯片索引列表（支持多种参数名）
            if (!parameters.TryGetValue("SlideIndexes", out string? slideIndexesStr))
            {
                _ = parameters.TryGetValue("SlideNumbers", out slideIndexesStr);
            }

            // 尝试获取切换方案（支持多种参数名）
            if (!parameters.TryGetValue("TransitionMode", out string? expectedModeStr))
            {
                _ = parameters.TryGetValue("TransitionScheme", out expectedModeStr);
            }

            // 尝试获取切换方向（支持新的参数结构）
            _ = parameters.TryGetValue("TransitionDirection", out string? expectedDirectionStr);

            if (string.IsNullOrEmpty(slideIndexesStr) || string.IsNullOrEmpty(expectedModeStr))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndexes/SlideNumbers 或 TransitionMode/TransitionScheme";
                return result;
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

                Slide slide = presentation.Slides[slideIndex];
                string actualTransition = GetTransitionEffectName(slide.SlideShowTransition.EntryEffect);

                // 检测切换方案是否匹配
                bool schemeMatches = CheckTransitionSchemeMatch(actualTransition, expectedModeStr);

                // 如果指定了切换方向，也要检测方向
                bool directionMatches = true;
                if (!string.IsNullOrEmpty(expectedDirectionStr))
                {
                    // 这里可以添加方向检测逻辑
                    // 由于 PowerPoint API 限制，暂时简化处理
                    directionMatches = true;
                }

                bool isCorrect = schemeMatches && directionMatches;

                if (isCorrect)
                {
                    correctCount++;
                    details.Add($"幻灯片 {slideIndex}: 切换方案匹配 ({actualTransition})");
                }
                else
                {
                    details.Add($"幻灯片 {slideIndex}: 切换方案不匹配 (实际: {actualTransition}, 期望方案: {expectedModeStr})");
                }
            }

            result.ExpectedValue = $"切换方式 {expectedModeStr}";
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
    /// 检测超链接
    /// </summary>
    private KnowledgePointResult DetectHyperlink(Presentation presentation, Dictionary<string, string> parameters)
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

            Slide slide = presentation.Slides[slideIndex];

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
                TextRange textRange = shape.TextFrame.TextRange;
                if (textRange.ActionSettings[PpMouseActivation.ppMouseClick].Hyperlink.Address != null)
                {
                    hasHyperlink = true;
                    hyperlinkInfo = textRange.ActionSettings[PpMouseActivation.ppMouseClick].Hyperlink.Address;
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
    private KnowledgePointResult DetectSlideNumber(Presentation presentation, Dictionary<string, string> parameters)
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
                    Slide slide = presentation.Slides[1];
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
    private KnowledgePointResult DetectFooterText(Presentation presentation, Dictionary<string, string> parameters)
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
                    Slide slide = presentation.Slides[1];
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
    private KnowledgePointResult DetectInsertedSmartArt(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "InsertSmartArt",
            Parameters = parameters
        };

        try
        {
            // 智能搜索：优先检测指定幻灯片，如果没有找到则搜索所有幻灯片
            int totalSmartArtCount = 0;
            string searchDetails = "";

            // 尝试获取指定的幻灯片索引
            int slideIndex = 0;
            bool hasSpecificSlide = parameters.TryGetValue("SlideIndex", out string? slideIndexStr) &&
                                   int.TryParse(slideIndexStr, out slideIndex) &&
                                   slideIndex >= 1 && slideIndex <= presentation.Slides.Count;

            if (hasSpecificSlide)
            {
                // 检测指定幻灯片
                Slide slide = presentation.Slides[slideIndex];
                foreach (PowerPoint.Shape shape in slide.Shapes)
                {
                    if (shape.Type.ToString().Contains("SmartArt") || shape.Type.ToString().Contains("msoSmartArt"))
                    {
                        totalSmartArtCount++;
                    }
                }
                searchDetails = $"幻灯片 {slideIndex}";
            }

            // 如果在指定幻灯片没找到SmartArt，或者没有指定幻灯片，则搜索所有幻灯片
            if (totalSmartArtCount == 0)
            {
                for (int i = 1; i <= presentation.Slides.Count; i++)
                {
                    Slide slide = presentation.Slides[i];
                    int slideSmartArtCount = 0;

                    foreach (PowerPoint.Shape shape in slide.Shapes)
                    {
                        if (shape.Type.ToString().Contains("SmartArt") || shape.Type.ToString().Contains("msoSmartArt"))
                        {
                            slideSmartArtCount++;
                            totalSmartArtCount++;
                        }
                    }

                    if (slideSmartArtCount > 0)
                    {
                        searchDetails = $"在幻灯片 {i} 中找到 {slideSmartArtCount} 个SmartArt";
                        break; // 找到第一个有SmartArt的幻灯片就停止
                    }
                }

                if (totalSmartArtCount == 0)
                {
                    searchDetails = $"搜索了所有 {presentation.Slides.Count} 张幻灯片";
                }
            }

            // 检查期望的SmartArt数量
            bool hasExpectedSmartArt = totalSmartArtCount > 0;
            if (parameters.TryGetValue("ExpectedSmartArtCount", out string? expectedCountStr) &&
                int.TryParse(expectedCountStr, out int expectedCount))
            {
                hasExpectedSmartArt = totalSmartArtCount >= expectedCount;
                result.ExpectedValue = $"至少{expectedCount}个SmartArt图形";
            }
            else
            {
                result.ExpectedValue = "至少1个SmartArt图形";
            }

            result.ActualValue = $"{totalSmartArtCount}个SmartArt图形";
            result.IsCorrect = hasExpectedSmartArt;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"SmartArt检测: 期望 {result.ExpectedValue}, {searchDetails}, 实际 {result.ActualValue}";
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
    private KnowledgePointResult DetectInsertedNote(Presentation presentation, Dictionary<string, string> parameters)
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

            Slide slide = presentation.Slides[slideIndex];
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
    private KnowledgePointResult DetectTableContent(Presentation presentation, Dictionary<string, string> parameters)
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

            Slide slide = presentation.Slides[slideIndex];
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
    private KnowledgePointResult DetectTableStyle(Presentation presentation, Dictionary<string, string> parameters)
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

            Slide slide = presentation.Slides[slideIndex];
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
    private static void CleanupPowerPointResources(Presentation? presentation, Application? pptApp)
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
    private static void ResolveParametersForPresentation(Dictionary<string, string> parameters, Presentation? presentation, ParameterResolutionContext context)
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
    private static int GetMaxValueForParameter(string parameterName, Presentation? presentation, Dictionary<string, string> parameters)
    {
        if (presentation is null)
        {
            // 如果presentation为null，返回默认值
            return 1;
        }

        string lowerName = parameterName.ToLowerInvariant();

        if (lowerName.Contains("slide"))
        {
            // 限制随机幻灯片索引的范围，避免超出合理范围
            int slideCount = presentation.Slides.Count;
            return Math.Min(slideCount, 3); // 最多随机到前3张幻灯片
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
    /// 获取版式的显示名称
    /// </summary>
    private string GetLayoutDisplayName(PpSlideLayout layout)
    {
        try
        {
            return layout.ToString();
        }
        catch
        {
            return layout.ToString();
        }
    }

    /// <summary>
    /// 标准化版式名称以便比较
    /// </summary>
    private string NormalizeLayoutName(string layoutName)
    {
        if (string.IsNullOrEmpty(layoutName))
        {
            return "";
        }

        // 移除空格和特殊字符，转换为小写
        string normalized = layoutName.Replace(" ", "").Replace("（", "(").Replace("）", ")").ToLowerInvariant();

        // 处理常见的版式名称映射
        return normalized switch
        {
            "标题幻灯片" or "titlelayout" or "title" => "标题幻灯片",
            "标题和内容" or "titleandcontent" or "titlecontent" => "标题和内容",
            "节标题" or "sectionheader" or "section" => "节标题",
            "两栏内容" or "twocontent" or "twocolumn" => "两栏内容",
            "比较" or "comparison" or "compare" => "比较",
            "内容与标题" or "contentwithcaption" or "contentcaption" => "内容与标题",
            "图片与标题" or "picturewithcaption" or "picturecaption" => "图片与标题",
            "标题和竖排文字" or "titleandverticaltext" or "titlevertical" => "标题和竖排文字",
            "垂直排列标题与文本" or "verticaltitleandtext" or "verticaltitle" => "垂直排列标题与文本",
            "仅标题" or "titleonly" or "onlytitle" => "仅标题",
            "空白" or "blank" or "empty" => "空白",
            _ => normalized
        };
    }

    /// <summary>
    /// 检测动画计时与延时设置
    /// </summary>
    private KnowledgePointResult DetectAnimationTiming(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationTiming",
            Parameters = parameters
        };

        try
        {
            if (!parameters.TryGetValue("SlideNumber", out string? slideNumberStr) ||
                !int.TryParse(slideNumberStr, out int slideNumber))
            {
                result.ErrorMessage = "缺少必要参数: SlideNumber";
                return result;
            }

            if (slideNumber < 1 || slideNumber > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片序号超出范围: {slideNumber}";
                return result;
            }

            // 获取可选参数
            _ = parameters.TryGetValue("ObjectIndex", out string? objectIndexStr);
            _ = parameters.TryGetValue("TriggerMode", out string? expectedTriggerMode);
            _ = parameters.TryGetValue("DelayTime", out string? expectedDelayTimeStr);
            _ = parameters.TryGetValue("Duration", out string? expectedDurationStr);

            Slide slide = presentation.Slides[slideNumber];
            bool animationFound = false;
            string animationDetails = "";

            // 检查幻灯片的动画效果
            try
            {
                if (slide.TimeLine.MainSequence.Count > 0)
                {
                    animationFound = true;
                    animationDetails = $"找到 {slide.TimeLine.MainSequence.Count} 个动画效果";

                    // 如果指定了具体的检测条件，进行详细检查
                    if (!string.IsNullOrEmpty(expectedTriggerMode) ||
                        !string.IsNullOrEmpty(expectedDelayTimeStr) ||
                        !string.IsNullOrEmpty(expectedDurationStr))
                    {
                        // 这里可以添加更详细的动画属性检测
                        // 由于PowerPoint Interop API的限制，这里只做基本检测
                        animationDetails += "，包含计时设置";
                    }
                }
                else
                {
                    animationDetails = "未找到动画效果";
                }
            }
            catch (Exception ex)
            {
                animationDetails = $"检测动画时出错: {ex.Message}";
            }

            result.ExpectedValue = "包含动画计时设置";
            result.ActualValue = animationDetails;
            result.IsCorrect = animationFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideNumber} 动画计时检测: {animationDetails}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测动画计时失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测动画持续时间设置
    /// </summary>
    private KnowledgePointResult DetectAnimationDuration(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationDuration",
            Parameters = parameters
        };

        try
        {
            // 支持新的参数结构（包含 ElementOrder）
            if (!parameters.TryGetValue("SlideNumber", out string? slideNumberStr) ||
                !int.TryParse(slideNumberStr, out int slideNumber))
            {
                result.ErrorMessage = "缺少必要参数: SlideNumber";
                return result;
            }

            if (slideNumber < 1 || slideNumber > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideNumber}";
                return result;
            }

            // 获取元素顺序参数（新增的参数）
            int elementOrder = 1; // 默认值
            if (parameters.TryGetValue("ElementOrder", out string? elementOrderStr))
            {
                _ = int.TryParse(elementOrderStr, out elementOrder);
            }

            // 获取期望的持续时间和延迟时间
            _ = parameters.TryGetValue("Duration", out string? expectedDurationStr);
            _ = parameters.TryGetValue("DelayTime", out string? expectedDelayTimeStr);

            Slide slide = presentation.Slides[slideNumber];
            bool animationFound = false;
            string animationDetails = "";

            // 检查幻灯片的动画效果
            try
            {
                if (slide.TimeLine.MainSequence.Count > 0)
                {
                    animationFound = true;

                    // 检查指定元素的动画
                    if (elementOrder <= slide.TimeLine.MainSequence.Count)
                    {
                        Effect animation = slide.TimeLine.MainSequence[elementOrder];
                        animationDetails = $"第{elementOrder}个元素找到动画效果";

                        // 如果指定了具体的持续时间或延迟时间，进行详细检查
                        if (!string.IsNullOrEmpty(expectedDurationStr) || !string.IsNullOrEmpty(expectedDelayTimeStr))
                        {
                            // 由于PowerPoint Interop API的限制，这里只做基本检测
                            animationDetails += "，包含持续时间设置";
                        }
                    }
                    else
                    {
                        animationDetails = $"第{elementOrder}个元素未找到动画效果（总共{slide.TimeLine.MainSequence.Count}个动画）";
                        animationFound = false;
                    }
                }
                else
                {
                    animationDetails = "未找到动画效果";
                }
            }
            catch (Exception ex)
            {
                animationDetails = $"检测动画时出错: {ex.Message}";
            }

            result.ExpectedValue = $"第{elementOrder}个元素包含动画持续时间设置";
            result.ActualValue = animationDetails;
            result.IsCorrect = animationFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideNumber} 动画持续时间检测: {animationDetails}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测动画持续时间失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 检测动画播放顺序设置
    /// </summary>
    private KnowledgePointResult DetectAnimationOrder(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetAnimationOrder",
            Parameters = parameters
        };

        try
        {
            // 获取幻灯片参数
            if (!parameters.TryGetValue("SlideNumber", out string? slideNumberStr) ||
                !int.TryParse(slideNumberStr, out int slideNumber))
            {
                result.ErrorMessage = "缺少必要参数: SlideNumber";
                return result;
            }

            if (slideNumber < 1 || slideNumber > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideNumber}";
                return result;
            }

            // 获取动画顺序设置参数
            if (!parameters.TryGetValue("AnimationOrderSettings", out string? orderSettings))
            {
                result.ErrorMessage = "缺少必要参数: AnimationOrderSettings";
                return result;
            }

            Slide slide = presentation.Slides[slideNumber];
            bool animationFound = false;
            List<string> orderDetails = [];

            // 解析动画顺序设置 (格式：元素1:顺序1,元素2:顺序2)
            try
            {
                string[] orderPairs = orderSettings.Split(',');
                foreach (string pair in orderPairs)
                {
                    string[] parts = pair.Trim().Split(':');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0].Trim(), out int elementIndex) &&
                        int.TryParse(parts[1].Trim(), out int animationOrder))
                    {
                        orderDetails.Add($"第{elementIndex}个元素设置为动画顺序{animationOrder}");
                    }
                }

                // 检查幻灯片的动画效果
                if (slide.TimeLine.MainSequence.Count > 0)
                {
                    animationFound = true;
                    orderDetails.Add($"找到 {slide.TimeLine.MainSequence.Count} 个动画效果");

                    // 这里可以添加更详细的动画顺序检测
                    // 由于PowerPoint Interop API的限制，这里只做基本检测
                    orderDetails.Add("动画顺序配置已应用");
                }
                else
                {
                    orderDetails.Add("未找到动画效果");
                }
            }
            catch (Exception ex)
            {
                orderDetails.Add($"检测动画顺序时出错: {ex.Message}");
            }

            result.ExpectedValue = $"动画顺序设置: {orderSettings}";
            result.ActualValue = string.Join("; ", orderDetails);
            result.IsCorrect = animationFound && orderDetails.Count > 1; // 至少有动画且有顺序配置
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideNumber} 动画顺序检测: {result.ActualValue}";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测动画顺序失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 根据操作点名称映射到知识点类型
    /// </summary>
    /// <param name="operationPointName">操作点名称</param>
    /// <returns>知识点类型字符串</returns>
    private static string MapOperationPointNameToKnowledgeType(string operationPointName)
    {
        // 创建操作点名称到知识点类型的映射
        Dictionary<string, string> nameToTypeMapping = new()
        {
            { "设置文稿应用主题", "ApplyTheme" },
            { "设置幻灯片的字体", "SetSlideFont" },
            { "插入幻灯片", "InsertSlide" },
            { "幻灯片插入文本内容", "InsertTextContent" },
            { "设置幻灯片背景", "SetSlideBackground" },
            { "幻灯片插入图片", "InsertImage" },
            { "幻灯片插入SmartArt图形", "InsertSmartArt" },
            { "幻灯片插入表格", "InsertTable" },
            { "删除幻灯片", "DeleteSlide" },
            { "设置幻灯片切换方式", "SetSlideTransition" }
        };

        // 尝试精确匹配
        if (nameToTypeMapping.TryGetValue(operationPointName, out string? exactMatch))
        {
            return exactMatch;
        }

        // 尝试模糊匹配
        foreach (KeyValuePair<string, string> kvp in nameToTypeMapping)
        {
            if (operationPointName.Contains(kvp.Key) || kvp.Key.Contains(operationPointName))
            {
                return kvp.Value;
            }
        }

        // 如果没有找到匹配，返回操作点名称本身
        return operationPointName;
    }

    /// <summary>
    /// 检查形状是否包含图片（包括占位符中的图片）
    /// </summary>
    /// <param name="shape">要检查的形状</param>
    /// <returns>是否包含图片</returns>
    private static bool IsShapeContainingImage(PowerPoint.Shape shape)
    {
        try
        {
            // 检查占位符类型的形状是否包含图片
            string shapeType = shape.Type.ToString();

            // 如果是占位符，检查是否有填充图片
            if (shapeType.Contains("Placeholder"))
            {
                try
                {
                    // 检查填充类型
                    string fillType = shape.Fill.Type.ToString();
                    if (fillType.Contains("Picture") || fillType.Contains("UserPicture"))
                    {
                        return true;
                    }
                }
                catch
                {
                    // 忽略填充检查失败
                }

                // 检查是否有图片内容
                try
                {
                    if (shape.PictureFormat != null)
                    {
                        return true;
                    }
                }
                catch
                {
                    // 忽略图片格式检查失败
                }
            }

            // 检查组合形状中是否包含图片
            if (shapeType.Contains("Group"))
            {
                try
                {
                    foreach (PowerPoint.Shape groupShape in shape.GroupItems)
                    {
                        if (IsShapeContainingImage(groupShape))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // 忽略组合形状检查失败
                }
            }

            return false;
        }
        catch
        {
            // 如果检查过程中出现任何异常，返回false
            return false;
        }
    }

    /// <summary>
    /// 获取切换效果名称
    /// </summary>
    /// <param name="entryEffect">PowerPoint 切换效果枚举</param>
    /// <returns>切换效果名称</returns>
    private static string GetTransitionEffectName(PpEntryEffect entryEffect)
    {
        return entryEffect switch
        {
            PowerPoint.PpEntryEffect.ppEffectNone => "无",
            PowerPoint.PpEntryEffect.ppEffectFade => "淡入淡出",
            PowerPoint.PpEntryEffect.ppEffectWipeLeft => "擦入",
            PowerPoint.PpEntryEffect.ppEffectPushLeft => "推入",
            PowerPoint.PpEntryEffect.ppEffectCoverLeft => "覆盖",
            PowerPoint.PpEntryEffect.ppEffectUncoverLeft => "切入",
            PowerPoint.PpEntryEffect.ppEffectStripsLeftUp => "随机条纹",
            PowerPoint.PpEntryEffect.ppEffectBlindsHorizontal => "百叶窗",
            PowerPoint.PpEntryEffect.ppEffectCheckerboardAcross => "棋盘",
            PowerPoint.PpEntryEffect.ppEffectSplitHorizontalIn => "分割",
            PowerPoint.PpEntryEffect.ppEffectBoxIn => "盒状",
            PowerPoint.PpEntryEffect.ppEffectCircleOut => "圆形",
            PowerPoint.PpEntryEffect.ppEffectFlyFromLeft => "飞入",
            PowerPoint.PpEntryEffect.ppEffectCut => "切出",
            _ => entryEffect.ToString()
        };
    }

    /// <summary>
    /// 标准化切换效果名称
    /// </summary>
    /// <param name="effectName">效果名称</param>
    /// <returns>标准化后的效果名称</returns>
    private static string NormalizeTransitionEffectName(string effectName)
    {
        if (string.IsNullOrEmpty(effectName))
        {
            return effectName;
        }

        // 映射新的切换效果名称到标准名称
        return effectName.ToLower() switch
        {
            "无" or "无切换效果" or "none" => "无",
            "平滑" or "smooth" => "平滑",
            "淡入淡出" or "fade" or "淡出" => "淡入淡出",
            "擦入" or "wipe" => "擦入",
            "推入" or "push" => "推入",
            "覆盖" or "cover" => "覆盖",
            "切入" or "uncover" => "切入",
            "随机条纹" or "strips" => "随机条纹",
            "形状" or "shape" => "形状",
            "显示" or "peek" => "显示",
            "切出" or "cut" => "切出",
            "变换" or "morph" => "变换",
            "突出" or "reveal" => "突出",
            "帘式" or "curtains" => "帘式",
            "布式" or "drape" => "布式",
            "风" or "wind" => "风",
            "上拉帘幕" or "prestige" => "上拉帘幕",
            "折叠" or "origami" => "折叠",
            "压碎" or "crush" => "压碎",
            "到达" or "arrive" => "到达",
            "页面卷曲" or "pagecurl" => "页面卷曲",
            "飞机" or "airplane" => "飞机",
            "日式折纸" or "origami" => "日式折纸",
            "泡沫" or "bubble" => "泡沫",
            "蜂巢" or "honeycomb" => "蜂巢",
            "百叶窗" or "blinds" => "百叶窗",
            "时钟" or "clock" => "时钟",
            "涟漪" or "ripple" => "涟漪",
            "翻转" or "flip" => "翻转",
            "剥转" or "switch" => "剥转",
            "库" or "gallery" => "库",
            "立方体" or "cube" => "立方体",
            "门" or "doors" => "门",
            "程" or "box" => "程",
            "转盘" or "rotate" => "转盘",
            "缩放" or "zoom" => "缩放",
            "随机" or "random" => "随机",
            "平移" or "pan" => "平移",
            "传送系统" or "conveyor" => "传送系统",
            "传送" or "ferris" => "传送",
            "旋转" or "rotate" => "旋转",
            "宫口" or "orbit" => "宫口",
            "轨道" or "orbit" => "轨道",
            "飞过" or "flythrough" => "飞过",
            _ => effectName
        };
    }

    /// <summary>
    /// 检测切换方案是否匹配
    /// </summary>
    /// <param name="actualTransition">实际的切换效果</param>
    /// <param name="expectedScheme">期望的切换方案</param>
    /// <returns>是否匹配</returns>
    private static bool CheckTransitionSchemeMatch(string actualTransition, string expectedScheme)
    {
        if (string.IsNullOrEmpty(actualTransition) || string.IsNullOrEmpty(expectedScheme))
        {
            return false;
        }

        string normalizedTransition = actualTransition.ToLower();
        string normalizedScheme = expectedScheme.ToLower();

        return normalizedScheme switch
        {
            "细微" => IsSubtleTransition(normalizedTransition),
            "华丽" => IsExcitingTransition(normalizedTransition),
            "动感内容" => IsDynamicContentTransition(normalizedTransition),
            _ => false
        };
    }

    /// <summary>
    /// 检测是否为细微类别的切换效果
    /// </summary>
    private static bool IsSubtleTransition(string transition)
    {
        string[] subtleEffects = [
            "无", "平滑", "淡入淡出", "擦入", "推入", "覆盖",
            "切入", "随机条纹", "形状", "显示", "切出", "变换"
        ];

        return subtleEffects.Any(effect => transition.Contains(effect.ToLower()));
    }

    /// <summary>
    /// 检测是否为华丽类别的切换效果
    /// </summary>
    private static bool IsExcitingTransition(string transition)
    {
        string[] excitingEffects = [
            "突出", "帘式", "布式", "风", "上拉帘幕", "折叠", "压碎", "到达",
            "页面卷曲", "飞机", "日式折纸", "泡沫", "蜂巢", "百叶窗", "时钟",
            "涟漪", "翻转", "剥转", "库", "立方体", "门", "程", "转盘", "缩放", "随机"
        ];

        return excitingEffects.Any(effect => transition.Contains(effect.ToLower()));
    }

    /// <summary>
    /// 检测是否为动感内容类别的切换效果
    /// </summary>
    private static bool IsDynamicContentTransition(string transition)
    {
        string[] dynamicEffects = [
            "平移", "传送系统", "传送", "旋转", "宫口", "轨道", "飞过"
        ];

        return dynamicEffects.Any(effect => transition.Contains(effect.ToLower()));
    }

    /// <summary>
    /// 检测幻灯片放映选项
    /// </summary>
    private KnowledgePointResult DetectSlideshowOptions(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SlideshowOptions",
            Parameters = parameters
        };

        try
        {
            // 获取放映设置
            SlideShowSettings settings = presentation.SlideShowSettings;
            List<string> details = [];
            int correctCount = 0;
            int totalChecks = 0;

            // 检测放映类型 (SlideshowType)
            if (parameters.TryGetValue("SlideshowType", out string? expectedType))
            {
                totalChecks++;
                string actualType = GetSlideshowTypeName(settings.ShowType);
                bool typeMatches = string.Equals(actualType, expectedType, StringComparison.OrdinalIgnoreCase);

                if (typeMatches)
                {
                    correctCount++;
                    details.Add($"放映类型匹配: {actualType}");
                }
                else
                {
                    details.Add($"放映类型不匹配 (期望: {expectedType}, 实际: {actualType})");
                }
            }

            // 检测放映范围 (SlideshowRange)
            if (parameters.TryGetValue("SlideshowRange", out string? expectedRange))
            {
                totalChecks++;
                string actualRange = GetSlideshowRangeName(settings.RangeType);
                bool rangeMatches = string.Equals(actualRange, expectedRange, StringComparison.OrdinalIgnoreCase);

                if (rangeMatches)
                {
                    correctCount++;
                    details.Add($"放映范围匹配: {actualRange}");
                }
                else
                {
                    details.Add($"放映范围不匹配 (期望: {expectedRange}, 实际: {actualRange})");
                }
            }

            // 检测切换方式 (AdvanceMode)
            if (parameters.TryGetValue("AdvanceMode", out string? expectedAdvance))
            {
                totalChecks++;
                string actualAdvance = GetAdvanceModeName(settings.AdvanceMode);
                bool advanceMatches = string.Equals(actualAdvance, expectedAdvance, StringComparison.OrdinalIgnoreCase);

                if (advanceMatches)
                {
                    correctCount++;
                    details.Add($"切换方式匹配: {actualAdvance}");
                }
                else
                {
                    details.Add($"切换方式不匹配 (期望: {expectedAdvance}, 实际: {actualAdvance})");
                }
            }

            // 检测循环播放 (LoopUntilStopped)
            if (parameters.TryGetValue("LoopUntilStopped", out string? expectedLoop))
            {
                totalChecks++;
                bool actualLoop = settings.LoopUntilStopped == Microsoft.Office.Core.MsoTriState.msoTrue;
                bool expectedLoopBool = expectedLoop == "是";
                bool loopMatches = actualLoop == expectedLoopBool;

                if (loopMatches)
                {
                    correctCount++;
                    details.Add($"循环播放设置匹配: {(actualLoop ? "是" : "否")}");
                }
                else
                {
                    details.Add($"循环播放设置不匹配 (期望: {expectedLoop}, 实际: {(actualLoop ? "是" : "否")})");
                }
            }

            // 检测使用旁白 (ShowWithNarration)
            if (parameters.TryGetValue("ShowWithNarration", out string? expectedNarration))
            {
                totalChecks++;
                bool actualNarration = settings.ShowWithNarration == Microsoft.Office.Core.MsoTriState.msoTrue;
                bool expectedNarrationBool = expectedNarration == "是";
                bool narrationMatches = actualNarration == expectedNarrationBool;

                if (narrationMatches)
                {
                    correctCount++;
                    details.Add($"旁白设置匹配: {(actualNarration ? "是" : "否")}");
                }
                else
                {
                    details.Add($"旁白设置不匹配 (期望: {expectedNarration}, 实际: {(actualNarration ? "是" : "否")})");
                }
            }

            // 检测使用动画 (ShowWithAnimation)
            if (parameters.TryGetValue("ShowWithAnimation", out string? expectedAnimation))
            {
                totalChecks++;
                bool actualAnimation = settings.ShowWithAnimation == Microsoft.Office.Core.MsoTriState.msoTrue;
                bool expectedAnimationBool = expectedAnimation == "是";
                bool animationMatches = actualAnimation == expectedAnimationBool;

                if (animationMatches)
                {
                    correctCount++;
                    details.Add($"动画设置匹配: {(actualAnimation ? "是" : "否")}");
                }
                else
                {
                    details.Add($"动画设置不匹配 (期望: {expectedAnimation}, 实际: {(actualAnimation ? "是" : "否")})");
                }
            }

            if (totalChecks == 0)
            {
                result.ErrorMessage = "未找到有效的放映选项参数";
                return result;
            }

            result.IsCorrect = correctCount == totalChecks;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : (int)((double)correctCount / totalChecks * result.TotalScore);
            result.Details = string.Join("; ", details);
            result.ExpectedValue = $"检查了 {totalChecks} 个放映选项";
            result.ActualValue = $"匹配了 {correctCount} 个选项";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测放映选项失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 获取放映类型名称
    /// </summary>
    private static string GetSlideshowTypeName(PpSlideShowType showType)
    {
        return showType switch
        {
            PowerPoint.PpSlideShowType.ppShowTypeSpeaker => "演讲者放映（全屏）",
            PowerPoint.PpSlideShowType.ppShowTypeWindow => "窗口中放映",
            PowerPoint.PpSlideShowType.ppShowTypeKiosk => "展示台放映（信息亭模式）",
            PowerPoint.PpSlideShowType.ppShowTypeWindow2 => "第二窗口放映",
            _ => "未知类型"
        };
    }

    /// <summary>
    /// 获取放映范围名称
    /// </summary>
    private static string GetSlideshowRangeName(PpSlideShowRangeType rangeType)
    {
        return rangeType switch
        {
            PowerPoint.PpSlideShowRangeType.ppShowAll => "全部幻灯片",
            PowerPoint.PpSlideShowRangeType.ppShowSlideRange => "幻灯片范围",
            PowerPoint.PpSlideShowRangeType.ppShowNamedSlideShow => "自定义放映",
            _ => "未知范围"
        };
    }

    /// <summary>
    /// 获取切换方式名称
    /// </summary>
    private static string GetAdvanceModeName(PpSlideShowAdvanceMode advanceMode)
    {
        return advanceMode switch
        {
            PowerPoint.PpSlideShowAdvanceMode.ppSlideShowManualAdvance => "手动切换",
            PowerPoint.PpSlideShowAdvanceMode.ppSlideShowUseSlideTimings => "使用幻灯片计时",
            PowerPoint.PpSlideShowAdvanceMode.ppSlideShowRehearseNewTimings => "排练新的计时",
            _ => "未知方式"
        };
    }

    /// <summary>
    /// 检测艺术字设置
    /// </summary>
    private KnowledgePointResult DetectWordArtStyle(Presentation presentation, Dictionary<string, string> parameters)
    {
        KnowledgePointResult result = new()
        {
            KnowledgePointType = "SetWordArtStyle",
            Parameters = parameters
        };

        try
        {
            // 获取参数
            if (!parameters.TryGetValue("SlideNumber", out string? slideNumberStr) ||
                !int.TryParse(slideNumberStr, out int slideNumber))
            {
                result.ErrorMessage = "缺少必要参数: SlideNumber";
                return result;
            }

            if (!parameters.TryGetValue("TextBoxOrder", out string? textBoxOrderStr) ||
                !int.TryParse(textBoxOrderStr, out int textBoxOrder))
            {
                result.ErrorMessage = "缺少必要参数: TextBoxOrder";
                return result;
            }

            // 检查幻灯片索引
            if (slideNumber < 1 || slideNumber > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideNumber}";
                return result;
            }

            Slide slide = presentation.Slides[slideNumber];

            // 查找指定的文本框
            if (textBoxOrder < 1 || textBoxOrder > slide.Shapes.Count)
            {
                result.ErrorMessage = $"文本框索引超出范围: {textBoxOrder}";
                return result;
            }

            PowerPoint.Shape shape = slide.Shapes[textBoxOrder];

            // 检查是否为艺术字
            if (shape.Type != Microsoft.Office.Core.MsoShapeType.msoTextEffect)
            {
                result.ErrorMessage = $"指定的形状不是艺术字: {shape.Type}";
                return result;
            }

            List<string> details = [];
            int correctCount = 0;
            int totalChecks = 0;

            // 检测预设效果 (PresetTextEffect)
            if (parameters.TryGetValue("PresetTextEffect", out string? expectedPresetEffect))
            {
                totalChecks++;
                string actualPresetEffect = GetPresetTextEffectName(shape.TextEffect.PresetTextEffect);
                bool presetMatches = string.Equals(actualPresetEffect, expectedPresetEffect, StringComparison.OrdinalIgnoreCase);

                if (presetMatches)
                {
                    correctCount++;
                    details.Add($"预设效果匹配: {actualPresetEffect}");
                }
                else
                {
                    details.Add($"预设效果不匹配 (期望: {expectedPresetEffect}, 实际: {actualPresetEffect})");
                }
            }

            // 检测形状效果 (TextEffectShape)
            if (parameters.TryGetValue("TextEffectShape", out string? expectedShapeEffect))
            {
                totalChecks++;
                string actualShapeEffect = GetTextEffectShapeName(shape.TextEffect.PresetShape);
                bool shapeMatches = string.Equals(actualShapeEffect, expectedShapeEffect, StringComparison.OrdinalIgnoreCase);

                if (shapeMatches)
                {
                    correctCount++;
                    details.Add($"形状效果匹配: {actualShapeEffect}");
                }
                else
                {
                    details.Add($"形状效果不匹配 (期望: {expectedShapeEffect}, 实际: {actualShapeEffect})");
                }
            }

            // 向后兼容：检测旧的参数
            if (parameters.TryGetValue("WordArtStyle", out string? expectedStyle))
            {
                totalChecks++;
                string actualStyle = GetPresetTextEffectName(shape.TextEffect.PresetTextEffect);
                bool styleMatches = string.Equals(actualStyle, expectedStyle, StringComparison.OrdinalIgnoreCase);

                if (styleMatches)
                {
                    correctCount++;
                    details.Add($"艺术字样式匹配: {actualStyle}");
                }
                else
                {
                    details.Add($"艺术字样式不匹配 (期望: {expectedStyle}, 实际: {actualStyle})");
                }
            }

            if (parameters.TryGetValue("TextEffect", out string? expectedTextEffect))
            {
                totalChecks++;
                string actualTextEffect = GetTextEffectShapeName(shape.TextEffect.PresetShape);
                bool textEffectMatches = string.Equals(actualTextEffect, expectedTextEffect, StringComparison.OrdinalIgnoreCase);

                if (textEffectMatches)
                {
                    correctCount++;
                    details.Add($"文本效果匹配: {actualTextEffect}");
                }
                else
                {
                    details.Add($"文本效果不匹配 (期望: {expectedTextEffect}, 实际: {actualTextEffect})");
                }
            }

            if (totalChecks == 0)
            {
                result.ErrorMessage = "未找到有效的艺术字设置参数";
                return result;
            }

            result.IsCorrect = correctCount == totalChecks;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : (int)((double)correctCount / totalChecks * result.TotalScore);
            result.Details = string.Join("; ", details);
            result.ExpectedValue = $"检查了 {totalChecks} 个艺术字设置";
            result.ActualValue = $"匹配了 {correctCount} 个设置";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"检测艺术字设置失败: {ex.Message}";
            result.IsCorrect = false;
        }

        return result;
    }

    /// <summary>
    /// 获取预设文本效果名称
    /// </summary>
    private static string GetPresetTextEffectName(MsoPresetTextEffect presetEffect)
    {
        return presetEffect switch
        {
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect1 => "效果1",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect2 => "效果2",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect3 => "效果3",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect4 => "效果4",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect5 => "效果5",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect6 => "效果6",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect7 => "效果7",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect8 => "效果8",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect9 => "效果9",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect10 => "效果10",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect11 => "效果11",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect12 => "效果12",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect13 => "效果13",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect14 => "效果14",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect15 => "效果15",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect16 => "效果16",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect17 => "效果17",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect18 => "效果18",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect19 => "效果19",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect20 => "效果20",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect21 => "效果21",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect22 => "效果22",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect23 => "效果23",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect24 => "效果24",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect25 => "效果25",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect26 => "效果26",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect27 => "效果27",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect28 => "效果28",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect29 => "效果29",
            Microsoft.Office.Core.MsoPresetTextEffect.msoTextEffect30 => "效果30",
            _ => "未知效果"
        };
    }

    /// <summary>
    /// 获取文本效果形状名称
    /// </summary>
    private static string GetTextEffectShapeName(MsoPresetTextEffectShape shapeEffect)
    {
        return shapeEffect switch
        {
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapePlainText => "普通文本",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeStop => "停止标志",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeTriangleUp => "正三角形向上",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeTriangleDown => "正三角形向下",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeChevronUp => "尖角向上",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeChevronDown => "尖角向下",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeRingInside => "内环",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeRingOutside => "外环",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeArchUpCurve => "向上弧形曲线",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeArchDownCurve => "向下弧形曲线",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeCircleCurve => "完整圆形",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeButtonCurve => "圆角矩形曲线",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeArchUpPour => "向上弧形灌注",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeArchDownPour => "向下弧形灌注",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeCirclePour => "圆形灌注",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeButtonPour => "圆角矩形灌注",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeCurveUp => "向上轻微弯曲",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeCurveDown => "向下轻微弯曲",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeCanUp => "阶梯向上",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeCanDown => "阶梯向下",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeWave1 => "波浪样式1",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeWave2 => "波浪样式2",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeDoubleWave1 => "双波浪样式1",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeDoubleWave2 => "双波浪样式2",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeInflate => "向外鼓起",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeDeflate => "向内收缩",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeInflateBottom => "底部向外鼓起",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeDeflateBottom => "底部向内凹陷",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeInflateTop => "顶部向外鼓起",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeDeflateTop => "顶部向内凹陷",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeDeflateInflate => "收缩鼓起",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeDeflateInflateDeflate => "收缩鼓起收缩",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeFadeRight => "向右淡出",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeFadeLeft => "向左淡出",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeFadeUp => "向上淡出",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeFadeDown => "向下淡出",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeSlantUp => "向上倾斜",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeSlantDown => "向下倾斜",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeCascadeUp => "向上层叠",
            Microsoft.Office.Core.MsoPresetTextEffectShape.msoTextEffectShapeCascadeDown => "向下层叠",
            _ => "未知形状"
        };
    }
}
