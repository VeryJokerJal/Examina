using BenchSuite.Models;
using BenchSuite.Services;
using System;
using System.Linq;

namespace BenchSuite.Examples;

/// <summary>
/// 验证示例 - 确保所有功能正常工作
/// </summary>
public class ValidationExample
{
    public static void RunValidation()
    {
        Console.WriteLine("=== BenchSuite 功能验证 ===\n");

        try
        {
            // 验证服务可用性
            ValidateServiceAvailability();

            // 验证默认值设置
            ValidateDefaultValues();

            // 验证颜色参数
            ValidateColorParameters();

            // 验证数字参数
            ValidateNumberParameters();

            // 验证枚举参数
            ValidateEnumParameters();

            // 验证文本参数
            ValidateTextParameters();

            // 验证与ExamLab的一致性
            ValidateConsistencyWithExamLab();

            Console.WriteLine("✅ 所有验证通过！功能正常工作。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 验证失败：{ex.Message}");
            Console.WriteLine($"详细信息：{ex.StackTrace}");
        }

        Console.WriteLine("\n=== 验证完成 ===");
    }

    private static void ValidateServiceAvailability()
    {
        Console.WriteLine("1. 验证服务可用性...");

        WordKnowledgeService service = WordKnowledgeService.Instance;
        if (service == null)
        {
            throw new Exception("WordKnowledgeService 实例为空");
        }

        var configs = service.GetAllOperationConfigs();
        if (configs.Count == 0)
        {
            throw new Exception("没有找到任何操作点配置");
        }

        Console.WriteLine($"   ✓ 服务可用，共有 {configs.Count} 个操作点配置");
    }

    private static void ValidateDefaultValues()
    {
        Console.WriteLine("2. 验证默认值设置...");

        WordKnowledgeService service = WordKnowledgeService.Instance;

        // 测试段落字体操作点
        OperationPointModel fontOp = service.CreateOperationPoint("SetParagraphFont");
        if (fontOp.Parameters.Count == 0)
        {
            throw new Exception("操作点参数为空");
        }

        // 验证所有参数都有默认值
        foreach (var param in fontOp.Parameters)
        {
            if (string.IsNullOrEmpty(param.DefaultValue))
            {
                throw new Exception($"参数 {param.Name} 缺少默认值");
            }
        }

        Console.WriteLine("   ✓ 所有参数都有默认值");
    }

    private static void ValidateColorParameters()
    {
        Console.WriteLine("3. 验证颜色参数...");

        WordKnowledgeService service = WordKnowledgeService.Instance;
        OperationPointModel colorOp = service.CreateOperationPoint("SetParagraphTextColor");

        var colorParam = colorOp.Parameters.FirstOrDefault(p => p.Name == "TextColor");
        if (colorParam == null)
        {
            throw new Exception("找不到颜色参数");
        }

        if (colorParam.Type != ParameterType.Color)
        {
            throw new Exception($"颜色参数类型错误，期望 Color，实际 {colorParam.Type}");
        }

        if (colorParam.DefaultValue != "#000000")
        {
            throw new Exception($"颜色参数默认值错误，期望 #000000，实际 {colorParam.DefaultValue}");
        }

        Console.WriteLine("   ✓ 颜色参数类型和默认值正确");
    }

    private static void ValidateNumberParameters()
    {
        Console.WriteLine("4. 验证数字参数...");

        WordKnowledgeService service = WordKnowledgeService.Instance;
        OperationPointModel fontSizeOp = service.CreateOperationPoint("SetParagraphFontSize");

        var fontSizeParam = fontSizeOp.Parameters.FirstOrDefault(p => p.Name == "FontSize");
        if (fontSizeParam == null)
        {
            throw new Exception("找不到字号参数");
        }

        if (fontSizeParam.Type != ParameterType.Number)
        {
            throw new Exception($"字号参数类型错误，期望 Number，实际 {fontSizeParam.Type}");
        }

        if (fontSizeParam.DefaultValue != "12")
        {
            throw new Exception($"字号参数默认值错误，期望 12，实际 {fontSizeParam.DefaultValue}");
        }

        var paragraphParam = fontSizeOp.Parameters.FirstOrDefault(p => p.Name == "ParagraphNumber");
        if (paragraphParam?.DefaultValue != "-1")
        {
            throw new Exception($"段落序号默认值错误，期望 -1，实际 {paragraphParam?.DefaultValue}");
        }

        Console.WriteLine("   ✓ 数字参数类型和默认值正确");
    }

