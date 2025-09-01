using BenchSuite.Services;
using BenchSuite.Models;
using System.Text;

namespace BenchSuite.Tests;

/// <summary>
/// C# MSBuild编译器单元测试
/// </summary>
public static class CSharpMSBuildCompilerTests
{
    /// <summary>
    /// 运行所有测试
    /// </summary>
    /// <returns>测试结果</returns>
    public static async Task<TestSuiteResult> RunAllTestsAsync()
    {
        TestSuiteResult result = new()
        {
            SuiteName = "CSharpMSBuildCompiler Tests",
            StartTime = DateTime.Now
        };

        List<TestCaseResult> testResults = [];

        // 基本编译测试
        testResults.Add(await TestBasicCompilationAsync());
        testResults.Add(await TestCompilationWithErrorsAsync());
        testResults.Add(await TestCompilationWithWarningsAsync());
        testResults.Add(await TestMultipleTargetFrameworksAsync());
        testResults.Add(await TestCodeExecutionAsync());
        testResults.Add(await TestDiagnosticsEnhancementAsync());
        testResults.Add(await TestTargetFrameworkValidationAsync());
        testResults.Add(await TestPerformanceAsync());

        result.TestCases = testResults;
        result.EndTime = DateTime.Now;
        result.TotalTests = testResults.Count;
        result.PassedTests = testResults.Count(t => t.Passed);
        result.FailedTests = testResults.Count(t => !t.Passed);
        result.IsSuccess = result.FailedTests == 0;

        return result;
    }

