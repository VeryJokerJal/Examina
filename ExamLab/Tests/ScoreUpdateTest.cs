using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ExamLab.Models;
using ExamLab.Services;

namespace ExamLab.Tests;

/// <summary>
/// 分值更新功能测试
/// </summary>
public static class ScoreUpdateTest
{
    /// <summary>
    /// 测试分值更新链条
    /// </summary>
    public static async Task TestScoreUpdateChainAsync()
    {
        Console.WriteLine("=== 分值更新功能测试 ===");
        
        // 创建测试数据
        ExamModule module = CreateTestModule();
        Question question = CreateTestQuestion();
        OperationPoint operationPoint = CreateTestOperationPoint();
        
        // 添加到集合中
        module.Questions.Add(question);
        question.OperationPoints.Add(operationPoint);
        
        Console.WriteLine($"初始状态:");
        Console.WriteLine($"  操作点分值: {operationPoint.Score}");
        Console.WriteLine($"  操作点启用状态: {operationPoint.IsEnabled}");
        Console.WriteLine($"  题目操作点数量: {question.OperationPoints.Count}");
        Console.WriteLine($"  题目C#类型: {question.CSharpQuestionType}");
        Console.WriteLine($"  题目总分: {question.TotalScore}");
        Console.WriteLine($"  模块总分: {module.TotalScore}");
        
        // 设置事件监听来验证通知
        bool questionTotalScoreChanged = false;
        bool moduleTotalScoreChanged = false;
        
        question.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Question.TotalScore))
            {
                questionTotalScoreChanged = true;
                Console.WriteLine($"  ✓ Question.TotalScore PropertyChanged 事件触发");
            }
        };
        
        module.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ExamModule.TotalScore))
            {
                moduleTotalScoreChanged = true;
                Console.WriteLine($"  ✓ ExamModule.TotalScore PropertyChanged 事件触发");
            }
        };
        
        // 修改操作点分值
        Console.WriteLine("\n修改操作点分值从 5 到 10...");
        operationPoint.Score = 10;

        // 等待一小段时间确保事件传播
        await Task.Delay(100);
        
        Console.WriteLine($"\n修改后状态:");
        Console.WriteLine($"  操作点分值: {operationPoint.Score}");
        Console.WriteLine($"  操作点启用状态: {operationPoint.IsEnabled}");
        Console.WriteLine($"  题目操作点数量: {question.OperationPoints.Count}");
        Console.WriteLine($"  题目总分: {question.TotalScore}");
        Console.WriteLine($"  模块总分: {module.TotalScore}");
        
        // 验证事件是否触发
        Console.WriteLine($"\n事件触发验证:");
        Console.WriteLine($"  Question.TotalScore 事件触发: {(questionTotalScoreChanged ? "✓" : "✗")}");
        Console.WriteLine($"  ExamModule.TotalScore 事件触发: {(moduleTotalScoreChanged ? "✓" : "✗")}");
        
        // 测试禁用操作点
        Console.WriteLine("\n禁用操作点...");
        questionTotalScoreChanged = false;
        moduleTotalScoreChanged = false;
        
        operationPoint.IsEnabled = false;

        // 等待一小段时间确保事件传播
        await Task.Delay(100);
        
        Console.WriteLine($"\n禁用后状态:");
        Console.WriteLine($"  操作点启用状态: {operationPoint.IsEnabled}");
        Console.WriteLine($"  题目总分: {question.TotalScore}");
        Console.WriteLine($"  模块总分: {module.TotalScore}");
        
        Console.WriteLine($"\n事件触发验证:");
        Console.WriteLine($"  Question.TotalScore 事件触发: {(questionTotalScoreChanged ? "✓" : "✗")}");
        Console.WriteLine($"  ExamModule.TotalScore 事件触发: {(moduleTotalScoreChanged ? "✓" : "✗")}");
    }
    
    /// <summary>
    /// 测试反序列化后的分值更新
    /// </summary>
    public static async Task TestDeserializationScoreUpdateAsync()
    {
        Console.WriteLine("\n=== 反序列化后分值更新测试 ===");
        
        // 创建测试数据并序列化
        ExamModule originalModule = CreateTestModule();
        Question originalQuestion = CreateTestQuestion();
        OperationPoint originalOperationPoint = CreateTestOperationPoint();
        
        originalModule.Questions.Add(originalQuestion);
        originalQuestion.OperationPoints.Add(originalOperationPoint);
        
        // 模拟反序列化过程（创建新对象）
        ExamModule deserializedModule = new()
        {
            Id = originalModule.Id,
            Name = originalModule.Name,
            Type = originalModule.Type,
            Description = originalModule.Description,
            Score = originalModule.Score,
            Order = originalModule.Order,
            IsEnabled = originalModule.IsEnabled
        };
        
        Question deserializedQuestion = new()
        {
            Id = originalQuestion.Id,
            Title = originalQuestion.Title,
            Content = originalQuestion.Content,
            Order = originalQuestion.Order,
            IsEnabled = originalQuestion.IsEnabled
        };
        
        OperationPoint deserializedOperationPoint = new()
        {
            Id = originalOperationPoint.Id,
            Name = originalOperationPoint.Name,
            Description = originalOperationPoint.Description,
            ModuleType = originalOperationPoint.ModuleType,
            Score = originalOperationPoint.Score,
            Order = originalOperationPoint.Order,
            IsEnabled = originalOperationPoint.IsEnabled
        };
        
        // 重建对象关系
        deserializedModule.Questions.Add(deserializedQuestion);
        deserializedQuestion.OperationPoints.Add(deserializedOperationPoint);
        
        // 重新初始化事件监听
        deserializedModule.ReinitializeEventListeners();
        
        Console.WriteLine($"反序列化后初始状态:");
        Console.WriteLine($"  操作点分值: {deserializedOperationPoint.Score}");
        Console.WriteLine($"  题目总分: {deserializedQuestion.TotalScore}");
        Console.WriteLine($"  模块总分: {deserializedModule.TotalScore}");
        
        // 设置事件监听
        bool questionTotalScoreChanged = false;
        bool moduleTotalScoreChanged = false;
        
        deserializedQuestion.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(Question.TotalScore))
            {
                questionTotalScoreChanged = true;
                Console.WriteLine($"  ✓ 反序列化后 Question.TotalScore PropertyChanged 事件触发");
            }
        };
        
        deserializedModule.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ExamModule.TotalScore))
            {
                moduleTotalScoreChanged = true;
                Console.WriteLine($"  ✓ 反序列化后 ExamModule.TotalScore PropertyChanged 事件触发");
            }
        };
        
        // 修改分值
        Console.WriteLine("\n修改反序列化后的操作点分值...");
        deserializedOperationPoint.Score = 15;

        // 等待一小段时间确保事件传播
        await Task.Delay(100);
        
        Console.WriteLine($"\n修改后状态:");
        Console.WriteLine($"  操作点分值: {deserializedOperationPoint.Score}");
        Console.WriteLine($"  题目总分: {deserializedQuestion.TotalScore}");
        Console.WriteLine($"  模块总分: {deserializedModule.TotalScore}");
        
        Console.WriteLine($"\n事件触发验证:");
        Console.WriteLine($"  Question.TotalScore 事件触发: {(questionTotalScoreChanged ? "✓" : "✗")}");
        Console.WriteLine($"  ExamModule.TotalScore 事件触发: {(moduleTotalScoreChanged ? "✓" : "✗")}");
    }
    
    private static ExamModule CreateTestModule()
    {
        return new ExamModule
        {
            Id = IdGeneratorService.GenerateModuleId(),
            Name = "测试模块",
            Type = ModuleType.Excel,
            Description = "用于测试的模块",
            Score = 100,
            Order = 1,
            IsEnabled = true
        };
    }
    
    private static Question CreateTestQuestion()
    {
        return new Question
        {
            Id = IdGeneratorService.GenerateQuestionId(),
            Title = "测试题目",
            Content = "用于测试的题目内容",
            Order = 1,
            IsEnabled = true
            // 不设置CSharpQuestionType，使用默认值
            // 由于我们会添加操作点，CalculateTotalScore会优先使用操作点分数
        };
    }
    
    private static OperationPoint CreateTestOperationPoint()
    {
        return new OperationPoint
        {
            Id = IdGeneratorService.GenerateOperationId(),
            Name = "测试操作点",
            Description = "用于测试的操作点",
            ModuleType = ModuleType.Excel,
            Score = 5,
            Order = 1,
            IsEnabled = true
        };
    }
}
