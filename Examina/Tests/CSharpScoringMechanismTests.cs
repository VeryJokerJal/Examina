using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using BenchSuite.Models;
using Examina.Services;
using Examina.Models.MockExam;
using System.Collections.ObjectModel;

namespace Examina.Tests;

/// <summary>
/// C# 模块评分机制测试
/// </summary>
public class CSharpScoringMechanismTests
{
    private readonly Mock<ILogger<BenchSuiteIntegrationService>> _mockLogger;
    private readonly BenchSuiteIntegrationService _service;

    public CSharpScoringMechanismTests()
    {
        _mockLogger = new Mock<ILogger<BenchSuiteIntegrationService>>();
        _service = new BenchSuiteIntegrationService(_mockLogger.Object);
    }

    /// <summary>
    /// 测试 C# 题目的特殊评分机制 - 有操作点的情况
    /// </summary>
    [Fact]
    public void CalculateCSharpQuestionScore_WithOperationPoints_ShouldUseOperationPointsSum()
    {
        // Arrange - C# 题目有操作点的情况
        var questionWithOperationPoints = new StudentMockExamQuestionDto
        {
            OriginalQuestionId = 1,
            Title = "C# 编程题",
            Content = "编写C#程序",
            Score = 50, // 题目原始分数
            OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>
            {
                new()
                {
                    Id = 1,
                    Name = "C# 操作1",
                    ModuleType = "C#",
                    Score = 30,
                    Order = 1,
                    Parameters = new ObservableCollection<StudentMockExamParameterDto>()
                },
                new()
                {
                    Id = 2,
                    Name = "C# 操作2", 
                    ModuleType = "C#",
                    Score = 20,
                    Order = 2,
                    Parameters = new ObservableCollection<StudentMockExamParameterDto>()
                }
            }
        };

        // Act - 使用反射调用私有方法
        var method = typeof(BenchSuiteIntegrationService).GetMethod("CalculateCSharpQuestionScore", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null,
            new Type[] { typeof(StudentMockExamQuestionDto), typeof(ModuleType) },
            null);
        
        var result = (decimal)method!.Invoke(null, new object[] { questionWithOperationPoints, ModuleType.CSharp })!;

        // Assert - 应该使用操作点分数总和 (30 + 20 = 50)，而不是题目原始分数
        Assert.Equal(50m, result);
    }

    /// <summary>
    /// 测试 C# 题目没有操作点时的评分机制
    /// </summary>
    [Fact]
    public void CalculateCSharpQuestionScore_WithoutOperationPoints_ShouldUseOriginalScore()
    {
        // Arrange - C# 题目没有操作点的情况
        var questionWithoutOperationPoints = new StudentMockExamQuestionDto
        {
            OriginalQuestionId = 1,
            Title = "C# 编程实现",
            Content = "使用C#语言编写一个class",
            Score = 100, // 题目原始分数
            OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>() // 空的操作点列表
        };

        // Act - 使用反射调用私有方法
        var method = typeof(BenchSuiteIntegrationService).GetMethod("CalculateCSharpQuestionScore", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null,
            new Type[] { typeof(StudentMockExamQuestionDto), typeof(ModuleType) },
            null);
        
        var result = (decimal)method!.Invoke(null, new object[] { questionWithoutOperationPoints, ModuleType.CSharp })!;

        // Assert - 应该使用题目原始分数
        Assert.Equal(100m, result);
    }

    /// <summary>
    /// 测试非 C# 模块的评分机制
    /// </summary>
    [Fact]
    public void CalculateCSharpQuestionScore_NonCSharpModule_ShouldUseOriginalScore()
    {
        // Arrange - Excel 题目
        var excelQuestion = new StudentMockExamQuestionDto
        {
            OriginalQuestionId = 1,
            Title = "Excel 操作题",
            Content = "完成Excel表格操作",
            Score = 80,
            OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>
            {
                new()
                {
                    Id = 1,
                    Name = "Excel 操作",
                    ModuleType = "Excel",
                    Score = 80,
                    Order = 1,
                    Parameters = new ObservableCollection<StudentMockExamParameterDto>()
                }
            }
        };

        // Act - 使用反射调用私有方法
        var method = typeof(BenchSuiteIntegrationService).GetMethod("CalculateCSharpQuestionScore", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null,
            new Type[] { typeof(StudentMockExamQuestionDto), typeof(ModuleType) },
            null);
        
        var result = (decimal)method!.Invoke(null, new object[] { excelQuestion, ModuleType.Excel })!;

        // Assert - 非C#模块应该直接使用题目原始分数
        Assert.Equal(80m, result);
    }

