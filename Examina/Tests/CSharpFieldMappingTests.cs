using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using BenchSuite.Models;
using Examina.Services;
using Examina.Models.MockExam;
using Examina.Models.Exam;
using Examina.Models.SpecializedTraining;
using System.Collections.ObjectModel;

namespace Examina.Tests;

/// <summary>
/// C# 字段映射测试
/// </summary>
public class CSharpFieldMappingTests
{
    private readonly Mock<ILogger<BenchSuiteIntegrationService>> _mockLogger;
    private readonly BenchSuiteIntegrationService _service;

    public CSharpFieldMappingTests()
    {
        _mockLogger = new Mock<ILogger<BenchSuiteIntegrationService>>();
        _service = new BenchSuiteIntegrationService(_mockLogger.Object);
    }

    /// <summary>
    /// 测试模拟考试中新增字段的映射
    /// </summary>
    [Fact]
    public void MapMockExamToExamModel_ShouldMapAdditionalFields()
    {
        // Arrange
        var mockExamDto = new StudentMockExamDto
        {
            Id = 1,
            Name = "字段映射测试",
            Description = "测试新增字段的映射",
            TotalScore = 100,
            DurationMinutes = 60,
            Questions = new ObservableCollection<StudentMockExamQuestionDto>
            {
                new()
                {
                    OriginalQuestionId = 1,
                    Title = "C# 配置题目",
                    Content = "测试题目配置和验证规则",
                    Score = 100,
                    SortOrder = 1,
                    QuestionConfig = "{\"type\":\"coding\",\"language\":\"csharp\"}",
                    AnswerValidationRules = "{\"required\":true,\"minLength\":10}",
                    Tags = "C#,编程,测试",
                    ProgramInput = "test input",
                    ExpectedOutput = "test output",
                    OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>
                    {
                        new()
                        {
                            Id = 1,
                            Name = "C# 操作",
                            ModuleType = "C#",
                            Score = 100,
                            Order = 1,
                            Parameters = new ObservableCollection<StudentMockExamParameterDto>()
                        }
                    }
                }
            }
        };

        // Act - 使用反射调用私有方法
        var method = typeof(BenchSuiteIntegrationService).GetMethod("MapMockExamToExamModel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = (ExamModel)method!.Invoke(_service, new object[] { mockExamDto, ModuleType.CSharp })!;

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Modules);
        Assert.Single(result.Modules[0].Questions);
        
        var question = result.Modules[0].Questions[0];
        
        // 验证新增字段的映射
        Assert.Equal("{\"type\":\"coding\",\"language\":\"csharp\"}", question.QuestionConfig);
        Assert.Equal("{\"required\":true,\"minLength\":10}", question.AnswerValidationRules);
        Assert.Equal("C#,编程,测试", question.Tags);
        
        // 验证C#特有字段
        Assert.Equal("test input", question.ProgramInput);
        Assert.Equal("test output", question.ExpectedOutput);
        Assert.Equal("CodeCompletion", question.CSharpQuestionType); // 默认推断类型
        Assert.Equal(100.0, question.CSharpDirectScore);
    }

