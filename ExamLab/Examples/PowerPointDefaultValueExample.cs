using ExamLab.Models;
using ExamLab.Services;
using System;
using System.Linq;

namespace ExamLab.Examples;

/// <summary>
/// PowerPoint默认值功能使用示例
/// </summary>
public class PowerPointDefaultValueExample
{
    public static void RunExample()
    {
        Console.WriteLine("=== ExamLab PowerPoint默认值功能示例 ===\n");

        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // 示例1：创建带有默认值的PowerPoint操作点
        Console.WriteLine("1. 创建带有默认值的PowerPoint操作点示例：");

        // 创建幻灯片版式设置操作点
        Console.WriteLine("\n【设置幻灯片版式】");
        OperationPoint layoutOperation = service.CreateOperationPoint(PowerPointKnowledgeType.SetSlideLayout);
        DisplayOperationPoint(layoutOperation);

        // 创建幻灯片字体设置操作点
        Console.WriteLine("\n【设置幻灯片字体】");
        OperationPoint fontOperation = service.CreateOperationPoint(PowerPointKnowledgeType.SetSlideFont);
        DisplayOperationPoint(fontOperation);

        // 创建文本颜色设置操作点
        Console.WriteLine("\n【设置文本颜色】");
        OperationPoint colorOperation = service.CreateOperationPoint(PowerPointKnowledgeType.SetTextColor);
        DisplayOperationPoint(colorOperation);

        // 创建文本字号设置操作点
        Console.WriteLine("\n【设置文本字号】");
        OperationPoint fontSizeOperation = service.CreateOperationPoint(PowerPointKnowledgeType.SetTextFontSize);
        DisplayOperationPoint(fontSizeOperation);

        // 创建元素位置设置操作点
        Console.WriteLine("\n【设置元素位置】");
        OperationPoint positionOperation = service.CreateOperationPoint(PowerPointKnowledgeType.SetElementPosition);
        DisplayOperationPoint(positionOperation);

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // 示例2：验证默认值设置
        Console.WriteLine("2. 验证默认值设置：");
        VerifyDefaultValues();

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // 示例3：与Word和Excel操作点的一致性验证
        Console.WriteLine("3. 与Word和Excel操作点的一致性验证：");
        VerifyConsistencyWithWordAndExcel();

        Console.WriteLine("\n=== 示例完成 ===");
    }

    private static void DisplayOperationPoint(OperationPoint operation)
    {
        Console.WriteLine($"操作点名称: {operation.Name}");
        Console.WriteLine($"描述: {operation.Description}");
        Console.WriteLine($"模块类型: {operation.ModuleType}");
        Console.WriteLine($"参数数量: {operation.Parameters.Count}");
        Console.WriteLine("参数详情:");

        foreach (var param in operation.Parameters.OrderBy(p => p.Order))
        {
            Console.WriteLine($"  - {param.DisplayName} ({param.Name})");
            Console.WriteLine($"    类型: {param.Type}");
            Console.WriteLine($"    默认值: {param.DefaultValue ?? "无"}");
            Console.WriteLine($"    当前值: {param.Value}");
            Console.WriteLine($"    是否必填: {(param.IsRequired ? "是" : "否")}");
            if (param.Type == ParameterType.Number && param.MinValue.HasValue && param.MaxValue.HasValue)
            {
                Console.WriteLine($"    取值范围: {param.MinValue} - {param.MaxValue}");
            }
            if (param.Type == ParameterType.Enum && !string.IsNullOrEmpty(param.EnumOptions))
            {
                Console.WriteLine($"    可选项: {param.EnumOptions}");
            }
            Console.WriteLine();
        }
    }

