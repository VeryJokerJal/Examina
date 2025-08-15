using BenchSuite.Services;
using BenchSuite.Models;

namespace BenchSuite.Tests;

/// <summary>
/// 文件评分功能测试程序
/// </summary>
public class TestFileScoring
{
    /// <summary>
    /// 运行文件评分测试
    /// </summary>
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🎯 C#文件评分功能测试");
        Console.WriteLine("=".PadRight(50, '='));
        Console.WriteLine();

        try
        {
            await TestBasicFileScoringAsync();
            Console.WriteLine();
            
            await TestInvalidFileHandlingAsync();
            Console.WriteLine();
            
            await TestMultipleQuestionsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 测试过程中发生异常: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }

        Console.WriteLine();
        Console.WriteLine("测试完成！");
    }

    /// <summary>
    /// 测试基本的文件评分功能
    /// </summary>
    private static async Task TestBasicFileScoringAsync()
    {
        Console.WriteLine("📝 测试基本文件评分功能");

        CSharpScoringService service = new();
        
        // 创建测试文件
        string testFile = Path.Combine(Path.GetTempPath(), "basic_test.cs");
        string code = @"
using System;

public class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}";

        await File.WriteAllTextAsync(testFile, code);
        Console.WriteLine($"✅ 测试文件已创建: {testFile}");

        // 创建简单的试卷模型
        ExamModel exam = new()
        {
            Id = "test-001",
            Name = "基础测试",
            Modules = 
            [
                new ExamModuleModel
                {
                    Type = ModuleType.CSharp,
                    Questions = 
                    [
                        new QuestionModel
                        {
                            Id = "q1",
                            Title = "实现加法",
                            QuestionType = "Implementation",
                            OperationPoints = 
                            [
                                new OperationPointModel
                                {
                                    ModuleType = ModuleType.CSharp,
                                    Score = 10,
                                    Parameters = 
                                    [
                                        new ConfigurationParameterModel
                                        {
                                            Name = "TestCode",
                                            Value = "// 简单测试"
                                        }
                                    ]
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // 执行评分
        ScoringResult result = await service.ScoreFileAsync(testFile, exam);
        
        Console.WriteLine($"   评分状态: {(result.IsSuccess ? "成功✅" : "失败❌")}");
        Console.WriteLine($"   总分: {result.TotalScore}");
        Console.WriteLine($"   得分: {result.AchievedScore}");
        Console.WriteLine($"   耗时: {result.ElapsedMilliseconds}ms");

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            Console.WriteLine($"   错误: {result.ErrorMessage}");
        }

        // 清理
        File.Delete(testFile);
        Console.WriteLine("🗑️ 测试文件已清理");
    }

    /// <summary>
    /// 测试无效文件处理
    /// </summary>
    private static async Task TestInvalidFileHandlingAsync()
    {
        Console.WriteLine("🚫 测试无效文件处理");

        CSharpScoringService service = new();
        ExamModel exam = new() { Modules = [] };

        // 测试不存在的文件
        ScoringResult result1 = await service.ScoreFileAsync("nonexistent.cs", exam);
        Console.WriteLine($"   不存在文件: {(result1.IsSuccess ? "意外成功" : "正确失败❌")}");

        // 测试不支持的文件类型
        string txtFile = Path.Combine(Path.GetTempPath(), "test.xyz");
        await File.WriteAllTextAsync(txtFile, "test");
        
        bool canProcess = service.CanProcessFile(txtFile);
        Console.WriteLine($"   不支持的扩展名: {(canProcess ? "意外支持" : "正确拒绝❌")}");
        
        File.Delete(txtFile);

        // 测试空文件
        string emptyFile = Path.Combine(Path.GetTempPath(), "empty.cs");
        await File.WriteAllTextAsync(emptyFile, "");
        
        ScoringResult result2 = await service.ScoreFileAsync(emptyFile, exam);
        Console.WriteLine($"   空文件处理: {(result2.IsSuccess ? "意外成功" : "正确失败❌")}");
        
        File.Delete(emptyFile);
    }

    /// <summary>
    /// 测试多题目评分
    /// </summary>
    private static async Task TestMultipleQuestionsAsync()
    {
        Console.WriteLine("📚 测试多题目评分");

        CSharpScoringService service = new();
        
        // 创建包含多个方法的测试文件
        string testFile = Path.Combine(Path.GetTempPath(), "multi_test.cs");
        string code = @"
using System;

public class MathHelper
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
    public int Multiply(int a, int b) => a * b;
}";

        await File.WriteAllTextAsync(testFile, code);

        // 创建包含多个题目的试卷
        ExamModel exam = new()
        {
            Id = "multi-test",
            Name = "多题目测试",
            Modules = 
            [
                new ExamModuleModel
                {
                    Type = ModuleType.CSharp,
                    Questions = 
                    [
                        new QuestionModel
                        {
                            Id = "q1",
                            Title = "加法实现",
                            QuestionType = "Implementation",
                            OperationPoints = 
                            [
                                new OperationPointModel
                                {
                                    ModuleType = ModuleType.CSharp,
                                    Score = 5
                                }
                            ]
                        },
                        new QuestionModel
                        {
                            Id = "q2", 
                            Title = "减法实现",
                            QuestionType = "Implementation",
                            OperationPoints = 
                            [
                                new OperationPointModel
                                {
                                    ModuleType = ModuleType.CSharp,
                                    Score = 5
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        ScoringResult result = await service.ScoreFileAsync(testFile, exam);
        
        Console.WriteLine($"   多题目评分: {(result.IsSuccess ? "成功✅" : "失败❌")}");
        Console.WriteLine($"   题目数量: {exam.Modules.SelectMany(m => m.Questions).Count()}");
        Console.WriteLine($"   知识点数量: {result.KnowledgePointResults.Count}");
        Console.WriteLine($"   总分: {result.TotalScore}");
        Console.WriteLine($"   得分: {result.AchievedScore}");

        File.Delete(testFile);
        Console.WriteLine("🗑️ 测试文件已清理");
    }
}
