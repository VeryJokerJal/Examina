using BenchSuite.Services;
using BenchSuite.Models;

namespace BenchSuite.Tests;

/// <summary>
/// AI代码分析功能测试
/// </summary>
public static class AICodeAnalysisTests
{
    /// <summary>
    /// 运行所有AI代码分析测试
    /// </summary>
    /// <returns>测试结果</returns>
    public static async Task<TestSuiteResult> RunAllTestsAsync()
    {
        TestSuiteResult result = new()
        {
            SuiteName = "AI Code Analysis Tests",
            StartTime = DateTime.Now
        };

        List<TestCaseResult> testResults = [];

        // 基础功能测试
        testResults.Add(await TestProjectFileReaderAsync());
        testResults.Add(await TestSingleCodeAnalysisAsync());
        testResults.Add(await TestCodeChunkingAsync());
        testResults.Add(await TestStructureAnalysisAsync());
        testResults.Add(await TestQualityAssessmentAsync());
        testResults.Add(await TestCodeExplanationAsync());
        testResults.Add(await TestImprovementSuggestionsAsync());
        testResults.Add(await TestAnalysisOptionsAsync());

        result.TestCases = testResults;
        result.EndTime = DateTime.Now;
        result.TotalTests = testResults.Count;
        result.PassedTests = testResults.Count(t => t.Passed);
        result.FailedTests = testResults.Count(t => !t.Passed);
        result.IsSuccess = result.FailedTests == 0;

        return result;
    }

