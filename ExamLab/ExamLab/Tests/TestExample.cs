using System;
using System.Linq;
using ExamLab.Models;

namespace ExamLab.Tests;

/// <summary>
/// 测试示例类
/// 展示如何使用知识点配置测试工具
/// </summary>
public static class TestExample
{
    /// <summary>
    /// 运行测试示例
    /// </summary>
    public static void RunExample()
    {
        Console.WriteLine("=== 知识点配置测试示例 ===\n");

        // 示例1：快速检查所有模块
        Example1_QuickCheck();

        // 示例2：测试Excel模块的特定知识点
        Example2_TestExcelKnowledgePoints();

        // 示例3：获取Word模块缺失的知识点
        Example3_GetMissingWordKnowledgePoints();

        // 示例4：生成PowerPoint模块的配置模板
        Example4_GeneratePowerPointTemplates();

        // 示例5：测试Windows模块操作类型
        Example5_TestWindowsOperations();

        Console.WriteLine("=== 测试示例完成 ===");
    }

    /// <summary>
    /// 示例1：快速检查所有模块的配置完整性
    /// </summary>
    private static void Example1_QuickCheck()
    {
        Console.WriteLine("📊 示例1：快速检查所有模块");
        Console.WriteLine("----------------------------------------");

        try
        {
            string result = TestRunner.QuickCheckAllModules();
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"快速检查失败: {ex.Message}");
        }

