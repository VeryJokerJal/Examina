using System;
using System.IO;
using System.Threading.Tasks;
using Examina.Models;
using Examina.Models.BenchSuite;
using Examina.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Examina.Tests;

/// <summary>
/// 目录结构修复测试
/// </summary>
public class DirectoryStructureFixTest
{
    private readonly ILogger<BenchSuiteDirectoryService> _logger;
    private readonly BenchSuiteDirectoryService _directoryService;

    public DirectoryStructureFixTest()
    {
        _logger = new LoggerFactory().CreateLogger<BenchSuiteDirectoryService>();
        _directoryService = new BenchSuiteDirectoryService(_logger);
    }

    /// <summary>
    /// 测试基础目录结构创建（不应该创建科目文件夹）
    /// </summary>
    [Fact]
    public async Task EnsureDirectoryStructureAsync_ShouldOnlyCreateBaseDirectory()
    {
        // Arrange
        string basePath = @"C:\河北对口计算机\";
        
        // 清理测试环境
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }

        // Act
        BenchSuiteDirectoryValidationResult result = await _directoryService.EnsureDirectoryStructureAsync();

        // Assert
        Assert.True(result.IsValid);
        Assert.True(Directory.Exists(basePath));
        
        // 验证不应该创建科目文件夹
        Assert.False(Directory.Exists(Path.Combine(basePath, "CSharp")));
        Assert.False(Directory.Exists(Path.Combine(basePath, "EXCEL")));
        Assert.False(Directory.Exists(Path.Combine(basePath, "PPT")));
        Assert.False(Directory.Exists(Path.Combine(basePath, "WINDOWS")));
        Assert.False(Directory.Exists(Path.Combine(basePath, "WORD")));
    }

    /// <summary>
    /// 测试考试目录结构创建（应该创建正确的层级结构）
    /// </summary>
    [Fact]
    public async Task EnsureExamDirectoryStructureAsync_ShouldCreateCorrectHierarchy()
    {
        // Arrange
        ExamType examType = ExamType.FormalExam;
        int examId = 1;
        string basePath = @"C:\河北对口计算机\";
        string expectedExamPath = Path.Combine(basePath, "OnlineExams", examId.ToString());
        
        // 清理测试环境
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }

        // Act
        BenchSuiteDirectoryValidationResult result = await _directoryService.EnsureExamDirectoryStructureAsync(examType, examId);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(Directory.Exists(basePath));
        Assert.True(Directory.Exists(Path.Combine(basePath, "OnlineExams")));
        Assert.True(Directory.Exists(expectedExamPath));
        
        // 验证科目文件夹在正确的位置
        Assert.True(Directory.Exists(Path.Combine(expectedExamPath, "CSharp")));
        Assert.True(Directory.Exists(Path.Combine(expectedExamPath, "EXCEL")));
        Assert.True(Directory.Exists(Path.Combine(expectedExamPath, "PPT")));
        Assert.True(Directory.Exists(Path.Combine(expectedExamPath, "WINDOWS")));
        Assert.True(Directory.Exists(Path.Combine(expectedExamPath, "WORD")));
        
        // 验证科目文件夹不在基础路径下
        Assert.False(Directory.Exists(Path.Combine(basePath, "CSharp")));
        Assert.False(Directory.Exists(Path.Combine(basePath, "EXCEL")));
        Assert.False(Directory.Exists(Path.Combine(basePath, "PPT")));
        Assert.False(Directory.Exists(Path.Combine(basePath, "WINDOWS")));
        Assert.False(Directory.Exists(Path.Combine(basePath, "WORD")));
    }

    /// <summary>
    /// 测试不同考试类型的目录结构
    /// </summary>
    [Theory]
    [InlineData(ExamType.FormalExam, "OnlineExams")]
    [InlineData(ExamType.MockExam, "MockExams")]
    [InlineData(ExamType.ComprehensiveTraining, "ComprehensiveTraining")]
    [InlineData(ExamType.SpecializedTraining, "SpecializedTraining")]
    public async Task EnsureExamDirectoryStructureAsync_ShouldCreateCorrectExamTypeFolder(ExamType examType, string expectedFolder)
    {
        // Arrange
        int examId = 1;
        string basePath = @"C:\河北对口计算机\";
        string expectedExamTypePath = Path.Combine(basePath, expectedFolder);
        string expectedExamPath = Path.Combine(expectedExamTypePath, examId.ToString());
        
        // 清理测试环境
        if (Directory.Exists(basePath))
        {
            Directory.Delete(basePath, true);
        }

        // Act
        BenchSuiteDirectoryValidationResult result = await _directoryService.EnsureExamDirectoryStructureAsync(examType, examId);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(Directory.Exists(expectedExamTypePath));
        Assert.True(Directory.Exists(expectedExamPath));
        
        // 验证科目文件夹在正确的位置
        Assert.True(Directory.Exists(Path.Combine(expectedExamPath, "CSharp")));
        Assert.True(Directory.Exists(Path.Combine(expectedExamPath, "EXCEL")));
        Assert.True(Directory.Exists(Path.Combine(expectedExamPath, "PPT")));
        Assert.True(Directory.Exists(Path.Combine(expectedExamPath, "WINDOWS")));
        Assert.True(Directory.Exists(Path.Combine(expectedExamPath, "WORD")));
    }

    /// <summary>
    /// 清理测试环境
    /// </summary>
    public void Dispose()
    {
        string basePath = @"C:\河北对口计算机\";
        if (Directory.Exists(basePath))
        {
            try
            {
                Directory.Delete(basePath, true);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
}
