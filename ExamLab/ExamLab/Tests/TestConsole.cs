using System;
using ExamLab.Models;

namespace ExamLab.Tests;

/// <summary>
/// 控制台测试程序
/// 用于在控制台环境下运行知识点配置测试
/// </summary>
public static class TestConsole
{
    /// <summary>
    /// 运行控制台测试
    /// </summary>
    public static void RunConsoleTest()
    {
        Console.WriteLine("=== 知识点配置测试控制台 ===\n");

        try
        {
            // 显示菜单
            ShowMenu();

            while (true)
            {
                Console.Write("\n请选择操作 (输入数字): ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                switch (input.Trim())
                {
                    case "1":
                        RunQuickCheck();
                        break;
                    case "2":
                        RunFullTest();
                        break;
                    case "3":
                        TestSpecificModule();
                        break;
                    case "4":
                        ShowMissingKnowledgePoints();
                        break;
                    case "5":
                        GenerateConfigTemplates();
                        break;
                    case "6":
                        RunQuickTest();
                        break;
                    case "0":
                        Console.WriteLine("退出测试程序");
                        return;
                    default:
                        Console.WriteLine("无效选择，请重新输入");
                        break;
                }

                Console.WriteLine("\n按任意键继续...");
                Console.ReadKey();
                Console.Clear();
                ShowMenu();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试程序发生错误: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
    }

    private static void ShowMenu()
    {
        Console.WriteLine("=== 知识点配置测试菜单 ===");
        Console.WriteLine("1. 快速检查所有模块");
        Console.WriteLine("2. 完整测试所有模块");
        Console.WriteLine("3. 测试特定模块");
        Console.WriteLine("4. 显示缺失知识点");
        Console.WriteLine("5. 生成配置模板");
        Console.WriteLine("6. 运行快速测试");
        Console.WriteLine("0. 退出");
        Console.WriteLine("========================");
    }

    private static void RunQuickCheck()
    {
        Console.WriteLine("\n=== 快速检查所有模块 ===");
        try
        {
            string result = TestRunner.QuickCheckAllModules();
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"快速检查失败: {ex.Message}");
        }
    }

    private static void RunFullTest()
    {
        Console.WriteLine("\n=== 完整测试所有模块 ===");
        try
        {
            string result = TestRunner.RunAllKnowledgePointTests();
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"完整测试失败: {ex.Message}");
        }
    }

    private static void TestSpecificModule()
    {
        Console.WriteLine("\n=== 测试特定模块 ===");
        Console.WriteLine("请选择要测试的模块:");
        Console.WriteLine("1. Excel");
        Console.WriteLine("2. Word");
        Console.WriteLine("3. PowerPoint");
        Console.WriteLine("4. Windows");

        Console.Write("请输入选择 (1-4): ");
        string? input = Console.ReadLine();

        ModuleType moduleType = input?.Trim() switch
        {
            "1" => ModuleType.Excel,
            "2" => ModuleType.Word,
            "3" => ModuleType.PowerPoint,
            "4" => ModuleType.Windows,
            _ => ModuleType.Excel
        };

        try
        {
            string result = TestRunner.RunSpecificModuleTest(moduleType);
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试 {moduleType} 模块失败: {ex.Message}");
        }
    }

    private static void ShowMissingKnowledgePoints()
    {
        Console.WriteLine("\n=== 显示缺失知识点 ===");
        Console.WriteLine("请选择要检查的模块:");
        Console.WriteLine("1. Excel");
        Console.WriteLine("2. Word");
        Console.WriteLine("3. PowerPoint");

        Console.Write("请输入选择 (1-3): ");
        string? input = Console.ReadLine();

        ModuleType moduleType = input?.Trim() switch
        {
            "1" => ModuleType.Excel,
            "2" => ModuleType.Word,
            "3" => ModuleType.PowerPoint,
            _ => ModuleType.Excel
        };

        try
        {
            string result = TestRunner.GetMissingKnowledgePoints(moduleType);
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取 {moduleType} 模块缺失知识点失败: {ex.Message}");
        }
    }

    private static void GenerateConfigTemplates()
    {
        Console.WriteLine("\n=== 生成配置模板 ===");
        Console.WriteLine("请选择要生成模板的模块:");
        Console.WriteLine("1. Excel");
        Console.WriteLine("2. Word");
        Console.WriteLine("3. PowerPoint");

        Console.Write("请输入选择 (1-3): ");
        string? input = Console.ReadLine();

        ModuleType moduleType = input?.Trim() switch
        {
            "1" => ModuleType.Excel,
            "2" => ModuleType.Word,
            "3" => ModuleType.PowerPoint,
            _ => ModuleType.Excel
        };

        try
        {
            string result = TestRunner.GenerateMissingConfigTemplates(moduleType);
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成 {moduleType} 模块配置模板失败: {ex.Message}");
        }
    }

    private static void RunQuickTest()
    {
        Console.WriteLine("\n=== 运行快速测试 ===");
        try
        {
            // QuickTest.RunQuickTest(); // QuickTest类不存在，暂时注释
            Console.WriteLine("快速测试功能暂时不可用");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"快速测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试特定知识点
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <param name="knowledgePointName">知识点名称</param>
    public static void TestSpecificKnowledgePoint(ModuleType moduleType, string knowledgePointName)
    {
        Console.WriteLine($"\n=== 测试 {moduleType} 模块的 {knowledgePointName} 知识点 ===");

        try
        {
            switch (moduleType)
            {
                case ModuleType.Excel:
                    if (Enum.TryParse<ExcelKnowledgeType>(knowledgePointName, out ExcelKnowledgeType excelType))
                    {
                        var operationPoint = Services.ExcelKnowledgeService.Instance.CreateOperationPoint(excelType);
                        Console.WriteLine($"✅ 成功创建操作点: {operationPoint.Name}");
                        Console.WriteLine($"   描述: {operationPoint.Description}");
                        Console.WriteLine($"   参数数量: {operationPoint.Parameters.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ 无效的Excel知识点类型: {knowledgePointName}");
                    }
                    break;

                case ModuleType.Word:
                    if (Enum.TryParse<WordKnowledgeType>(knowledgePointName, out WordKnowledgeType wordType))
                    {
                        var operationPoint = Services.WordKnowledgeService.Instance.CreateOperationPoint(wordType);
                        Console.WriteLine($"✅ 成功创建操作点: {operationPoint.Name}");
                        Console.WriteLine($"   描述: {operationPoint.Description}");
                        Console.WriteLine($"   参数数量: {operationPoint.Parameters.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ 无效的Word知识点类型: {knowledgePointName}");
                    }
                    break;

                case ModuleType.PowerPoint:
                    if (Enum.TryParse<PowerPointKnowledgeType>(knowledgePointName, out PowerPointKnowledgeType pptType))
                    {
                        var operationPoint = Services.PowerPointKnowledgeService.Instance.CreateOperationPoint(pptType);
                        Console.WriteLine($"✅ 成功创建操作点: {operationPoint.Name}");
                        Console.WriteLine($"   描述: {operationPoint.Description}");
                        Console.WriteLine($"   参数数量: {operationPoint.Parameters.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ 无效的PowerPoint知识点类型: {knowledgePointName}");
                    }
                    break;

                default:
                    Console.WriteLine($"❌ 不支持的模块类型: {moduleType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 批量测试知识点
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <param name="knowledgePointNames">知识点名称列表</param>
    public static void BatchTestKnowledgePoints(ModuleType moduleType, string[] knowledgePointNames)
    {
        Console.WriteLine($"\n=== 批量测试 {moduleType} 模块知识点 ===");

        int successCount = 0;
        int failCount = 0;

        foreach (string knowledgePointName in knowledgePointNames)
        {
            try
            {
                TestSpecificKnowledgePoint(moduleType, knowledgePointName);
                successCount++;
            }
            catch
            {
                failCount++;
            }
        }

        Console.WriteLine($"\n批量测试结果: 成功 {successCount}, 失败 {failCount}");
    }
}
