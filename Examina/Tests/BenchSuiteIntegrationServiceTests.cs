using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using BenchSuite.Models;
using Examina.Services;
using Examina.Models.MockExam;
using System.Collections.ObjectModel;

namespace Examina.Tests;

/// <summary>
/// BenchSuiteIntegrationService 的单元测试
/// </summary>
public class BenchSuiteIntegrationServiceTests
{
    private readonly Mock<ILogger<BenchSuiteIntegrationService>> _mockLogger;
    private readonly BenchSuiteIntegrationService _service;

    public BenchSuiteIntegrationServiceTests()
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
    [InlineData("Excel", ModuleType.Excel)]
    [InlineData("Word", ModuleType.Word)]
    [InlineData("PowerPoint", ModuleType.PowerPoint)]
    [InlineData("ppt", ModuleType.PowerPoint)]
    [InlineData("Windows", ModuleType.Windows)]
    public void ParseModuleType_ShouldCorrectlyParseVariants(string input, ModuleType expected)
    {
        // 使用反射访问私有方法进行测试
        var method = typeof(BenchSuiteIntegrationService).GetMethod("ParseModuleType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = (ModuleType)method!.Invoke(null, new object[] { input })!;
        
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试模块类型匹配功能
    /// </summary>
    [Theory]
    [InlineData("CSharp", ModuleType.CSharp, true)]
    [InlineData("C#", ModuleType.CSharp, true)]
    [InlineData("csharp", ModuleType.CSharp, true)]
    [InlineData("编程", ModuleType.CSharp, true)]
    [InlineData("Excel", ModuleType.CSharp, false)]
    [InlineData("Word", ModuleType.CSharp, false)]
    [InlineData("", ModuleType.CSharp, false)]
    [InlineData(null, ModuleType.CSharp, false)]
    public void IsModuleTypeMatch_ShouldCorrectlyMatchTypes(string input, ModuleType target, bool expected)
    {
        // 使用反射访问私有方法进行测试
        var method = typeof(BenchSuiteIntegrationService).GetMethod("IsModuleTypeMatch", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var result = (bool)method!.Invoke(null, new object[] { input, target })!;
        
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// 测试模拟考试映射中 C# 模块的过滤逻辑
    /// </summary>
    [Fact]
    public void MapMockExamToExamModel_ShouldFilterCSharpQuestionsCorrectly()
    {
        // Arrange
        var mockExamDto = new StudentMockExamDto
        {
            Id = 1,
            Name = "测试模拟考试",
            Description = "包含多种模块类型的测试考试",
            TotalScore = 100,
            DurationMinutes = 120,
            Questions = new ObservableCollection<StudentMockExamQuestionDto>
            {
                new()
                {
                    OriginalQuestionId = 1,
                    Title = "C# 编程题",
                    Content = "编写一个简单的C#程序",
                    Score = 50,
                    SortOrder = 1,
                    OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>
                    {
                        new()
                        {
                            Id = 1,
                            Name = "C# 操作点",
                            Description = "C# 编程操作",
                            ModuleType = "C#", // 使用 C# 变体
                            Score = 50,
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
                    Score = 50,
                    SortOrder = 2,
                    OperationPoints = new ObservableCollection<StudentMockExamOperationPointDto>
                    {
                        new()
                        {
                            Id = 2,
                            Name = "Excel 操作点",
                            Description = "Excel 表格操作",
                            ModuleType = "Excel",
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
        Assert.Single(result.Modules); // 应该只有一个 C# 模块
        Assert.Equal(ModuleType.CSharp, result.Modules[0].Type);
        Assert.Single(result.Modules[0].Questions); // 应该只有一个 C# 题目
        Assert.Equal("C# 编程题", result.Modules[0].Questions[0].Title);
        Assert.Single(result.Modules[0].Questions[0].OperationPoints); // 应该只有一个 C# 操作点
        Assert.Equal(ModuleType.CSharp, result.Modules[0].Questions[0].OperationPoints[0].ModuleType);
    }
}