    /// <summary>
    /// 测试 C# 题目类型推断
    /// </summary>
    [Theory]
    [InlineData("代码补全题", "请补全以下代码", "CodeCompletion")]
    [InlineData("调试纠错", "找出代码中的错误", "Debugging")]
    [InlineData("编写实现", "实现一个方法", "Implementation")]
    [InlineData("C# 编程", "编写程序", "CodeCompletion")] // 默认类型
    public void GetCSharpQuestionTypeString_ShouldCorrectlyInferType(string title, string content, string expectedType)
    {
        // Arrange
        var question = new StudentMockExamQuestionDto
        {
            Title = title,
            Content = content
        };

        // Act - 使用反射调用私有方法
        var method = typeof(BenchSuiteIntegrationService).GetMethod("GetCSharpQuestionTypeString", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null,
            new Type[] { typeof(StudentMockExamQuestionDto) },
            null);
        
        var result = (string?)method!.Invoke(null, new object[] { question });

        // Assert
        Assert.Equal(expectedType, result);
    }

    /// <summary>
    /// 测试完整的 C# 题目映射，验证评分机制的正确应用
    /// </summary>
    [Fact]
    public void MapMockExamToExamModel_CSharpQuestion_ShouldApplyCorrectScoring()
    {
        // Arrange
        var mockExamDto = new StudentMockExamDto
        {
            Id = 1,
            Name = "C# 评分测试",
            Description = "测试C#题目的评分机制",
            TotalScore = 150,
            DurationMinutes = 120,
            Questions = new ObservableCollection<StudentMockExamQuestionDto>
            {
                // 有操作点的C#题目
                new()
                {
                    OriginalQuestionId = 1,
                    Title = "C# 编程题",
                    Content = "编写C#程序",
                    Score = 100, // 原始分数
                    SortOrder = 1,
                    ProgramInput = "test input",
                    ExpectedOutput = "test output",
                    OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>
                    {
                        new()
                        {
                            Id = 1,
                            Name = "C# 操作1",
                            ModuleType = "C#",
                            Score = 60, // 操作点分数
                            Order = 1,
                            Parameters = new ObservableCollection<StudentMockExamParameterDto>()
                        },
                        new()
                        {
                            Id = 2,
                            Name = "C# 操作2",
                            ModuleType = "C#",
                            Score = 40, // 操作点分数
                            Order = 2,
                            Parameters = new ObservableCollection<StudentMockExamParameterDto>()
                        }
                    }
                },
                // 没有操作点的C#题目
                new()
                {
                    OriginalQuestionId = 2,
                    Title = "C# 编程实现",
                    Content = "使用C#语言编写一个class",
                    Score = 50, // 原始分数
                    SortOrder = 2,
                    ProgramInput = "input2",
                    ExpectedOutput = "output2",
                    OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>() // 空操作点
                }
            }
        };

        // Act - 使用反射调用私有方法
        var method = typeof(BenchSuiteIntegrationService).GetMethod("MapMockExamToExamModel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = (ExamModel)method!.Invoke(_service, new object[] { mockExamDto, ModuleType.CSharp })!;

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Modules); // 应该有一个 C# 模块
        Assert.Equal(ModuleType.CSharp, result.Modules[0].Type);
        Assert.Equal(2, result.Modules[0].Questions.Count); // 应该有两个 C# 题目

        // 验证第一个题目（有操作点）的评分
        var firstQuestion = result.Modules[0].Questions.First(q => q.Id == "1");
        Assert.Equal(100.0, firstQuestion.Score); // 应该使用操作点分数总和 (60 + 40 = 100)
        Assert.Equal(2, firstQuestion.OperationPoints.Count); // 应该有两个操作点
        Assert.Equal("test input", firstQuestion.ProgramInput);
        Assert.Equal("test output", firstQuestion.ExpectedOutput);

        // 验证第二个题目（没有操作点）的评分
        var secondQuestion = result.Modules[0].Questions.First(q => q.Id == "2");
        Assert.Equal(50.0, secondQuestion.Score); // 应该使用原始分数
        Assert.Single(secondQuestion.OperationPoints); // 应该有一个自动创建的默认操作点
        Assert.Equal("C#编程操作", secondQuestion.OperationPoints[0].Name);
        Assert.Equal("input2", secondQuestion.ProgramInput);
        Assert.Equal("output2", secondQuestion.ExpectedOutput);
    }
}
