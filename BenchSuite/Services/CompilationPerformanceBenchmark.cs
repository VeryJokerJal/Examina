using BenchSuite.Models;
using System.Diagnostics;
using System.Text;

namespace BenchSuite.Services;

/// <summary>
/// 编译性能基准测试 - 对比新旧实现的性能差异
/// </summary>
public static class CompilationPerformanceBenchmark
{
    /// <summary>
    /// 性能测试结果
    /// </summary>
    public class PerformanceResult
    {
        public string TestName { get; set; } = string.Empty;
        public long MSBuildTimeMs { get; set; }
        public long RoslynTimeMs { get; set; }
        public bool MSBuildSuccess { get; set; }
        public bool RoslynSuccess { get; set; }
        public int MSBuildMemoryMB { get; set; }
        public int RoslynMemoryMB { get; set; }
        public double PerformanceRatio => RoslynTimeMs > 0 ? (double)MSBuildTimeMs / RoslynTimeMs : 0;
        public string PerformanceAnalysis { get; set; } = string.Empty;
    }

    /// <summary>
    /// 运行完整的性能基准测试
    /// </summary>
    /// <returns>性能测试结果</returns>
    public static async Task<List<PerformanceResult>> RunBenchmarkAsync()
    {
        List<PerformanceResult> results = [];

        Console.WriteLine("🚀 开始编译性能基准测试...\n");

        // 测试用例
        var testCases = GetTestCases();

        foreach (var testCase in testCases)
        {
            Console.WriteLine($"📊 测试: {testCase.Name}");
            
            PerformanceResult result = await RunSingleBenchmarkAsync(testCase.Name, testCase.Code);
            results.Add(result);
            
            Console.WriteLine($"   MSBuild: {result.MSBuildTimeMs}ms, Roslyn: {result.RoslynTimeMs}ms");
            Console.WriteLine($"   性能比率: {result.PerformanceRatio:F2}x\n");
        }

        // 生成总体分析
        GenerateOverallAnalysis(results);

        return results;
    }

    /// <summary>
    /// 运行单个性能测试
    /// </summary>
    /// <param name="testName">测试名称</param>
    /// <param name="sourceCode">源代码</param>
    /// <returns>性能结果</returns>
    private static async Task<PerformanceResult> RunSingleBenchmarkAsync(string testName, string sourceCode)
    {
        PerformanceResult result = new() { TestName = testName };

        // 预热
        await WarmupAsync(sourceCode);

        // 测试MSBuild编译
        var msbuildResult = await MeasureCompilationAsync(
            () => CSharpMSBuildCompiler.CompileCodeAsync(sourceCode),
            "MSBuild");

        result.MSBuildTimeMs = msbuildResult.TimeMs;
        result.MSBuildSuccess = msbuildResult.Success;
        result.MSBuildMemoryMB = msbuildResult.MemoryMB;

        // 测试Roslyn编译（回退方案）
        var roslynResult = await MeasureCompilationAsync(
            () => Task.FromResult(CSharpCompilationChecker.CompileCode(sourceCode)),
            "Roslyn");

        result.RoslynTimeMs = roslynResult.TimeMs;
        result.RoslynSuccess = roslynResult.Success;
        result.RoslynMemoryMB = roslynResult.MemoryMB;

        // 分析性能
        result.PerformanceAnalysis = AnalyzePerformance(result);

        return result;
    }

