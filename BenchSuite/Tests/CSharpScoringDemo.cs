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
            Console.WriteLine("📁 演示文件评分功能");
            Console.WriteLine("-".PadRight(50, '-'));

            await DemoFileBasedScoringAsync();

            Console.WriteLine();
            Console.WriteLine("🧪 快速验证文件评分功能");
            Console.WriteLine("-".PadRight(50, '-'));

            await QuickTestFileScoringAsync();
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

    /// <summary>
    /// 演示基于文件路径的评分功能
    /// </summary>
    private static async Task DemoFileBasedScoringAsync()
    {
        CSharpScoringService service = new();

        Console.WriteLine("📝 创建测试文件和试卷模型");

        // 创建临时测试文件
        string tempFilePath = Path.Combine(Path.GetTempPath(), "demo_student_code.cs");
        string studentCode = @"
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

        char[] chars = input.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    /// <summary>
    /// 计算字符串长度
    /// </summary>
    /// <param name=""input"">输入字符串</param>
    /// <returns>字符串长度</returns>
    public static int GetLength(string input)
    {
        return input?.Length ?? 0;
    }
}";

        try
        {
            await File.WriteAllTextAsync(tempFilePath, studentCode);
            Console.WriteLine($"✅ 测试文件已创建: {tempFilePath}");

            // 创建试卷模型
            ExamModel examModel = CreateDemoExamModel();
            Console.WriteLine($"✅ 试卷模型已创建: {examModel.Name}");

            // 验证文件处理能力
            Console.WriteLine($"\n🔍 文件处理验证:");
            Console.WriteLine($"   支持的扩展名: {string.Join(", ", service.GetSupportedExtensions())}");
            Console.WriteLine($"   可以处理该文件: {service.CanProcessFile(tempFilePath)}");

            // 执行文件评分
            Console.WriteLine($"\n⚡ 开始文件评分...");
            ScoringResult result = await service.ScoreFileAsync(tempFilePath, examModel);

            // 显示评分结果
            Console.WriteLine($"\n📊 评分结果:");
            Console.WriteLine($"   评分状态: {(result.IsSuccess ? "成功✅" : "失败❌")}");
            Console.WriteLine($"   总分: {result.TotalScore}");
            Console.WriteLine($"   得分: {result.AchievedScore}");
            Console.WriteLine($"   得分率: {result.ScoreRate:P2}");
            Console.WriteLine($"   耗时: {result.ElapsedMilliseconds}ms");
            Console.WriteLine($"   知识点数量: {result.KnowledgePointResults.Count}");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"   错误信息: {result.ErrorMessage}");
            }

            // 显示详细的知识点结果
            if (result.KnowledgePointResults.Count > 0)
            {
                Console.WriteLine($"\n📋 知识点详情:");
                foreach (var kpResult in result.KnowledgePointResults)
                {
                    Console.WriteLine($"   • {kpResult.KnowledgePointType}: {(kpResult.IsCorrect ? "正确✅" : "错误❌")}");
                    Console.WriteLine($"     得分: {kpResult.AchievedScore}/{kpResult.TotalScore}");
                    if (!string.IsNullOrEmpty(kpResult.Details))
                    {
                        Console.WriteLine($"     详情: {kpResult.Details}");
                    }
                }
            }

            // 测试单个题目评分
            var firstQuestion = examModel.Modules.FirstOrDefault(m => m.Type == ModuleType.CSharp)?.Questions.FirstOrDefault();
            if (firstQuestion != null)
            {
                Console.WriteLine($"\n🎯 单题目评分测试:");
                ScoringResult questionResult = await service.ScoreQuestionAsync(tempFilePath, firstQuestion);
                Console.WriteLine($"   题目: {firstQuestion.Title}");
                Console.WriteLine($"   状态: {(questionResult.IsSuccess ? "成功✅" : "失败❌")}");
                Console.WriteLine($"   得分: {questionResult.AchievedScore}/{questionResult.TotalScore}");
            }

            Console.WriteLine($"\n🎉 文件评分演示完成!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 文件评分演示异常: {ex.Message}");
        }
        finally
        {
            // 清理临时文件
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
                Console.WriteLine($"🗑️ 临时文件已清理");
            }
        }
    }

    /// <summary>
    /// 创建演示用的试卷模型
    /// </summary>
    /// <returns>试卷模型</returns>
    private static ExamModel CreateDemoExamModel()
    {
        return new ExamModel
        {
            Id = "demo-exam-001",
            Name = "C#字符串处理能力测试",
            Description = "测试学生的C#字符串处理编程能力",
            TotalScore = 20,
            Modules =
            [
                new ExamModuleModel
                {
                    Id = "csharp-string-module",
                    Name = "C#字符串处理模块",
                    Type = ModuleType.CSharp,
                    Score = 20,
                    Questions =
                    [
                        new QuestionModel
                        {
                            Id = "string-reverse-question",
                            Title = "字符串反转功能",
                            Content = "实现字符串反转功能，要求处理空字符串和null值",
                            QuestionType = "Implementation",
                            Score = 15,
                            OperationPoints =
                            [
                                new OperationPointModel
                                {
                                    Id = "reverse-implementation",
                                    Name = "实现Reverse方法",
                                    Description = "正确实现字符串反转逻辑",
                                    ModuleType = ModuleType.CSharp,
                                    Score = 15,
                                    Parameters =
                                    [
                                        new ConfigurationParameterModel
                                        {
                                            Name = "TestCode",
                                            Value = @"
public class StringHelperTests
{
    [Test]
    public void TestReverse()
    {
        if (StringHelper.Reverse(""hello"") != ""olleh"")
            throw new Exception(""Reverse test failed for 'hello'"");

        if (StringHelper.Reverse(""abc"") != ""cba"")
            throw new Exception(""Reverse test failed for 'abc'"");

        if (StringHelper.Reverse("""") != """")
            throw new Exception(""Empty string test failed"");

        if (StringHelper.Reverse(null) != null)
            throw new Exception(""Null test failed"");
    }
}"
                                        }
                                    ]
                                }
                            ]
                        },
                        new QuestionModel
                        {
                            Id = "string-length-question",
                            Title = "字符串长度计算",
                            Content = "实现安全的字符串长度计算功能",
                            QuestionType = "Implementation",
                            Score = 5,
                            OperationPoints =
                            [
                                new OperationPointModel
                                {
                                    Id = "length-implementation",
                                    Name = "实现GetLength方法",
                                    Description = "安全地计算字符串长度",
                                    ModuleType = ModuleType.CSharp,
                                    Score = 5,
                                    Parameters =
                                    [
                                        new ConfigurationParameterModel
                                        {
                                            Name = "TestCode",
                                            Value = @"
public class StringHelperTests
{
    [Test]
    public void TestGetLength()
    {
        if (StringHelper.GetLength(""hello"") != 5)
            throw new Exception(""Length test failed for 'hello'"");

        if (StringHelper.GetLength("""") != 0)
            throw new Exception(""Empty string length test failed"");

        if (StringHelper.GetLength(null) != 0)
            throw new Exception(""Null string length test failed"");
    }
}"
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    /// <summary>
    /// 快速验证文件评分功能
    /// </summary>
    private static async Task QuickTestFileScoringAsync()
    {
        try
        {
            CSharpScoringService service = new();

            // 测试支持的文件扩展名
            var extensions = service.GetSupportedExtensions().ToList();
            Console.WriteLine($"✅ 支持的文件扩展名: {string.Join(", ", extensions)}");

            // 创建简单测试文件
            string testFile = Path.Combine(Path.GetTempPath(), "quick_test.cs");
            await File.WriteAllTextAsync(testFile, "public class Test { public int Add(int a, int b) => a + b; }");

            // 验证文件处理能力
            bool canProcess = service.CanProcessFile(testFile);
            Console.WriteLine($"✅ 可以处理测试文件: {canProcess}");

            // 创建最小试卷模型
            ExamModel exam = new()
            {
                Modules =
                [
                    new ExamModuleModel
                    {
                        Type = ModuleType.CSharp,
                        Questions =
                        [
                            new QuestionModel
                            {
                                QuestionType = "Implementation",
                                OperationPoints =
                                [
                                    new OperationPointModel
                                    {
                                        ModuleType = ModuleType.CSharp,
                                        Score = 1
                                    }
                                ]
                            }
                        ]
                    }
                ]
            };

            // 执行文件评分
            ScoringResult result = await service.ScoreFileAsync(testFile, exam);
            Console.WriteLine($"✅ 文件评分执行: {(result.IsSuccess ? "成功" : "失败")}");
            Console.WriteLine($"   总分: {result.TotalScore}, 得分: {result.AchievedScore}");
            Console.WriteLine($"   知识点数量: {result.KnowledgePointResults.Count}");
            Console.WriteLine($"   耗时: {result.ElapsedMilliseconds}ms");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"   错误信息: {result.ErrorMessage}");
            }

            // 测试单题目评分
            var question = exam.Modules.First().Questions.First();
            ScoringResult questionResult = await service.ScoreQuestionAsync(testFile, question);
            Console.WriteLine($"✅ 单题目评分: {(questionResult.IsSuccess ? "成功" : "失败")}");

            // 清理
            File.Delete(testFile);
            Console.WriteLine($"✅ 文件评分功能验证完成!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 快速测试异常: {ex.Message}");
        }
    }
}
