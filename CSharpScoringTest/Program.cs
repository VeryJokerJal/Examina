namespace CSharpScoringTest;

/// <summary>
/// C#代码评分测试程序
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("=== C#代码评分系统测试 ===");
        Console.WriteLine();

        // 创建评分服务
        CSharpScoringService scoringService = new();

        // 测试用例1：简单的Hello World程序
        await TestSimpleProgram(scoringService);

        // 测试用例2：有输入输出的程序
        await TestInputOutputProgram(scoringService);

        // 测试用例3：编译错误的程序
        await TestCompilationError(scoringService);

        // 测试用例4：运行时错误的程序
        await TestRuntimeError(scoringService);

        // 测试用例5：输出不匹配的程序
        await TestOutputMismatch(scoringService);

        Console.WriteLine("=== 测试完成 ===");
    }

    /// <summary>
    /// 测试简单程序
    /// </summary>
    private static async Task TestSimpleProgram(CSharpScoringService scoringService)
    {
        Console.WriteLine("测试1: 简单Hello World程序");
        Console.WriteLine("----------------------------------------");

        string sourceCode = @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
}";

        string programInput = "";
        string expectedOutput = "Hello, World!";

        CSharpScoringConfiguration configuration = new()
        {
            EnableAIScoring = false, // 暂时禁用AI评分以避免API调用
            ExecutionTimeoutSeconds = 5,
            MaxAIScore = 30.0m
        };

        CSharpScoringResult result = await scoringService.ScoreCSharpCodeAsync(
            sourceCode, programInput, expectedOutput, configuration);

        DisplayResult(result);
        Console.WriteLine();
    }

    /// <summary>
    /// 测试有输入输出的程序
    /// </summary>
    private static async Task TestInputOutputProgram(CSharpScoringService scoringService)
    {
        Console.WriteLine("测试2: 输入输出程序");
        Console.WriteLine("----------------------------------------");

        string sourceCode = @"
using System;

class Program
{
    static void Main()
    {
        string input = Console.ReadLine();
        Console.WriteLine($""输入的内容是: {input}"");
    }
}";

        string programInput = "测试输入";
        string expectedOutput = "输入的内容是: 测试输入";

        CSharpScoringConfiguration configuration = new()
        {
            EnableAIScoring = false,
            ExecutionTimeoutSeconds = 5,
            MaxAIScore = 30.0m
        };

        CSharpScoringResult result = await scoringService.ScoreCSharpCodeAsync(
            sourceCode, programInput, expectedOutput, configuration);

        DisplayResult(result);
        Console.WriteLine();
    }

    /// <summary>
    /// 测试编译错误
    /// </summary>
    private static async Task TestCompilationError(CSharpScoringService scoringService)
    {
        Console.WriteLine("测试3: 编译错误程序");
        Console.WriteLine("----------------------------------------");

        string sourceCode = @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello, World!"" // 缺少右括号
    }
}";

        string programInput = "";
        string expectedOutput = "Hello, World!";

        CSharpScoringConfiguration configuration = new()
        {
            EnableAIScoring = false,
            ExecutionTimeoutSeconds = 5,
            MaxAIScore = 30.0m
        };

        CSharpScoringResult result = await scoringService.ScoreCSharpCodeAsync(
            sourceCode, programInput, expectedOutput, configuration);

        DisplayResult(result);
        Console.WriteLine();
    }

    /// <summary>
    /// 测试运行时错误
    /// </summary>
    private static async Task TestRuntimeError(CSharpScoringService scoringService)
    {
        Console.WriteLine("测试4: 运行时错误程序");
        Console.WriteLine("----------------------------------------");

        string sourceCode = @"
using System;

class Program
{
    static void Main()
    {
        int[] array = new int[5];
        Console.WriteLine(array[10]); // 数组越界
    }
}";

        string programInput = "";
        string expectedOutput = "0";

        CSharpScoringConfiguration configuration = new()
        {
            EnableAIScoring = false,
            ExecutionTimeoutSeconds = 5,
            MaxAIScore = 30.0m
        };

        CSharpScoringResult result = await scoringService.ScoreCSharpCodeAsync(
            sourceCode, programInput, expectedOutput, configuration);

        DisplayResult(result);
        Console.WriteLine();
    }

    /// <summary>
    /// 测试输出不匹配
    /// </summary>
    private static async Task TestOutputMismatch(CSharpScoringService scoringService)
    {
        Console.WriteLine("测试5: 输出不匹配程序");
        Console.WriteLine("----------------------------------------");

        string sourceCode = @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello, C#!"");
    }
}";

        string programInput = "";
        string expectedOutput = "Hello, World!";

        CSharpScoringConfiguration configuration = new()
        {
            EnableAIScoring = false,
            ExecutionTimeoutSeconds = 5,
            MaxAIScore = 30.0m
        };

        CSharpScoringResult result = await scoringService.ScoreCSharpCodeAsync(
            sourceCode, programInput, expectedOutput, configuration);

        DisplayResult(result);
        Console.WriteLine();
    }

    /// <summary>
    /// 显示评分结果
    /// </summary>
    private static void DisplayResult(CSharpScoringResult result)
    {
        Console.WriteLine($"评分阶段: {result.ScoringStage}");
        Console.WriteLine($"评分成功: {result.IsSuccess}");
        Console.WriteLine($"最终得分: {result.FinalScore:F1}/{result.TotalScore:F1}");

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            Console.WriteLine($"错误信息: {result.ErrorMessage}");
        }

        // 编译结果
        Console.WriteLine($"编译成功: {result.CompilationResult.IsSuccess}");
        if (!result.CompilationResult.IsSuccess && result.CompilationResult.Errors.Count > 0)
        {
            Console.WriteLine("编译错误:");
            foreach (string error in result.CompilationResult.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        // 执行结果
        if (result.CompilationResult.IsSuccess)
        {
            Console.WriteLine($"执行成功: {result.ExecutionResult.IsSuccess}");
            Console.WriteLine($"程序输出: '{result.ExecutionResult.Output}'");
            Console.WriteLine($"输出匹配: {result.OutputMatches}");

            if (!string.IsNullOrEmpty(result.ExecutionResult.ErrorOutput))
            {
                Console.WriteLine($"错误输出: {result.ExecutionResult.ErrorOutput}");
            }

            if (result.ExecutionResult.IsTimeout)
            {
                Console.WriteLine("程序执行超时");
            }
        }

        // AI评分结果
        if (result.AIScoringResult.IsSuccess)
        {
            Console.WriteLine($"AI评分: {result.AIScoringResult.TotalScore:F1}/30");
            Console.WriteLine($"逻辑性: {result.AIScoringResult.LogicScore:F1}/10");
            Console.WriteLine($"冗余检测: {result.AIScoringResult.RedundancyScore:F1}/10");
            Console.WriteLine($"结构性: {result.AIScoringResult.StructureScore:F1}/5");
            Console.WriteLine($"效率性: {result.AIScoringResult.EfficiencyScore:F1}/5");

            if (result.AIScoringResult.Issues.Count > 0)
            {
                Console.WriteLine("发现的问题:");
                foreach (string issue in result.AIScoringResult.Issues)
                {
                    Console.WriteLine($"  - {issue}");
                }
            }

            if (result.AIScoringResult.Suggestions.Count > 0)
            {
                Console.WriteLine("改进建议:");
                foreach (string suggestion in result.AIScoringResult.Suggestions)
                {
                    Console.WriteLine($"  - {suggestion}");
                }
            }
        }
    }
}
