using System;
using System.Collections.Generic;

namespace BenchSuite.Console.TestData
{
    /// <summary>
    /// 验证参数映射修复的测试类
    /// </summary>
    public static class ParameterMappingTest
    {
        /// <summary>
        /// 测试路径格式标准化
        /// </summary>
        public static void TestPathNormalization()
        {
            Console.WriteLine("=== 路径格式标准化测试 ===");

            // 测试用例：EL导出的路径格式
            string[] testPaths = {
                "\\WINDOWS\\calc.exe",           // EL导出格式
                "\\WINDOWS\\2",                  // EL导出目录格式
                "\\WINDOWS\\chcp.com",           // EL导出文件格式
                "C:\\Windows\\System32\\cmd.exe", // 标准格式
                "Windows/System32/notepad.exe",  // Unix风格分隔符
                "relative\\path\\file.txt"       // 相对路径
            };

            foreach (string testPath in testPaths)
            {
                string normalizedPath = NormalizePath(testPath);
                Console.WriteLine($"原始路径: {testPath}");
                Console.WriteLine($"标准化后: {normalizedPath}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 测试参数映射
        /// </summary>
        public static void TestParameterMapping()
        {
            Console.WriteLine("=== 参数映射测试 ===");

            // 测试删除操作参数映射
            Console.WriteLine("1. 删除操作参数映射测试:");
            
            // EL导出格式的参数
            Dictionary<string, string> deleteParams = new()
            {
                { "TargetPath", "\\WINDOWS\\calc.exe" }
            };

            bool deleteParamFound = TestDeleteFileParameters(deleteParams);
            Console.WriteLine($"删除操作参数映射: {(deleteParamFound ? "✅ 成功" : "❌ 失败")}");
            Console.WriteLine();

            // 测试复制重命名操作参数映射
            Console.WriteLine("2. 复制重命名操作参数映射测试:");
            
            // EL导出格式的参数
            Dictionary<string, string> copyRenameParams = new()
            {
                { "SourcePath", "\\WINDOWS\\chcp.com" },
                { "DestinationPath", "\\WINDOWS\\2" },
                { "NewName", "chcp2.com" }
            };

            bool copyRenameParamFound = TestCopyAndRenameParameters(copyRenameParams);
            Console.WriteLine($"复制重命名操作参数映射: {(copyRenameParamFound ? "✅ 成功" : "❌ 失败")}");
        }

        /// <summary>
        /// 模拟DetectDeleteFile的参数检查逻辑
        /// </summary>
        private static bool TestDeleteFileParameters(Dictionary<string, string> parameters)
        {
            // 支持多种参数名称：FilePath（BS标准）和TargetPath（EL导出）
            string? filePath = null;
            if (parameters.TryGetValue("FilePath", out filePath) && !string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine($"  找到FilePath参数: {filePath}");
                return true;
            }
            else if (parameters.TryGetValue("TargetPath", out filePath) && !string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine($"  找到TargetPath参数: {filePath}");
                string normalizedPath = NormalizePath(filePath);
                Console.WriteLine($"  标准化后路径: {normalizedPath}");
                return true;
            }
            else
            {
                Console.WriteLine("  ❌ 缺少文件路径参数（FilePath或TargetPath）");
                return false;
            }
        }

        /// <summary>
        /// 模拟DetectCopyAndRename的参数检查逻辑
        /// </summary>
        private static bool TestCopyAndRenameParameters(Dictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue("SourcePath", out string? sourcePath) || string.IsNullOrEmpty(sourcePath))
            {
                Console.WriteLine("  ❌ 缺少源路径参数");
                return false;
            }

            if (!parameters.TryGetValue("DestinationPath", out string? destinationPath) || string.IsNullOrEmpty(destinationPath))
            {
                Console.WriteLine("  ❌ 缺少目标路径参数");
                return false;
            }

            if (!parameters.TryGetValue("NewName", out string? newName) || string.IsNullOrEmpty(newName))
            {
                Console.WriteLine("  ❌ 缺少新名称参数");
                return false;
            }

            Console.WriteLine($"  找到SourcePath参数: {sourcePath}");
            Console.WriteLine($"  找到DestinationPath参数: {destinationPath}");
            Console.WriteLine($"  找到NewName参数: {newName}");

            // 处理路径格式兼容性
            string normalizedSourcePath = NormalizePath(sourcePath);
            string normalizedDestinationPath = NormalizePath(destinationPath);

            Console.WriteLine($"  标准化源路径: {normalizedSourcePath}");
            Console.WriteLine($"  标准化目标路径: {normalizedDestinationPath}");

            return true;
        }

        /// <summary>
        /// 模拟NormalizePath方法
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            // 处理EL导出的路径格式：\WINDOWS\calc.exe -> C:\WINDOWS\calc.exe
            if (path.StartsWith("\\") && !path.StartsWith("\\\\"))
            {
                // 如果路径以单个反斜杠开头，添加C:前缀
                path = "C:" + path;
            }

            // 标准化路径分隔符
            path = path.Replace('/', '\\');

            // 处理相对路径（简化版本，实际实现会使用Path.GetFullPath）
            if (!path.Contains(":") && !path.StartsWith("\\\\"))
            {
                // 如果是相对路径，添加当前目录前缀（简化处理）
                path = "C:\\CurrentDirectory\\" + path;
            }

            return path;
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("开始参数映射修复验证测试...\n");
            
            TestPathNormalization();
            Console.WriteLine(new string('=', 50));
            TestParameterMapping();
            
            Console.WriteLine("\n测试完成！");
        }
    }
}
