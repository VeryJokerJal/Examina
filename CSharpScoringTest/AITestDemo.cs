namespace CSharpScoringTest;

/// <summary>
/// AI评分演示程序
/// </summary>
public static class AITestDemo
{
    /// <summary>
    /// 运行AI评分演示
    /// </summary>
    public static async Task RunAITestAsync()
    {
        Console.WriteLine("=== C#代码AI评分演示 ===");
        Console.WriteLine();

        // 创建评分服务
        CSharpScoringService scoringService = new();

        // 测试用例：包含冗余代码的程序
        await TestRedundantCode(scoringService);

        Console.WriteLine("=== AI评分演示完成 ===");
    }

    /// <summary>
    /// 测试包含冗余代码的程序
    /// </summary>
    private static async Task TestRedundantCode(CSharpScoringService scoringService)
    {
        Console.WriteLine("AI评分测试: 包含冗余代码的程序");
        Console.WriteLine("----------------------------------------");

        string sourceCode = @"
using System;

class Program
{
    static void Main()
    {
        // 冗余的变量声明
        int a = 5;
        int b = 10;
        int c = 15;
        int d = 20;
        
        // 重复的代码块
        Console.WriteLine(""计算开始"");
        int result1 = a + b;
        Console.WriteLine(""第一次计算: "" + result1);
        
        Console.WriteLine(""计算开始"");
        int result2 = c + d;
        Console.WriteLine(""第二次计算: "" + result2);
        
        // 不必要的变量
        int temp1 = result1;
        int temp2 = result2;
        int finalResult = temp1 + temp2;
        
        Console.WriteLine(""最终结果: "" + finalResult);
    }
}";

        string programInput = "";
        string expectedOutput = @"计算开始
第一次计算: 15
计算开始
第二次计算: 35
最终结果: 50";

        // 注意：这里需要设置真实的OpenAI API密钥才能测试AI评分
        CSharpScoringConfiguration configuration = new()
        {
            EnableAIScoring = false, // 设置为true并提供API密钥来启用AI评分
            OpenAIApiKey = "your-openai-api-key-here", // 替换为真实的API密钥
            ExecutionTimeoutSeconds = 10,
            MaxAIScore = 30.0m
        };

        CSharpScoringResult result = await scoringService.ScoreCSharpCodeAsync(
            sourceCode, programInput, expectedOutput, configuration);

        DisplayAIResult(result);
        Console.WriteLine();
    }

    /// <summary>
    /// 显示AI评分结果
    /// </summary>
    private static void DisplayAIResult(CSharpScoringResult result)
    {
        Console.WriteLine($"评分阶段: {result.ScoringStage}");
        Console.WriteLine($"评分成功: {result.IsSuccess}");
        Console.WriteLine($"最终得分: {result.FinalScore:F1}/{result.TotalScore:F1}");

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            Console.WriteLine($"错误信息: {result.ErrorMessage}");
        }

        // 编译和执行结果
        Console.WriteLine($"编译成功: {result.CompilationResult.IsSuccess}");
        Console.WriteLine($"执行成功: {result.ExecutionResult.IsSuccess}");
        Console.WriteLine($"输出匹配: {result.OutputMatches}");

        // AI评分详细结果
        if (result.AIScoringResult.IsSuccess)
        {
            Console.WriteLine();
            Console.WriteLine("=== AI评分详细结果 ===");
            Console.WriteLine($"总分: {result.AIScoringResult.TotalScore:F1}/30");
            Console.WriteLine($"逻辑性得分: {result.AIScoringResult.LogicScore:F1}/10");
            Console.WriteLine($"冗余检测得分: {result.AIScoringResult.RedundancyScore:F1}/10");
            Console.WriteLine($"结构得分: {result.AIScoringResult.StructureScore:F1}/5");
            Console.WriteLine($"效率得分: {result.AIScoringResult.EfficiencyScore:F1}/5");

            if (result.AIScoringResult.Issues.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("发现的问题:");
                foreach (string issue in result.AIScoringResult.Issues)
                {
                    Console.WriteLine($"  - {issue}");
                }
            }

            if (result.AIScoringResult.Suggestions.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("改进建议:");
                foreach (string suggestion in result.AIScoringResult.Suggestions)
                {
                    Console.WriteLine($"  - {suggestion}");
                }
            }

            if (!string.IsNullOrEmpty(result.AIScoringResult.DetailedFeedback))
            {
                Console.WriteLine();
                Console.WriteLine("详细反馈:");
                Console.WriteLine(result.AIScoringResult.DetailedFeedback);
            }
        }
        else if (!string.IsNullOrEmpty(result.AIScoringResult.ErrorMessage))
        {
            Console.WriteLine($"AI评分失败: {result.AIScoringResult.ErrorMessage}");
        }
    }
}