    private static void ValidateEnumParameters()
    {
        Console.WriteLine("5. 验证枚举参数...");

        WordKnowledgeService service = WordKnowledgeService.Instance;
        OperationPointModel fontOp = service.CreateOperationPoint("SetParagraphFont");

        var fontFamilyParam = fontOp.Parameters.FirstOrDefault(p => p.Name == "FontFamily");
        if (fontFamilyParam == null)
        {
            throw new Exception("找不到字体类型参数");
        }

        if (fontFamilyParam.Type != ParameterType.Enum)
        {
            throw new Exception($"字体类型参数类型错误，期望 Enum，实际 {fontFamilyParam.Type}");
        }

        if (fontFamilyParam.DefaultValue != "宋体")
        {
            throw new Exception($"字体类型默认值错误，期望 宋体，实际 {fontFamilyParam.DefaultValue}");
        }

        Console.WriteLine("   ✓ 枚举参数类型和默认值正确");
    }

    private static void ValidateTextParameters()
    {
        Console.WriteLine("6. 验证文本参数...");

        WordKnowledgeService service = WordKnowledgeService.Instance;
        OperationPointModel watermarkOp = service.CreateOperationPoint("SetWatermarkText");

        var textParam = watermarkOp.Parameters.FirstOrDefault(p => p.Name == "WatermarkText");
        if (textParam == null)
        {
            throw new Exception("找不到水印文字参数");
        }

        if (textParam.Type != ParameterType.Text)
        {
            throw new Exception($"水印文字参数类型错误，期望 Text，实际 {textParam.Type}");
        }

        if (textParam.DefaultValue != "机密")
        {
            throw new Exception($"水印文字默认值错误，期望 机密，实际 {textParam.DefaultValue}");
        }

        Console.WriteLine("   ✓ 文本参数类型和默认值正确");
    }

    private static void ValidateConsistencyWithExamLab()
    {
        Console.WriteLine("7. 验证与ExamLab项目的一致性...");

        WordKnowledgeService service = WordKnowledgeService.Instance;

        // 验证操作点名称一致性
        var configs = service.GetAllOperationConfigs();
        string[] expectedOperations = [
            "SetParagraphFont", "SetParagraphFontSize", "SetParagraphTextColor",
            "SetPaperSize", "SetPageMargins", "SetWatermarkText",
            "SetTableRowsColumns", "InsertAutoShape", "SetTextBoxBorderColor",
            "FindAndReplace"
        ];

        foreach (string operation in expectedOperations)
        {
            if (!configs.ContainsKey(operation))
            {
                throw new Exception($"缺少操作点配置：{operation}");
            }
        }

        // 验证参数类型一致性
        var colorOp = service.CreateOperationPoint("SetParagraphTextColor");
        var colorParam = colorOp.Parameters.FirstOrDefault(p => p.Name == "TextColor");
        if (colorParam?.Type != ParameterType.Color)
        {
            throw new Exception("颜色参数类型与ExamLab不一致");
        }

        // 验证默认值一致性
        var fontOp = service.CreateOperationPoint("SetParagraphFont");
        var paragraphParam = fontOp.Parameters.FirstOrDefault(p => p.Name == "ParagraphNumber");
        if (paragraphParam?.DefaultValue != "-1")
        {
            throw new Exception("段落序号默认值与ExamLab不一致");
        }

        Console.WriteLine("   ✓ 与ExamLab项目保持一致");
    }
}
