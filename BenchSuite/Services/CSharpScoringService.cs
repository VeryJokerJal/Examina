using BenchSuite.Interfaces;
using BenchSuite.Models;

namespace BenchSuite.Services;

/// <summary>
/// C#编程题打分服务 - 支持三种评分模式
/// </summary>
public class CSharpScoringService : ICSharpScoringService
{
    /// <summary>
    /// 对C#代码进行评分
    /// </summary>
    /// <param name="templateCode">模板代码（包含NotImplementedException的填空）</param>
    /// <param name="studentCode">学生提交的代码</param>
    /// <param name="expectedImplementations">期望的实现代码列表</param>
    /// <param name="mode">评分模式</param>
    /// <returns>评分结果</returns>
    public async Task<CSharpScoringResult> ScoreCodeAsync(string templateCode, string studentCode, List<string> expectedImplementations, CSharpScoringMode mode)
    {
        CSharpScoringResult result = new()
        {
            Mode = mode,
            StartTime = DateTime.Now
        };

        try
        {
            switch (mode)
            {
                case CSharpScoringMode.CodeCompletion:
                    await ScoreCodeCompletionAsync(result, templateCode, studentCode, expectedImplementations);
                    break;

                case CSharpScoringMode.CompilationCheck:
                    await ScoreCompilationCheckAsync(result, studentCode);
                    break;

                case CSharpScoringMode.UnitTest:
                    await ScoreUnitTestAsync(result, studentCode, expectedImplementations);
                    break;

                default:
                    result.IsSuccess = false;
                    result.ErrorMessage = $"不支持的评分模式: {mode}";
                    break;
            }
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"评分过程中发生异常: {ex.Message}";
            result.Details = ex.StackTrace ?? "";
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    /// <summary>
    /// 检测代码补全填空
    /// </summary>
    /// <param name="templateCode">模板代码</param>
    /// <param name="studentCode">学生代码</param>
    /// <param name="expectedImplementations">期望实现</param>
    /// <returns>填空检测结果</returns>
    public async Task<List<FillBlankResult>> DetectFillBlanksAsync(string templateCode, string studentCode, List<string> expectedImplementations)
    {
        return await Task.Run(() => CSharpCodeCompletionGrader.Grade(templateCode, expectedImplementations, studentCode));
    }

    /// <summary>
    /// 编译检查代码
    /// </summary>
    /// <param name="sourceCode">源代码</param>
    /// <param name="references">引用程序集</param>
    /// <returns>编译结果</returns>
    public async Task<CompilationResult> CompileCodeAsync(string sourceCode, List<string>? references = null)
    {
        return await Task.Run(() => CSharpCompilationChecker.CompileCode(sourceCode, references));
    }

    /// <summary>
    /// 运行单元测试
    /// </summary>
    /// <param name="studentCode">学生代码</param>
    /// <param name="testCode">测试代码</param>
    /// <param name="references">引用程序集</param>
    /// <returns>测试结果</returns>
    public async Task<UnitTestResult> RunUnitTestsAsync(string studentCode, string testCode, List<string>? references = null)
    {
        return await CSharpUnitTestRunner.RunUnitTestsAsync(studentCode, testCode, references);
    }

    /// <summary>
    /// 代码补全模式评分
    /// </summary>
    private async Task ScoreCodeCompletionAsync(CSharpScoringResult result, string templateCode, string studentCode, List<string> expectedImplementations)
    {
        List<FillBlankResult> fillResults = await DetectFillBlanksAsync(templateCode, studentCode, expectedImplementations);
        
        result.FillBlankResults = fillResults;
        result.TotalScore = fillResults.Count;
        result.AchievedScore = fillResults.Count(f => f.Matched);
        result.IsSuccess = true;
        
        int totalBlanks = fillResults.Count;
        int correctBlanks = fillResults.Count(f => f.Matched);
        
        result.Details = $"代码补全评分完成。总填空数: {totalBlanks}, 正确填空数: {correctBlanks}";
        
        if (correctBlanks == 0 && totalBlanks > 0)
        {
            result.Details += "\n所有填空都不正确，请检查代码实现。";
        }
        else if (correctBlanks < totalBlanks)
        {
            List<int> incorrectBlanks = fillResults
                .Where(f => !f.Matched)
                .Select(f => f.BlankIndex + 1)
                .ToList();
            result.Details += $"\n错误的填空位置: {string.Join(", ", incorrectBlanks)}";
        }
    }

    /// <summary>
    /// 编译检查模式评分
    /// </summary>
    private async Task ScoreCompilationCheckAsync(CSharpScoringResult result, string studentCode)
    {
        CompilationResult compilationResult = await CompileCodeAsync(studentCode);
        
        result.CompilationResult = compilationResult;
        result.TotalScore = 1;
        result.AchievedScore = compilationResult.IsSuccess ? 1 : 0;
        result.IsSuccess = true;
        
        if (compilationResult.IsSuccess)
        {
            result.Details = $"编译成功。编译耗时: {compilationResult.CompilationTimeMs}ms";
            
            if (compilationResult.Warnings.Count > 0)
            {
                result.Details += $"\n警告数量: {compilationResult.Warnings.Count}";
            }
        }
        else
        {
            result.Details = $"编译失败。错误数量: {compilationResult.Errors.Count}";
            
            if (compilationResult.Errors.Count > 0)
            {
                result.Details += "\n主要错误:";
                foreach (CompilationError error in compilationResult.Errors.Take(3))
                {
                    result.Details += $"\n  行 {error.Line}: {error.Message}";
                }
                
                if (compilationResult.Errors.Count > 3)
                {
                    result.Details += $"\n  ... 还有 {compilationResult.Errors.Count - 3} 个错误";
                }
            }
        }
    }

    /// <summary>
    /// 单元测试模式评分
    /// </summary>
    private async Task ScoreUnitTestAsync(CSharpScoringResult result, string studentCode, List<string> expectedImplementations)
    {
        if (expectedImplementations.Count == 0)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "单元测试模式需要提供测试代码";
            return;
        }

        string testCode = expectedImplementations[0]; // 第一个元素作为测试代码
        UnitTestResult testResult = await RunUnitTestsAsync(studentCode, testCode);
        
        result.UnitTestResult = testResult;
        result.TotalScore = testResult.TotalTests;
        result.AchievedScore = testResult.PassedTests;
        result.IsSuccess = true;
        
        if (testResult.IsSuccess)
        {
            result.Details = $"所有测试通过。总测试数: {testResult.TotalTests}, 执行耗时: {testResult.ExecutionTimeMs}ms";
        }
        else
        {
            result.Details = $"部分测试失败。通过: {testResult.PassedTests}/{testResult.TotalTests}";
            
            if (!string.IsNullOrEmpty(testResult.ErrorMessage))
            {
                result.Details += $"\n错误信息: {testResult.ErrorMessage}";
            }
            
            List<TestCaseResult> failedTests = testResult.TestCaseResults.Where(t => !t.Passed).Take(3).ToList();
            if (failedTests.Count > 0)
            {
                result.Details += "\n失败的测试:";
                foreach (TestCaseResult failedTest in failedTests)
                {
                    result.Details += $"\n  {failedTest.TestName}: {failedTest.ErrorMessage}";
                }
            }
        }
    }

    #region IScoringService 基础接口实现

    public Task<ScoringResult> ScoreAsync(string filePath, List<OperationPointModel> operationPoints)
    {
        throw new NotSupportedException("C#打分服务不支持基于文件路径的评分，请使用ScoreCodeAsync方法");
    }

    public Task<KnowledgePointResult> DetectKnowledgePointAsync(string filePath, string knowledgePointType, Dictionary<string, string> parameters)
    {
        throw new NotSupportedException("C#打分服务不支持基于文件路径的知识点检测，请使用相应的代码评分方法");
    }

    public Task<List<KnowledgePointResult>> DetectKnowledgePointsAsync(string filePath, List<OperationPointModel> knowledgePoints)
    {
        throw new NotSupportedException("C#打分服务不支持基于文件路径的知识点检测，请使用相应的代码评分方法");
    }

    #endregion
}
