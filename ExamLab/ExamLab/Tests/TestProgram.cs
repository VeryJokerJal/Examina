using System;

namespace ExamLab.Tests;

/// <summary>
/// 测试程序入口
/// </summary>
public static class TestProgram
{
    /// <summary>
    /// 主测试方法
    /// </summary>
    public static void Main()
    {
        try
        {
            Console.WriteLine("=== 知识点配置测试程序 ===\n");

            // 运行快速检查
            Console.WriteLine("1. 快速检查所有模块配置状态：");
            Console.WriteLine("----------------------------------------");
            RunTest.RunQuickCheck();

            Console.WriteLine("\n\n2. 测试新增的Excel知识点：");
            Console.WriteLine("----------------------------------------");
            RunTest.TestExcelKnowledgePoints();

            Console.WriteLine("\n\n3. 测试新增的Word知识点：");
            Console.WriteLine("----------------------------------------");
            RunTest.TestWordKnowledgePoints();

            Console.WriteLine("\n\n=== 测试程序完成 ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"测试程序发生错误: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 运行详细测试
    /// </summary>
    public static void RunDetailedTests()
    {
        try
        {
            Console.WriteLine("=== 详细测试开始 ===\n");

            // Excel模块详细测试
            Console.WriteLine("Excel模块详细测试：");
            Console.WriteLine("----------------------------------------");
            RunTest.RunExcelTest();

            Console.WriteLine("\n\nWord模块详细测试：");
            Console.WriteLine("----------------------------------------");
            RunTest.RunWordTest();

            Console.WriteLine("\n\nPowerPoint模块详细测试：");
            Console.WriteLine("----------------------------------------");
            RunTest.RunPowerPointTest();

            Console.WriteLine("\n=== 详细测试完成 ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"详细测试发生错误: {ex.Message}");
        }
    }
}