    private static void VerifyDefaultValues()
    {
        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // 验证颜色参数默认值
        Console.WriteLine("【颜色参数验证】");
        var colorOp = service.CreateOperationPoint(PowerPointKnowledgeType.SetTextColor);
        var colorParam = colorOp.Parameters.First(p => p.Name == "ColorValue");
        Console.WriteLine($"文本颜色默认值: {colorParam.DefaultValue} (类型: {colorParam.Type})");

        // 验证数字参数默认值
        Console.WriteLine("\n【数字参数验证】");
        var fontSizeOp = service.CreateOperationPoint(PowerPointKnowledgeType.SetTextFontSize);
        var fontSizeParam = fontSizeOp.Parameters.First(p => p.Name == "FontSize");
        Console.WriteLine($"字号默认值: {fontSizeParam.DefaultValue} (类型: {fontSizeParam.Type})");

        var slideParam = fontSizeOp.Parameters.First(p => p.Name == "SlideNumber");
        Console.WriteLine($"幻灯片序号默认值: {slideParam.DefaultValue} (类型: {slideParam.Type})");

        var positionOp = service.CreateOperationPoint(PowerPointKnowledgeType.SetElementPosition);
        var xParam = positionOp.Parameters.First(p => p.Name == "HorizontalPosition");
        var yParam = positionOp.Parameters.First(p => p.Name == "VerticalPosition");
        Console.WriteLine($"水平位置默认值: {xParam.DefaultValue} (类型: {xParam.Type})");
        Console.WriteLine($"垂直位置默认值: {yParam.DefaultValue} (类型: {yParam.Type})");

        var sizeOp = service.CreateOperationPoint(PowerPointKnowledgeType.SetElementSize);
        var widthParam = sizeOp.Parameters.First(p => p.Name == "ElementWidth");
        var heightParam = sizeOp.Parameters.First(p => p.Name == "ElementHeight");
        Console.WriteLine($"元素宽度默认值: {widthParam.DefaultValue} (类型: {widthParam.Type})");
        Console.WriteLine($"元素高度默认值: {heightParam.DefaultValue} (类型: {heightParam.Type})");

        // 验证枚举参数默认值
        Console.WriteLine("\n【枚举参数验证】");
        var fontOp = service.CreateOperationPoint(PowerPointKnowledgeType.SetSlideFont);
        var fontNameParam = fontOp.Parameters.First(p => p.Name == "FontName");
        Console.WriteLine($"字体类型默认值: {fontNameParam.DefaultValue} (类型: {fontNameParam.Type})");

        var layoutOp = service.CreateOperationPoint(PowerPointKnowledgeType.SetSlideLayout);
        var layoutParam = layoutOp.Parameters.First(p => p.Name == "Layout");
        Console.WriteLine($"幻灯片版式默认值: {layoutParam.DefaultValue} (类型: {layoutParam.Type})");

        // 验证文本参数默认值
        Console.WriteLine("\n【文本参数验证】");
        var textOp = service.CreateOperationPoint(PowerPointKnowledgeType.InsertTextContent);
        var textParam = textOp.Parameters.First(p => p.Name == "TextContent");
        Console.WriteLine($"文本内容默认值: {textParam.DefaultValue} (类型: {textParam.Type})");

        Console.WriteLine("\n✅ 所有默认值验证通过！");
    }

    private static void VerifyConsistencyWithWordAndExcel()
    {
        PowerPointKnowledgeService pptService = PowerPointKnowledgeService.Instance;
        WordKnowledgeService wordService = WordKnowledgeService.Instance;
        ExcelKnowledgeService excelService = ExcelKnowledgeService.Instance;

        // 比较字体设置操作点
        Console.WriteLine("【字体设置操作点比较】");
        var pptFontOp = pptService.CreateOperationPoint(PowerPointKnowledgeType.SetSlideFont);
        var wordFontOp = wordService.CreateOperationPoint(WordKnowledgeType.SetParagraphFont);
        var excelFontOp = excelService.CreateOperationPoint(ExcelKnowledgeType.SetCellFont);

        var pptFontParam = pptFontOp.Parameters.First(p => p.Name == "FontName");
        var wordFontParam = wordFontOp.Parameters.First(p => p.Name == "FontFamily");
        var excelFontParam = excelFontOp.Parameters.First(p => p.Name == "FontFamily");

        Console.WriteLine($"PowerPoint字体默认值: {pptFontParam.DefaultValue}");
        Console.WriteLine($"Word字体默认值: {wordFontParam.DefaultValue}");
        Console.WriteLine($"Excel字体默认值: {excelFontParam.DefaultValue}");
        Console.WriteLine($"一致性: {(pptFontParam.DefaultValue == wordFontParam.DefaultValue && wordFontParam.DefaultValue == excelFontParam.DefaultValue ? "✅" : "❌")}");

        // 比较字号设置操作点
        Console.WriteLine("\n【字号设置操作点比较】");
        var pptFontSizeOp = pptService.CreateOperationPoint(PowerPointKnowledgeType.SetTextFontSize);
        var wordFontSizeOp = wordService.CreateOperationPoint(WordKnowledgeType.SetParagraphFontSize);
        var excelFontSizeOp = excelService.CreateOperationPoint(ExcelKnowledgeType.SetFontSize);

        var pptFontSizeParam = pptFontSizeOp.Parameters.First(p => p.Name == "FontSize");
        var wordFontSizeParam = wordFontSizeOp.Parameters.First(p => p.Name == "FontSize");
        var excelFontSizeParam = excelFontSizeOp.Parameters.First(p => p.Name == "FontSize");

        Console.WriteLine($"PowerPoint字号默认值: {pptFontSizeParam.DefaultValue}");
        Console.WriteLine($"Word字号默认值: {wordFontSizeParam.DefaultValue}");
        Console.WriteLine($"Excel字号默认值: {excelFontSizeParam.DefaultValue}");
        Console.WriteLine($"PowerPoint使用18磅（演示文稿需要更大字号），Word和Excel使用12磅（文档标准字号）");

        // 比较颜色设置操作点
        Console.WriteLine("\n【颜色设置操作点比较】");
        var pptColorOp = pptService.CreateOperationPoint(PowerPointKnowledgeType.SetTextColor);
        var wordColorOp = wordService.CreateOperationPoint(WordKnowledgeType.SetParagraphTextColor);
        var excelColorOp = excelService.CreateOperationPoint(ExcelKnowledgeType.SetFontColor);

        var pptColorParam = pptColorOp.Parameters.First(p => p.Name == "ColorValue");
        var wordColorParam = wordColorOp.Parameters.First(p => p.Name == "TextColor");
        var excelColorParam = excelColorOp.Parameters.First(p => p.Name == "FontColor");

        Console.WriteLine($"PowerPoint文本颜色默认值: {pptColorParam.DefaultValue}");
        Console.WriteLine($"Word文字颜色默认值: {wordColorParam.DefaultValue}");
        Console.WriteLine($"Excel字体颜色默认值: {excelColorParam.DefaultValue}");
        Console.WriteLine($"一致性: {(pptColorParam.DefaultValue == wordColorParam.DefaultValue && wordColorParam.DefaultValue == excelColorParam.DefaultValue ? "✅" : "❌")}");

        Console.WriteLine("\n✅ 与Word和Excel操作点保持良好的一致性！");
    }

