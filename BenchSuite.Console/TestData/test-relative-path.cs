using System;
using System.Collections.Generic;
using System.IO;
using BenchSuite.Services;

namespace BenchSuite.Console.TestData
{
    /// <summary>
    /// 相对路径处理功能测试类
    /// </summary>
    public static class RelativePathTest
    {
        /// <summary>
        /// 测试相对路径标准化功能
        /// </summary>
        public static void TestPathNormalization()
        {
            Console.WriteLine("=== 相对路径标准化测试 ===");

            // 创建WindowsScoringService实例
            WindowsScoringService service = new();

            // 测试用例1：不设置基础路径
            Console.WriteLine("1. 不设置基础路径的情况:");
            TestPathNormalizationWithBasePath(service, null, [
                "\\TestFiles\\test-file.txt",
                "\\TestFiles\\Backup",
                "\\WINDOWS\\calc.exe"
            ]);

            Console.WriteLine();

            // 测试用例2：设置基础路径
            Console.WriteLine("2. 设置基础路径的情况:");
            string testBasePath = "D:\\TestEnvironment";
            service.SetBasePath(testBasePath);
            TestPathNormalizationWithBasePath(service, testBasePath, [
                "\\TestFiles\\test-file.txt",
                "\\TestFiles\\Backup",
                "\\WINDOWS\\calc.exe",
                "relative\\path\\file.txt"
            ]);
        }

        /// <summary>
        /// 测试指定基础路径下的路径标准化
        /// </summary>
        private static void TestPathNormalizationWithBasePath(WindowsScoringService service, string? basePath, string[] testPaths)
        {
            Console.WriteLine($"基础路径: {basePath ?? "未设置"}");
            
            foreach (string testPath in testPaths)
            {
                // 由于NormalizePath是私有方法，我们通过参数检测来间接测试
                Dictionary<string, string> parameters = new()
                {
                    { "TargetPath", testPath }
                };

                try
                {
                    // 通过DetectKnowledgePointAsync间接测试路径标准化
                    var result = service.DetectKnowledgePointAsync("DeleteFile", parameters).Result;
                    
                    Console.WriteLine($"  原始路径: {testPath}");
                    Console.WriteLine($"  处理结果: {(result.ErrorMessage?.Contains("不存在") == true ? "路径已标准化" : result.ErrorMessage ?? "处理成功")}");
                    
                    // 从错误信息中提取标准化后的路径
                    if (result.Details != null && result.Details.Contains("文件"))
                    {
                        Console.WriteLine($"  详细信息: {result.Details}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  处理失败: {ex.Message}");
                }
                
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试命令行参数解析
        /// </summary>
        public static void TestCommandLineArguments()
        {
            Console.WriteLine("=== 命令行参数解析测试 ===");

            // 测试用例
            string[][] testArgs = [
                ["exam.json"],
                ["exam.json", "--base-path", "D:\\TestEnvironment"],
                ["exam.json", "-bp", "C:\\Projects\\Test"],
                ["exam.json", "--base-path", "D:\\TestEnvironment", "--other-option", "value"]
            ];

            foreach (string[] args in testArgs)
            {
                Console.WriteLine($"测试参数: [{string.Join(", ", args)}]");
                
                try
                {
                    var result = ParseTestArguments(args);
                    Console.WriteLine($"  试卷文件: {result.examFilePath}");
                    Console.WriteLine($"  基础路径: {result.basePath ?? "未设置"}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  解析失败: {ex.Message}");
                }
                
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 模拟命令行参数解析逻辑
        /// </summary>
        private static (string examFilePath, string? basePath) ParseTestArguments(string[] args)
        {
            string examFilePath;
            string? basePath = null;

            if (args.Length >= 1)
            {
                examFilePath = args[0];

                // 解析可选的基础路径参数
                for (int i = 1; i < args.Length; i++)
                {
                    if ((args[i] == "--base-path" || args[i] == "-bp") && i + 1 < args.Length)
                    {
                        basePath = args[i + 1];
                        break;
                    }
                }
            }
            else
            {
                throw new ArgumentException("缺少试卷文件路径参数");
            }

            return (examFilePath, basePath);
        }

        /// <summary>
        /// 创建测试环境
        /// </summary>
        public static void CreateTestEnvironment(string basePath)
        {
            Console.WriteLine($"=== 创建测试环境: {basePath} ===");

            try
            {
                // 创建测试目录
                string testFilesDir = Path.Combine(basePath, "TestFiles");
                string backupDir = Path.Combine(testFilesDir, "Backup");

                Directory.CreateDirectory(testFilesDir);
                Directory.CreateDirectory(backupDir);

                // 创建测试文件
                string sourceFile = Path.Combine(testFilesDir, "source.txt");
                string testFile = Path.Combine(testFilesDir, "test-file.txt");

                File.WriteAllText(sourceFile, "这是源文件内容");
                File.WriteAllText(testFile, "这是测试文件内容");

                Console.WriteLine($"✅ 测试环境创建成功:");
                Console.WriteLine($"   目录: {testFilesDir}");
                Console.WriteLine($"   目录: {backupDir}");
                Console.WriteLine($"   文件: {sourceFile}");
                Console.WriteLine($"   文件: {testFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试环境创建失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理测试环境
        /// </summary>
        public static void CleanupTestEnvironment(string basePath)
        {
            Console.WriteLine($"=== 清理测试环境: {basePath} ===");

            try
            {
                if (Directory.Exists(basePath))
                {
                    Directory.Delete(basePath, true);
                    Console.WriteLine("✅ 测试环境清理完成");
                }
                else
                {
                    Console.WriteLine("ℹ️ 测试环境不存在，无需清理");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试环境清理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("开始相对路径处理功能测试...\n");

            // 测试路径标准化
            TestPathNormalization();
            Console.WriteLine(new string('=', 50));

            // 测试命令行参数解析
            TestCommandLineArguments();
            Console.WriteLine(new string('=', 50));

            // 测试环境管理
            string testBasePath = Path.Combine(Path.GetTempPath(), "BenchSuite_RelativePathTest");
            CreateTestEnvironment(testBasePath);
            Console.WriteLine();
            CleanupTestEnvironment(testBasePath);

            Console.WriteLine("\n所有测试完成！");
        }
    }
}