    /// <summary>
    /// 测试基本编译功能
    /// </summary>
    private static async Task<TestCaseResult> TestBasicCompilationAsync()
    {
        TestCaseResult result = new() { TestName = "基本编译测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string sourceCode = @"
using System;

public class HelloWorld
{
    public static void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
}";

            CompilationResult compilationResult = await CSharpMSBuildCompiler.CompileCodeAsync(sourceCode);

            result.Passed = compilationResult.IsSuccess && compilationResult.AssemblyBytes != null;
            result.Output = $"编译成功: {compilationResult.IsSuccess}, 程序集大小: {compilationResult.AssemblyBytes?.Length ?? 0} bytes";
            
            if (!result.Passed)
            {
                result.ErrorMessage = $"编译失败: {compilationResult.Details}";
                if (compilationResult.Errors.Count > 0)
                {
                    result.ErrorMessage += "\n错误: " + string.Join(", ", compilationResult.Errors.Select(e => e.Message));
                }
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试编译错误处理
    /// </summary>
    private static async Task<TestCaseResult> TestCompilationWithErrorsAsync()
    {
        TestCaseResult result = new() { TestName = "编译错误处理测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string sourceCode = @"
using System;

public class ErrorTest
{
    public static void Main()
    {
        Console.WriteLine(""Missing semicolon"")  // 缺少分号
        undeclaredVariable = 5; // 未声明的变量
    }
}";

            CompilationResult compilationResult = await CSharpMSBuildCompiler.CompileCodeAsync(sourceCode);

            result.Passed = !compilationResult.IsSuccess && compilationResult.Errors.Count > 0;
            result.Output = $"编译失败（预期）: {!compilationResult.IsSuccess}, 错误数量: {compilationResult.Errors.Count}";
            
            if (result.Passed)
            {
                result.Output += "\n检测到的错误: " + string.Join(", ", compilationResult.Errors.Select(e => e.Code));
            }
            else
            {
                result.ErrorMessage = "应该检测到编译错误但没有检测到";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试编译警告处理
    /// </summary>
    private static async Task<TestCaseResult> TestCompilationWithWarningsAsync()
    {
        TestCaseResult result = new() { TestName = "编译警告处理测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string sourceCode = @"
using System;

public class WarningTest
{
    public static void Main()
    {
        int unusedVariable = 42; // 未使用的变量
        Console.WriteLine(""Hello, World!"");
    }
}";

            CompilationResult compilationResult = await CSharpMSBuildCompiler.CompileCodeAsync(sourceCode);

            result.Passed = compilationResult.IsSuccess;
            result.Output = $"编译成功: {compilationResult.IsSuccess}, 警告数量: {compilationResult.Warnings.Count}";
            
            if (compilationResult.Warnings.Count > 0)
            {
                result.Output += "\n检测到的警告: " + string.Join(", ", compilationResult.Warnings.Select(w => w.Code));
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试多目标框架支持
    /// </summary>
    private static async Task<TestCaseResult> TestMultipleTargetFrameworksAsync()
    {
        TestCaseResult result = new() { TestName = "多目标框架支持测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string sourceCode = @"
using System;

public class FrameworkTest
{
    public static void Main()
    {
        Console.WriteLine($""Running on {Environment.Version}"");
    }
}";

            List<string> frameworks = ["net9.0", "net8.0", "net6.0"];
            List<bool> results = [];

            foreach (string framework in frameworks)
            {
                try
                {
                    CompilationResult compilationResult = await CSharpMSBuildCompiler.CompileCodeAsync(sourceCode, framework);
                    results.Add(compilationResult.IsSuccess);
                }
                catch
                {
                    results.Add(false);
                }
            }

            int successCount = results.Count(r => r);
            result.Passed = successCount >= 2; // 至少支持2个框架
            result.Output = $"支持的框架数量: {successCount}/{frameworks.Count}";
            
            for (int i = 0; i < frameworks.Count; i++)
            {
                result.Output += $"\n{frameworks[i]}: {(results[i] ? "✅" : "❌")}";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试代码执行功能
    /// </summary>
    private static async Task<TestCaseResult> TestCodeExecutionAsync()
    {
        TestCaseResult result = new() { TestName = "代码执行测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string sourceCode = @"
using System;

public class ExecutionTest
{
    public static void Main()
    {
        Console.WriteLine(""Execution Test Success"");
    }
}";

            CompilationResult compilationResult = await CSharpMSBuildCompiler.CompileCodeAsync(sourceCode, "net9.0", null, "Exe");

            if (compilationResult.IsSuccess)
            {
                var (success, output, error) = CSharpMSBuildCompiler.TryExecuteCode(compilationResult);
                
                result.Passed = success && output.Contains("Execution Test Success");
                result.Output = $"执行成功: {success}\n输出: {output}";
                
                if (!string.IsNullOrEmpty(error))
                {
                    result.Output += $"\n错误: {error}";
                }
            }
            else
            {
                result.Passed = false;
                result.ErrorMessage = "编译失败，无法测试执行";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试诊断信息增强
    /// </summary>
    private static async Task<TestCaseResult> TestDiagnosticsEnhancementAsync()
    {
        TestCaseResult result = new() { TestName = "诊断信息增强测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string sourceCode = @"
using System;

public class DiagnosticsTest
{
    public static void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
}";

            CompilationResult compilationResult = await CSharpMSBuildCompiler.CompileCodeAsync(sourceCode);

            result.Passed = compilationResult.IsSuccess && 
                           compilationResult.Details.Contains("代码统计") &&
                           compilationResult.Details.Contains("编译性能");
            
            result.Output = "诊断信息增强功能正常";
            
            if (!result.Passed)
            {
                result.ErrorMessage = "诊断信息增强功能未正常工作";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试目标框架验证
    /// </summary>
    private static async Task<TestCaseResult> TestTargetFrameworkValidationAsync()
    {
        TestCaseResult result = new() { TestName = "目标框架验证测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string sourceCode = @"
using System;

public class FrameworkValidationTest
{
    public static void Main()
    {
        Console.WriteLine(""Framework Validation Test"");
    }
}";

            // 测试无效框架
            bool exceptionThrown = false;
            try
            {
                await CSharpMSBuildCompiler.CompileCodeAsync(sourceCode, "invalid-framework");
            }
            catch (ArgumentException)
            {
                exceptionThrown = true;
            }

            // 测试有效框架
            CompilationResult validResult = await CSharpMSBuildCompiler.CompileCodeAsync(sourceCode, "net8.0");

            result.Passed = exceptionThrown && validResult.IsSuccess;
            result.Output = $"无效框架异常: {exceptionThrown}, 有效框架编译: {validResult.IsSuccess}";
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试编译性能
    /// </summary>
    private static async Task<TestCaseResult> TestPerformanceAsync()
    {
        TestCaseResult result = new() { TestName = "编译性能测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string sourceCode = @"
using System;

public class PerformanceTest
{
    public static void Main()
    {
        for (int i = 0; i < 1000; i++)
        {
            Console.WriteLine($""Iteration {i}"");
        }
    }
}";

            CompilationResult compilationResult = await CSharpMSBuildCompiler.CompileCodeAsync(sourceCode);

            result.Passed = compilationResult.IsSuccess && compilationResult.CompilationTimeMs < 30000; // 30秒内完成
            result.Output = $"编译时间: {compilationResult.CompilationTimeMs}ms, 编译成功: {compilationResult.IsSuccess}";
            
            if (compilationResult.CompilationTimeMs > 10000)
            {
                result.Output += " (编译时间较长)";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }
}

/// <summary>
/// 测试套件结果
/// </summary>
public class TestSuiteResult
{
    public string SuiteName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public bool IsSuccess { get; set; }
    public List<TestCaseResult> TestCases { get; set; } = [];
    
    public TimeSpan Duration => EndTime - StartTime;
    public double SuccessRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;
}
