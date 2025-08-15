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

                case CSharpScoringMode.Debugging:
                    await ScoreDebuggingAsync(result, templateCode, studentCode, expectedImplementations);
                    break;

                case CSharpScoringMode.Implementation:
                    await ScoreImplementationAsync(result, studentCode, expectedImplementations);
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
    /// 调试纠错评分
    /// </summary>
    /// <param name="buggyCode">包含错误的代码</param>
    /// <param name="studentFixedCode">学生修复后的代码</param>
    /// <param name="expectedErrors">期望发现的错误列表</param>
    /// <param name="testCode">验证修复的测试代码</param>
    /// <returns>调试结果</returns>
    public async Task<DebuggingResult> DebugCodeAsync(string buggyCode, string studentFixedCode, List<string> expectedErrors, string? testCode = null)
    {
        return await CSharpDebuggingGrader.DebugCodeAsync(buggyCode, studentFixedCode, expectedErrors, testCode);
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
    /// 调试纠错模式评分
    /// </summary>
    private async Task ScoreDebuggingAsync(CSharpScoringResult result, string buggyCode, string studentCode, List<string> expectedImplementations)
    {
        if (expectedImplementations.Count == 0)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "调试纠错模式需要提供期望的错误列表";
            return;
        }

        // 第一个元素作为期望错误列表，第二个元素（如果有）作为测试代码
        List<string> expectedErrors = expectedImplementations[0].Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        string? testCode = expectedImplementations.Count > 1 ? expectedImplementations[1] : null;

        DebuggingResult debuggingResult = await DebugCodeAsync(buggyCode, studentCode, expectedErrors, testCode);

        result.DebuggingResult = debuggingResult;
        result.TotalScore = debuggingResult.TotalErrors;
        result.AchievedScore = debuggingResult.FixedErrors;
        result.IsSuccess = true;
        result.Details = debuggingResult.Details;

        // 同时进行编译检查
        CompilationResult compilationResult = await CompileCodeAsync(studentCode);
        result.CompilationResult = compilationResult;

        if (!compilationResult.IsSuccess)
        {
            result.Details += $"\n⚠️ 修复后的代码仍有编译错误: {compilationResult.Errors.Count} 个";
        }
    }

    /// <summary>
    /// 编写实现模式评分
    /// </summary>
    private async Task ScoreImplementationAsync(CSharpScoringResult result, string studentCode, List<string> expectedImplementations)
    {
        if (expectedImplementations.Count == 0)
        {
            result.IsSuccess = false;
            result.ErrorMessage = "编写实现模式需要提供测试代码";
            return;
        }

        // 1. 首先进行编译检查
        CompilationResult compilationResult = await CompileCodeAsync(studentCode);
        result.CompilationResult = compilationResult;

        if (!compilationResult.IsSuccess)
        {
            result.TotalScore = 1;
            result.AchievedScore = 0;
            result.IsSuccess = true;
            result.Details = $"编译失败。错误数量: {compilationResult.Errors.Count}";
            return;
        }

        // 2. 运行单元测试
        string testCode = expectedImplementations[0]; // 第一个元素作为测试代码
        UnitTestResult testResult = await RunUnitTestsAsync(studentCode, testCode);

        result.UnitTestResult = testResult;
        result.TotalScore = testResult.TotalTests + 1; // +1 for compilation
        result.AchievedScore = testResult.PassedTests + 1; // +1 for successful compilation
        result.IsSuccess = true;

        if (testResult.IsSuccess)
        {
            result.Details = $"实现完成。编译成功，所有测试通过。总测试数: {testResult.TotalTests}, 执行耗时: {testResult.ExecutionTimeMs}ms";
        }
        else
        {
            result.Details = $"实现部分完成。编译成功，但部分测试失败。通过: {testResult.PassedTests}/{testResult.TotalTests}";

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

    public Task<ScoringResult> ScoreFileAsync(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        throw new NotSupportedException("C#打分服务不支持基于文件路径的评分，请使用ScoreCodeAsync方法");
    }

    public ScoringResult ScoreFile(string filePath, ExamModel examModel, ScoringConfiguration? configuration = null)
    {
        throw new NotSupportedException("C#打分服务不支持基于文件路径的评分，请使用ScoreCodeAsync方法");
    }

    public Task<ScoringResult> ScoreQuestionAsync(string filePath, QuestionModel question, ScoringConfiguration? configuration = null)
    {
        throw new NotSupportedException("C#打分服务不支持基于文件路径的评分，请使用ScoreCodeAsync方法");
    }

    public bool CanProcessFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return GetSupportedExtensions().Contains(extension);
    }

    public IEnumerable<string> GetSupportedExtensions()
    {
        return [".cs", ".txt"]; // 支持C#源代码文件和文本文件
    }

    #endregion
}
