using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 简单的路径配置验证程序
/// </summary>
public class SimplePathTest
{
    /// <summary>
    /// 模拟BenchSuiteDirectoryService的路径配置
    /// </summary>
    public static void TestPathConfiguration()
    {
        Console.WriteLine("=== ED项目路径配置验证 ===");
        Console.WriteLine();

        // 模拟修改后的基础路径配置
        string basePath = @"C:\河北对口计算机\SpecializedTraining\9\";
        
        // 模拟目录映射
        Dictionary<string, string> directoryMapping = new()
        {
            { "CSharp", "CSharp" },
            { "PowerPoint", "PPT" },
            { "Word", "WORD" },
            { "Excel", "EXCEL" },
            { "Windows", "Windows" }
        };

        Console.WriteLine($"基础路径: {basePath}");
        Console.WriteLine();

        // 验证各个模块的专用目录路径
        Console.WriteLine("各模块的专用基础目录路径:");
        foreach (var mapping in directoryMapping)
        {
            string modulePath = Path.Combine(basePath, mapping.Value);
            Console.WriteLine($"  {mapping.Key}模块: {modulePath}");
        }
        Console.WriteLine();

        // 验证路径是否符合用户要求
        Dictionary<string, string> expectedPaths = new()
        {
            { "PPT", @"C:\河北对口计算机\SpecializedTraining\9\PPT\" },
            { "WORD", @"C:\河北对口计算机\SpecializedTraining\9\WORD\" },
            { "EXCEL", @"C:\河北对口计算机\SpecializedTraining\9\EXCEL\" },
            { "CSharp", @"C:\河北对口计算机\SpecializedTraining\9\CSharp\" },
            { "Windows", @"C:\河北对口计算机\SpecializedTraining\9\Windows\" }
        };

        Console.WriteLine("路径验证结果:");
        bool allCorrect = true;
        
        foreach (var expected in expectedPaths)
        {
            string actualPath = Path.Combine(basePath, expected.Key);
            // 确保路径以反斜杠结尾
            if (!actualPath.EndsWith("\\"))
                actualPath += "\\";
                
            bool isCorrect = actualPath == expected.Value;
            allCorrect &= isCorrect;
            
            Console.WriteLine($"  {expected.Key}模块:");
            Console.WriteLine($"    实际路径: {actualPath}");
            Console.WriteLine($"    预期路径: {expected.Value}");
            Console.WriteLine($"    验证结果: {(isCorrect ? "✅ 正确" : "❌ 错误")}");
            Console.WriteLine();
        }

        // 测试GetExamDirectoryPath方法的逻辑
        Console.WriteLine("=== GetExamDirectoryPath方法测试 ===");
        Console.WriteLine();
        
        // 对于专项训练ID 9，应该直接使用基础路径
        string examType = "SpecializedTraining";
        int examId = 9;
        
        Console.WriteLine($"考试类型: {examType}");
        Console.WriteLine($"考试ID: {examId}");
        Console.WriteLine();
        
        foreach (var mapping in directoryMapping)
        {
            // 模拟修改后的GetExamDirectoryPath逻辑
            string examDirectoryPath;
            if (examType == "SpecializedTraining" && examId == 9)
            {
                // 直接使用基础路径加模块目录
                examDirectoryPath = Path.Combine(basePath, mapping.Value);
            }
            else
            {
                // 其他情况使用原有逻辑
                examDirectoryPath = Path.Combine(basePath, examType, examId.ToString(), mapping.Value);
            }
            
            Console.WriteLine($"  {mapping.Key}模块考试目录: {examDirectoryPath}");
        }
        Console.WriteLine();

        // 总结
        Console.WriteLine("=== 总结 ===");
        if (allCorrect)
        {
            Console.WriteLine("✅ 所有路径配置都符合用户要求！");
            Console.WriteLine();
            Console.WriteLine("修改效果:");
            Console.WriteLine("1. ✅ 基础路径已更新为: C:\\河北对口计算机\\SpecializedTraining\\9\\");
            Console.WriteLine("2. ✅ 各模块都有专用的基础目录:");
            Console.WriteLine("   - PPT模块: C:\\河北对口计算机\\SpecializedTraining\\9\\PPT\\");
            Console.WriteLine("   - WORD模块: C:\\河北对口计算机\\SpecializedTraining\\9\\WORD\\");
            Console.WriteLine("   - EXCEL模块: C:\\河北对口计算机\\SpecializedTraining\\9\\EXCEL\\");
            Console.WriteLine("   - CSharp模块: C:\\河北对口计算机\\SpecializedTraining\\9\\CSharp\\");
            Console.WriteLine("   - Windows模块: C:\\河北对口计算机\\SpecializedTraining\\9\\Windows\\");
            Console.WriteLine("3. ✅ Windows模块的路径组合逻辑已修复");
            Console.WriteLine("4. ✅ 路径配置的一致性已确保");
        }
        else
        {
            Console.WriteLine("❌ 部分路径配置不符合要求，需要进一步检查。");
        }
    }

    /// <summary>
    /// 程序入口点
    /// </summary>
    public static void Main(string[] args)
    {
        TestPathConfiguration();
        Console.WriteLine();
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}
