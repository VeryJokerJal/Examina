using BenchSuite.Services;
using BenchSuite.Models;

namespace BenchSuite.Tests;

/// <summary>
/// AI扣分评估功能测试
/// </summary>
public static class AIDeductionAssessmentTests
{
    /// <summary>
    /// 运行所有扣分评估测试
    /// </summary>
    /// <returns>测试结果</returns>
    public static async Task<TestSuiteResult> RunAllTestsAsync()
    {
        TestSuiteResult result = new()
        {
            SuiteName = "AI Deduction Assessment Tests",
            StartTime = DateTime.Now
        };

        List<TestCaseResult> testResults = [];

        // 基础功能测试
        testResults.Add(await TestBasicDeductionAssessmentAsync());
        testResults.Add(await TestScoringResultGenerationAsync());
        testResults.Add(await TestMultipleDeductionPointsAsync());
        testResults.Add(await TestDeductionCategoriesAsync());
        testResults.Add(await TestSeverityLevelsAsync());
        testResults.Add(await TestLocationInfoAsync());
        testResults.Add(await TestDetailedDescriptionAsync());
        testResults.Add(await TestIntegrationWithCodeAnalysisAsync());

        result.TestCases = testResults;
        result.EndTime = DateTime.Now;
        result.TotalTests = testResults.Count;
        result.PassedTests = testResults.Count(t => t.Passed);
        result.FailedTests = testResults.Count(t => !t.Passed);
        result.IsSuccess = result.FailedTests == 0;

        return result;
    }

    /// <summary>
    /// 测试基本扣分评估功能
    /// </summary>
    private static async Task<TestCaseResult> TestBasicDeductionAssessmentAsync()
    {
        TestCaseResult result = new() { TestName = "基本扣分评估测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                using System;
                
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var result = ""; // 使用var关键字
                        for (int i = 0; i < 1000; i++)
                        {
                            result += i.ToString(); // 性能问题：字符串拼接
                        }
                        // 缺乏错误处理
                    }
                }
                """;

            AIDeductionAssessmentService.DeductionAssessmentResult assessmentResult = 
                await AIDeductionAssessmentService.AssessSingleCodeDeductionsAsync(testCode, "TestClass.cs");

            result.Passed = assessmentResult.IsSuccess && 
                           assessmentResult.DeductionPointCount > 0 &&
                           assessmentResult.ScoringResults.Count > 0;

            result.Output = $"评估成功: {assessmentResult.IsSuccess}, 扣分点数: {assessmentResult.DeductionPointCount}, 总扣分: {assessmentResult.TotalDeduction}";

            if (!result.Passed)
            {
                result.ErrorMessage = $"扣分评估失败: {assessmentResult.ErrorMessage}";
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
    /// 测试CSharpScoringResult生成
    /// </summary>
    private static async Task<TestCaseResult> TestScoringResultGenerationAsync()
    {
        TestCaseResult result = new() { TestName = "CSharpScoringResult生成测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                public class ScoringTest
                {
                    public void BadMethod()
                    {
                        var x = 1; // 代码风格问题
                        string s = "";
                        for (int i = 0; i < 100; i++)
                        {
                            s += i; // 性能问题
                        }
                    }
                }
                """;

            AIDeductionAssessmentService.DeductionAssessmentResult assessmentResult = 
                await AIDeductionAssessmentService.AssessSingleCodeDeductionsAsync(testCode, "ScoringTest.cs");

            result.Passed = assessmentResult.IsSuccess && 
                           assessmentResult.ScoringResults.Count > 0;

            if (result.Passed)
            {
                // 验证CSharpScoringResult的结构
                foreach (CSharpScoringResult scoringResult in assessmentResult.ScoringResults)
                {
                    if (scoringResult.TotalScore <= 0 || string.IsNullOrEmpty(scoringResult.Details))
                    {
                        result.Passed = false;
                        result.ErrorMessage = "CSharpScoringResult结构不正确";
                        break;
                    }
                }
            }

            result.Output = $"生成了 {assessmentResult.ScoringResults.Count} 个CSharpScoringResult";

