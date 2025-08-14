using System;
using System.IO;
using BenchSuite.Tests;

namespace TestStability
{
    /// <summary>
    /// PowerPoint评分稳定性测试程序
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PowerPoint评分稳定性测试程序");
            Console.WriteLine("============================");

            // 创建测试实例
            PowerPointScoringStabilityTest stabilityTest = new();

            // 检查是否提供了测试文件路径
            string testFilePath = null;
            if (args.Length > 0 && File.Exists(args[0]))
            {
                testFilePath = args[0];
                Console.WriteLine($"使用测试文件: {testFilePath}");
            }
            else
            {
                Console.WriteLine("未提供有效的测试文件路径，将只进行参数解析测试");
                Console.WriteLine("用法: TestStability.exe <PPT文件路径>");
            }

            Console.WriteLine();

            try
            {
                // 运行所有测试
                stabilityTest.RunAllTests(testFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中发生错误: {ex.Message}");
                Console.WriteLine($"详细信息: {ex}");
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
