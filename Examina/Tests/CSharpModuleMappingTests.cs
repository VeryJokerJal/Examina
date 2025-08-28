using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using BenchSuite.Models;
using Examina.Services;
using Examina.Models.MockExam;
using System.Collections.ObjectModel;

namespace Examina.Tests;

/// <summary>
/// C# 模块数据映射测试
/// </summary>
public class CSharpModuleMappingTests
{
    private readonly Mock<ILogger<BenchSuiteIntegrationService>> _mockLogger;
    private readonly BenchSuiteIntegrationService _service;

    public CSharpModuleMappingTests()
    {
        _mockLogger = new Mock<ILogger<BenchSuiteIntegrationService>>();
        _service = new BenchSuiteIntegrationService(_mockLogger.Object);
    }

    /// <summary>
    /// 测试 C# 模块类型的各种字符串变体是否能正确匹配
    /// </summary>
    [Theory]
    [InlineData("CSharp", ModuleType.CSharp)]
    [InlineData("csharp", ModuleType.CSharp)]
    [InlineData("C#", ModuleType.CSharp)]
    [InlineData("c#", ModuleType.CSharp)]
    [InlineData("C-Sharp", ModuleType.CSharp)]
    [InlineData("c_sharp", ModuleType.CSharp)]
    [InlineData("cs", ModuleType.CSharp)]
    [InlineData("dotnet", ModuleType.CSharp)]
    [InlineData(".net", ModuleType.CSharp)]
    [InlineData("编程", ModuleType.CSharp)]
    [InlineData("程序设计", ModuleType.CSharp)]
    public void ParseModuleType_ShouldCorrectlyParseVariants(string input, ModuleType expected)
    {
        // 使用反射访问私有方法进行测试
        var method = typeof(BenchSuiteIntegrationService).GetMethod("ParseModuleType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = (ModuleType)method!.Invoke(null, new object[] { input })!;
        
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试 C# 题目识别功能
    /// </summary>
    [Theory]
    [InlineData("C# 编程题", "编写一个简单的C#程序", null, null, true)]
    [InlineData("编程实现", "使用C#语言实现功能", null, null, true)]
    [InlineData("代码补全", "完成以下代码", null, null, true)]
    [InlineData("程序设计", "设计一个class", null, null, true)]
    [InlineData("控制台输出", "使用Console.WriteLine输出", null, null, true)]
    [InlineData("C#题目", "测试内容", "input", "output", true)]
    [InlineData("Excel操作", "完成表格操作", null, null, false)]
    [InlineData("Word文档", "编辑文档", null, null, false)]
    public void IsCSharpQuestion_ShouldCorrectlyIdentifyQuestions(
        string title, string content, string? programInput, string? expectedOutput, bool expected)
    {
        // Arrange
        var question = new StudentMockExamQuestionDto
        {
            Title = title,
            Content = content,
            ProgramInput = programInput,
            ExpectedOutput = expectedOutput
        };

        // 使用反射访问私有方法进行测试
        var method = typeof(BenchSuiteIntegrationService).GetMethod("IsCSharpQuestion", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = (bool)method!.Invoke(null, new object[] { question })!;
        
        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试模拟考试中 C# 题目的映射（包含操作点的情况）
    /// </summary>
    [Fact]
    public void MapMockExamToExamModel_WithCSharpOperationPoints_ShouldMapCorrectly()
    {
        // Arrange
        var mockExamDto = new StudentMockExamDto
        {
            Id = 1,
            Name = "C# 编程测试",
            Description = "包含C#编程题目的测试",
            TotalScore = 100,
            DurationMinutes = 120,
            Questions = new ObservableCollection<StudentMockExamQuestionDto>
            {
                new()
                {
                    OriginalQuestionId = 1,
                    Title = "C# 基础编程",
                    Content = "编写一个简单的C#程序",
                    Score = 100,
                    SortOrder = 1,
                    ProgramInput = "test input",
                    ExpectedOutput = "test output",
                    OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>
                    {
                        new()
                        {
                            Id = 1,
                            Name = "C# 编程操作",
                            Description = "C# 编程操作点",
                            ModuleType = "C#", // 使用 C# 变体
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
        Assert.Single(result.Modules); // 应该有一个 C# 模块
        Assert.Equal(ModuleType.CSharp, result.Modules[0].Type);
        Assert.Single(result.Modules[0].Questions); // 应该有一个 C# 题目
        Assert.Equal("C# 基础编程", result.Modules[0].Questions[0].Title);
        Assert.Single(result.Modules[0].Questions[0].OperationPoints); // 应该有一个 C# 操作点
        Assert.Equal(ModuleType.CSharp, result.Modules[0].Questions[0].OperationPoints[0].ModuleType);
        
        // 验证 C# 特有字段
        Assert.Equal("test input", result.Modules[0].Questions[0].ProgramInput);
        Assert.Equal("test output", result.Modules[0].Questions[0].ExpectedOutput);
    }

    /// <summary>
    /// 测试模拟考试中 C# 题目的映射（没有操作点的情况）
    /// </summary>
    [Fact]
    public void MapMockExamToExamModel_WithoutOperationPoints_ShouldCreateDefaultOperationPoint()
    {
        // Arrange
        var mockExamDto = new StudentMockExamDto
        {
            Id = 1,
            Name = "C# 编程测试",
            Description = "包含C#编程题目的测试",
            TotalScore = 100,
            DurationMinutes = 120,
            Questions = new ObservableCollection<StudentMockExamQuestionDto>
            {
                new()
                {
                    OriginalQuestionId = 1,
                    Title = "C# 编程实现",
                    Content = "使用C#语言编写一个class",
                    Score = 100,
                    SortOrder = 1,
                    ProgramInput = "test input",
                    ExpectedOutput = "test output",
                    OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>() // 空的操作点列表
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
        Assert.Single(result.Modules[0].Questions); // 应该有一个 C# 题目
        Assert.Equal("C# 编程实现", result.Modules[0].Questions[0].Title);
        Assert.Single(result.Modules[0].Questions[0].OperationPoints); // 应该有一个默认创建的操作点
        Assert.Equal(ModuleType.CSharp, result.Modules[0].Questions[0].OperationPoints[0].ModuleType);
        Assert.Equal("C#编程操作", result.Modules[0].Questions[0].OperationPoints[0].Name);
        
        // 验证 C# 特有字段
        Assert.Equal("test input", result.Modules[0].Questions[0].ProgramInput);
        Assert.Equal("test output", result.Modules[0].Questions[0].ExpectedOutput);
    }

    /// <summary>
    /// 测试混合题目类型的过滤（只应该返回 C# 题目）
    /// </summary>
    [Fact]
    public void MapMockExamToExamModel_MixedQuestionTypes_ShouldOnlyReturnCSharpQuestions()
    {
        // Arrange
        var mockExamDto = new StudentMockExamDto
        {
            Id = 1,
            Name = "混合类型测试",
            Description = "包含多种类型题目的测试",
            TotalScore = 200,
            DurationMinutes = 120,
            Questions = new ObservableCollection<StudentMockExamQuestionDto>
            {
                new()
                {
                    OriginalQuestionId = 1,
                    Title = "C# 编程题",
                    Content = "编写C#程序",
                    Score = 100,
                    SortOrder = 1,
                    OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>
                    {
                        new()
                        {
                            Id = 1,
                            Name = "C# 操作",
                            ModuleType = "csharp",
                            Score = 100,
                            Order = 1,
                            Parameters = new ObservableCollection<StudentMockExamParameterDto>()
                        }
                    }
                },
                new()
                {
                    OriginalQuestionId = 2,
                    Title = "Excel 操作题",
                    Content = "完成Excel表格操作",
                    Score = 100,
                    SortOrder = 2,
                    OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>
                    {
                        new()
                        {
                            Id = 2,
                            Name = "Excel 操作",
                            ModuleType = "Excel",
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
        Assert.Single(result.Modules); // 应该只有一个 C# 模块
        Assert.Equal(ModuleType.CSharp, result.Modules[0].Type);
        Assert.Single(result.Modules[0].Questions); // 应该只有一个 C# 题目
        Assert.Equal("C# 编程题", result.Modules[0].Questions[0].Title);
        Assert.Single(result.Modules[0].Questions[0].OperationPoints); // 应该只有一个 C# 操作点
        Assert.Equal(ModuleType.CSharp, result.Modules[0].Questions[0].OperationPoints[0].ModuleType);
    }
}
