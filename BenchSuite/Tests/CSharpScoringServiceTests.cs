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

        await TestCompilationCheckModeAsync();
        Console.WriteLine();

        await TestUnitTestModeAsync();
        Console.WriteLine();

        await TestCodeCompletionGraderAsync();
        Console.WriteLine();

        await TestCompilationCheckerAsync();
        Console.WriteLine();

        await TestUnitTestRunnerAsync();
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

    public int Multiply(int a, int b)
    {
        // 填空2：实现乘法
        throw new NotImplementedException();
    }
}";

        // 期望实现
        List<string> expectedImplementations = 
        [
            @"int result = a + b;
              Console.WriteLine($""结果: {result}"");
              return result;",
            
            @"return a * b;"
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

    public int Multiply(int a, int b)
    {
        return a * b;
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
    /// 测试编译检查模式
    /// </summary>
    public async Task TestCompilationCheckModeAsync()
    {
        Console.WriteLine("🔧 测试编译检查模式");

        // 正确的代码
        string correctCode = @"
using System;

class Program 
{
    static void Main() 
    {
        Console.WriteLine(""Hello, World!"");
    }

    public static int Add(int a, int b)
    {
        return a + b;
    }
}";

        // 错误的代码
        string incorrectCode = @"
using System;

class Program 
{
    static void Main() 
    {
        Console.WriteLine(""Hello, World!"");
        int result = Add(1, 2, 3); // 参数数量错误
    }

    public static int Add(int a, int b)
    {
        return a + b;
    }
}";

        try
        {
            // 测试正确代码
            Console.WriteLine("  测试正确代码:");
            CSharpScoringResult correctResult = await _csharpScoringService.ScoreCodeAsync(
                "", correctCode, [], CSharpScoringMode.CompilationCheck);

            Console.WriteLine($"    编译成功: {correctResult.CompilationResult?.IsSuccess}");
            Console.WriteLine($"    得分: {correctResult.AchievedScore}/{correctResult.TotalScore}");
            Console.WriteLine($"    详细信息: {correctResult.Details}");

            // 测试错误代码
            Console.WriteLine("  测试错误代码:");
            CSharpScoringResult incorrectResult = await _csharpScoringService.ScoreCodeAsync(
                "", incorrectCode, [], CSharpScoringMode.CompilationCheck);

            Console.WriteLine($"    编译成功: {incorrectResult.CompilationResult?.IsSuccess}");
            Console.WriteLine($"    得分: {incorrectResult.AchievedScore}/{incorrectResult.TotalScore}");
            Console.WriteLine($"    错误数量: {incorrectResult.CompilationResult?.Errors.Count}");
            Console.WriteLine($"    详细信息: {incorrectResult.Details}");

            if (correctResult.CompilationResult?.IsSuccess == true && 
                incorrectResult.CompilationResult?.IsSuccess == false)
            {
                Console.WriteLine("✅ 编译检查模式测试通过");
            }
            else
            {
                Console.WriteLine("❌ 编译检查模式测试失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 编译检查模式测试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试单元测试模式
    /// </summary>
    public async Task TestUnitTestModeAsync()
    {
        Console.WriteLine("🧪 测试单元测试模式");

        // 学生代码
        string studentCode = @"
using System;

public class Calculator 
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public int Subtract(int a, int b)
    {
        return a - b;
    }

    public int Multiply(int a, int b)
    {
        return a * b; // 正确实现
    }

    public int Divide(int a, int b)
    {
        return a + b; // 错误实现，应该是除法
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
    public void TestSubtract()
    {
        var calc = new Calculator();
        var result = calc.Subtract(5, 3);
        if (result != 2)
            throw new Exception($""Subtract test failed: expected 2, got {result}"");
    }

    [Test]
    public void TestMultiply()
    {
        var calc = new Calculator();
        var result = calc.Multiply(3, 4);
        if (result != 12)
            throw new Exception($""Multiply test failed: expected 12, got {result}"");
    }

    [Test]
    public void TestDivide()
    {
        var calc = new Calculator();
        var result = calc.Divide(8, 2);
        if (result != 4)
            throw new Exception($""Divide test failed: expected 4, got {result}"");
    }
}";

        try
        {
            CSharpScoringResult result = await _csharpScoringService.ScoreCodeAsync(
                "", studentCode, [testCode], CSharpScoringMode.UnitTest);

            Console.WriteLine($"  评分模式: {result.Mode}");
            Console.WriteLine($"  总测试数: {result.UnitTestResult?.TotalTests}");
            Console.WriteLine($"  通过测试数: {result.UnitTestResult?.PassedTests}");
            Console.WriteLine($"  失败测试数: {result.UnitTestResult?.FailedTests}");
            Console.WriteLine($"  得分: {result.AchievedScore}/{result.TotalScore}");
            Console.WriteLine($"  得分率: {result.ScoreRate:P2}");
            Console.WriteLine($"  详细信息: {result.Details}");
            Console.WriteLine($"  耗时: {result.ElapsedMilliseconds}ms");

            if (result.UnitTestResult?.TestCaseResults.Count > 0)
            {
                Console.WriteLine("  测试用例结果:");
                foreach (TestCaseResult testCase in result.UnitTestResult.TestCaseResults)
                {
                    Console.WriteLine($"    {testCase.TestName}: {(testCase.Passed ? "✅" : "❌")}");
                    if (!testCase.Passed && !string.IsNullOrEmpty(testCase.ErrorMessage))
                    {
                        Console.WriteLine($"      错误: {testCase.ErrorMessage}");
                    }
                }
            }

            if (result.IsSuccess)
            {
                Console.WriteLine("✅ 单元测试模式测试通过");
            }
            else
            {
                Console.WriteLine("⚠️ 单元测试模式测试部分通过");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 单元测试模式测试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试代码补全评分器
    /// </summary>
    public async Task TestCodeCompletionGraderAsync()
    {
        Console.WriteLine("🔍 测试代码补全评分器");

        string template = @"
class Demo {
    public int Add(int a, int b) {
        Console.WriteLine(1);
        // 填空1
        throw new NotImplementedException();
    }
}";

        List<string> expected =
        [
            @"int s = a + b;
              Console.WriteLine(s);
              return s;"
        ];

        string student = @"
class Demo {
    public int Add(int a, int b) {
        Console.WriteLine(1);
        int s = a + b;
        Console.WriteLine(s);
        return s;
    }
}";

        try
        {
            List<FillBlankResult> results = await _csharpScoringService.DetectFillBlanksAsync(template, student, expected);

            Console.WriteLine("  评分结果:");
            foreach (FillBlankResult r in results)
            {
                Console.WriteLine($"    Blank #{r.BlankIndex} @ {r.Descriptor.LocationSummary}");
                Console.WriteLine($"      结果: {(r.Matched ? "通过✅" : "未通过❌")} 说明: {r.Message}");
            }

            if (results.All(r => r.Matched))
            {
                Console.WriteLine("✅ 代码补全评分器测试通过");
            }
            else
            {
                Console.WriteLine("❌ 代码补全评分器测试失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 代码补全评分器测试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试编译检查器
    /// </summary>
    public async Task TestCompilationCheckerAsync()
    {
        Console.WriteLine("⚙️ 测试编译检查器");

        string validCode = @"
using System;
class Test {
    static void Main() {
        Console.WriteLine(""Hello"");
    }
}";

        string invalidCode = @"
using System;
class Test {
    static void Main() {
        Console.WriteLine(""Hello"" // 缺少右括号
    }
}";

        try
        {
            // 测试有效代码
            CompilationResult validResult = await _csharpScoringService.CompileCodeAsync(validCode);
            Console.WriteLine($"  有效代码编译: {(validResult.IsSuccess ? "成功✅" : "失败❌")}");
            Console.WriteLine($"    耗时: {validResult.CompilationTimeMs}ms");

            // 测试无效代码
            CompilationResult invalidResult = await _csharpScoringService.CompileCodeAsync(invalidCode);
            Console.WriteLine($"  无效代码编译: {(invalidResult.IsSuccess ? "成功✅" : "失败❌")}");
            Console.WriteLine($"    错误数量: {invalidResult.Errors.Count}");

            if (validResult.IsSuccess && !invalidResult.IsSuccess)
            {
                Console.WriteLine("✅ 编译检查器测试通过");
            }
            else
            {
                Console.WriteLine("❌ 编译检查器测试失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 编译检查器测试异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试单元测试运行器
    /// </summary>
    public async Task TestUnitTestRunnerAsync()
    {
        Console.WriteLine("🏃 测试单元测试运行器");

        string studentCode = @"
public class MathHelper
{
    public static int Add(int a, int b) => a + b;
    public static int Subtract(int a, int b) => a - b;
}";

        string testCode = @"
public class MathTests
{
    [Test]
    public void TestAdd()
    {
        if (MathHelper.Add(2, 3) != 5)
            throw new Exception(""Add failed"");
    }

    [Test]
    public void TestSubtract()
    {
        if (MathHelper.Subtract(5, 3) != 2)
            throw new Exception(""Subtract failed"");
    }
}";

        try
        {
            UnitTestResult result = await _csharpScoringService.RunUnitTestsAsync(studentCode, testCode);

            Console.WriteLine($"  测试运行: {(result.IsSuccess ? "成功✅" : "失败❌")}");
            Console.WriteLine($"  总测试数: {result.TotalTests}");
            Console.WriteLine($"  通过数: {result.PassedTests}");
            Console.WriteLine($"  失败数: {result.FailedTests}");
            Console.WriteLine($"  耗时: {result.ExecutionTimeMs}ms");

            if (result.IsSuccess && result.PassedTests > 0)
            {
                Console.WriteLine("✅ 单元测试运行器测试通过");
            }
            else
            {
                Console.WriteLine("❌ 单元测试运行器测试失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 单元测试运行器测试异常: {ex.Message}");
        }
    }
}
