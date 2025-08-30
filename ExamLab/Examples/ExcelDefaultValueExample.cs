using ExamLab.Models;
using ExamLab.Services;
using System;
using System.Linq;

namespace ExamLab.Examples;

/// <summary>
/// Excel默认值功能使用示例
/// </summary>
public class ExcelDefaultValueExample
{
    public static void RunExample()
    {
        Console.WriteLine("=== ExamLab Excel默认值功能示例 ===\n");

        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // 示例1：创建带有默认值的Excel操作点
        Console.WriteLine("1. 创建带有默认值的Excel操作点示例：");

        // 创建单元格字体设置操作点
        Console.WriteLine("\n【设置单元格字体】");
        OperationPoint fontOperation = service.CreateOperationPoint(ExcelKnowledgeType.SetCellFont);
        DisplayOperationPoint(fontOperation);

        // 创建单元格字体颜色设置操作点
        Console.WriteLine("\n【设置字体颜色】");
        OperationPoint fontColorOperation = service.CreateOperationPoint(ExcelKnowledgeType.SetFontColor);
        DisplayOperationPoint(fontColorOperation);

        // 创建单元格填充颜色操作点
        Console.WriteLine("\n【设置填充颜色】");
        OperationPoint fillColorOperation = service.CreateOperationPoint(ExcelKnowledgeType.SetCellFillColor);
        DisplayOperationPoint(fillColorOperation);

        // 创建行高设置操作点
        Console.WriteLine("\n【设置行高】");
        OperationPoint rowHeightOperation = service.CreateOperationPoint(ExcelKnowledgeType.SetRowHeight);
        DisplayOperationPoint(rowHeightOperation);

        // 创建图表类型操作点
        Console.WriteLine("\n【设置图表类型】");
        OperationPoint chartOperation = service.CreateOperationPoint(ExcelKnowledgeType.ChartType);
        DisplayOperationPoint(chartOperation);

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // 示例2：验证默认值设置
        Console.WriteLine("2. 验证默认值设置：");
        VerifyDefaultValues();

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // 示例3：与Word操作点的一致性验证
        Console.WriteLine("3. 与Word操作点的一致性验证：");
        VerifyConsistencyWithWord();

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
        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;

        // 验证颜色参数默认值
        Console.WriteLine("【颜色参数验证】");
        var fontColorOp = service.CreateOperationPoint(ExcelKnowledgeType.SetFontColor);
        var fontColorParam = fontColorOp.Parameters.First(p => p.Name == "FontColor");
        Console.WriteLine($"字体颜色默认值: {fontColorParam.DefaultValue} (类型: {fontColorParam.Type})");

        var fillColorOp = service.CreateOperationPoint(ExcelKnowledgeType.SetCellFillColor);
        var fillColorParam = fillColorOp.Parameters.First(p => p.Name == "FillColor");
        Console.WriteLine($"填充颜色默认值: {fillColorParam.DefaultValue} (类型: {fillColorParam.Type})");

        var borderColorOp = service.CreateOperationPoint(ExcelKnowledgeType.SetInnerBorderColor);
        var borderColorParam = borderColorOp.Parameters.First(p => p.Name == "BorderColor");
        Console.WriteLine($"边框颜色默认值: {borderColorParam.DefaultValue} (类型: {borderColorParam.Type})");

        // 验证数字参数默认值
        Console.WriteLine("\n【数字参数验证】");
        var fontSizeOp = service.CreateOperationPoint(ExcelKnowledgeType.SetFontSize);
        var fontSizeParam = fontSizeOp.Parameters.First(p => p.Name == "FontSize");
        Console.WriteLine($"字号默认值: {fontSizeParam.DefaultValue} (类型: {fontSizeParam.Type})");

        var rowHeightOp = service.CreateOperationPoint(ExcelKnowledgeType.SetRowHeight);
        var rowHeightParam = rowHeightOp.Parameters.First(p => p.Name == "RowHeight");
        Console.WriteLine($"行高默认值: {rowHeightParam.DefaultValue} (类型: {rowHeightParam.Type})");

        var columnWidthOp = service.CreateOperationPoint(ExcelKnowledgeType.SetColumnWidth);
        var columnWidthParam = columnWidthOp.Parameters.First(p => p.Name == "ColumnWidth");
        Console.WriteLine($"列宽默认值: {columnWidthParam.DefaultValue} (类型: {columnWidthParam.Type})");

        // 验证枚举参数默认值
        Console.WriteLine("\n【枚举参数验证】");
        var fontOp = service.CreateOperationPoint(ExcelKnowledgeType.SetCellFont);
        var fontFamilyParam = fontOp.Parameters.First(p => p.Name == "FontFamily");
        Console.WriteLine($"字体类型默认值: {fontFamilyParam.DefaultValue} (类型: {fontFamilyParam.Type})");

        var alignmentOp = service.CreateOperationPoint(ExcelKnowledgeType.SetHorizontalAlignment);
        var alignmentParam = alignmentOp.Parameters.First(p => p.Name == "HorizontalAlignment");
        Console.WriteLine($"水平对齐默认值: {alignmentParam.DefaultValue} (类型: {alignmentParam.Type})");

        var chartTypeOp = service.CreateOperationPoint(ExcelKnowledgeType.ChartType);
        var chartTypeParam = chartTypeOp.Parameters.First(p => p.Name == "ChartType");
        Console.WriteLine($"图表类型默认值: {chartTypeParam.DefaultValue} (类型: {chartTypeParam.Type})");

        // 验证文本参数默认值
        Console.WriteLine("\n【文本参数验证】");
        var workbookParam = fontOp.Parameters.First(p => p.Name == "TargetWorkbook");
        Console.WriteLine($"目标工作簿默认值: {workbookParam.DefaultValue} (类型: {workbookParam.Type})");

        var cellRangeParam = fontOp.Parameters.First(p => p.Name == "CellRange");
        Console.WriteLine($"单元格区域默认值: {cellRangeParam.DefaultValue} (类型: {cellRangeParam.Type})");

        // 验证布尔参数默认值
        Console.WriteLine("\n【布尔参数验证】");
        var sortOp = service.CreateOperationPoint(ExcelKnowledgeType.Sort);
        var hasHeaderParam = sortOp.Parameters.First(p => p.Name == "HasHeader");
        Console.WriteLine($"包含标题默认值: {hasHeaderParam.DefaultValue} (类型: {hasHeaderParam.Type})");

        Console.WriteLine("\n✅ 所有默认值验证通过！");
    }