    /// <summary>
    /// 测量编译性能
    /// </summary>
    /// <param name="compilationFunc">编译函数</param>
    /// <param name="compilerName">编译器名称</param>
    /// <returns>测量结果</returns>
    private static async Task<(long TimeMs, bool Success, int MemoryMB)> MeasureCompilationAsync(
        Func<Task<CompilationResult>> compilationFunc,
        string compilerName)
    {
        // 强制垃圾回收
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long initialMemory = GC.GetTotalMemory(false);
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            CompilationResult result = await compilationFunc();
            stopwatch.Stop();

            long finalMemory = GC.GetTotalMemory(false);
            int memoryUsedMB = (int)((finalMemory - initialMemory) / 1024 / 1024);

            return (stopwatch.ElapsedMilliseconds, result.IsSuccess, Math.Max(0, memoryUsedMB));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.WriteLine($"   ⚠️ {compilerName}编译异常: {ex.Message}");
            return (stopwatch.ElapsedMilliseconds, false, 0);
        }
    }

    /// <summary>
    /// 预热编译器
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    private static async Task WarmupAsync(string sourceCode)
    {
        try
        {
            // MSBuild预热
            await CSharpMSBuildCompiler.CompileCodeAsync("using System; class Warmup { }");
            
            // Roslyn预热
            CSharpCompilationChecker.CompileCode("using System; class Warmup { }");
        }
        catch
        {
            // 忽略预热错误
        }
    }

    /// <summary>
    /// 分析性能结果
    /// </summary>
    /// <param name="result">性能结果</param>
    /// <returns>分析报告</returns>
    private static string AnalyzePerformance(PerformanceResult result)
    {
        StringBuilder analysis = new();

        if (result.PerformanceRatio < 0.8)
        {
            analysis.AppendLine("🚀 MSBuild编译速度显著优于Roslyn");
        }
        else if (result.PerformanceRatio < 1.2)
        {
            analysis.AppendLine("⚖️ MSBuild和Roslyn编译速度相当");
        }
        else if (result.PerformanceRatio < 2.0)
        {
            analysis.AppendLine("🐌 MSBuild编译速度略慢于Roslyn");
        }
        else
        {
            analysis.AppendLine("⚠️ MSBuild编译速度明显慢于Roslyn，需要优化");
        }

        // 内存使用分析
        if (result.MSBuildMemoryMB > result.RoslynMemoryMB * 2)
        {
            analysis.AppendLine("💾 MSBuild内存使用较高，建议优化");
        }
        else if (result.MSBuildMemoryMB < result.RoslynMemoryMB)
        {
            analysis.AppendLine("💾 MSBuild内存使用更优");
        }

        // 成功率分析
        if (result.MSBuildSuccess && !result.RoslynSuccess)
        {
            analysis.AppendLine("✅ MSBuild编译成功率更高");
        }
        else if (!result.MSBuildSuccess && result.RoslynSuccess)
        {
            analysis.AppendLine("❌ MSBuild编译失败，Roslyn成功");
        }

        return analysis.ToString().TrimEnd();
    }

    /// <summary>
    /// 生成总体分析报告
    /// </summary>
    /// <param name="results">所有测试结果</param>
    private static void GenerateOverallAnalysis(List<PerformanceResult> results)
    {
        Console.WriteLine("📈 总体性能分析:");
        Console.WriteLine(new string('=', 50));

        double avgRatio = results.Where(r => r.PerformanceRatio > 0).Average(r => r.PerformanceRatio);
        long avgMSBuildTime = (long)results.Average(r => r.MSBuildTimeMs);
        long avgRoslynTime = (long)results.Average(r => r.RoslynTimeMs);
        int msbuildSuccessCount = results.Count(r => r.MSBuildSuccess);
        int roslynSuccessCount = results.Count(r => r.RoslynSuccess);

        Console.WriteLine($"平均性能比率: {avgRatio:F2}x");
        Console.WriteLine($"平均MSBuild时间: {avgMSBuildTime}ms");
        Console.WriteLine($"平均Roslyn时间: {avgRoslynTime}ms");
        Console.WriteLine($"MSBuild成功率: {msbuildSuccessCount}/{results.Count} ({(double)msbuildSuccessCount/results.Count*100:F1}%)");
        Console.WriteLine($"Roslyn成功率: {roslynSuccessCount}/{results.Count} ({(double)roslynSuccessCount/results.Count*100:F1}%)");

        Console.WriteLine("\n🎯 优化建议:");
        if (avgRatio > 1.5)
        {
            Console.WriteLine("• MSBuild编译速度需要优化");
            Console.WriteLine("• 考虑启用并行编译");
            Console.WriteLine("• 优化临时文件管理");
        }
        else if (avgRatio < 0.8)
        {
            Console.WriteLine("• MSBuild性能表现优秀");
            Console.WriteLine("• 可以完全替代Roslyn编译");
        }
        else
        {
            Console.WriteLine("• MSBuild性能表现良好");
            Console.WriteLine("• 可以安全迁移到MSBuild");
        }

        if (msbuildSuccessCount < roslynSuccessCount)
        {
            Console.WriteLine("• 需要改进MSBuild的错误处理");
            Console.WriteLine("• 检查目标框架兼容性");
        }
    }

    /// <summary>
    /// 获取测试用例
    /// </summary>
    /// <returns>测试用例列表</returns>
    private static List<(string Name, string Code)> GetTestCases()
    {
        return 
        [
            ("简单Hello World", @"
using System;
class Program 
{ 
    static void Main() 
    { 
        Console.WriteLine(""Hello, World!""); 
    } 
}"),

            ("复杂类结构", @"
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestNamespace
{
    public class ComplexClass
    {
        private List<string> items = new();
        
        public void AddItem(string item)
        {
            items.Add(item);
        }
        
        public IEnumerable<string> GetFilteredItems(Func<string, bool> predicate)
        {
            return items.Where(predicate);
        }
    }
    
    class Program
    {
        static void Main()
        {
            var complex = new ComplexClass();
            complex.AddItem(""test"");
            Console.WriteLine(complex.GetFilteredItems(x => x.Length > 2).Count());
        }
    }
}"),

            ("LINQ查询", @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        var numbers = Enumerable.Range(1, 100);
        var result = numbers
            .Where(x => x % 2 == 0)
            .Select(x => x * x)
            .Sum();
        Console.WriteLine(result);
    }
}"),

            ("异步编程", @"
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        await DoWorkAsync();
        Console.WriteLine(""Work completed"");
    }
    
    static async Task DoWorkAsync()
    {
        await Task.Delay(100);
        Console.WriteLine(""Async work done"");
    }
}")
        ];
    }

    /// <summary>
    /// 运行快速性能检查
    /// </summary>
    /// <returns>是否通过性能检查</returns>
    public static async Task<bool> QuickPerformanceCheckAsync()
    {
        Console.WriteLine("⚡ 运行快速性能检查...");

        string simpleCode = @"
using System;
class QuickTest 
{ 
    static void Main() 
    { 
        Console.WriteLine(""Quick test""); 
    } 
}";

        var result = await RunSingleBenchmarkAsync("快速检查", simpleCode);
        
        bool passed = result.MSBuildSuccess && result.MSBuildTimeMs < 10000; // 10秒内完成
        
        Console.WriteLine($"快速检查结果: {(passed ? "✅ 通过" : "❌ 失败")}");
        Console.WriteLine($"MSBuild时间: {result.MSBuildTimeMs}ms");
        
        return passed;
    }
}