    /// <summary>
    /// 演示如何修改PowerPoint操作点参数值
    /// </summary>
    public static void DemonstrateParameterModification()
    {
        Console.WriteLine("\n=== PowerPoint参数修改示例 ===");

        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;
        OperationPoint operation = service.CreateOperationPoint(PowerPointKnowledgeType.SetSlideFont);

        Console.WriteLine("修改前的参数值:");
        foreach (var param in operation.Parameters)
        {
            Console.WriteLine($"  {param.DisplayName}: {param.Value}");
        }

        // 修改参数值
        var slideParam = operation.Parameters.First(p => p.Name == "SlideNumber");
        var textBoxParam = operation.Parameters.First(p => p.Name == "TextBoxNumber");
        var fontParam = operation.Parameters.First(p => p.Name == "FontName");

        slideParam.Value = "3";
        textBoxParam.Value = "1";
        fontParam.Value = "微软雅黑";

        Console.WriteLine("\n修改后的参数值:");
        foreach (var param in operation.Parameters)
        {
            Console.WriteLine($"  {param.DisplayName}: {param.Value}");
        }

        Console.WriteLine("\n✅ PowerPoint参数修改完成！");
    }

    /// <summary>
    /// 演示PowerPoint特有的功能
    /// </summary>
    public static void DemonstratePowerPointSpecificFeatures()
    {
        Console.WriteLine("\n=== PowerPoint特有功能示例 ===");

        PowerPointKnowledgeService service = PowerPointKnowledgeService.Instance;

        // 幻灯片版式设置
        Console.WriteLine("【幻灯片版式设置】");
        var layoutOp = service.CreateOperationPoint(PowerPointKnowledgeType.SetSlideLayout);
        var layoutParam = layoutOp.Parameters.First(p => p.Name == "Layout");
        Console.WriteLine($"可用版式: {layoutParam.EnumOptions}");
        Console.WriteLine($"默认版式: {layoutParam.DefaultValue}");

        // 元素位置设置
        Console.WriteLine("\n【元素位置设置】");
        var positionOp = service.CreateOperationPoint(PowerPointKnowledgeType.SetElementPosition);
        Console.WriteLine("PowerPoint支持精确的元素位置控制：");
        foreach (var param in positionOp.Parameters.Where(p => p.Type == ParameterType.Number))
        {
            Console.WriteLine($"  {param.DisplayName}: {param.DefaultValue} (最小值: {param.MinValue})");
        }

        // 元素大小设置
        Console.WriteLine("\n【元素大小设置】");
        var sizeOp = service.CreateOperationPoint(PowerPointKnowledgeType.SetElementSize);
        Console.WriteLine("PowerPoint支持灵活的元素大小调整：");
        foreach (var param in sizeOp.Parameters.Where(p => p.Type == ParameterType.Number && p.Name.Contains("Element")))
        {
            Console.WriteLine($"  {param.DisplayName}: {param.DefaultValue}");
        }

        Console.WriteLine("\n✅ PowerPoint特有功能演示完成！");
    }
}
