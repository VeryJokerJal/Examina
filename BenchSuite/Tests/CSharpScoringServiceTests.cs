using BenchSuite.Services;
using BenchSuite.Models;

namespace BenchSuite.Tests;

/// <summary>
/// C#编程题打分服务测试类
/// </summary>
public class CSharpScoringServiceTests
{
    private readonly CSharpScoringService _csharpScoringService;

    public CSharpScoringServiceTests()
    {
        _csharpScoringService = new CSharpScoringService();
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    public async Task RunAllTestsAsync()
    {
        Console.WriteLine("=== C#编程题打分服务测试 ===");
        Console.WriteLine();

        await TestCodeCompletionModeAsync();
        Console.WriteLine();

        await TestDebuggingModeAsync();
        Console.WriteLine();

        await TestImplementationModeAsync();
        Console.WriteLine();

        Console.WriteLine("=== 所有测试完成 ===");
    }

    /// <summary>
    /// 测试代码补全模式
    /// </summary>
    public async Task TestCodeCompletionModeAsync()
    {
        Console.WriteLine("📝 测试代码补全模式");

        // 模板代码
        string template = @"
using System;

class Calculator
{
    public int Add(int a, int b)
    {
        Console.WriteLine(""计算中..."");
        // 填空1：实现加法
        throw new NotImplementedException();
    }
}";

        // 期望实现
        List<string> expectedImplementations =
        [
            @"int result = a + b;
              Console.WriteLine($""结果: {result}"");
              return result;"
        ];

        // 学生代码
        string studentCode = @"
using System;

class Calculator
{
    public int Add(int a, int b)
    {
        Console.WriteLine(""计算中..."");
        int result = a + b;
        Console.WriteLine($""结果: {result}"");
        return result;
    }
}";

        try
        {
            CSharpScoringResult result = await _csharpScoringService.ScoreCodeAsync(
                template, studentCode, expectedImplementations, CSharpScoringMode.CodeCompletion);

            Console.WriteLine($"  评分模式: {result.Mode}");
            Console.WriteLine($"  总分: {result.TotalScore}");
            Console.WriteLine($"  得分: {result.AchievedScore}");
            Console.WriteLine($"  得分率: {result.ScoreRate:P2}");
            Console.WriteLine($"  是否成功: {result.IsSuccess}");
            Console.WriteLine($"  详细信息: {result.Details}");
            Console.WriteLine($"  耗时: {result.ElapsedMilliseconds}ms");

            Console.WriteLine("  填空结果:");
            foreach (FillBlankResult fillResult in result.FillBlankResults)
            {
                Console.WriteLine($"    填空 #{fillResult.BlankIndex + 1} @ {fillResult.Descriptor.LocationSummary}");
                Console.WriteLine($"      匹配: {(fillResult.Matched ? "✅" : "❌")}");
                Console.WriteLine($"      消息: {fillResult.Message}");
            }

            if (result.IsSuccess && result.AchievedScore == result.TotalScore)
            {
                Console.WriteLine("✅ 代码补全模式测试通过");
            }
            else
            {
                Console.WriteLine("⚠️ 代码补全模式测试部分通过");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 代码补全模式测试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试调试纠错模式
    /// </summary>
    public async Task TestDebuggingModeAsync()
    {
        Console.WriteLine("🐛 测试调试纠错模式");

        // 包含错误的代码
        string buggyCode = @"
using System;

class Calculator
{
    public int Add(int a, int b)
    {
        return a - b; // 错误：应该是加法，不是减法
    }
}";

        // 学生修复后的代码
        string fixedCode = @"
using System;

class Calculator
{
    public int Add(int a, int b)
    {
        return a + b; // 修复：改为正确的加法
    }
}";

        // 期望发现的错误
        List<string> expectedErrors =
        [
            "减法错误"
        ];

        try
        {
            CSharpScoringResult result = await _csharpScoringService.ScoreCodeAsync(
                buggyCode, fixedCode, expectedErrors, CSharpScoringMode.Debugging);

            Console.WriteLine($"  评分模式: {result.Mode}");
            Console.WriteLine($"  总错误数: {result.DebuggingResult?.TotalErrors}");
            Console.WriteLine($"  已修复错误数: {result.DebuggingResult?.FixedErrors}");
            Console.WriteLine($"  剩余错误数: {result.DebuggingResult?.RemainingErrors}");
            Console.WriteLine($"  得分: {result.AchievedScore}/{result.TotalScore}");
            Console.WriteLine($"  详细信息: {result.Details}");

            if (result.IsSuccess)
            {
                Console.WriteLine("✅ 调试纠错模式测试通过");
            }
            else
            {
                Console.WriteLine("⚠️ 调试纠错模式测试部分通过");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 调试纠错模式测试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试编写实现模式
    /// </summary>
    public async Task TestImplementationModeAsync()
    {
        Console.WriteLine("💻 测试编写实现模式");

        // 学生代码
        string studentCode = @"
using System;

public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public int Multiply(int a, int b)
    {
        return a * b;
    }
}";

        // 测试代码
        string testCode = @"
public class CalculatorTests
{
    [Test]
    public void TestAdd()
    {
        var calc = new Calculator();
        var result = calc.Add(2, 3);
        if (result != 5)
            throw new Exception($""Add test failed: expected 5, got {result}"");
    }

    [Test]
    public void TestMultiply()
    {
        var calc = new Calculator();
        var result = calc.Multiply(3, 4);
        if (result != 12)
            throw new Exception($""Multiply test failed: expected 12, got {result}"");
    }
}";

        try
        {
            CSharpScoringResult result = await _csharpScoringService.ScoreCodeAsync(
                "", studentCode, [testCode], CSharpScoringMode.Implementation);

            Console.WriteLine($"  评分模式: {result.Mode}");
            Console.WriteLine($"  编译成功: {result.CompilationResult?.IsSuccess}");
            Console.WriteLine($"  总测试数: {result.UnitTestResult?.TotalTests}");
            Console.WriteLine($"  通过测试数: {result.UnitTestResult?.PassedTests}");
            Console.WriteLine($"  得分: {result.AchievedScore}/{result.TotalScore}");
            Console.WriteLine($"  详细信息: {result.Details}");

            if (result.IsSuccess)
            {
                Console.WriteLine("✅ 编写实现模式测试通过");
            }
            else
            {
                Console.WriteLine("⚠️ 编写实现模式测试部分通过");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 编写实现模式测试异常: {ex.Message}");
        }
    }
}