    /// <summary>
    /// 测试综合实训中新增字段的映射
    /// </summary>
    [Fact]
    public void MapComprehensiveTrainingToExamModel_ShouldMapAdditionalFields()
    {
        // Arrange
        var trainingDto = new StudentComprehensiveTrainingDto
        {
            Id = 1,
            Name = "综合实训字段测试",
            Description = "测试综合实训字段映射",
            TotalScore = 200,
            Duration = 90,
            Modules = new ObservableCollection<StudentComprehensiveTrainingModuleDto>
            {
                new()
                {
                    Id = 1,
                    Name = "C#模块",
                    Type = "CSharp",
                    Description = "C#编程模块",
                    Score = 200,
                    Order = 1,
                    Questions = new List<StudentComprehensiveTrainingQuestionDto>
                    {
                        new()
                        {
                            Id = 1,
                            Title = "C# 综合题目",
                            Content = "综合实训C#题目",
                            Score = 200,
                            SortOrder = 1,
                            QuestionConfig = "{\"framework\":\"netcore\"}",
                            AnswerValidationRules = "{\"timeout\":300}",
                            Tags = "综合,实训,C#",
                            ProgramInput = "comprehensive input",
                            ExpectedOutput = "comprehensive output",
                            OperationPoints = new List<StudentComprehensiveTrainingOperationPointDto>
                            {
                                new()
                                {
                                    Id = 1,
                                    Name = "综合C#操作",
                                    ModuleType = "CSharp",
                                    Score = 200,
                                    Order = 1,
                                    Parameters = new List<StudentComprehensiveTrainingParameterDto>()
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act - 使用反射调用私有方法
        var method = typeof(BenchSuiteIntegrationService).GetMethod("MapComprehensiveTrainingToExamModel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = (ExamModel)method!.Invoke(_service, new object[] { trainingDto, ModuleType.CSharp })!;

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Modules);
        Assert.Single(result.Modules[0].Questions);
        
        var question = result.Modules[0].Questions[0];
        
        // 验证新增字段的映射
        Assert.Equal("{\"framework\":\"netcore\"}", question.QuestionConfig);
        Assert.Equal("{\"timeout\":300}", question.AnswerValidationRules);
        Assert.Equal("综合,实训,C#", question.Tags);
        
        // 验证C#特有字段
        Assert.Equal("comprehensive input", question.ProgramInput);
        Assert.Equal("comprehensive output", question.ExpectedOutput);
        Assert.Equal("CodeCompletion", question.CSharpQuestionType);
        Assert.Equal(200.0, question.CSharpDirectScore);
    }

    /// <summary>
    /// 测试专项训练中新增字段的映射
    /// </summary>
    [Fact]
    public void MapSpecializedTrainingToExamModel_ShouldMapAdditionalFields()
    {
        // Arrange
        var trainingDto = new StudentSpecializedTrainingDto
        {
            Id = 1,
            Name = "专项训练字段测试",
            Description = "测试专项训练字段映射",
            ModuleType = "CSharp",
            TotalScore = 150,
            Duration = 75,
            Questions = new ObservableCollection<StudentSpecializedTrainingQuestionDto>
            {
                new()
                {
                    Id = 1,
                    Title = "C# 专项题目",
                    Content = "专项训练C#题目",
                    Score = 150,
                    Order = 1,
                    QuestionConfig = "{\"specialMode\":true}",
                    AnswerValidationRules = "{\"strictMode\":true}",
                    Tags = "专项,训练,C#",
                    OperationPoints = new ObservableCollection<StudentSpecializedTrainingOperationPointDto>
                    {
                        new()
                        {
                            Id = 1,
                            Name = "专项C#操作",
                            ModuleType = "CSharp",
                            Score = 150,
                            Order = 1,
                            Parameters = new ObservableCollection<StudentSpecializedTrainingParameterDto>()
                        }
                    }
                }
            }
        };

        // Act - 使用反射调用私有方法
        var method = typeof(BenchSuiteIntegrationService).GetMethod("MapSpecializedTrainingToExamModel", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = (ExamModel)method!.Invoke(_service, new object[] { trainingDto, ModuleType.CSharp })!;

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Modules);
        Assert.Single(result.Modules[0].Questions);
        
        var question = result.Modules[0].Questions[0];
        
        // 验证新增字段的映射
        Assert.Equal("{\"specialMode\":true}", question.QuestionConfig);
        Assert.Equal("{\"strictMode\":true}", question.AnswerValidationRules);
        Assert.Equal("专项,训练,C#", question.Tags);
        
        // 验证C#特有字段（专项训练DTO中没有ProgramInput和ExpectedOutput）
        Assert.Null(question.ProgramInput);
        Assert.Null(question.ExpectedOutput);
        Assert.Equal("CodeCompletion", question.CSharpQuestionType);
        Assert.Equal(150.0, question.CSharpDirectScore);
    }

    /// <summary>
    /// 测试C#题目类型推断的准确性
    /// </summary>
    [Theory]
    [InlineData("代码补全练习", "请补全以下C#代码", "CodeCompletion")]
    [InlineData("调试纠错题", "找出并修复代码中的错误", "Debugging")]
    [InlineData("编写实现", "实现一个完整的C#方法", "Implementation")]
    [InlineData("C#编程", "编写程序", "CodeCompletion")] // 默认类型
    public void CSharpQuestionTypeInference_ShouldBeAccurate(string title, string content, string expectedType)
    {
        // Arrange
        var questionDto = new StudentMockExamQuestionDto
        {
            OriginalQuestionId = 1,
            Title = title,
            Content = content,
            Score = 100,
            SortOrder = 1,
            OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>
            {
                new()
                {
                    Id = 1,
                    Name = "C# 操作",
                    ModuleType = "C#",
                    Score = 100,
                    Order = 1,
                    Parameters = new ObservableCollection<StudentMockExamParameterDto>()
                }
            }
        };

        // Act - 使用反射调用私有方法
        var method = typeof(BenchSuiteIntegrationService).GetMethod("GetCSharpQuestionTypeString", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null,
            new Type[] { typeof(StudentMockExamQuestionDto) },
            null);
        
        var result = (string?)method!.Invoke(null, new object[] { questionDto });

        // Assert
        Assert.Equal(expectedType, result);
    }
}
