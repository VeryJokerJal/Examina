using System;
using ExamLab.Models;

namespace ExamLab.Tests;

/// <summary>
/// 快速测试类
/// 用于验证测试脚本是否正常工作
/// </summary>
public static class QuickTest
{
    /// <summary>
    /// 运行快速测试
    /// </summary>
    public static void RunQuickTest()
    {
        Console.WriteLine("=== 快速测试开始 ===\n");

        try
        {
            // 测试TestRunner的快速检查功能
            Console.WriteLine("1. 测试快速检查功能:");
            string quickCheckResult = TestRunner.QuickCheckAllModules();
            Console.WriteLine(quickCheckResult);
            Console.WriteLine();

            // 测试Excel模块的几个知识点
            Console.WriteLine("2. 测试Excel模块部分知识点:");
            TestExcelKnowledgePoints();
            Console.WriteLine();

            // 测试Word模块的几个知识点
            Console.WriteLine("3. 测试Word模块部分知识点:");
            TestWordKnowledgePoints();
            Console.WriteLine();

            // 测试PowerPoint模块的几个知识点
            Console.WriteLine("4. 测试PowerPoint模块部分知识点:");
            TestPowerPointKnowledgePoints();
            Console.WriteLine();

            Console.WriteLine("=== 快速测试完成 ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"快速测试失败: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
    }

    private static void TestExcelKnowledgePoints()
    {
        // 测试几个已知配置的Excel知识点
        ExcelKnowledgeType[] testTypes = 
        [
            ExcelKnowledgeType.FillOrCopyCellContent,
            ExcelKnowledgeType.MergeCells,
            ExcelKnowledgeType.SetCellFont,
            ExcelKnowledgeType.SetInnerBorderStyle,
            ExcelKnowledgeType.SetInnerBorderColor,
            ExcelKnowledgeType.SetHorizontalAlignment,
            ExcelKnowledgeType.UseFunction,
            ExcelKnowledgeType.SetRowHeight,
            ExcelKnowledgeType.SetColumnWidth,
            ExcelKnowledgeType.SetCellFillColor
        ];

        int successCount = 0;
        int failCount = 0;

        foreach (ExcelKnowledgeType type in testTypes)
        {
            try
            {
                var operationPoint = Services.ExcelKnowledgeService.Instance.CreateOperationPoint(type);
                Console.WriteLine($"  ✅ {type} - {operationPoint.Name}");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ {type} - {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine($"Excel测试结果: 成功 {successCount}, 失败 {failCount}");
    }

    private static void TestWordKnowledgePoints()
    {
        // 测试几个Word知识点
        WordKnowledgeType[] testTypes = 
        [
            WordKnowledgeType.SetParagraphAlignment,
            WordKnowledgeType.SetParagraphIndentation,
            WordKnowledgeType.SetParagraphSpacing,
            WordKnowledgeType.SetFontName,
            WordKnowledgeType.SetFontSize
        ];

        int successCount = 0;
        int failCount = 0;

        foreach (WordKnowledgeType type in testTypes)
        {
            try
            {
                var operationPoint = Services.WordKnowledgeService.Instance.CreateOperationPoint(type);
                Console.WriteLine($"  ✅ {type} - {operationPoint.Name}");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ {type} - {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine($"Word测试结果: 成功 {successCount}, 失败 {failCount}");
    }

    private static void TestPowerPointKnowledgePoints()
    {
        // 测试几个PowerPoint知识点
        PowerPointKnowledgeType[] testTypes = 
        [
            PowerPointKnowledgeType.SetSlideLayout,
            PowerPointKnowledgeType.InsertSlide,
            PowerPointKnowledgeType.DeleteSlide,
            PowerPointKnowledgeType.InsertTextContent,
            PowerPointKnowledgeType.SetFontName
        ];

        int successCount = 0;
        int failCount = 0;

        foreach (PowerPointKnowledgeType type in testTypes)
        {
            try
            {
                var operationPoint = Services.PowerPointKnowledgeService.Instance.CreateOperationPoint(type);
                Console.WriteLine($"  ✅ {type} - {operationPoint.Name}");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ {type} - {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine($"PowerPoint测试结果: 成功 {successCount}, 失败 {failCount}");
    }

    /// <summary>
    /// 测试特定模块的缺失知识点
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    public static void TestMissingKnowledgePoints(ModuleType moduleType)
    {
        Console.WriteLine($"=== 测试 {moduleType} 模块缺失知识点 ===\n");

        try
        {
            string missingPoints = TestRunner.GetMissingKnowledgePoints(moduleType);
            Console.WriteLine(missingPoints);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 生成特定模块的配置模板
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    public static void GenerateConfigTemplates(ModuleType moduleType)
    {
        Console.WriteLine($"=== 生成 {moduleType} 模块配置模板 ===\n");

        try
        {
            string templates = TestRunner.GenerateMissingConfigTemplates(moduleType);
            Console.WriteLine(templates);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成模板失败: {ex.Message}");
        }
    }
}
