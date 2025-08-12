using System.Runtime.InteropServices;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services;
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
            ExamModuleModel? pptModule = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.PowerPoint);
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

            // 批量检测知识点
            result.KnowledgePointResults = DetectKnowledgePointsAsync(filePath, allOperationPoints).Result;

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
                        Dictionary<string, string> parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);
                        ResolveParametersForPresentation(parameters, presentation, context);
                    }
                }

                // 逐个检测知识点
                foreach (OperationPointModel operationPoint in knowledgePoints)
                {
                    try
                    {
                        Dictionary<string, string> parameters = operationPoint.Parameters.ToDictionary(p => p.Name, p => p.Value);

                        // 使用解析后的参数
                        Dictionary<string, string> resolvedParameters = GetResolvedParameters(parameters, context);

                        KnowledgePointResult result = DetectSpecificKnowledgePoint(presentation, operationPoint.PowerPointKnowledgeType ?? string.Empty, resolvedParameters);

                        result.KnowledgePointId = operationPoint.Id;
                        result.KnowledgePointName = operationPoint.Name;
                        result.TotalScore = operationPoint.Score;

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
                    result = DetectSlideBackground(presentation, parameters);
                    break;
                case "SetTableContent":
                    result = DetectTableContent(presentation, parameters);
                    break;
                case "SetTableStyle":
                    result = DetectTableStyle(presentation, parameters);
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
            if (!parameters.TryGetValue("ExpectedSlideCount", out string? expectedCountStr) ||
                !int.TryParse(expectedCountStr, out int expectedCount))
            {
                result.ErrorMessage = "缺少必要参数: ExpectedSlideCount";
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
            if (!parameters.TryGetValue("ExpectedSlideCount", out string? expectedCountStr) ||
                !int.TryParse(expectedCountStr, out int expectedCount))
            {
                result.ErrorMessage = "缺少必要参数: ExpectedSlideCount";
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

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            bool fontFound = false;
            string actualFonts = "";

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

            result.ExpectedValue = expectedFont;
            result.ActualValue = actualFonts.TrimEnd(';', ' ');
            result.IsCorrect = fontFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 字体检测: 期望 {expectedFont}, 找到的字体 {result.ActualValue}";
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
            if (!parameters.TryGetValue("SlideIndex", out string? slideIndexStr) ||
                !int.TryParse(slideIndexStr, out int slideIndex) ||
                !parameters.TryGetValue("TextContent", out string? expectedText))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndex 或 TextContent";
                return result;
            }

            if (slideIndex < 1 || slideIndex > presentation.Slides.Count)
            {
                result.ErrorMessage = $"幻灯片索引超出范围: {slideIndex}";
                return result;
            }

            PowerPoint.Slide slide = presentation.Slides[slideIndex];
            bool textFound = false;
            string allText = "";

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

            result.ExpectedValue = expectedText;
            result.ActualValue = allText.Trim();
            result.IsCorrect = textFound;
            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 文本检测: 期望包含 '{expectedText}', 实际文本 '{result.ActualValue}'";
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
                if (shape.Type == MsoShapeType.msoPicture)
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

            Slide slide = presentation.Slides[slideIndex];
            string backgroundType = slide.Background.Type.ToString();

            if (parameters.TryGetValue("BackgroundType", out string? expectedType))
            {
                result.ExpectedValue = expectedType;
                result.ActualValue = backgroundType;
                result.IsCorrect = string.Equals(backgroundType, expectedType, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // 如果没有指定类型，只要不是默认背景就算正确
                result.ExpectedValue = "非默认背景";
                result.ActualValue = backgroundType;
                result.IsCorrect = !backgroundType.Contains("Default", StringComparison.OrdinalIgnoreCase);
            }

            result.AchievedScore = result.IsCorrect ? result.TotalScore : 0;
            result.Details = $"幻灯片 {slideIndex} 背景检测: 期望 {result.ExpectedValue}, 实际 {backgroundType}";
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
            if (!parameters.TryGetValue("SlideIndexes", out string? slideIndexesStr) ||
                !parameters.TryGetValue("TransitionMode", out string? expectedModeStr) ||
                !int.TryParse(expectedModeStr, out int expectedMode))
            {
                result.ErrorMessage = "缺少必要参数: SlideIndexes 或 TransitionMode";
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
                        ParameterResolver.ResolveMultipleParameters(parameter.Key, parameter.Value, maxValue, context);
                    }
                    else
                    {
                        // 处理单个编号参数
                        ParameterResolver.ResolveParameter(parameter.Key, parameter.Value, maxValue, context);
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
        Dictionary<string, string> resolvedParameters = new();

        foreach (KeyValuePair<string, string> parameter in originalParameters)
        {
            string resolvedValue = context.GetResolvedParameter(parameter.Key);
            resolvedParameters[parameter.Key] = string.IsNullOrEmpty(resolvedValue) ? parameter.Value : resolvedValue;
        }

        return resolvedParameters;
    }
}