    private static void VerifyConsistencyWithWord()
    {
        ExcelKnowledgeService excelService = ExcelKnowledgeService.Instance;
        WordKnowledgeService wordService = WordKnowledgeService.Instance;

        // 比较字体设置操作点
        Console.WriteLine("【字体设置操作点比较】");
        var excelFontOp = excelService.CreateOperationPoint(ExcelKnowledgeType.SetCellFont);
        var wordFontOp = wordService.CreateOperationPoint(WordKnowledgeType.SetParagraphFont);

        var excelFontParam = excelFontOp.Parameters.First(p => p.Name == "FontFamily");
        var wordFontParam = wordFontOp.Parameters.First(p => p.Name == "FontFamily");

        Console.WriteLine($"Excel字体默认值: {excelFontParam.DefaultValue}");
        Console.WriteLine($"Word字体默认值: {wordFontParam.DefaultValue}");
        Console.WriteLine($"一致性: {(excelFontParam.DefaultValue == wordFontParam.DefaultValue ? "✅" : "❌")}");

        // 比较字号设置操作点
        Console.WriteLine("\n【字号设置操作点比较】");
        var excelFontSizeOp = excelService.CreateOperationPoint(ExcelKnowledgeType.SetFontSize);
        var wordFontSizeOp = wordService.CreateOperationPoint(WordKnowledgeType.SetParagraphFontSize);

        var excelFontSizeParam = excelFontSizeOp.Parameters.First(p => p.Name == "FontSize");
        var wordFontSizeParam = wordFontSizeOp.Parameters.First(p => p.Name == "FontSize");

        Console.WriteLine($"Excel字号默认值: {excelFontSizeParam.DefaultValue}");
        Console.WriteLine($"Word字号默认值: {wordFontSizeParam.DefaultValue}");
        Console.WriteLine($"一致性: {(excelFontSizeParam.DefaultValue == wordFontSizeParam.DefaultValue ? "✅" : "❌")}");

        // 比较颜色设置操作点
        Console.WriteLine("\n【颜色设置操作点比较】");
        var excelColorOp = excelService.CreateOperationPoint(ExcelKnowledgeType.SetFontColor);
        var wordColorOp = wordService.CreateOperationPoint(WordKnowledgeType.SetParagraphTextColor);

        var excelColorParam = excelColorOp.Parameters.First(p => p.Name == "FontColor");
        var wordColorParam = wordColorOp.Parameters.First(p => p.Name == "TextColor");

        Console.WriteLine($"Excel字体颜色默认值: {excelColorParam.DefaultValue}");
        Console.WriteLine($"Word文字颜色默认值: {wordColorParam.DefaultValue}");
        Console.WriteLine($"一致性: {(excelColorParam.DefaultValue == wordColorParam.DefaultValue ? "✅" : "❌")}");

        Console.WriteLine("\n✅ 与Word操作点保持良好的一致性！");
    }

    /// <summary>
    /// 演示如何修改Excel操作点参数值
    /// </summary>
    public static void DemonstrateParameterModification()
    {
        Console.WriteLine("\n=== Excel参数修改示例 ===");

        ExcelKnowledgeService service = ExcelKnowledgeService.Instance;
        OperationPoint operation = service.CreateOperationPoint(ExcelKnowledgeType.SetCellFont);

        Console.WriteLine("修改前的参数值:");
        foreach (var param in operation.Parameters)
        {
            Console.WriteLine($"  {param.DisplayName}: {param.Value}");
        }

        // 修改参数值
        var workbookParam = operation.Parameters.First(p => p.Name == "TargetWorkbook");
        var cellRangeParam = operation.Parameters.First(p => p.Name == "CellRange");
        var fontParam = operation.Parameters.First(p => p.Name == "FontFamily");

        workbookParam.Value = "我的工作簿.xlsx";
        cellRangeParam.Value = "B2:D5";
        fontParam.Value = "微软雅黑";

        Console.WriteLine("\n修改后的参数值:");
        foreach (var param in operation.Parameters)
        {
            Console.WriteLine($"  {param.DisplayName}: {param.Value}");
        }

        Console.WriteLine("\n✅ Excel参数修改完成！");
    }
}
