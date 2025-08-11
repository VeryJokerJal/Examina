using System;
using System.Collections.Generic;
using System.Linq;
using ExamLab.Models;
using ExamLab.Services;
using ExamLab.ViewModels;

namespace ExamLab.Tests;

/// <summary>
/// 模块知识点测试类
/// 用于测试各模块的AddOperationPoint功能，识别缺失的知识点配置
/// </summary>
public static class ModuleKnowledgePointTests
{
    /// <summary>
    /// 测试所有模块的知识点配置
    /// </summary>
    public static void TestAllModules()
    {
        Console.WriteLine("=== 开始测试所有模块的知识点配置 ===\n");

        TestExcelModule();
        TestWordModule();
        TestPowerPointModule();
        TestWindowsModule();

        Console.WriteLine("=== 测试完成 ===");
    }

    /// <summary>
    /// 测试Excel模块知识点
    /// </summary>
    public static void TestExcelModule()
    {
        Console.WriteLine("📊 测试Excel模块知识点配置");
        Console.WriteLine("----------------------------------------");

        // 获取所有Excel知识点类型
        ExcelKnowledgeType[] allExcelTypes = Enum.GetValues<ExcelKnowledgeType>();
        List<string> missingConfigs = [];
        List<string> successfulConfigs = [];

        foreach (ExcelKnowledgeType knowledgeType in allExcelTypes)
        {
            try
            {
                // 尝试创建操作点
                OperationPoint operationPoint = ExcelKnowledgeService.Instance.CreateOperationPoint(knowledgeType);
                successfulConfigs.Add($"✅ {knowledgeType} - {operationPoint.Name}");
            }
            catch (Exception ex)
            {
                missingConfigs.Add($"❌ {knowledgeType} - {ex.Message}");
            }
        }

        // 输出结果
        Console.WriteLine($"Excel模块总计: {allExcelTypes.Length} 个知识点");
        Console.WriteLine($"已配置: {successfulConfigs.Count} 个");
        Console.WriteLine($"缺失配置: {missingConfigs.Count} 个\n");

        if (successfulConfigs.Count > 0)
        {
            Console.WriteLine("✅ 已配置的知识点:");
            foreach (string config in successfulConfigs)
            {
                Console.WriteLine($"  {config}");
            }
            Console.WriteLine();
        }

        if (missingConfigs.Count > 0)
        {
            Console.WriteLine("❌ 缺失配置的知识点:");
            foreach (string config in missingConfigs)
            {
                Console.WriteLine($"  {config}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("----------------------------------------\n");
    }

    /// <summary>
    /// 测试Word模块知识点
    /// </summary>
    public static void TestWordModule()
    {
        Console.WriteLine("📝 测试Word模块知识点配置");
        Console.WriteLine("----------------------------------------");

        // 获取所有Word知识点类型
        WordKnowledgeType[] allWordTypes = Enum.GetValues<WordKnowledgeType>();
        List<string> missingConfigs = [];
        List<string> successfulConfigs = [];

        foreach (WordKnowledgeType knowledgeType in allWordTypes)
        {
            try
            {
                // 尝试创建操作点
                OperationPoint operationPoint = WordKnowledgeService.Instance.CreateOperationPoint(knowledgeType);
                successfulConfigs.Add($"✅ {knowledgeType} - {operationPoint.Name}");
            }
            catch (Exception ex)
            {
                missingConfigs.Add($"❌ {knowledgeType} - {ex.Message}");
            }
        }

        // 输出结果
        Console.WriteLine($"Word模块总计: {allWordTypes.Length} 个知识点");
        Console.WriteLine($"已配置: {successfulConfigs.Count} 个");
        Console.WriteLine($"缺失配置: {missingConfigs.Count} 个\n");

        if (successfulConfigs.Count > 0)
        {
            Console.WriteLine("✅ 已配置的知识点:");
            foreach (string config in successfulConfigs)
            {
                Console.WriteLine($"  {config}");
            }
            Console.WriteLine();
        }

        if (missingConfigs.Count > 0)
        {
            Console.WriteLine("❌ 缺失配置的知识点:");
            foreach (string config in missingConfigs)
            {
                Console.WriteLine($"  {config}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("----------------------------------------\n");
    }

    /// <summary>
    /// 测试PowerPoint模块知识点
    /// </summary>
    public static void TestPowerPointModule()
    {
        Console.WriteLine("🎨 测试PowerPoint模块知识点配置");
        Console.WriteLine("----------------------------------------");

        // 获取所有PowerPoint知识点类型
        PowerPointKnowledgeType[] allPptTypes = Enum.GetValues<PowerPointKnowledgeType>();
        List<string> missingConfigs = [];
        List<string> successfulConfigs = [];

        foreach (PowerPointKnowledgeType knowledgeType in allPptTypes)
        {
            try
            {
                // 尝试创建操作点
                OperationPoint operationPoint = PowerPointKnowledgeService.Instance.CreateOperationPoint(knowledgeType);
                successfulConfigs.Add($"✅ {knowledgeType} - {operationPoint.Name}");
            }
            catch (Exception ex)
            {
                missingConfigs.Add($"❌ {knowledgeType} - {ex.Message}");
            }
        }

        // 输出结果
        Console.WriteLine($"PowerPoint模块总计: {allPptTypes.Length} 个知识点");
        Console.WriteLine($"已配置: {successfulConfigs.Count} 个");
        Console.WriteLine($"缺失配置: {missingConfigs.Count} 个\n");

        if (successfulConfigs.Count > 0)
        {
            Console.WriteLine("✅ 已配置的知识点:");
            foreach (string config in successfulConfigs)
            {
                Console.WriteLine($"  {config}");
            }
            Console.WriteLine();
        }

        if (missingConfigs.Count > 0)
        {
            Console.WriteLine("❌ 缺失配置的知识点:");
            foreach (string config in missingConfigs)
            {
                Console.WriteLine($"  {config}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("----------------------------------------\n");
    }

    /// <summary>
    /// 测试Windows模块操作类型
    /// </summary>
    public static void TestWindowsModule()
    {
        Console.WriteLine("🖥️ 测试Windows模块操作类型");
        Console.WriteLine("----------------------------------------");

        // 创建测试用的ExamModule和题目
        ExamModule testModule = new()
        {
            Id = Guid.NewGuid(),
            Name = "测试Windows模块",
            ModuleType = ModuleType.Windows
        };

        ScoringQuestion testQuestion = new()
        {
            Id = Guid.NewGuid(),
            Title = "测试题目",
            Description = "用于测试的题目"
        };

        testModule.Questions.Add(testQuestion);

        // 创建WindowsModuleViewModel
        WindowsModuleViewModel viewModel = new(testModule)
        {
            SelectedQuestion = testQuestion
        };

        // 获取所有Windows操作类型
        WindowsOperationType[] allWindowsTypes = Enum.GetValues<WindowsOperationType>();
        List<string> successfulOperations = [];
        List<string> failedOperations = [];

        foreach (WindowsOperationType operationType in allWindowsTypes)
        {
            try
            {
                // 保存当前操作点数量
                int beforeCount = testQuestion.OperationPoints.Count;

                // 尝试添加操作点
                viewModel.AddOperationPointByTypeCommand.Execute(operationType.ToString());

                // 检查是否成功添加
                if (testQuestion.OperationPoints.Count > beforeCount)
                {
                    OperationPoint addedPoint = testQuestion.OperationPoints.Last();
                    successfulOperations.Add($"✅ {operationType} - {addedPoint.Name}");
                }
                else
                {
                    failedOperations.Add($"❌ {operationType} - 操作点未添加");
                }
            }
            catch (Exception ex)
            {
                failedOperations.Add($"❌ {operationType} - {ex.Message}");
            }
        }

        // 输出结果
        Console.WriteLine($"Windows模块总计: {allWindowsTypes.Length} 个操作类型");
        Console.WriteLine($"成功添加: {successfulOperations.Count} 个");
        Console.WriteLine($"添加失败: {failedOperations.Count} 个\n");

        if (successfulOperations.Count > 0)
        {
            Console.WriteLine("✅ 成功添加的操作类型:");
            foreach (string operation in successfulOperations)
            {
                Console.WriteLine($"  {operation}");
            }
            Console.WriteLine();
        }

        if (failedOperations.Count > 0)
        {
            Console.WriteLine("❌ 添加失败的操作类型:");
            foreach (string operation in failedOperations)
            {
                Console.WriteLine($"  {operation}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("----------------------------------------\n");
    }

    /// <summary>
    /// 测试特定模块的知识点
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    public static void TestSpecificModule(ModuleType moduleType)
    {
        switch (moduleType)
        {
            case ModuleType.Excel:
                TestExcelModule();
                break;
            case ModuleType.Word:
                TestWordModule();
                break;
            case ModuleType.PowerPoint:
                TestPowerPointModule();
                break;
            case ModuleType.Windows:
                TestWindowsModule();
                break;
            default:
                Console.WriteLine($"不支持的模块类型: {moduleType}");
                break;
        }
    }

    /// <summary>
    /// 生成缺失知识点配置的代码模板
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    public static void GenerateMissingConfigTemplates(ModuleType moduleType)
    {
        Console.WriteLine($"=== 生成 {moduleType} 模块缺失知识点的配置模板 ===\n");

        switch (moduleType)
        {
            case ModuleType.Excel:
                GenerateExcelMissingTemplates();
                break;
            case ModuleType.Word:
                GenerateWordMissingTemplates();
                break;
            case ModuleType.PowerPoint:
                GeneratePowerPointMissingTemplates();
                break;
            default:
                Console.WriteLine($"暂不支持为 {moduleType} 模块生成模板");
                break;
        }
    }

    private static void GenerateExcelMissingTemplates()
    {
        ExcelKnowledgeType[] allTypes = Enum.GetValues<ExcelKnowledgeType>();
        
        foreach (ExcelKnowledgeType type in allTypes)
        {
            try
            {
                ExcelKnowledgeService.Instance.CreateOperationPoint(type);
            }
            catch
            {
                Console.WriteLine($@"
// {type}
configs[ExcelKnowledgeType.{type}] = new ExcelKnowledgeConfig
{{
    KnowledgeType = ExcelKnowledgeType.{type},
    Name = ""{type}"",
    Description = ""描述{type}的功能"",
    Category = ""Excel基础操作"",
    ParameterTemplates =
    [
        new() {{ Name = ""TargetWorkbook"", DisplayName = ""目标工作簿"", Description = ""目标工作簿"", Type = ParameterType.Text, IsRequired = true, Order = 1 }},
        new() {{ Name = ""OperationType"", DisplayName = ""操作类型"", Description = ""操作类型"", Type = ParameterType.Text, IsRequired = true, Order = 2, DefaultValue = ""A"" }},
        new() {{ Name = ""Description"", DisplayName = ""文本题目描述"", Description = ""题目描述"", Type = ParameterType.Text, IsRequired = true, Order = 3 }}
    ]
}};");
            }
        }
    }

    private static void GenerateWordMissingTemplates()
    {
        WordKnowledgeType[] allTypes = Enum.GetValues<WordKnowledgeType>();
        
        foreach (WordKnowledgeType type in allTypes)
        {
            try
            {
                WordKnowledgeService.Instance.CreateOperationPoint(type);
            }
            catch
            {
                Console.WriteLine($@"
// {type}
configs[WordKnowledgeType.{type}] = new WordKnowledgeConfig
{{
    KnowledgeType = WordKnowledgeType.{type},
    Name = ""{type}"",
    Description = ""描述{type}的功能"",
    Category = ""段落操作"",
    ParameterTemplates =
    [
        new() {{ Name = ""ParagraphNumber"", DisplayName = ""段落序号"", Description = ""第几个段落"", Type = ParameterType.Number, IsRequired = true, Order = 1, MinValue = 1 }},
        new() {{ Name = ""Description"", DisplayName = ""文本题目描述"", Description = ""题目描述"", Type = ParameterType.Text, IsRequired = true, Order = 2 }}
    ]
}};");
            }
        }
    }

    private static void GeneratePowerPointMissingTemplates()
    {
        PowerPointKnowledgeType[] allTypes = Enum.GetValues<PowerPointKnowledgeType>();
        
        foreach (PowerPointKnowledgeType type in allTypes)
        {
            try
            {
                PowerPointKnowledgeService.Instance.CreateOperationPoint(type);
            }
            catch
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
}
