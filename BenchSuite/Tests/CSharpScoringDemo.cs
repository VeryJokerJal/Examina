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

            Console.WriteLine();
            Console.WriteLine("📊 演示不同难度的题目");
            Console.WriteLine("-".PadRight(50, '-'));
            
            await DemoVariousDifficultyLevelsAsync();
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

        // 2. 编译检查模式
        Console.WriteLine("\n2️⃣ 编译检查模式评分:");
        var compilationResult = await service.ScoreCodeAsync("", studentCode, [], CSharpScoringMode.CompilationCheck);
        Console.WriteLine($"   编译: {(compilationResult.CompilationResult?.IsSuccess == true ? "成功✅" : "失败❌")}");
        Console.WriteLine($"   耗时: {compilationResult.CompilationResult?.CompilationTimeMs}ms");

        // 3. 单元测试模式
        Console.WriteLine("\n3️⃣ 单元测试模式评分:");
        var testResult = await service.ScoreCodeAsync("", studentCode, [testCode], CSharpScoringMode.UnitTest);
        Console.WriteLine($"   测试: {testResult.UnitTestResult?.PassedTests}/{testResult.UnitTestResult?.TotalTests} 通过");
        Console.WriteLine($"   状态: {(testResult.UnitTestResult?.IsSuccess == true ? "全部通过✅" : "部分失败❌")}");

        // 综合评分
        decimal totalScore = (completionResult.AchievedScore / completionResult.TotalScore) * 40 +
                           (compilationResult.CompilationResult?.IsSuccess == true ? 20 : 0) +
                           ((decimal)(testResult.UnitTestResult?.PassedTests ?? 0) / (testResult.UnitTestResult?.TotalTests ?? 1)) * 40;

        Console.WriteLine($"\n🎯 综合评分: {totalScore:F1}/100");
        Console.WriteLine($"   等级: {GetGradeLevel(totalScore)}");
    }

    /// <summary>
    /// 演示不同难度级别的题目
    /// </summary>
    private static async Task DemoVariousDifficultyLevelsAsync()
    {
        CSharpScoringService service = new();

        // 初级题目：简单计算
        Console.WriteLine("🟢 初级题目：简单加法计算器");
        await DemoBasicCalculatorAsync(service);

        Console.WriteLine();

        // 中级题目：数组操作
        Console.WriteLine("🟡 中级题目：数组排序");
        await DemoArraySortingAsync(service);

        Console.WriteLine();

        // 高级题目：算法实现
        Console.WriteLine("🔴 高级题目：斐波那契数列");
        await DemoFibonacciAsync(service);
    }

    /// <summary>
    /// 演示基础计算器题目
    /// </summary>
    private static async Task DemoBasicCalculatorAsync(CSharpScoringService service)
    {
        string studentCode = @"
public class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
}";

        string testCode = @"
public class CalculatorTests
{
    [Test]
    public void TestBasicOperations()
    {
        var calc = new Calculator();
        if (calc.Add(2, 3) != 5) throw new Exception(""Add failed"");
        if (calc.Subtract(5, 2) != 3) throw new Exception(""Subtract failed"");
    }
}";

        var result = await service.ScoreCodeAsync("", studentCode, [testCode], CSharpScoringMode.UnitTest);
        Console.WriteLine($"   测试结果: {result.UnitTestResult?.PassedTests}/{result.UnitTestResult?.TotalTests} 通过");
        Console.WriteLine($"   难度评估: 简单 ⭐");
    }

    /// <summary>
    /// 演示数组排序题目
    /// </summary>
    private static async Task DemoArraySortingAsync(CSharpScoringService service)
    {
        string studentCode = @"
using System;

public class ArrayHelper
{
    public static int[] BubbleSort(int[] array)
    {
        if (array == null || array.Length <= 1) return array;
        
        int[] result = new int[array.Length];
        Array.Copy(array, result, array.Length);
        
        for (int i = 0; i < result.Length - 1; i++)
        {
            for (int j = 0; j < result.Length - 1 - i; j++)
            {
                if (result[j] > result[j + 1])
                {
                    int temp = result[j];
                    result[j] = result[j + 1];
                    result[j + 1] = temp;
                }
            }
        }
        return result;
    }
}";

        string testCode = @"
using System.Linq;

public class ArrayHelperTests
{
    [Test]
    public void TestBubbleSort()
    {
        var input = new int[] { 3, 1, 4, 1, 5, 9, 2, 6 };
        var expected = new int[] { 1, 1, 2, 3, 4, 5, 6, 9 };
        var result = ArrayHelper.BubbleSort(input);
        
        if (!result.SequenceEqual(expected))
            throw new Exception(""Sort failed"");
    }
}";

        var result = await service.ScoreCodeAsync("", studentCode, [testCode], CSharpScoringMode.UnitTest);
        Console.WriteLine($"   测试结果: {result.UnitTestResult?.PassedTests}/{result.UnitTestResult?.TotalTests} 通过");
        Console.WriteLine($"   难度评估: 中等 ⭐⭐⭐");
    }

    /// <summary>
    /// 演示斐波那契数列题目
    /// </summary>
    private static async Task DemoFibonacciAsync(CSharpScoringService service)
    {
        string studentCode = @"
public class MathHelper
{
    public static long Fibonacci(int n)
    {
        if (n <= 1) return n;
        
        long a = 0, b = 1;
        for (int i = 2; i <= n; i++)
        {
            long temp = a + b;
            a = b;
            b = temp;
        }
        return b;
    }
}";

        string testCode = @"
public class MathHelperTests
{
    [Test]
    public void TestFibonacci()
    {
        if (MathHelper.Fibonacci(0) != 0) throw new Exception(""Fib(0) failed"");
        if (MathHelper.Fibonacci(1) != 1) throw new Exception(""Fib(1) failed"");
        if (MathHelper.Fibonacci(10) != 55) throw new Exception(""Fib(10) failed"");
        if (MathHelper.Fibonacci(20) != 6765) throw new Exception(""Fib(20) failed"");
    }
}";

        var result = await service.ScoreCodeAsync("", studentCode, [testCode], CSharpScoringMode.UnitTest);
        Console.WriteLine($"   测试结果: {result.UnitTestResult?.PassedTests}/{result.UnitTestResult?.TotalTests} 通过");
        Console.WriteLine($"   难度评估: 困难 ⭐⭐⭐⭐⭐");
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
