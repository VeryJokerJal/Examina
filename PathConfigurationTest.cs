using System;
using System.Collections.Generic;
using Examina.Services;
using Microsoft.Extensions.Logging;
using BenchSuite.Models;

namespace Examina.Tests
{
    /// <summary>
    /// 简单的控制台Logger实现
    /// </summary>
    public class ConsoleLogger : ILogger<BenchSuiteDirectoryService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // 简单实现，不输出日志
        }
    }

    /// <summary>
    /// 路径配置测试类 - 验证修改后的路径配置是否符合用户要求
    /// </summary>
    public class PathConfigurationTest
    {
        /// <summary>
        /// 测试各个模块的专用基础目录路径
        /// </summary>
        public static void TestModuleBasePaths()
        {
            Console.WriteLine("=== 测试各个模块的专用基础目录路径 ===");
            
            // 创建简单的Logger实现
            ILogger<BenchSuiteDirectoryService> logger = new ConsoleLogger();
            
            // 创建BenchSuiteDirectoryService实例
            BenchSuiteDirectoryService directoryService = new(logger);
            
            // 测试基础路径
            string basePath = directoryService.GetBasePath();
            Console.WriteLine($"基础路径: {basePath}");
            Console.WriteLine($"预期路径: C:\\河北对口计算机\\SpecializedTraining\\9\\");
            Console.WriteLine($"基础路径正确: {basePath == @"C:\河北对口计算机\SpecializedTraining\9\"}");
            Console.WriteLine();
            
            // 测试各个模块的目录路径
            Dictionary<ModuleType, string> expectedPaths = new()
            {
                { ModuleType.PowerPoint, @"C:\河北对口计算机\SpecializedTraining\9\PPT\" },
                { ModuleType.Word, @"C:\河北对口计算机\SpecializedTraining\9\WORD\" },
                { ModuleType.Excel, @"C:\河北对口计算机\SpecializedTraining\9\EXCEL\" },
                { ModuleType.CSharp, @"C:\河北对口计算机\SpecializedTraining\9\CSharp\" },
                { ModuleType.Windows, @"C:\河北对口计算机\SpecializedTraining\9\Windows\" }
            };
            
            foreach (KeyValuePair<ModuleType, string> expected in expectedPaths)
            {
                try
                {
                    string actualPath = directoryService.GetDirectoryPath(expected.Key);
                    bool isCorrect = actualPath == expected.Value;
                    
                    Console.WriteLine($"模块: {expected.Key}");
                    Console.WriteLine($"  实际路径: {actualPath}");
                    Console.WriteLine($"  预期路径: {expected.Value}");
                    Console.WriteLine($"  路径正确: {isCorrect}");
                    
                    if (!isCorrect)
                    {
                        Console.WriteLine($"  ❌ 路径不匹配!");
                    }
                    else
                    {
                        Console.WriteLine($"  ✅ 路径正确");
                    }
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"模块 {expected.Key} 测试失败: {ex.Message}");
                    Console.WriteLine();
                }
            }
        }
        
        /// <summary>
        /// 测试考试目录路径生成
        /// </summary>
        public static void TestExamDirectoryPaths()
        {
            Console.WriteLine("=== 测试考试目录路径生成 ===");
            
            // 创建简单的Logger实现
            ILogger<BenchSuiteDirectoryService> logger = new ConsoleLogger();

            // 创建BenchSuiteDirectoryService实例
            BenchSuiteDirectoryService directoryService = new(logger);
            
            // 测试专项训练的考试目录路径
            ExamType examType = ExamType.SpecializedTraining;
            int examId = 9;
            
            Dictionary<ModuleType, string> expectedExamPaths = new()
            {
                { ModuleType.PowerPoint, @"C:\河北对口计算机\SpecializedTraining\9\PPT\" },
                { ModuleType.Word, @"C:\河北对口计算机\SpecializedTraining\9\WORD\" },
                { ModuleType.Excel, @"C:\河北对口计算机\SpecializedTraining\9\EXCEL\" },
                { ModuleType.CSharp, @"C:\河北对口计算机\SpecializedTraining\9\CSharp\" },
                { ModuleType.Windows, @"C:\河北对口计算机\SpecializedTraining\9\Windows\" }
            };
            
            Console.WriteLine($"考试类型: {examType}");
            Console.WriteLine($"考试ID: {examId}");
            Console.WriteLine();
            
            foreach (KeyValuePair<ModuleType, string> expected in expectedExamPaths)
            {
                try
                {
                    string actualPath = directoryService.GetExamDirectoryPath(examType, examId, expected.Key);
                    
                    Console.WriteLine($"模块: {expected.Key}");
                    Console.WriteLine($"  实际路径: {actualPath}");
                    Console.WriteLine($"  预期路径: {expected.Value}");

                    bool isCorrect = actualPath == expected.Value;
                    if (!isCorrect)
                    {
                        Console.WriteLine($"  ❌ 路径不匹配!");
                    }
                    else
                    {
                        Console.WriteLine($"  ✅ 路径正确");
                    }
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"模块 {expected.Key} 测试失败: {ex.Message}");
                    Console.WriteLine();
                }
            }
        }
        
        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("开始路径配置测试...");
            Console.WriteLine();

            TestModuleBasePaths();
            TestExamDirectoryPaths();

            Console.WriteLine("路径配置测试完成。");
        }

        /// <summary>
        /// 程序入口点
        /// </summary>
        public static void Main(string[] args)
        {
            RunAllTests();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}