    /// <summary>
    /// 测试项目文件读取功能
    /// </summary>
    private static async Task<TestCaseResult> TestProjectFileReaderAsync()
    {
        TestCaseResult result = new() { TestName = "项目文件读取测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            // 创建临时项目文件进行测试
            string tempDir = Path.Combine(Path.GetTempPath(), "AICodeAnalysisTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);

            try
            {
                // 创建测试项目文件
                string projectFile = Path.Combine(tempDir, "TestProject.csproj");
                await File.WriteAllTextAsync(projectFile, """
                    <Project Sdk="Microsoft.NET.Sdk">
                      <PropertyGroup>
                        <TargetFramework>net9.0</TargetFramework>
                      </PropertyGroup>
                    </Project>
                    """);

                // 创建测试源文件
                string sourceFile = Path.Combine(tempDir, "Program.cs");
                await File.WriteAllTextAsync(sourceFile, """
                    using System;
                    
                    namespace TestProject
                    {
                        public class Program
                        {
                            public static void Main()
                            {
                                Console.WriteLine("Hello, World!");
                            }
                        }
                    }
                    """);

                // 测试读取功能
                ProjectFileReaderService.ProjectCodeResult projectResult = 
                    await ProjectFileReaderService.ReadProjectCodeAsync(projectFile);

                result.Passed = projectResult.IsSuccess && 
                               projectResult.SourceFiles.Count > 0 &&
                               !string.IsNullOrEmpty(projectResult.CombinedSourceCode);

                result.Output = $"读取成功: {projectResult.IsSuccess}, 文件数: {projectResult.SourceFiles.Count}";

                if (!result.Passed)
                {
                    result.ErrorMessage = $"项目文件读取失败: {projectResult.ErrorMessage}";
                }
            }
            finally
            {
                // 清理临时文件
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // 忽略清理错误
                }
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试单个代码分析功能
    /// </summary>
    private static async Task<TestCaseResult> TestSingleCodeAnalysisAsync()
    {
        TestCaseResult result = new() { TestName = "单个代码分析测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                using System;
                
                public class Calculator
                {
                    public int Add(int a, int b)
                    {
                        return a + b;
                    }
                    
                    public int Multiply(int a, int b)
                    {
                        return a * b;
                    }
                }
                """;

            AICodeAnalysisService.CodeAnalysisResult analysisResult = 
                await AICodeAnalysisService.AnalyzeSingleCodeAsync(testCode, "Calculator.cs");

            result.Passed = analysisResult.IsSuccess;
            result.Output = $"分析成功: {analysisResult.IsSuccess}, 耗时: {analysisResult.AnalysisTimeMs}ms";

            if (!result.Passed)
            {
                result.ErrorMessage = $"代码分析失败: {analysisResult.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试代码分块功能
    /// </summary>
    private static async Task<TestCaseResult> TestCodeChunkingAsync()
    {
        TestCaseResult result = new() { TestName = "代码分块测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            // 创建一个较大的代码文件
            string largeCode = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"// Line {i}"));

            CodeAnalysisOptions options = new()
            {
                MaxChunkSize = 1000, // 小的块大小以测试分块
                IncludeStructureAnalysis = true,
                IncludeQualityAssessment = false,
                IncludeCodeExplanation = false,
                IncludeImprovementSuggestions = false
            };

            AICodeAnalysisService.CodeAnalysisResult analysisResult = 
                await AICodeAnalysisService.AnalyzeSingleCodeAsync(largeCode, "LargeFile.cs", options);

            result.Passed = analysisResult.IsSuccess && analysisResult.CodeChunkCount > 1;
            result.Output = $"分块成功: {analysisResult.CodeChunkCount} 个块";

            if (!result.Passed)
            {
                result.ErrorMessage = "代码分块功能未正常工作";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试结构分析功能
    /// </summary>
    private static async Task<TestCaseResult> TestStructureAnalysisAsync()
    {
        TestCaseResult result = new() { TestName = "结构分析测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                using System;
                
                namespace TestProject
                {
                    public interface IRepository<T>
                    {
                        void Add(T item);
                        T GetById(int id);
                    }
                    
                    public class UserRepository : IRepository<User>
                    {
                        public void Add(User item) { }
                        public User GetById(int id) { return new User(); }
                    }
                    
                    public class User
                    {
                        public int Id { get; set; }
                        public string Name { get; set; }
                    }
                }
                """;

            CodeAnalysisOptions options = new()
            {
                IncludeStructureAnalysis = true,
                IncludeQualityAssessment = false,
                IncludeCodeExplanation = false,
                IncludeImprovementSuggestions = false
            };

            AICodeAnalysisService.CodeAnalysisResult analysisResult = 
                await AICodeAnalysisService.AnalyzeSingleCodeAsync(testCode, "Repository.cs", options);

            result.Passed = analysisResult.IsSuccess && 
                           analysisResult.StructureAnalysis != null &&
                           analysisResult.StructureAnalysis.ArchitectureScore > 0;

            result.Output = $"结构分析完成，架构评分: {analysisResult.StructureAnalysis?.ArchitectureScore ?? 0}";

            if (!result.Passed)
            {
                result.ErrorMessage = "结构分析功能未正常工作";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试质量评估功能
    /// </summary>
    private static async Task<TestCaseResult> TestQualityAssessmentAsync()
    {
        TestCaseResult result = new() { TestName = "质量评估测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                public class QualityTest
                {
                    public void TestMethod()
                    {
                        // 一些测试代码
                        int x = 1;
                        int y = 2;
                        int z = x + y;
                    }
                }
                """;

            CodeAnalysisOptions options = new()
            {
                IncludeStructureAnalysis = false,
                IncludeQualityAssessment = true,
                IncludeCodeExplanation = false,
                IncludeImprovementSuggestions = false
            };

            AICodeAnalysisService.CodeAnalysisResult analysisResult = 
                await AICodeAnalysisService.AnalyzeSingleCodeAsync(testCode, "QualityTest.cs", options);

            result.Passed = analysisResult.IsSuccess && 
                           analysisResult.QualityAssessment != null &&
                           analysisResult.QualityAssessment.OverallQualityScore > 0;

            result.Output = $"质量评估完成，总分: {analysisResult.QualityAssessment?.OverallQualityScore ?? 0}";

            if (!result.Passed)
            {
                result.ErrorMessage = "质量评估功能未正常工作";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试代码解释功能
    /// </summary>
    private static async Task<TestCaseResult> TestCodeExplanationAsync()
    {
        TestCaseResult result = new() { TestName = "代码解释测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                public class ExplanationTest
                {
                    public int Fibonacci(int n)
                    {
                        if (n <= 1) return n;
                        return Fibonacci(n - 1) + Fibonacci(n - 2);
                    }
                }
                """;

            CodeAnalysisOptions options = new()
            {
                IncludeStructureAnalysis = false,
                IncludeQualityAssessment = false,
                IncludeCodeExplanation = true,
                IncludeImprovementSuggestions = false
            };

            AICodeAnalysisService.CodeAnalysisResult analysisResult = 
                await AICodeAnalysisService.AnalyzeSingleCodeAsync(testCode, "ExplanationTest.cs", options);

            result.Passed = analysisResult.IsSuccess && 
                           analysisResult.CodeExplanation != null &&
                           !string.IsNullOrEmpty(analysisResult.CodeExplanation.OverallExplanation);

            result.Output = "代码解释功能正常";

            if (!result.Passed)
            {
                result.ErrorMessage = "代码解释功能未正常工作";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试改进建议功能
    /// </summary>
    private static async Task<TestCaseResult> TestImprovementSuggestionsAsync()
    {
        TestCaseResult result = new() { TestName = "改进建议测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                public class ImprovementTest
                {
                    public void BadMethod()
                    {
                        // 一些可以改进的代码
                        string s = "";
                        for (int i = 0; i < 1000; i++)
                        {
                            s += i.ToString();
                        }
                    }
                }
                """;

            CodeAnalysisOptions options = new()
            {
                IncludeStructureAnalysis = false,
                IncludeQualityAssessment = false,
                IncludeCodeExplanation = false,
                IncludeImprovementSuggestions = true
            };

            AICodeAnalysisService.CodeAnalysisResult analysisResult = 
                await AICodeAnalysisService.AnalyzeSingleCodeAsync(testCode, "ImprovementTest.cs", options);

            result.Passed = analysisResult.IsSuccess && 
                           analysisResult.ImprovementSuggestions != null;

            result.Output = "改进建议功能正常";

            if (!result.Passed)
            {
                result.ErrorMessage = "改进建议功能未正常工作";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// 测试分析选项功能
    /// </summary>
    private static async Task<TestCaseResult> TestAnalysisOptionsAsync()
    {
        TestCaseResult result = new() { TestName = "分析选项测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = "public class OptionsTest { }";

            // 测试不同的选项组合
            CodeAnalysisOptions options1 = new()
            {
                IncludeStructureAnalysis = true,
                IncludeQualityAssessment = false,
                IncludeCodeExplanation = false,
                IncludeImprovementSuggestions = false
            };

            AICodeAnalysisService.CodeAnalysisResult result1 = 
                await AICodeAnalysisService.AnalyzeSingleCodeAsync(testCode, "Test.cs", options1);

            CodeAnalysisOptions options2 = new()
            {
                IncludeStructureAnalysis = false,
                IncludeQualityAssessment = true,
                IncludeCodeExplanation = false,
                IncludeImprovementSuggestions = false
            };

            AICodeAnalysisService.CodeAnalysisResult result2 = 
                await AICodeAnalysisService.AnalyzeSingleCodeAsync(testCode, "Test.cs", options2);

            result.Passed = result1.IsSuccess && result2.IsSuccess &&
                           result1.StructureAnalysis != null && result1.QualityAssessment == null &&
                           result2.StructureAnalysis == null && result2.QualityAssessment != null;

            result.Output = "分析选项功能正常";

            if (!result.Passed)
            {
                result.ErrorMessage = "分析选项功能未正常工作";
            }
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.ErrorMessage = $"测试异常: {ex.Message}";
            result.StackTrace = ex.StackTrace;
        }
        finally
        {
            result.ExecutionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;
        }

        return result;
    }
}