            if (!result.Passed && string.IsNullOrEmpty(result.ErrorMessage))
            {
                result.ErrorMessage = "CSharpScoringResult生成失败";
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
    /// 测试多个扣分点识别
    /// </summary>
    private static async Task<TestCaseResult> TestMultipleDeductionPointsAsync()
    {
        TestCaseResult result = new() { TestName = "多个扣分点识别测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                using System;
                
                public class MultipleIssues
                {
                    public void ProblematicMethod()
                    {
                        var data = ""; // 问题1: 使用var
                        
                        // 问题2: 缺乏错误处理的文件操作
                        string content = System.IO.File.ReadAllText("file.txt");
                        
                        // 问题3: 性能问题 - 字符串拼接
                        for (int i = 0; i < 1000; i++)
                        {
                            data += content;
                        }
                        
                        // 问题4: 硬编码字符串
                        if (data == "hardcoded_value")
                        {
                            Console.WriteLine("Found");
                        }
                    }
                }
                """;

            AIDeductionAssessmentService.DeductionAssessmentResult assessmentResult = 
                await AIDeductionAssessmentService.AssessSingleCodeDeductionsAsync(testCode, "MultipleIssues.cs");

            // 期望识别出多个问题
            result.Passed = assessmentResult.IsSuccess && 
                           assessmentResult.DeductionPointCount >= 2 &&
                           assessmentResult.ScoringResults.Count >= 2;

            result.Output = $"识别出 {assessmentResult.DeductionPointCount} 个扣分点";

            if (!result.Passed)
            {
                result.ErrorMessage = "未能识别出足够的扣分点";
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
    /// 测试扣分类别识别
    /// </summary>
    private static async Task<TestCaseResult> TestDeductionCategoriesAsync()
    {
        TestCaseResult result = new() { TestName = "扣分类别识别测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                public class CategoryTest
                {
                    public void TestMethod()
                    {
                        var x = 1; // code_style
                        string s = "";
                        for (int i = 0; i < 100; i++)
                        {
                            s += i; // performance_issue
                        }
                        // 缺乏try-catch // error_handling
                    }
                }
                """;

            AIDeductionAssessmentService.DeductionAssessmentResult assessmentResult = 
                await AIDeductionAssessmentService.AssessSingleCodeDeductionsAsync(testCode, "CategoryTest.cs");

            result.Passed = assessmentResult.IsSuccess && 
                           assessmentResult.AssessmentResponse != null &&
                           assessmentResult.AssessmentResponse.IssueCategoryStats.Count > 0;

            if (result.Passed && assessmentResult.AssessmentResponse != null)
            {
                Dictionary<string, int> stats = assessmentResult.AssessmentResponse.IssueCategoryStats;
                result.Output = $"识别的类别: {string.Join(", ", stats.Keys)}";
            }

            if (!result.Passed)
            {
                result.ErrorMessage = "扣分类别识别失败";
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
    /// 测试严重程度分级
    /// </summary>
    private static async Task<TestCaseResult> TestSeverityLevelsAsync()
    {
        TestCaseResult result = new() { TestName = "严重程度分级测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                public class SeverityTest
                {
                    public void CriticalIssue()
                    {
                        // 严重问题：缺乏错误处理的危险操作
                        System.IO.File.Delete("important_file.txt");
                        
                        // 中等问题：性能问题
                        string result = "";
                        for (int i = 0; i < 1000; i++)
                        {
                            result += i;
                        }
                        
                        // 轻微问题：代码风格
                        var x = 1;
                    }
                }
                """;

            AIDeductionAssessmentService.DeductionAssessmentResult assessmentResult = 
                await AIDeductionAssessmentService.AssessSingleCodeDeductionsAsync(testCode, "SeverityTest.cs");

            result.Passed = assessmentResult.IsSuccess && 
                           assessmentResult.AssessmentResponse != null &&
                           assessmentResult.AssessmentResponse.DeductionPoints.Count > 0;

            if (result.Passed && assessmentResult.AssessmentResponse != null)
            {
                List<string> severities = assessmentResult.AssessmentResponse.DeductionPoints
                    .Select(dp => dp.Severity)
                    .Distinct()
                    .ToList();

                result.Output = $"识别的严重程度: {string.Join(", ", severities)}";
            }

            if (!result.Passed)
            {
                result.ErrorMessage = "严重程度分级失败";
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
    /// 测试位置信息准确性
    /// </summary>
    private static async Task<TestCaseResult> TestLocationInfoAsync()
    {
        TestCaseResult result = new() { TestName = "位置信息准确性测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                public class LocationTest
                {
                    public void TestMethod()
                    {
                        var problematicVariable = 1;
                    }
                }
                """;

            AIDeductionAssessmentService.DeductionAssessmentResult assessmentResult = 
                await AIDeductionAssessmentService.AssessSingleCodeDeductionsAsync(testCode, "LocationTest.cs");

            result.Passed = assessmentResult.IsSuccess && 
                           assessmentResult.AssessmentResponse != null &&
                           assessmentResult.AssessmentResponse.DeductionPoints.Count > 0;

            if (result.Passed && assessmentResult.AssessmentResponse != null)
            {
                DeductionPoint firstPoint = assessmentResult.AssessmentResponse.DeductionPoints[0];
                bool hasLocationInfo = firstPoint.LocationInfo != null &&
                                     !string.IsNullOrEmpty(firstPoint.LocationInfo.FileName);

                result.Passed = hasLocationInfo;
                result.Output = $"位置信息: 文件={firstPoint.LocationInfo?.FileName}, 行号={firstPoint.LocationInfo?.LineNumber}";
            }

            if (!result.Passed)
            {
                result.ErrorMessage = "位置信息不准确或缺失";
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
    /// 测试详细描述生成
    /// </summary>
    private static async Task<TestCaseResult> TestDetailedDescriptionAsync()
    {
        TestCaseResult result = new() { TestName = "详细描述生成测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                public class DescriptionTest
                {
                    public void TestMethod()
                    {
                        var x = 1;
                    }
                }
                """;

            AIDeductionAssessmentService.DeductionAssessmentResult assessmentResult = 
                await AIDeductionAssessmentService.AssessSingleCodeDeductionsAsync(testCode, "DescriptionTest.cs");

            result.Passed = assessmentResult.IsSuccess && 
                           assessmentResult.ScoringResults.Count > 0;

            if (result.Passed)
            {
                CSharpScoringResult firstResult = assessmentResult.ScoringResults[0];
                bool hasDetailedDescription = !string.IsNullOrEmpty(firstResult.Details) &&
                                            firstResult.Details.Contains("扣分点ID") &&
                                            firstResult.Details.Contains("问题描述") &&
                                            firstResult.Details.Contains("改进建议");
                
                result.Passed = hasDetailedDescription;
                result.Output = "详细描述包含必要信息";
            }

            if (!result.Passed)
            {
                result.ErrorMessage = "详细描述不完整";
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
    /// 测试与代码分析服务的集成
    /// </summary>
    private static async Task<TestCaseResult> TestIntegrationWithCodeAnalysisAsync()
    {
        TestCaseResult result = new() { TestName = "代码分析服务集成测试" };
        DateTime startTime = DateTime.Now;

        try
        {
            string testCode = """
                public class IntegrationTest
                {
                    public void TestMethod()
                    {
                        var data = "";
                        for (int i = 0; i < 100; i++)
                        {
                            data += i;
                        }
                    }
                }
                """;

            CodeAnalysisOptions options = new()
            {
                IncludeStructureAnalysis = false,
                IncludeQualityAssessment = false,
                IncludeCodeExplanation = false,
                IncludeImprovementSuggestions = false,
                IncludeDeductionAssessment = true
            };

            AICodeAnalysisService.CodeAnalysisResult analysisResult = 
                await AICodeAnalysisService.AnalyzeSingleCodeAsync(testCode, "IntegrationTest.cs", options);

            result.Passed = analysisResult.IsSuccess && 
                           analysisResult.DeductionAssessment != null &&
                           analysisResult.ScoringResults.Count > 0;

            result.Output = $"集成测试成功，生成了 {analysisResult.ScoringResults.Count} 个评分结果";

            if (!result.Passed)
            {
                result.ErrorMessage = "与代码分析服务集成失败";
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
