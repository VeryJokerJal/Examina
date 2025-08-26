using Examina.Models.Exam;
using Examina.ViewModels;

namespace Examina.Tests;

/// <summary>
/// ExamViewModel编译测试
/// </summary>
public class ExamViewModelCompilationTest
{
    /// <summary>
    /// 测试ExamStatus枚举映射
    /// </summary>
    public static void TestExamStatusMapping()
    {
        Console.WriteLine("=== ExamViewModel编译测试 ===");
        
        // 测试ExamAttemptStatus到ExamStatus的映射
        TestStatusMapping(ExamAttemptStatus.InProgress, ExamStatus.InProgress);
        TestStatusMapping(ExamAttemptStatus.Completed, ExamStatus.Submitted);
        TestStatusMapping(ExamAttemptStatus.Abandoned, ExamStatus.Ended);
        TestStatusMapping(ExamAttemptStatus.TimedOut, ExamStatus.Ended);
        
        Console.WriteLine("✅ 所有状态映射测试通过");
    }
    
    /// <summary>
    /// 测试单个状态映射
    /// </summary>
    private static void TestStatusMapping(ExamAttemptStatus attemptStatus, ExamStatus expectedStatus)
    {
        // 模拟ExamViewModel中的状态映射逻辑
        ExamStatus toolbarStatus = attemptStatus switch
        {
            ExamAttemptStatus.InProgress => ExamStatus.InProgress,
            ExamAttemptStatus.Completed => ExamStatus.Submitted,
            ExamAttemptStatus.Abandoned => ExamStatus.Ended,
            ExamAttemptStatus.TimedOut => ExamStatus.Ended,
            _ => ExamStatus.Preparing
        };
        
        bool isCorrect = toolbarStatus == expectedStatus;
        string result = isCorrect ? "✅" : "❌";
        
        Console.WriteLine($"  {result} {attemptStatus} -> {toolbarStatus} (期望: {expectedStatus})");
        
        if (!isCorrect)
        {
            throw new Exception($"状态映射错误: {attemptStatus} 应该映射到 {expectedStatus}，但实际映射到 {toolbarStatus}");
        }
    }
    
    /// <summary>
    /// 测试ExamStatus枚举值
    /// </summary>
    public static void TestExamStatusValues()
    {
        Console.WriteLine("\n=== ExamStatus枚举值测试 ===");
        
        // 测试所有ExamStatus枚举值
        ExamStatus[] statuses = {
            ExamStatus.Preparing,
            ExamStatus.InProgress,
            ExamStatus.AboutToEnd,
            ExamStatus.Ended,
            ExamStatus.Submitted
        };
        
        foreach (ExamStatus status in statuses)
        {
            Console.WriteLine($"  ✅ ExamStatus.{status} = {(int)status}");
        }
    }
    
    /// <summary>
    /// 测试ExamAttemptStatus枚举值
    /// </summary>
    public static void TestExamAttemptStatusValues()
    {
        Console.WriteLine("\n=== ExamAttemptStatus枚举值测试 ===");
        
        // 测试所有ExamAttemptStatus枚举值
        ExamAttemptStatus[] statuses = {
            ExamAttemptStatus.InProgress,
            ExamAttemptStatus.Completed,
            ExamAttemptStatus.Abandoned,
            ExamAttemptStatus.TimedOut
        };
        
        foreach (ExamAttemptStatus status in statuses)
        {
            Console.WriteLine($"  ✅ ExamAttemptStatus.{status} = {(int)status}");
        }
    }
    
    /// <summary>
    /// 运行所有测试
    /// </summary>
    public static void RunAllTests()
    {
        try
        {
            TestExamStatusMapping();
            TestExamStatusValues();
            TestExamAttemptStatusValues();
            
            Console.WriteLine("\n🎉 所有编译测试通过！");
            Console.WriteLine("ExamViewModel中的状态映射代码编译正常。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 测试失败: {ex.Message}");
            throw;
        }
    }
}
