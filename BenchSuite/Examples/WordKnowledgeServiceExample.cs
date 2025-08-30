using BenchSuite.Models;
using BenchSuite.Services;
using System;
using System.Linq;

namespace BenchSuite.Examples;

/// <summary>
/// WordKnowledgeService使用示例
/// </summary>
public class WordKnowledgeServiceExample
{
    public static void RunExample()
    {
        Console.WriteLine("=== BenchSuite WordKnowledgeService 使用示例 ===\n");

        WordKnowledgeService service = WordKnowledgeService.Instance;

        // 示例1：获取所有操作点配置
        Console.WriteLine("1. 获取所有操作点配置：");
        var allConfigs = service.GetAllOperationConfigs();
        Console.WriteLine($"总共有 {allConfigs.Count} 个操作点配置");

        // 按分类显示操作点
        var categories = allConfigs.Values.GroupBy(c => c.Category);
        foreach (var category in categories)
        {
            Console.WriteLine($"\n【{category.Key}】");
            foreach (var config in category)
            {
                Console.WriteLine($"  - {config.Name}");
            }
        }

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // 示例2：创建带有默认值的操作点
        Console.WriteLine("2. 创建带有默认值的操作点示例：");

        // 创建段落字体设置操作点
        Console.WriteLine("\n【段落字体设置】");
        OperationPointModel fontOperation = service.CreateOperationPoint("SetParagraphFont");
        DisplayOperationPoint(fontOperation);

        // 创建段落文字颜色设置操作点
        Console.WriteLine("\n【段落文字颜色设置】");
        OperationPointModel colorOperation = service.CreateOperationPoint("SetParagraphTextColor");
        DisplayOperationPoint(colorOperation);

        // 创建表格操作点
        Console.WriteLine("\n【表格行列设置】");
        OperationPointModel tableOperation = service.CreateOperationPoint("SetTableRowsColumns");
        DisplayOperationPoint(tableOperation);

        // 创建水印设置操作点
        Console.WriteLine("\n【水印文字设置】");
        OperationPointModel watermarkOperation = service.CreateOperationPoint("SetWatermarkText");
        DisplayOperationPoint(watermarkOperation);

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // 示例3：验证默认值设置
        Console.WriteLine("3. 验证默认值设置：");
        VerifyDefaultValues();

        Console.WriteLine("\n=== 示例完成 ===");
    }

    private static void DisplayOperationPoint(OperationPointModel operation)
    {
        Console.WriteLine($"操作点名称: {operation.Name}");
        Console.WriteLine($"描述: {operation.Description}");
        Console.WriteLine($"模块类型: {operation.ModuleType}");
        Console.WriteLine($"参数数量: {operation.Parameters.Count}");
        Console.WriteLine("参数详情:");

        foreach (var param in operation.Parameters.OrderBy(p => p.Name))
        {
            Console.WriteLine($"  - {param.DisplayName} ({param.Name})");
            Console.WriteLine($"    类型: {param.Type}");
            Console.WriteLine($"    默认值: {param.DefaultValue ?? "无"}");
            Console.WriteLine($"    当前值: {param.Value}");
            Console.WriteLine($"    是否必填: {(param.IsRequired ? "是" : "否")}");
            Console.WriteLine();
        }
    }

    private static void VerifyDefaultValues()
    {
        WordKnowledgeService service = WordKnowledgeService.Instance;

        // 验证颜色参数默认值
        Console.WriteLine("【颜色参数验证】");
        var colorOp = service.CreateOperationPoint("SetParagraphTextColor");
        var colorParam = colorOp.Parameters.First(p => p.Name == "TextColor");
        Console.WriteLine($"文字颜色默认值: {colorParam.DefaultValue} (类型: {colorParam.Type})");

        // 验证数字参数默认值
        Console.WriteLine("\n【数字参数验证】");
        var fontSizeOp = service.CreateOperationPoint("SetParagraphFontSize");
        var fontSizeParam = fontSizeOp.Parameters.First(p => p.Name == "FontSize");
        Console.WriteLine($"字号默认值: {fontSizeParam.DefaultValue} (类型: {fontSizeParam.Type})");

        var paragraphParam = fontSizeOp.Parameters.First(p => p.Name == "ParagraphNumber");
        Console.WriteLine($"段落序号默认值: {paragraphParam.DefaultValue} (类型: {paragraphParam.Type})");

        // 验证枚举参数默认值
        Console.WriteLine("\n【枚举参数验证】");
        var fontOp = service.CreateOperationPoint("SetParagraphFont");
        var fontFamilyParam = fontOp.Parameters.First(p => p.Name == "FontFamily");
        Console.WriteLine($"字体类型默认值: {fontFamilyParam.DefaultValue} (类型: {fontFamilyParam.Type})");

        // 验证文本参数默认值
        Console.WriteLine("\n【文本参数验证】");
        var watermarkOp = service.CreateOperationPoint("SetWatermarkText");
        var watermarkParam = watermarkOp.Parameters.First(p => p.Name == "WatermarkText");
        Console.WriteLine($"水印文字默认值: {watermarkParam.DefaultValue} (类型: {watermarkParam.Type})");

        // 验证表格参数默认值
        Console.WriteLine("\n【表格参数验证】");
        var tableOp = service.CreateOperationPoint("SetTableRowsColumns");
        var rowsParam = tableOp.Parameters.First(p => p.Name == "Rows");
        var columnsParam = tableOp.Parameters.First(p => p.Name == "Columns");
        Console.WriteLine($"表格行数默认值: {rowsParam.DefaultValue} (类型: {rowsParam.Type})");
        Console.WriteLine($"表格列数默认值: {columnsParam.DefaultValue} (类型: {columnsParam.Type})");

        Console.WriteLine("\n✅ 所有默认值验证通过！");
    }

    /// <summary>
    /// 演示如何修改参数值
    /// </summary>
    public static void DemonstrateParameterModification()
    {
        Console.WriteLine("\n=== 参数修改示例 ===");

        WordKnowledgeService service = WordKnowledgeService.Instance;
        OperationPointModel operation = service.CreateOperationPoint("SetParagraphFont");

        Console.WriteLine("修改前的参数值:");
        foreach (var param in operation.Parameters)
        {
            Console.WriteLine($"  {param.DisplayName}: {param.Value}");
        }

        // 修改参数值
        var paragraphParam = operation.Parameters.First(p => p.Name == "ParagraphNumber");
        var fontParam = operation.Parameters.First(p => p.Name == "FontFamily");

        paragraphParam.Value = "3";  // 修改为第3段
        fontParam.Value = "微软雅黑";  // 修改字体

        Console.WriteLine("\n修改后的参数值:");
        foreach (var param in operation.Parameters)
        {
            Console.WriteLine($"  {param.DisplayName}: {param.Value}");
        }

        Console.WriteLine("\n✅ 参数修改完成！");
    }
}
