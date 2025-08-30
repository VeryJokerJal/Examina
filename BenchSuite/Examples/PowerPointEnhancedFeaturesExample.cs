using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services.OpenXml;

namespace BenchSuite.Examples;

/// <summary>
/// PowerPoint OpenXML增强功能使用示例
/// </summary>
public class PowerPointEnhancedFeaturesExample
{
    /// <summary>
    /// 演示如何使用新实现的核心检测功能
    /// </summary>
    public static async Task DemonstrateEnhancedFeaturesAsync()
    {
        Console.WriteLine("=== PowerPoint OpenXML 增强功能演示 ===");
        Console.WriteLine();

        // 创建PowerPoint评分服务实例
        IPowerPointScoringService service = new PowerPointOpenXmlScoringService();
        
        // 示例文件路径（实际使用时请替换为真实文件）
        string sampleFilePath = "Examples/sample_presentation.pptx";
        
        Console.WriteLine($"使用示例文件: {sampleFilePath}");
        Console.WriteLine();

        // 1. 文本样式检测示例
        await DemonstrateTextStyleDetectionAsync(service, sampleFilePath);
        
        // 2. 元素位置检测示例
        await DemonstrateElementPositionDetectionAsync(service, sampleFilePath);
        
        // 3. 元素大小检测示例
        await DemonstrateElementSizeDetectionAsync(service, sampleFilePath);
        
        // 4. 文本对齐检测示例
        await DemonstrateTextAlignmentDetectionAsync(service, sampleFilePath);
        
        // 5. 超链接检测示例
        await DemonstrateHyperlinkDetectionAsync(service, sampleFilePath);
        
        // 6. 幻灯片编号检测示例
        await DemonstrateSlideNumberDetectionAsync(service, sampleFilePath);
        
        // 7. 页脚文本检测示例
        await DemonstrateFooterTextDetectionAsync(service, sampleFilePath);

        Console.WriteLine("=== 演示完成 ===");
    }

