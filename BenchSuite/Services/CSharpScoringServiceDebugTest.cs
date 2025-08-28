using BenchSuite.Models;
using BenchSuite.Services;
using System;
using System.Collections.Generic;

namespace BenchSuite.Services
{
    /// <summary>
    /// CSharpScoringService调试测试类
    /// </summary>
    public static class CSharpScoringServiceDebugTest
    {
        /// <summary>
        /// 测试从CodeBlanks提取数据的场景
        /// </summary>
        public static void TestCodeBlanksExtraction()
        {
            Console.WriteLine("=== 测试CodeBlanks数据提取 ===");

            // 创建测试题目模型（模拟EL/EW项目的数据结构）
            QuestionModel question = new()
            {
                Id = "test-001",
                Title = "C#代码补全测试题",
                QuestionType = "CodeCompletion",
                CSharpQuestionType = "CodeCompletion",
                Content = "请完成以下C#代码的填空部分",
                OperationPoints = [], // 空的操作点列表，模拟EL/EW项目的情况
                CodeBlanks = new List<CodeBlankModel>
                {
                    new()
                    {
                        Id = "blank-001",
                        Name = "填空1",
                        Description = @"public class Calculator
{
    public int Add(int a, int b)
    {
        throw new NotImplementedException();
    }
}",
                        Order = 1,
                        IsEnabled = true,
                        StandardAnswer = "return a + b;",
                        Score = 5.0
                    },
                    new()
                    {
                        Id = "blank-002", 
                        Name = "填空2",
                        Description = "// 第二个填空的描述",
                        Order = 2,
                        IsEnabled = true,
                        StandardAnswer = "Console.WriteLine(\"Hello World\");",
                        Score = 3.0
                    }
                }
            };

            // 输出调试信息
            string debugInfo = CSharpScoringService.DebugQuestionModel(question);
            Console.WriteLine(debugInfo);

            Console.WriteLine("\n=== 测试完成 ===");
        }

        /// <summary>
        /// 测试从操作点提取数据的场景
        /// </summary>
        public static void TestOperationPointsExtraction()
        {
            Console.WriteLine("=== 测试操作点数据提取 ===");

            // 创建测试题目模型（模拟传统操作点方式）
            QuestionModel question = new()
            {
                Id = "test-002",
                Title = "C#操作点测试题",
                QuestionType = "CodeCompletion",
                Content = "请完成以下C#代码",
                OperationPoints = new List<OperationPointModel>
                {
                    new()
                    {
                        Id = "op-001",
                        Name = "C#代码补全",
                        ModuleType = ModuleType.CSharp,
                        IsEnabled = true,
                        Score = 10.0,
                        Parameters = new List<ConfigurationParameterModel>
                        {
                            new()
                            {
                                Name = "TemplateCode",
                                Value = @"public class Test
{
    public void Method()
    {
        throw new NotImplementedException();
    }
}"
                            },
                            new()
                            {
                                Name = "ExpectedImplementation",
                                Value = "Console.WriteLine(\"Test\");"
                            }
                        }
                    }
                },
                CodeBlanks = null
            };

            // 输出调试信息
            string debugInfo = CSharpScoringService.DebugQuestionModel(question);
            Console.WriteLine(debugInfo);

            Console.WriteLine("\n=== 测试完成 ===");
        }

        /// <summary>
        /// 测试混合数据源的场景
        /// </summary>
        public static void TestMixedDataSources()
        {
            Console.WriteLine("=== 测试混合数据源 ===");

            // 创建测试题目模型（同时包含操作点和CodeBlanks）
            QuestionModel question = new()
            {
                Id = "test-003",
                Title = "混合数据源测试题",
                QuestionType = "Implementation",
                CSharpQuestionType = "CodeCompletion", // 这里故意设置不同的类型来测试优先级
                Content = @"```csharp
public class MixedTest
{
    // 从Content中提取的代码
    throw new NotImplementedException();
}
```",
                OperationPoints = new List<OperationPointModel>
                {
                    new()
                    {
                        Id = "op-mixed",
                        Name = "混合操作点",
                        ModuleType = ModuleType.CSharp,
                        IsEnabled = true,
                        Score = 8.0,
                        Parameters = new List<ConfigurationParameterModel>
                        {
                            new()
                            {
                                Name = "TestCode",
                                Value = "// 从操作点获取的测试代码"
                            }
                        }
                    }
                },
                CodeBlanks = new List<CodeBlankModel>
                {
                    new()
                    {
                        Id = "mixed-blank",
                        Name = "混合填空",
                        Description = "// 从CodeBlanks获取的描述",
                        Order = 1,
                        IsEnabled = true,
                        StandardAnswer = "// 从CodeBlanks获取的标准答案",
                        Score = 6.0
                    }
                }
            };

            // 输出调试信息
            string debugInfo = CSharpScoringService.DebugQuestionModel(question);
            Console.WriteLine(debugInfo);

            Console.WriteLine("\n=== 测试完成 ===");
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("开始CSharpScoringService调试测试...\n");

            TestCodeBlanksExtraction();
            Console.WriteLine("\n" + new string('=', 80) + "\n");

            TestOperationPointsExtraction();
            Console.WriteLine("\n" + new string('=', 80) + "\n");

            TestMixedDataSources();

            Console.WriteLine("\n所有测试完成！");
        }
    }
}