        Console.WriteLine("----------------------------------------\n");
    }

    /// <summary>
    /// 示例2：测试Excel模块的特定知识点
    /// </summary>
    private static void Example2_TestExcelKnowledgePoints()
    {
        Console.WriteLine("📊 示例2：测试Excel模块特定知识点");
        Console.WriteLine("----------------------------------------");

        // 测试几个已知的Excel知识点
        ExcelKnowledgeType[] testTypes = 
        [
            ExcelKnowledgeType.FillOrCopyCellContent,
            ExcelKnowledgeType.MergeCells,
            ExcelKnowledgeType.SetCellFont,
            ExcelKnowledgeType.UseFunction,
            ExcelKnowledgeType.Filter,
            ExcelKnowledgeType.ChartType
        ];

        foreach (ExcelKnowledgeType type in testTypes)
        {
            try
            {
                var operationPoint = Services.ExcelKnowledgeService.Instance.CreateOperationPoint(type);
                Console.WriteLine($"✅ {type}: {operationPoint.Name}");
                Console.WriteLine($"   参数数量: {operationPoint.Parameters.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {type}: {ex.Message}");
            }
        }

        Console.WriteLine("----------------------------------------\n");
    }

    /// <summary>
    /// 示例3：获取Word模块缺失的知识点
    /// </summary>
    private static void Example3_GetMissingWordKnowledgePoints()
    {
        Console.WriteLine("📝 示例3：获取Word模块缺失知识点");
        Console.WriteLine("----------------------------------------");

        try
        {
            string missingPoints = TestRunner.GetMissingKnowledgePoints(ModuleType.Word);
            Console.WriteLine(missingPoints);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取Word缺失知识点失败: {ex.Message}");
        }

        Console.WriteLine("----------------------------------------\n");
    }

    /// <summary>
    /// 示例4：生成PowerPoint模块的配置模板
    /// </summary>
    private static void Example4_GeneratePowerPointTemplates()
    {
        Console.WriteLine("🎨 示例4：生成PowerPoint模块配置模板");
        Console.WriteLine("----------------------------------------");

        try
        {
            // 只显示前几个缺失的知识点模板，避免输出过长
            PowerPointKnowledgeType[] allTypes = Enum.GetValues<PowerPointKnowledgeType>();
            int missingCount = 0;

            foreach (PowerPointKnowledgeType type in allTypes)
            {
                try
                {
                    Services.PowerPointKnowledgeService.Instance.CreateOperationPoint(type);
                }
                catch
                {
                    missingCount++;
                    if (missingCount <= 3) // 只显示前3个缺失的知识点模板
                    {
                        Console.WriteLine($@"
// {type}
configs[PowerPointKnowledgeType.{type}] = new PowerPointKnowledgeConfig
{{
    KnowledgeType = PowerPointKnowledgeType.{type},
    Name = ""{type}"",
    Description = ""描述{type}的功能"",
    Category = ""幻灯片操作"",
    ParameterTemplates =
    [
        new() {{ Name = ""SlideNumber"", DisplayName = ""幻灯片编号"", Description = ""第几张幻灯片"", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 }},
        new() {{ Name = ""Description"", DisplayName = ""文本题目描述"", Description = ""题目描述"", Type = ParameterType.Text, IsRequired = true, Order = 2 }}
    ]
}};");
                    }
                }
            }

            if (missingCount > 3)
            {
                Console.WriteLine($"\n... 还有 {missingCount - 3} 个知识点需要配置");
            }

            Console.WriteLine($"\nPowerPoint模块总计缺失 {missingCount} 个知识点配置");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成PowerPoint配置模板失败: {ex.Message}");
        }

        Console.WriteLine("----------------------------------------\n");
    }

    /// <summary>
    /// 示例5：测试Windows模块操作类型
    /// </summary>
    private static void Example5_TestWindowsOperations()
    {
        Console.WriteLine("🖥️ 示例5：测试Windows模块操作类型");
        Console.WriteLine("----------------------------------------");

        try
        {
            // 创建测试用的ExamModule和题目
            ExamModule testModule = new()
            {
                Id = "test-windows-module",
                Name = "测试Windows模块",
                Type = ModuleType.Windows
            };

            Question testQuestion = new()
            {
                Id = "test-question-1",
                Title = "测试题目",
                Content = "用于测试的题目"
            };

            testModule.Questions.Add(testQuestion);

            // 创建WindowsModuleViewModel
            ViewModels.WindowsModuleViewModel viewModel = new(testModule)
            {
                SelectedQuestion = testQuestion
            };

            // 测试几个Windows操作类型
            WindowsOperationType[] testTypes = 
            [
                WindowsOperationType.QuickCreate,
                WindowsOperationType.CreateOperation,
                WindowsOperationType.DeleteOperation,
                WindowsOperationType.CopyOperation,
                WindowsOperationType.MoveOperation
            ];

            foreach (WindowsOperationType operationType in testTypes)
            {
                try
                {
                    int beforeCount = testQuestion.OperationPoints.Count;
                    viewModel.AddOperationPointByTypeCommand.Execute(operationType.ToString());
                    
                    if (testQuestion.OperationPoints.Count > beforeCount)
                    {
                        var addedPoint = testQuestion.OperationPoints.Last();
                        Console.WriteLine($"✅ {operationType}: {addedPoint.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ {operationType}: 操作点未添加");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ {operationType}: {ex.Message}");
                }
            }

            Console.WriteLine($"\n总计添加了 {testQuestion.OperationPoints.Count} 个操作点");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试Windows模块失败: {ex.Message}");
        }

        Console.WriteLine("----------------------------------------\n");
    }

    /// <summary>
    /// 运行特定模块的详细测试
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    public static void RunDetailedModuleTest(ModuleType moduleType)
    {
        Console.WriteLine($"=== {moduleType} 模块详细测试 ===\n");

        try
        {
            // 运行特定模块测试
            string result = TestRunner.RunSpecificModuleTest(moduleType);
            Console.WriteLine(result);

            // 获取缺失知识点
            if (moduleType != ModuleType.Windows && moduleType != ModuleType.CSharp)
            {
                Console.WriteLine($"\n=== {moduleType} 模块缺失知识点 ===");
                string missingPoints = TestRunner.GetMissingKnowledgePoints(moduleType);
                Console.WriteLine(missingPoints);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"详细测试 {moduleType} 模块失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 演示如何使用测试工具进行开发调试
    /// </summary>
    public static void DemonstrateDevelopmentWorkflow()
    {
        Console.WriteLine("=== 开发调试工作流程演示 ===\n");

        Console.WriteLine("1. 首先进行快速检查，了解整体状况：");
        Example1_QuickCheck();

        Console.WriteLine("2. 针对特定模块进行详细测试：");
        RunDetailedModuleTest(ModuleType.Excel);

        Console.WriteLine("3. 根据测试结果，生成缺失配置的代码模板：");
        try
        {
            string templates = TestRunner.GenerateMissingConfigTemplates(ModuleType.Excel);
            // 只显示前几行，避免输出过长
            string[] lines = templates.Split('\n');
            for (int i = 0; i < Math.Min(10, lines.Length); i++)
            {
                Console.WriteLine(lines[i]);
            }
            if (lines.Length > 10)
            {
                Console.WriteLine("... (更多模板内容)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成模板失败: {ex.Message}");
        }

        Console.WriteLine("\n4. 添加配置后，再次测试验证：");
        Console.WriteLine("   (在实际开发中，这里会重新运行测试)");

        Console.WriteLine("\n=== 工作流程演示完成 ===");
    }
}