    /// <summary>
    /// 演示文本样式检测
    /// </summary>
    private static async Task DemonstrateTextStyleDetectionAsync(IPowerPointScoringService service, string filePath)
    {
        Console.WriteLine("1. 文本样式检测演示");
        Console.WriteLine("-------------------");

        try
        {
            // 检测粗体文本
            Dictionary<string, string> boldParameters = new()
            {
                { "SlideIndex", "1" },
                { "StyleType", "Bold" }
            };

            KnowledgePointResult boldResult = await service.DetectKnowledgePointAsync(filePath, "SetTextStyle", boldParameters);
            Console.WriteLine($"粗体检测: {(boldResult.IsCorrect ? "✅ 找到" : "❌ 未找到")}");
            Console.WriteLine($"详情: {boldResult.Details}");

            // 检测斜体文本
            Dictionary<string, string> italicParameters = new()
            {
                { "SlideIndex", "1" },
                { "StyleType", "Italic" }
            };

            KnowledgePointResult italicResult = await service.DetectKnowledgePointAsync(filePath, "SetTextStyle", italicParameters);
            Console.WriteLine($"斜体检测: {(italicResult.IsCorrect ? "✅ 找到" : "❌ 未找到")}");
            Console.WriteLine($"详情: {italicResult.Details}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 文本样式检测异常: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 演示元素位置检测
    /// </summary>
    private static async Task DemonstrateElementPositionDetectionAsync(IPowerPointScoringService service, string filePath)
    {
        Console.WriteLine("2. 元素位置检测演示");
        Console.WriteLine("-------------------");

        try
        {
            Dictionary<string, string> parameters = new()
            {
                { "SlideIndex", "1" },
                { "ElementType", "Shape" },
                { "ExpectedX", "914400" },  // PowerPoint单位
                { "ExpectedY", "685800" }
            };

            KnowledgePointResult result = await service.DetectKnowledgePointAsync(filePath, "SetElementPosition", parameters);
            Console.WriteLine($"位置检测: {(result.IsCorrect ? "✅ 位置正确" : "❌ 位置不符")}");
            Console.WriteLine($"期望值: {result.ExpectedValue}");
            Console.WriteLine($"实际值: {result.ActualValue}");
            Console.WriteLine($"详情: {result.Details}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 元素位置检测异常: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 演示元素大小检测
    /// </summary>
    private static async Task DemonstrateElementSizeDetectionAsync(IPowerPointScoringService service, string filePath)
    {
        Console.WriteLine("3. 元素大小检测演示");
        Console.WriteLine("-------------------");

        try
        {
            Dictionary<string, string> parameters = new()
            {
                { "SlideIndex", "1" },
                { "ElementType", "Shape" },
                { "ExpectedWidth", "2743200" },   // PowerPoint单位
                { "ExpectedHeight", "914400" }
            };

            KnowledgePointResult result = await service.DetectKnowledgePointAsync(filePath, "SetElementSize", parameters);
            Console.WriteLine($"大小检测: {(result.IsCorrect ? "✅ 大小正确" : "❌ 大小不符")}");
            Console.WriteLine($"期望值: {result.ExpectedValue}");
            Console.WriteLine($"实际值: {result.ActualValue}");
            Console.WriteLine($"详情: {result.Details}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 元素大小检测异常: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 演示文本对齐检测
    /// </summary>
    private static async Task DemonstrateTextAlignmentDetectionAsync(IPowerPointScoringService service, string filePath)
    {
        Console.WriteLine("4. 文本对齐检测演示");
        Console.WriteLine("-------------------");

        try
        {
            Dictionary<string, string> parameters = new()
            {
                { "SlideIndex", "1" },
                { "Alignment", "Center" }
            };

            KnowledgePointResult result = await service.DetectKnowledgePointAsync(filePath, "SetTextAlignment", parameters);
            Console.WriteLine($"对齐检测: {(result.IsCorrect ? "✅ 对齐正确" : "❌ 对齐不符")}");
            Console.WriteLine($"期望值: {result.ExpectedValue}");
            Console.WriteLine($"实际值: {result.ActualValue}");
            Console.WriteLine($"详情: {result.Details}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 文本对齐检测异常: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 演示超链接检测
    /// </summary>
    private static async Task DemonstrateHyperlinkDetectionAsync(IPowerPointScoringService service, string filePath)
    {
        Console.WriteLine("5. 超链接检测演示");
        Console.WriteLine("-----------------");

        try
        {
            Dictionary<string, string> parameters = new()
            {
                { "SlideIndex", "1" },
                { "ExpectedUrl", "https://www.microsoft.com" }
            };

            KnowledgePointResult result = await service.DetectKnowledgePointAsync(filePath, "InsertHyperlink", parameters);
            Console.WriteLine($"超链接检测: {(result.IsCorrect ? "✅ 找到超链接" : "❌ 未找到超链接")}");
            Console.WriteLine($"期望值: {result.ExpectedValue}");
            Console.WriteLine($"实际值: {result.ActualValue}");
            Console.WriteLine($"详情: {result.Details}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 超链接检测异常: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 演示幻灯片编号检测
    /// </summary>
    private static async Task DemonstrateSlideNumberDetectionAsync(IPowerPointScoringService service, string filePath)
    {
        Console.WriteLine("6. 幻灯片编号检测演示");
        Console.WriteLine("---------------------");

        try
        {
            Dictionary<string, string> parameters = new()
            {
                { "SlideIndex", "1" }
            };

            KnowledgePointResult result = await service.DetectKnowledgePointAsync(filePath, "SetSlideNumber", parameters);
            Console.WriteLine($"编号检测: {(result.IsCorrect ? "✅ 显示编号" : "❌ 未显示编号")}");
            Console.WriteLine($"期望值: {result.ExpectedValue}");
            Console.WriteLine($"实际值: {result.ActualValue}");
            Console.WriteLine($"详情: {result.Details}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 幻灯片编号检测异常: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 演示页脚文本检测
    /// </summary>
    private static async Task DemonstrateFooterTextDetectionAsync(IPowerPointScoringService service, string filePath)
    {
        Console.WriteLine("7. 页脚文本检测演示");
        Console.WriteLine("-------------------");

        try
        {
            Dictionary<string, string> parameters = new()
            {
                { "SlideIndex", "1" },
                { "ExpectedText", "公司机密" }
            };

            KnowledgePointResult result = await service.DetectKnowledgePointAsync(filePath, "SetFooterText", parameters);
            Console.WriteLine($"页脚检测: {(result.IsCorrect ? "✅ 找到页脚" : "❌ 未找到页脚")}");
            Console.WriteLine($"期望值: {result.ExpectedValue}");
            Console.WriteLine($"实际值: {result.ActualValue}");
            Console.WriteLine($"详情: {result.Details}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 页脚文本检测异常: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 创建完整的评分示例
    /// </summary>
    public static async Task DemonstrateCompleteScoring()
    {
        Console.WriteLine("=== 完整评分示例 ===");
        Console.WriteLine();

        try
        {
            IPowerPointScoringService service = new PowerPointOpenXmlScoringService();
            string filePath = "Examples/sample_presentation.pptx";

            // 创建包含增强功能的试卷模型
            ExamModel examModel = CreateEnhancedExamModel();

            // 执行完整评分
            ScoringResult result = await service.ScoreFileAsync(filePath, examModel);

            Console.WriteLine($"评分结果: {(result.IsSuccess ? "✅ 成功" : "❌ 失败")}");
            Console.WriteLine($"总分: {result.TotalScore}");
            Console.WriteLine($"获得分: {result.AchievedScore}");
            Console.WriteLine($"正确率: {(result.TotalScore > 0 ? (double)result.AchievedScore / result.TotalScore * 100 : 0):F1}%");
            Console.WriteLine();

            Console.WriteLine("知识点检测详情:");
            foreach (var kpResult in result.KnowledgePointResults)
            {
                Console.WriteLine($"- {kpResult.KnowledgePointName}: {(kpResult.IsCorrect ? "✅" : "❌")} ({kpResult.AchievedScore}/{kpResult.TotalScore}分)");
                if (!string.IsNullOrEmpty(kpResult.Details))
                {
                    Console.WriteLine($"  详情: {kpResult.Details}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 完整评分异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 创建包含增强功能的试卷模型
    /// </summary>
    private static ExamModel CreateEnhancedExamModel()
    {
        return new ExamModel
        {
            Id = "enhanced-ppt-exam",
            Title = "PowerPoint增强功能测试试卷",
            Modules =
            [
                new ExamModuleModel
                {
                    Type = ModuleType.PowerPoint,
                    Questions =
                    [
                        new QuestionModel
                        {
                            Id = "enhanced-q1",
                            Title = "PowerPoint增强功能测试",
                            OperationPoints =
                            [
                                new OperationPointModel
                                {
                                    Id = "op-text-style",
                                    Name = "SetTextStyle",
                                    Score = 10,
                                    ModuleType = ModuleType.PowerPoint,
                                    IsEnabled = true,
                                    Parameters =
                                    [
                                        new ParameterModel { Name = "SlideIndex", Value = "1" },
                                        new ParameterModel { Name = "StyleType", Value = "Bold" }
                                    ]
                                },
                                new OperationPointModel
                                {
                                    Id = "op-element-position",
                                    Name = "SetElementPosition",
                                    Score = 15,
                                    ModuleType = ModuleType.PowerPoint,
                                    IsEnabled = true,
                                    Parameters =
                                    [
                                        new ParameterModel { Name = "SlideIndex", Value = "1" },
                                        new ParameterModel { Name = "ElementType", Value = "Shape" }
                                    ]
                                },
                                new OperationPointModel
                                {
                                    Id = "op-text-alignment",
                                    Name = "SetTextAlignment",
                                    Score = 10,
                                    ModuleType = ModuleType.PowerPoint,
                                    IsEnabled = true,
                                    Parameters =
                                    [
                                        new ParameterModel { Name = "SlideIndex", Value = "1" },
                                        new ParameterModel { Name = "Alignment", Value = "Center" }
                                    ]
                                },
                                new OperationPointModel
                                {
                                    Id = "op-hyperlink",
                                    Name = "InsertHyperlink",
                                    Score = 15,
                                    ModuleType = ModuleType.PowerPoint,
                                    IsEnabled = true,
                                    Parameters =
                                    [
                                        new ParameterModel { Name = "SlideIndex", Value = "1" }
                                    ]
                                },
                                new OperationPointModel
                                {
                                    Id = "op-slide-number",
                                    Name = "SetSlideNumber",
                                    Score = 10,
                                    ModuleType = ModuleType.PowerPoint,
                                    IsEnabled = true,
                                    Parameters =
                                    [
                                        new ParameterModel { Name = "SlideIndex", Value = "1" }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }
}
