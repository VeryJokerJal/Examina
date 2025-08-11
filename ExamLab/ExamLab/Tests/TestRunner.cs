using System;
using System.IO;
using System.Text;
using ExamLab.Models;

namespace ExamLab.Tests;

/// <summary>
/// 测试运行器
/// 提供简单的接口来运行各种测试
/// </summary>
public static class TestRunner
{
    /// <summary>
    /// 运行所有模块的知识点测试
    /// </summary>
    public static string RunAllKnowledgePointTests()
    {
        StringBuilder output = new();
        
        // 重定向控制台输出到StringBuilder
        StringWriter stringWriter = new(output);
        Console.SetOut(stringWriter);

        try
        {
            ModuleKnowledgePointTests.TestAllModules();
        }
        catch (Exception ex)
        {
            output.AppendLine($"测试过程中发生错误: {ex.Message}");
        }
        finally
        {
            // 恢复控制台输出
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }

        return output.ToString();
    }

    /// <summary>
    /// 运行特定模块的知识点测试
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <returns>测试结果</returns>
    public static string RunSpecificModuleTest(ModuleType moduleType)
    {
        StringBuilder output = new();
        
        // 重定向控制台输出到StringBuilder
        StringWriter stringWriter = new(output);
        Console.SetOut(stringWriter);

        try
        {
            ModuleKnowledgePointTests.TestSpecificModule(moduleType);
        }
        catch (Exception ex)
        {
            output.AppendLine($"测试 {moduleType} 模块时发生错误: {ex.Message}");
        }
        finally
        {
            // 恢复控制台输出
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }

        return output.ToString();
    }

    /// <summary>
    /// 生成缺失知识点配置的代码模板
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <returns>生成的代码模板</returns>
    public static string GenerateMissingConfigTemplates(ModuleType moduleType)
    {
        StringBuilder output = new();
        
        // 重定向控制台输出到StringBuilder
        StringWriter stringWriter = new(output);
        Console.SetOut(stringWriter);

        try
        {
            ModuleKnowledgePointTests.GenerateMissingConfigTemplates(moduleType);
        }
        catch (Exception ex)
        {
            output.AppendLine($"生成 {moduleType} 模块配置模板时发生错误: {ex.Message}");
        }
        finally
        {
            // 恢复控制台输出
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }

        return output.ToString();
    }

    /// <summary>
    /// 快速检查所有模块的配置完整性
    /// </summary>
    /// <returns>检查结果摘要</returns>
    public static string QuickCheckAllModules()
    {
        StringBuilder summary = new();
        
        try
        {
            // 检查Excel模块
            ExcelKnowledgeType[] excelTypes = Enum.GetValues<ExcelKnowledgeType>();
            int excelConfigured = 0;
            foreach (ExcelKnowledgeType type in excelTypes)
            {
                try
                {
                    ExamLab.Services.ExcelKnowledgeService.Instance.CreateOperationPoint(type);
                    excelConfigured++;
                }
                catch { }
            }

            // 检查Word模块
            WordKnowledgeType[] wordTypes = Enum.GetValues<WordKnowledgeType>();
            int wordConfigured = 0;
            foreach (WordKnowledgeType type in wordTypes)
            {
                try
                {
                    ExamLab.Services.WordKnowledgeService.Instance.CreateOperationPoint(type);
                    wordConfigured++;
                }
                catch { }
            }

            // 检查PowerPoint模块
            PowerPointKnowledgeType[] pptTypes = Enum.GetValues<PowerPointKnowledgeType>();
            int pptConfigured = 0;
            foreach (PowerPointKnowledgeType type in pptTypes)
            {
                try
                {
                    ExamLab.Services.PowerPointKnowledgeService.Instance.CreateOperationPoint(type);
                    pptConfigured++;
                }
                catch { }
            }

            // 生成摘要
            summary.AppendLine("=== 模块知识点配置完整性检查 ===");
            summary.AppendLine();
            summary.AppendLine($"📊 Excel模块: {excelConfigured}/{excelTypes.Length} ({(double)excelConfigured / excelTypes.Length * 100:F1}%)");
            summary.AppendLine($"📝 Word模块: {wordConfigured}/{wordTypes.Length} ({(double)wordConfigured / wordTypes.Length * 100:F1}%)");
            summary.AppendLine($"🎨 PowerPoint模块: {pptConfigured}/{pptTypes.Length} ({(double)pptConfigured / pptTypes.Length * 100:F1}%)");
            summary.AppendLine();

            int totalConfigured = excelConfigured + wordConfigured + pptConfigured;
            int totalTypes = excelTypes.Length + wordTypes.Length + pptTypes.Length;
            summary.AppendLine($"📈 总体完成度: {totalConfigured}/{totalTypes} ({(double)totalConfigured / totalTypes * 100:F1}%)");

            if (totalConfigured == totalTypes)
            {
                summary.AppendLine("🎉 所有知识点配置完整！");
            }
            else
            {
                summary.AppendLine($"⚠️  还有 {totalTypes - totalConfigured} 个知识点需要配置");
            }
        }
        catch (Exception ex)
        {
            summary.AppendLine($"检查过程中发生错误: {ex.Message}");
        }

        return summary.ToString();
    }

    /// <summary>
    /// 获取特定模块缺失的知识点列表
    /// </summary>
    /// <param name="moduleType">模块类型</param>
    /// <returns>缺失的知识点列表</returns>
    public static string GetMissingKnowledgePoints(ModuleType moduleType)
    {
        StringBuilder missing = new();
        
        try
        {
            missing.AppendLine($"=== {moduleType} 模块缺失的知识点 ===");
            missing.AppendLine();

            switch (moduleType)
            {
                case ModuleType.Excel:
                    ExcelKnowledgeType[] excelTypes = Enum.GetValues<ExcelKnowledgeType>();
                    foreach (ExcelKnowledgeType type in excelTypes)
                    {
                        try
                        {
                            ExamLab.Services.ExcelKnowledgeService.Instance.CreateOperationPoint(type);
                        }
                        catch
                        {
                            missing.AppendLine($"❌ {type}");
                        }
                    }
                    break;

                case ModuleType.Word:
                    WordKnowledgeType[] wordTypes = Enum.GetValues<WordKnowledgeType>();
                    foreach (WordKnowledgeType type in wordTypes)
                    {
                        try
                        {
                            ExamLab.Services.WordKnowledgeService.Instance.CreateOperationPoint(type);
                        }
                        catch
                        {
                            missing.AppendLine($"❌ {type}");
                        }
                    }
                    break;

                case ModuleType.PowerPoint:
                    PowerPointKnowledgeType[] pptTypes = Enum.GetValues<PowerPointKnowledgeType>();
                    foreach (PowerPointKnowledgeType type in pptTypes)
                    {
                        try
                        {
                            ExamLab.Services.PowerPointKnowledgeService.Instance.CreateOperationPoint(type);
                        }
                        catch
                        {
                            missing.AppendLine($"❌ {type}");
                        }
                    }
                    break;

                default:
                    missing.AppendLine($"不支持的模块类型: {moduleType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            missing.AppendLine($"获取缺失知识点时发生错误: {ex.Message}");
        }

        return missing.ToString();
    }
}
