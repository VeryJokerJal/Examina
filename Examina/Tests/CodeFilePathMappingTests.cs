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
/// CodeFilePath 和 DocumentFilePath 字段映射测试
/// </summary>
public class CodeFilePathMappingTests
{
    private readonly Mock<ILogger<BenchSuiteIntegrationService>> _mockLogger;
    private readonly BenchSuiteIntegrationService _service;

    public CodeFilePathMappingTests()
    {
        _mockLogger = new Mock<ILogger<BenchSuiteIntegrationService>>();
        _service = new BenchSuiteIntegrationService(_mockLogger.Object);
    }

    /// <summary>
    /// 测试模拟考试中 CodeFilePath 和 DocumentFilePath 字段的映射
    /// </summary>
    [Fact]
    public void MapMockExamToExamModel_ShouldMapFilePathFields()
    {
        // Arrange
        var mockExamDto = new StudentMockExamDto
        {
            Id = 1,
            Name = "文件路径映射测试",
            Description = "测试CodeFilePath和DocumentFilePath字段映射",
            TotalScore = 100,
            DurationMinutes = 60,
            Questions = new ObservableCollection<StudentMockExamQuestionDto>
            {
                new()
                {
                    OriginalQuestionId = 1,
                    Title = "C# 代码文件题目",
                    Content = "加载并编辑C#代码文件",
                    Score = 100,
                    SortOrder = 1,
                    CodeFilePath = "C:\\Code\\Program.cs",
                    DocumentFilePath = "C:\\Documents\\Document.docx",
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
        
        // 验证文件路径字段的映射
        Assert.Equal("C:\\Code\\Program.cs", question.CodeFilePath);
        Assert.Equal("C:\\Documents\\Document.docx", question.DocumentFilePath);
        
        // 验证其他C#字段
        Assert.Equal("test input", question.ProgramInput);
        Assert.Equal("test output", question.ExpectedOutput);
    }

    /// <summary>
    /// 测试综合实训中 CodeFilePath 和 DocumentFilePath 字段的映射
    /// </summary>
    [Fact]
    public void MapComprehensiveTrainingToExamModel_ShouldMapFilePathFields()
    {
        // Arrange
        var trainingDto = new StudentComprehensiveTrainingDto
        {
            Id = 1,
            Name = "综合实训文件路径测试",
            Description = "测试综合实训文件路径映射",
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
                            Title = "C# 综合代码题目",
                            Content = "综合实训C#代码文件处理",
                            Score = 200,
                            SortOrder = 1,
                            CodeFilePath = "C:\\Training\\ComprehensiveCode.cs",
                            DocumentFilePath = "C:\\Training\\TrainingDoc.xlsx",
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
        
        // 验证文件路径字段的映射
        Assert.Equal("C:\\Training\\ComprehensiveCode.cs", question.CodeFilePath);
        Assert.Equal("C:\\Training\\TrainingDoc.xlsx", question.DocumentFilePath);
        
        // 验证其他C#字段
        Assert.Equal("comprehensive input", question.ProgramInput);
        Assert.Equal("comprehensive output", question.ExpectedOutput);
    }

    /// <summary>
    /// 测试专项训练中 CodeFilePath 和 DocumentFilePath 字段的映射
    /// </summary>
    [Fact]
    public void MapSpecializedTrainingToExamModel_ShouldMapFilePathFields()
    {
        // Arrange
        var trainingDto = new StudentSpecializedTrainingDto
        {
            Id = 1,
            Name = "专项训练文件路径测试",
            Description = "测试专项训练文件路径映射",
            ModuleType = "CSharp",
            TotalScore = 150,
            Duration = 75,
            Questions = new ObservableCollection<StudentSpecializedTrainingQuestionDto>
            {
                new()
                {
                    Id = 1,
                    Title = "C# 专项代码题目",
                    Content = "专项训练C#代码文件处理",
                    Score = 150,
                    Order = 1,
                    CodeFilePath = "C:\\Specialized\\SpecialCode.cs",
                    DocumentFilePath = "C:\\Specialized\\SpecialDoc.pptx",
                    ProgramInput = "specialized input",
                    ExpectedOutput = "specialized output",
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
        
        // 验证文件路径字段的映射
        Assert.Equal("C:\\Specialized\\SpecialCode.cs", question.CodeFilePath);
        Assert.Equal("C:\\Specialized\\SpecialDoc.pptx", question.DocumentFilePath);
        
        // 验证其他C#字段
        Assert.Equal("specialized input", question.ProgramInput);
        Assert.Equal("specialized output", question.ExpectedOutput);
    }

    /// <summary>
    /// 测试空文件路径的处理
    /// </summary>
    [Fact]
    public void MapToExamModel_WithNullFilePaths_ShouldHandleGracefully()
    {
        // Arrange
        var mockExamDto = new StudentMockExamDto
        {
            Id = 1,
            Name = "空文件路径测试",
            Description = "测试空文件路径的处理",
            TotalScore = 50,
            DurationMinutes = 30,
            Questions = new ObservableCollection<StudentMockExamQuestionDto>
            {
                new()
                {
                    OriginalQuestionId = 1,
                    Title = "无文件路径题目",
                    Content = "没有文件路径的题目",
                    Score = 50,
                    SortOrder = 1,
                    CodeFilePath = null, // 空的代码文件路径
                    DocumentFilePath = null, // 空的文档文件路径
                    OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>
                    {
                        new()
                        {
                            Id = 1,
                            Name = "基础操作",
                            ModuleType = "C#",
                            Score = 50,
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
        
        // 验证空文件路径的处理
        Assert.Null(question.CodeFilePath);
        Assert.Null(question.DocumentFilePath);
        
        // 确保其他字段正常
        Assert.Equal("无文件路径题目", question.Title);
        Assert.Equal(50.0, question.Score);
    }
}
