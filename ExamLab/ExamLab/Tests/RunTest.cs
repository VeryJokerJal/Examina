using System;
using ExamLab.Models;

namespace ExamLab.Tests;

/// <summary>
/// 运行测试的简单类
/// </summary>
public static class RunTest
{
    /// <summary>
    /// 运行快速检查
    /// </summary>
    public static void RunQuickCheck()
    {
        try
        {
            Console.WriteLine("=== 快速检查所有模块配置状态 ===\n");
            
            string result = TestRunner.QuickCheckAllModules();
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 运行Excel模块测试
    /// </summary>
    public static void RunExcelTest()
    {
        try
        {
            Console.WriteLine("=== Excel模块详细测试 ===\n");
            
            string result = TestRunner.RunSpecificModuleTest(ModuleType.Excel);
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Excel测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 运行Word模块测试
    /// </summary>
    public static void RunWordTest()
    {
        try
        {
            Console.WriteLine("=== Word模块详细测试 ===\n");
            
            string result = TestRunner.RunSpecificModuleTest(ModuleType.Word);
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Word测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 运行PowerPoint模块测试
    /// </summary>
    public static void RunPowerPointTest()
    {
        try
        {
            Console.WriteLine("=== PowerPoint模块详细测试 ===\n");
            
            string result = TestRunner.RunSpecificModuleTest(ModuleType.PowerPoint);
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PowerPoint测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试特定的Excel知识点
    /// </summary>
    public static void TestExcelKnowledgePoints()
    {
        Console.WriteLine("=== 测试Excel新增知识点 ===\n");

        ExcelKnowledgeType[] newTypes = 
        [
            ExcelKnowledgeType.SetFontStyle,
            ExcelKnowledgeType.SetFontSize,
            ExcelKnowledgeType.SetFontColor,
            ExcelKnowledgeType.SetNumberFormat,
            ExcelKnowledgeType.SetPatternFillStyle,
            ExcelKnowledgeType.SetPatternFillColor,
            ExcelKnowledgeType.SetOuterBorderStyle,
            ExcelKnowledgeType.SetOuterBorderColor,
            ExcelKnowledgeType.AddUnderline,
            ExcelKnowledgeType.ConditionalFormat,
            ExcelKnowledgeType.SetCellStyleData,
            ExcelKnowledgeType.Subtotal,
            ExcelKnowledgeType.AdvancedFilterCondition,
            ExcelKnowledgeType.AdvancedFilterData,
            ExcelKnowledgeType.ChartMove,
            ExcelKnowledgeType.CategoryAxisDataRange,
            ExcelKnowledgeType.ValueAxisDataRange,
            ExcelKnowledgeType.ChartTitleFormat
        ];

        int successCount = 0;
        int failCount = 0;

        foreach (ExcelKnowledgeType type in newTypes)
        {
            try
            {
                var operationPoint = Services.ExcelKnowledgeService.Instance.CreateOperationPoint(type);
                Console.WriteLine($"✅ {type}: {operationPoint.Name} ({operationPoint.Parameters.Count}个参数)");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {type}: {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine($"\n新增Excel知识点测试结果: 成功 {successCount}, 失败 {failCount}");
    }

    /// <summary>
    /// 测试特定的Word知识点
    /// </summary>
    public static void TestWordKnowledgePoints()
    {
        Console.WriteLine("=== 测试Word新增知识点 ===\n");

        WordKnowledgeType[] newTypes = 
        [
            WordKnowledgeType.SetParagraphBorderColor,
            WordKnowledgeType.SetParagraphBorderStyle,
            WordKnowledgeType.SetParagraphBorderWidth,
            WordKnowledgeType.SetParagraphShading,
            WordKnowledgeType.SetFooterText,
            WordKnowledgeType.SetFooterFont,
            WordKnowledgeType.SetFooterFontSize,
            WordKnowledgeType.SetFooterAlignment,
            WordKnowledgeType.SetPageNumber,
            WordKnowledgeType.SetPageBackground,
            WordKnowledgeType.SetPageBorderColor,
            WordKnowledgeType.SetPageBorderStyle,
            WordKnowledgeType.SetPageBorderWidth
        ];

        int successCount = 0;
        int failCount = 0;

        foreach (WordKnowledgeType type in newTypes)
        {
            try
            {
                var operationPoint = Services.WordKnowledgeService.Instance.CreateOperationPoint(type);
                Console.WriteLine($"✅ {type}: {operationPoint.Name} ({operationPoint.Parameters.Count}个参数)");
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {type}: {ex.Message}");
                failCount++;
            }
        }

        Console.WriteLine($"\n新增Word知识点测试结果: 成功 {successCount}, 失败 {failCount}");
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    public static void RunAllTests()
    {
        Console.WriteLine("=== 开始运行所有测试 ===\n");

        // 1. 快速检查
        RunQuickCheck();
        Console.WriteLine("\n" + new string('=', 50) + "\n");

        // 2. 测试新增的Excel知识点
        TestExcelKnowledgePoints();
        Console.WriteLine("\n" + new string('=', 50) + "\n");

        // 3. 测试新增的Word知识点
        TestWordKnowledgePoints();
        Console.WriteLine("\n" + new string('=', 50) + "\n");

        // 4. Excel模块详细测试
        RunExcelTest();
        Console.WriteLine("\n" + new string('=', 50) + "\n");

        // 5. Word模块详细测试
        RunWordTest();

        Console.WriteLine("\n=== 所有测试完成 ===");
    }
}
