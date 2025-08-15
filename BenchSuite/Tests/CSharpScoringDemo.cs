using BenchSuite.Services;
using BenchSuite.Tests;

namespace BenchSuite.Tests;

/// <summary>
/// C#编程题打分功能演示程序
/// </summary>
public class CSharpScoringDemo
{
    /// <summary>
    /// 主演示方法
    /// </summary>
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🎯 C#编程题打分系统演示");
        Console.WriteLine("=".PadRight(50, '='));
        Console.WriteLine();

        try
        {
            // 运行基础测试
            CSharpScoringServiceTests tests = new();
            await tests.RunAllTestsAsync();

            Console.WriteLine();
            Console.WriteLine("🚀 演示完整评分流程");
            Console.WriteLine("-".PadRight(50, '-'));
            
            await DemoCompleteWorkflowAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 演示过程中发生异常: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }

        Console.WriteLine();
        Console.WriteLine("演示完成，按任意键退出...");
        Console.ReadKey();
    }

    /// <summary>
    /// 演示完整的评分工作流程
    /// </summary>
    private static async Task DemoCompleteWorkflowAsync()
    {
        CSharpScoringService service = new();

        // 题目：实现字符串反转功能
        Console.WriteLine("📝 题目：实现字符串反转功能");

        string template = @"
using System;

public class StringHelper
{
    /// <summary>
    /// 反转字符串
    /// </summary>
    /// <param name=""input"">输入字符串</param>
    /// <returns>反转后的字符串</returns>
    public static string Reverse(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        // TODO: 实现字符串反转逻辑
        throw new NotImplementedException();
    }
}";

        string studentCode = @"
using System;

public class StringHelper
{
    public static string Reverse(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        char[] chars = input.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }
}";

        List<string> expectedImplementations = 
        [
            @"char[] chars = input.ToCharArray();
              Array.Reverse(chars);
              return new string(chars);"
        ];

        string testCode = @"
public class StringHelperTests
{
    [Test]
    public void TestReverse()
    {
        if (StringHelper.Reverse(""hello"") != ""olleh"")
            throw new Exception(""Reverse test failed"");
            
        if (StringHelper.Reverse(""abc"") != ""cba"")
            throw new Exception(""Reverse test failed"");
            
        if (StringHelper.Reverse("""") != """")
            throw new Exception(""Empty string test failed"");
    }
}";

        // 1. 代码补全模式
        Console.WriteLine("\n1️⃣ 代码补全模式评分:");
        var completionResult = await service.ScoreCodeAsync(template, studentCode, expectedImplementations, CSharpScoringMode.CodeCompletion);
        Console.WriteLine($"   得分: {completionResult.AchievedScore}/{completionResult.TotalScore}");
        Console.WriteLine($"   状态: {(completionResult.AchievedScore == completionResult.TotalScore ? "完全正确✅" : "部分正确⚠️")}");

        // 2. 调试纠错模式（模拟）
        Console.WriteLine("\n2️⃣ 调试纠错模式评分:");
        string buggyTemplate = template.Replace("Array.Reverse(chars);", "// 这里有错误");
        var debuggingResult = await service.ScoreCodeAsync(buggyTemplate, studentCode, ["缺少实现"], CSharpScoringMode.Debugging);
        Console.WriteLine($"   修复: {debuggingResult.DebuggingResult?.FixedErrors}/{debuggingResult.DebuggingResult?.TotalErrors} 个错误");
        Console.WriteLine($"   状态: {(debuggingResult.DebuggingResult?.IsSuccess == true ? "全部修复✅" : "部分修复⚠️")}");

        // 3. 编写实现模式
        Console.WriteLine("\n3️⃣ 编写实现模式评分:");
        var implementationResult = await service.ScoreCodeAsync("", studentCode, [testCode], CSharpScoringMode.Implementation);
        Console.WriteLine($"   编译: {(implementationResult.CompilationResult?.IsSuccess == true ? "成功✅" : "失败❌")}");
        Console.WriteLine($"   测试: {implementationResult.UnitTestResult?.PassedTests}/{implementationResult.UnitTestResult?.TotalTests} 通过");
        Console.WriteLine($"   状态: {(implementationResult.UnitTestResult?.IsSuccess == true ? "全部通过✅" : "部分失败❌")}");

        // 综合评分
        decimal totalScore = (completionResult.AchievedScore / Math.Max(completionResult.TotalScore, 1)) * 30 +
                           (debuggingResult.AchievedScore / Math.Max(debuggingResult.TotalScore, 1)) * 30 +
                           (implementationResult.AchievedScore / Math.Max(implementationResult.TotalScore, 1)) * 40;

        Console.WriteLine($"\n🎯 综合评分: {totalScore:F1}/100");
        Console.WriteLine($"   等级: {GetGradeLevel(totalScore)}");
    }

    /// <summary>
    /// 获取等级评定
    /// </summary>
    private static string GetGradeLevel(decimal score)
    {
        return score switch
        {
            >= 90 => "优秀 (A)",
            >= 80 => "良好 (B)",
            >= 70 => "中等 (C)",
            >= 60 => "及格 (D)",
            _ => "不及格 (F)"
        };
    }
}
