using System.Text.Json;
using BenchSuite.Interfaces;
using BenchSuite.Models;
using BenchSuite.Services;

namespace BenchSuite.Examples;

/// <summary>
/// AI逻辑性判分功能示例和测试
/// </summary>
public static class AILogicalScoringExample
{
    /// <summary>
    /// 运行AI逻辑性判分示例
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <param name="customEndpoint">自定义API端点（可选）</param>
    /// <returns>示例运行结果</returns>
    public static async Task<bool> RunExampleAsync(string apiKey, string? customEndpoint = null)
    {
        Console.WriteLine("=== AI逻辑性判分功能示例 ===\n");

        try
        {
            // 1. 配置AI服务
            AIServiceConfiguration config = new()
            {
                ApiKey = apiKey,
                ApiEndpoint = customEndpoint ?? "https://api.gptnb.ai/v1/chat/completions",
                ModelName = "gpt-5-2025-08-07",
                MaxTokens = 2000,
                Temperature = 0.1,
                TimeoutSeconds = 30,
                EnableStructuredOutput = true
            };

            // 2. 创建AI判分服务
            IAILogicalScoringService aiService = new AILogicalScoringService(config);

            // 3. 验证服务可用性
            Console.WriteLine("验证AI服务连接...");
            bool isAvailable = await aiService.IsServiceAvailableAsync();
            Console.WriteLine($"AI服务状态: {(isAvailable ? "✅ 可用" : "❌ 不可用")}\n");

            if (!isAvailable)
            {
                Console.WriteLine("AI服务不可用，无法继续示例");
                return false;
            }

            // 4. 测试示例代码
            await TestCorrectImplementation(aiService);
            await TestIncorrectImplementation(aiService);
            TestStructuredOutputParsing();

            Console.WriteLine("=== AI逻辑性判分示例完成 ===");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"示例运行失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 测试正确的代码实现
    /// </summary>
    private static async Task TestCorrectImplementation(IAILogicalScoringService aiService)
    {
        Console.WriteLine("--- 测试正确的代码实现 ---");

        string correctCode = """
            using System;
            using System.Text;

            public class StringNumberExtractor
            {
                public static int ExtractAndSum(string input)
                {
                    if (string.IsNullOrEmpty(input))
                        return 0;

                    StringBuilder currentNumber = new();
                    int sum = 0;

                    foreach (char c in input)
                    {
                        if (char.IsDigit(c))
                        {
                            currentNumber.Append(c);
                        }
                        else if (currentNumber.Length > 0)
                        {
                            sum += int.Parse(currentNumber.ToString());
                            currentNumber.Clear();
                        }
                    }

                    // 处理字符串末尾的数字
                    if (currentNumber.Length > 0)
                    {
                        sum += int.Parse(currentNumber.ToString());
                    }

                    return sum;
                }
            }
            """;

        string problemDescription = "从字符串中提取所有数字并计算它们的总和";

        AILogicalScoringResult result = await aiService.ScoreLogicalReasoningAsync(
            correctCode,
            problemDescription,
            "对于输入'a123b45c6'，应该返回174（123+45+6）"
        );

        PrintScoringResult("正确实现", result);
    }

    /// <summary>
    /// 测试有问题的代码实现
    /// </summary>
    private static async Task TestIncorrectImplementation(IAILogicalScoringService aiService)
    {
        Console.WriteLine("\n--- 测试有问题的代码实现 ---");

        string incorrectCode = """
            using System;

            public class StringNumberExtractor
            {
                public static int ExtractAndSum(string input)
                {
                    int sum = 0;
                    string num = "";

                    foreach (char c in input)
                    {
                        if (char.IsDigit(c))
                        {
                            num += c;
                        }
                        else 
                        {
                            if (num.Length > 0)
                            {
                                sum += int.Parse(num);
                                num = "";
                            }
                        }
                    }
                    // 缺少对字符串末尾数字的处理
                    return sum;
                }
            }
            """;

        string problemDescription = "从字符串中提取所有数字并计算它们的总和";

        AILogicalScoringResult result = await aiService.ScoreLogicalReasoningAsync(
            incorrectCode,
            problemDescription,
            "对于输入'a123b45c6'，应该返回174（123+45+6）"
        );

        PrintScoringResult("有问题的实现", result);
    }

    /// <summary>
    /// 测试结构化输出解析
    /// </summary>
    private static void TestStructuredOutputParsing()
    {
        Console.WriteLine("\n--- 测试结构化输出解析 ---");

        // 模拟AI返回的JSON响应
        string mockJsonResponse = """
            {
                "steps": [
                    {
                        "explanation": "分析代码结构",
                        "output": "代码使用了StringBuilder来构建数字字符串，这是一个好的实践",
                        "step_type": "structure_analysis"
                    },
                    {
                        "explanation": "检查逻辑流程",
                        "output": "代码正确地遍历了字符串中的每个字符",
                        "step_type": "logic_analysis"
                    },
                    {
                        "explanation": "验证边界情况处理",
                        "output": "代码正确处理了字符串末尾的数字",
                        "step_type": "boundary_check"
                    }
                ],
                "final_answer": "代码实现正确，逻辑清晰，考虑了边界情况",
                "logical_score": 95,
                "logical_errors": [],
                "improvement_suggestions": [
                    "可以添加输入参数的null检查",
                    "可以考虑使用更高效的数字解析方法"
                ]
            }
            """;

        try
        {
            CSharpLogicalAnalysisResponse? response = JsonSerializer.Deserialize<CSharpLogicalAnalysisResponse>(mockJsonResponse);

            if (response != null)
            {
                Console.WriteLine("✅ JSON解析成功");
                Console.WriteLine($"分析步骤数: {response.Steps.Count}");
                Console.WriteLine($"逻辑评分: {response.LogicalScore}");
                Console.WriteLine($"最终答案: {response.FinalAnswer}");
                Console.WriteLine($"改进建议数: {response.ImprovementSuggestions.Count}");
            }
            else
            {
                Console.WriteLine("❌ JSON解析失败");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ JSON解析异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 打印评分结果
    /// </summary>
    private static void PrintScoringResult(string testName, AILogicalScoringResult result)
    {
        Console.WriteLine($"\n{testName} - 评分结果:");
        Console.WriteLine($"成功: {(result.IsSuccess ? "✅" : "❌")}");

        if (result.IsSuccess)
        {
            Console.WriteLine($"逻辑评分: {result.LogicalScore}/100");
            Console.WriteLine($"处理耗时: {result.ProcessingTimeMs}ms");
            Console.WriteLine($"分析步骤数: {result.Steps.Count}");

            if (result.Steps.Count > 0)
            {
                Console.WriteLine("主要分析步骤:");
                foreach (ReasoningStep step in result.Steps.Take(2))
                {
                    Console.WriteLine($"  • {step.Explanation}");
                }
            }

            if (!string.IsNullOrEmpty(result.FinalAnswer))
            {
                Console.WriteLine($"最终评估: {result.FinalAnswer}");
            }
        }
        else
        {
            Console.WriteLine($"错误信息: {result.ErrorMessage}");
        }
    }

    /// <summary>
    /// 测试完整的C#评分服务集成
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <param name="customEndpoint">自定义API端点（可选）</param>
    /// <returns>测试结果</returns>
    public static async Task<bool> TestCSharpScoringServiceIntegrationAsync(string apiKey, string? customEndpoint = null)
    {
        Console.WriteLine("\n=== 测试C#评分服务AI集成 ===");

        try
        {
            // 1. 配置AI服务
            AIServiceConfiguration config = AILogicalScoringConfiguration.CreateDefaultConfiguration(apiKey, customEndpoint);

            IAILogicalScoringService aiService = new AILogicalScoringService(config);

            // 2. 创建C#评分服务（集成AI功能）
            CSharpScoringService scoringService = new(aiService);

            // 3. 准备测试数据
            string studentCode = """
                using System;

                public class Calculator
                {
                    public static int Add(int a, int b)
                    {
                        return a + b;
                    }
                    
                    public static int Multiply(int a, int b)
                    {
                        return a * b;
                    }
                }
                """;

            string testCode = """
                using System;
                using NUnit.Framework;

                [TestFixture]
                public class CalculatorTests
                {
                    [Test]
                    public void TestAdd()
                    {
                        Assert.AreEqual(5, Calculator.Add(2, 3));
                        Assert.AreEqual(0, Calculator.Add(-1, 1));
                    }
                    
                    [Test]
                    public void TestMultiply()
                    {
                        Assert.AreEqual(6, Calculator.Multiply(2, 3));
                        Assert.AreEqual(0, Calculator.Multiply(0, 5));
                    }
                }
                """;

            List<string> expectedImplementations = [testCode];

            // 4. 执行评分
            CSharpScoringResult result = await scoringService.ScoreCodeAsync(
                "", // 模板代码
                studentCode,
                expectedImplementations,
                CSharpScoringMode.Implementation
            );

            // 5. 输出结果
            Console.WriteLine($"评分成功: {(result.IsSuccess ? "✅" : "❌")}");
            Console.WriteLine($"总分: {result.TotalScore}");
            Console.WriteLine($"得分: {result.AchievedScore}");
            Console.WriteLine($"得分率: {result.ScoreRate:P2}");

            if (result.AILogicalResult != null)
            {
                Console.WriteLine($"AI逻辑评分: {result.AILogicalResult.LogicalScore}/100");
                Console.WriteLine($"AI分析成功: {(result.AILogicalResult.IsSuccess ? "✅" : "❌")}");
            }

            Console.WriteLine($"详细信息:\n{result.Details}");

            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"集成测试失败: {ex.Message}");
            return false;
        }
    }
}
